using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;

namespace HdtCollectionExporter.Services
{
    public interface IAtomicFile
    {
        void Write(string path, string text, bool overwrite);
    }

    public sealed class AtomicFile : IAtomicFile
    {
        public void Write(string path, string text, bool overwrite)
        {
            var temp = Stage(path, text);
            try { Promote(temp, path, overwrite); }
            finally { if(File.Exists(temp)) File.Delete(temp); }
        }

        public static string Stage(string path, string text)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)));
            var temp = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using(var stream = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                using(var writer = new StreamWriter(stream, new UTF8Encoding(false)))
                {
                    writer.Write(text);
                    writer.Flush();
                    stream.Flush(true);
                }
                return temp;
            }
            catch { if(File.Exists(temp)) File.Delete(temp); throw; }
        }

        public static void Promote(string temp, string path, bool overwrite)
        {
            if(overwrite && File.Exists(path)) File.Replace(temp, path, path + ".bak", true);
            else File.Move(temp, path);
        }

        // Stage every format before exposing any final file. A promotion failure
        // may expose earlier files, which the caller reports; the baseline stays put.
        public static IList<string> WriteBatch(IDictionary<string, string> files, CancellationToken token)
        {
            var staged = new Dictionary<string, string>();
            var completed = new List<string>();
            try
            {
                foreach(var file in files) { token.ThrowIfCancellationRequested(); staged.Add(file.Key, Stage(file.Key, file.Value)); }
                token.ThrowIfCancellationRequested();
                foreach(var file in staged) { Promote(file.Value, file.Key, false); completed.Add(file.Key); }
                return completed;
            }
            catch(Exception ex)
            {
                if(completed.Count > 0) throw new PartialExportException(completed, ex);
                throw;
            }
            finally { foreach(var temp in staged.Values) if(File.Exists(temp)) File.Delete(temp); }
        }
    }

    public sealed class PartialExportException : IOException
    {
        public IList<string> Files { get; private set; }
        public PartialExportException(IList<string> files, Exception inner)
            : base("Some export files were saved; the baseline was not advanced.", inner) { Files = files; }
    }
}
