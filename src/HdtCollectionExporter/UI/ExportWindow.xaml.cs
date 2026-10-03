using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using HdtCollectionExporter.Models;
using HdtCollectionExporter.Services;
using HdtCollectionExporter.Settings;
using Forms = System.Windows.Forms;

namespace HdtCollectionExporter.UI
{
    public partial class ExportWindow : Window
    {
        private readonly PluginSettings _settings;
        private readonly CollectionExportService _service;
        private readonly Action _save;
        private ExportWindowText _text;
        private readonly CancellationTokenSource _lifetime = new CancellationTokenSource();
        private CancellationTokenSource _operation;
        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
        private CollectionPreview _preview;
        private bool _busy;
        private bool _applying;
        private string _lastFile;

        public ExportWindow(PluginSettings settings, CollectionExportService service, Action save)
            : this(settings, service, save, ExportWindowText.ForLanguage(settings.Language)) { }
        public ExportWindow(PluginSettings settings, CollectionExportService service, Action save, ExportWindowText text)
        {
            _settings = settings ?? throw new ArgumentNullException("settings");
            _service = service ?? throw new ArgumentNullException("service");
            _save = save ?? throw new ArgumentNullException("save");
            _text = text ?? ExportWindowText.English();
            InitializeComponent();
            var work = SystemParameters.WorkArea;
            MaxWidth = work.Width; MaxHeight = work.Height;
            MinWidth = Math.Min(MinWidth, work.Width); MinHeight = Math.Min(MinHeight, work.Height);
            Width = Math.Min(work.Width, Math.Max(MinWidth, _settings.WindowWidth));
            Height = Math.Min(work.Height, Math.Max(740, _settings.WindowHeight));
            _applying = true;
            LanguageBox.ItemsSource = new[] { "Auto", "Русский", "English" };
            LanguageBox.SelectedIndex = _settings.Language == "ru" ? 1 : _settings.Language == "en" ? 2 : 0;
            _applying = false;
            OutputFolderTextBox.Text = _settings.OutputFolder;
            NamesCheck.IsChecked = _settings.IncludeCardNames;
            PremiumCheck.IsChecked = _settings.IncludeGoldenCount;
            MetadataCheck.IsChecked = _settings.IncludeMetadata;
            ApplyText();
            FormatBox.SelectedIndex = Math.Max(0, Math.Min(2, _settings.FormatIndex));
            ModeBox.SelectedIndex = Math.Max(0, Math.Min(1, _settings.ModeIndex));
            StatusText.Text = _text["WaitingGame"];
            _timer.Tick += async delegate
            {
                if(_busy || _lifetime.IsCancellationRequested) return;
                if(_preview == null) await RefreshAsync();
                else if(DateTimeOffset.Now - _preview.ReadAt > TimeSpan.FromMinutes(5)) ReadyText.Text = _text["Stale"];
            };
        }

        private string T(string key) { return _text[key]; }
        private void ApplyText()
        {
            _applying = true;
            var mode = ModeBox.SelectedIndex;
            Title = "Manacost · " + T("Title"); TitleText.Text = T("Title"); SubtitleText.Text = T("Subtitle");
            RefreshButton.Content = T("Refresh"); UniqueLabel.Text = T("Unique"); CopiesLabel.Text = T("Copies");
            PremiumLabel.Text = T("Premium"); DustLabel.Text = T("Dust");
            ExportTab.Header = T("Export"); HistoryTab.Header = T("History"); SummaryTab.Header = T("Summary");
            ExportHeading.Text = T("Heading"); ModeLabel.Text = T("Mode"); FormatLabel.Text = T("Format");
            ModeBox.ItemsSource = new[] { T("Full"), T("Changes") }; ModeBox.SelectedIndex = mode < 0 ? 0 : mode;
            var format = FormatBox.SelectedIndex; FormatBox.ItemsSource = new[] { "JSON", "CSV", "JSON + CSV" }; FormatBox.SelectedIndex = format < 0 ? 2 : format;
            FolderLabel.Text = T("Folder"); BrowseButton.Content = T("Browse"); AdvancedExpander.Header = T("Advanced");
            NamesCheck.Content = T("Names"); PremiumCheck.Content = T("PremiumCsv"); MetadataCheck.Content = T("Metadata"); OptionsHint.Text = T("OptionsHint");
            SetBaselineButton.Content = T("SetBaseline"); ImportButton.Content = T("Import"); ClearButton.Content = T("Clear");
            HistoryHint.Text = T("HistoryHint"); EarlierLabel.Text = T("Earlier"); LaterLabel.Text = T("Later");
            CompareButton.Content = T("Compare"); HistoryBaselineButton.Content = T("HistoryBaseline"); HistoryFolderButton.Content = T("OpenHistory"); PruneButton.Content = T("Prune");
            SetsLabel.Text = T("Sets"); RaritiesLabel.Text = T("Rarities"); CancelButton.Content = T("Cancel");
            OpenFolderButton.Content = T("OpenFolder"); ShowFileButton.Content = T("ShowFile"); CopyPathButton.Content = T("CopyPath");
            _applying = false;
            DisplayPreview();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e) { _timer.Start(); await RefreshAsync(); }
        private void OnClosing(object sender, CancelEventArgs e)
        {
            _timer.Stop(); _lifetime.Cancel();
            Persist();
        }
        private void Persist()
        {
            _settings.OutputFolder = OutputFolderTextBox.Text;
            _settings.IncludeCardNames = NamesCheck.IsChecked == true;
            _settings.IncludeGoldenCount = PremiumCheck.IsChecked == true;
            _settings.IncludeMetadata = MetadataCheck.IsChecked == true;
            _settings.WindowWidth = Width; _settings.WindowHeight = Height;
            _settings.FormatIndex = FormatBox.SelectedIndex; _settings.ModeIndex = ModeBox.SelectedIndex;
            try { _save(); } catch { StatusText.Text = T("SettingsSaveFailed"); }
        }
        private void LanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if(_applying || LanguageBox.SelectedIndex < 0 || _text == null) return;
            _settings.Language = LanguageBox.SelectedIndex == 1 ? "ru" : LanguageBox.SelectedIndex == 2 ? "en" : "auto";
            _text = ExportWindowText.ForLanguage(_settings.Language); ApplyText();
            StatusText.Text = T(_preview == null ? "WaitingGame" : "Ready");
        }
        private void SelectionChanged(object sender, SelectionChangedEventArgs e) { if(!_applying && _text != null) DisplayPreview(); }
        private void DisplayPreview()
        {
            if(_preview == null)
            {
                AccountText.Text = T("Account"); ReadyText.Text = T("Waiting"); PreviewText.Text = T("WaitingGame");
                SetBaselineButton.IsEnabled = ImportButton.IsEnabled = ClearButton.IsEnabled = false;
                CompareButton.IsEnabled = HistoryBaselineButton.IsEnabled = HistoryFolderButton.IsEnabled = PruneButton.IsEnabled = false;
                ExportButton.Content = T("Save"); ExportButton.IsEnabled = false; return;
            }
            SetBaselineButton.IsEnabled = ImportButton.IsEnabled = ClearButton.IsEnabled = true;
            CompareButton.IsEnabled = HistoryBaselineButton.IsEnabled = HistoryFolderButton.IsEnabled = PruneButton.IsEnabled = true;
            AccountText.Text = string.IsNullOrEmpty(_preview.Document.User.BattleTag) ? T("Title") : _preview.Document.User.BattleTag;
            ReadyText.Text = T(DateTimeOffset.Now - _preview.ReadAt > TimeSpan.FromMinutes(5) ? "Stale" : "Ready");
            UniqueValue.Text = _preview.OwnedCards.ToString("N0"); CopiesValue.Text = _preview.Copies.ToString("N0");
            PremiumValue.Text = _preview.Premium.ToString("N0"); DustValue.Text = _preview.Document.Dust.ToString("N0");
            ReadTimeText.Text = string.Format(T("ReadTime"), _preview.ReadAt.LocalDateTime.ToString("HH:mm:ss"));
            if(ModeBox.SelectedIndex == 1)
            {
                if(_preview.Changes == null) PreviewText.Text = T(_preview.BaselineIssue ?? "FirstHint");
                else { var s = _preview.Changes.Summary; PreviewText.Text = string.Format(T("PreviewChanges"), s.TotalChanges, s.CardsAdded, s.CardsRemoved, s.CardsChanged); }
                ExportButton.Content = T(_preview.Changes == null ? "CreateBaseline" : "SaveChanges");
            }
            else { PreviewText.Text = string.Format(T("PreviewFull"), _preview.OwnedCards, _preview.Copies); ExportButton.Content = T("Save"); }
            SummaryHint.Text = string.Format(T(_preview.Catalog != null && _preview.Catalog.Count > 0 ? "SummaryCatalog" : "SummaryHint"), _preview.Trial);
            SetsList.ItemsSource = _preview.Sets; RaritiesList.ItemsSource = _preview.Rarities;
            ExportButton.IsEnabled = !_busy;
            UpdateHistory();
        }
        private void UpdateHistory()
        {
            if(_preview == null) return;
            var history = _service.Store.History(_preview.Document.User);
            HistoryList.ItemsSource = history;
            var valid = history.Where(h => h.IsValid && h.CompleteCounts).ToList();
            EarlierBox.ItemsSource = valid; LaterBox.ItemsSource = valid;
            if(valid.Count >= 2) { EarlierBox.SelectedIndex = 1; LaterBox.SelectedIndex = 0; }
            StorageText.Text = string.Format(T("Storage"), history.Count, history.Sum(h => h.Bytes) / 1048576.0);
        }
        private async Task RunAsync(string phase, Func<CancellationToken, Task> action)
        {
            if(_busy || _lifetime.IsCancellationRequested) return;
            _busy = true;
            _operation = CancellationTokenSource.CreateLinkedTokenSource(_lifetime.Token);
            Progress.Visibility = Visibility.Visible; CancelButton.Visibility = Visibility.Visible;
            ExportControls.IsEnabled = false; HistoryControls.IsEnabled = false; RefreshButton.IsEnabled = false; LanguageBox.IsEnabled = false;
            ExportButton.IsEnabled = false;
            StatusText.Text = T(phase);
            try { await action(_operation.Token); }
            catch(OperationCanceledException) { if(!_lifetime.IsCancellationRequested) StatusText.Text = T("Cancelled"); }
            catch(PartialExportException ex) { StatusText.Text = string.Format(T("Partial"), string.Join(", ", ex.Files.Select(Path.GetFileName))); Result(ex.Files.FirstOrDefault()); }
            catch(Exception ex)
            {
                Trace.TraceError("Collection Exporter operation: " + ex.GetType().Name);
                StatusText.Text = string.Format(T("Failed"), ex is IOException || ex is UnauthorizedAccessException ? T("Folder") : T("ChooseHistory"));
            }
            finally
            {
                _operation.Dispose(); _operation = null; _busy = false;
                if(!_lifetime.IsCancellationRequested)
                {
                    Progress.Visibility = Visibility.Collapsed; CancelButton.Visibility = Visibility.Collapsed;
                    ExportControls.IsEnabled = true; HistoryControls.IsEnabled = true; RefreshButton.IsEnabled = true; LanguageBox.IsEnabled = true;
                    ExportButton.IsEnabled = _preview != null;
                }
            }
        }
        private async Task RefreshAsync()
        {
            await RunAsync("Reading", async token =>
            {
                using(var timeout = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    timeout.CancelAfter(TimeSpan.FromSeconds(10));
                    try { _preview = await _service.PrepareAsync(timeout.Token); DisplayPreview(); StatusText.Text = T(_preview.BaselineIssue ?? "Ready"); }
                    catch(Exception ex) when(ex is CollectionUnavailableException || ex is OperationCanceledException)
                    {
                        token.ThrowIfCancellationRequested();
                        _preview = null;
                        UniqueValue.Text = CopiesValue.Text = PremiumValue.Text = DustValue.Text = "—";
                        ReadTimeText.Text = ""; SetsList.ItemsSource = null; RaritiesList.ItemsSource = null; HistoryList.ItemsSource = null;
                        EarlierBox.ItemsSource = null; LaterBox.ItemsSource = null;
                        DisplayPreview();
                        var game = Process.GetProcessesByName("Hearthstone");
                        var running = game.Length > 0; foreach(var process in game) process.Dispose();
                        StatusText.Text = T(running ? "WaitingRead" : "WaitingGame");
                    }
                }
            });
        }
        private async void RefreshClicked(object sender, RoutedEventArgs e) { await RefreshAsync(); }
        private void CancelClicked(object sender, RoutedEventArgs e) { if(_operation != null) _operation.Cancel(); }
        private ExportOptions Options()
        {
            return new ExportOptions { OutputFolder = OutputFolderTextBox.Text, IncludeCardNames = NamesCheck.IsChecked == true,
                IncludeGoldenCount = PremiumCheck.IsChecked == true, IncludeMetadata = MetadataCheck.IsChecked == true };
        }
        private async void ExportClicked(object sender, RoutedEventArgs e)
        {
            if(_preview == null) return;
            if(DateTimeOffset.Now - _preview.ReadAt > TimeSpan.FromMinutes(5)) { await RefreshAsync(); return; }
            Persist();
            var format = FormatBox.SelectedIndex == 0 ? ExportFormat.Json : FormatBox.SelectedIndex == 1 ? ExportFormat.Csv : ExportFormat.Both;
            var options = Options(); var changes = ModeBox.SelectedIndex == 1; var preview = _preview;
            await RunAsync("Saving", async token =>
            {
                var result = await _service.ExportPreparedAsync(preview, changes, format, options, token);
                if(_lifetime.IsCancellationRequested) return;
                _settings.LastExportTimeUtc = DateTime.UtcNow;
                StatusText.Text = result.Warning != null ? T(result.Warning) : result.BaselineCreated ? T("BaselineSaved") : string.Format(T("Saved"), result.Files.Count, Path.GetFileName(result.Files.First()));
                Result(result.Files.FirstOrDefault());
                // The committed document is the new baseline, with no additional HDT read.
                if(result.Warning == null) { preview.Changes = CollectionExportService.Compare(preview.Document, preview.Document); preview.BaselineChecksum = _service.Store.Baseline(preview.Document.User).Checksum; preview.BaselineIssue = null; }
                DisplayPreview(); Persist();
            });
        }
        private void BrowseClicked(object sender, RoutedEventArgs e)
        {
            using(var dialog = new Forms.FolderBrowserDialog { Description = T("Folder"), SelectedPath = OutputFolderTextBox.Text, ShowNewFolderButton = true })
                if(dialog.ShowDialog() == Forms.DialogResult.OK) { OutputFolderTextBox.Text = dialog.SelectedPath; Persist(); }
        }
        private async void SetBaselineClicked(object sender, RoutedEventArgs e)
        {
            if(_preview == null) return;
            await RunAsync("Saving", token => { token.ThrowIfCancellationRequested(); _service.SavePreparedBaseline(_preview); _preview.Changes = CollectionExportService.Compare(_preview.Document, _preview.Document); _preview.BaselineChecksum = _service.Store.Baseline(_preview.Document.User).Checksum; _preview.BaselineIssue = null; DisplayPreview(); StatusText.Text = T("BaselineSaved"); return Task.CompletedTask; });
        }
        private async void ImportClicked(object sender, RoutedEventArgs e)
        {
            if(_preview == null) return;
            using(var dialog = new Forms.OpenFileDialog { Filter = "JSON (*.json)|*.json", Title = T("Import") })
                if(dialog.ShowDialog() == Forms.DialogResult.OK)
                    await RunAsync("Saving", token => { token.ThrowIfCancellationRequested(); _service.ImportBaselineFile(dialog.FileName); _preview.Changes = null; _preview.BaselineChecksum = null; _preview.BaselineIssue = "LegacyCountsUnknown"; DisplayPreview(); StatusText.Text = T("Imported"); return Task.CompletedTask; });
        }
        private async void ClearClicked(object sender, RoutedEventArgs e)
        {
            if(_preview == null) return;
            await RunAsync("Saving", token => { token.ThrowIfCancellationRequested(); _service.ClearBaseline(); _preview.Changes = null; _preview.BaselineChecksum = null; _preview.BaselineIssue = null; DisplayPreview(); StatusText.Text = T("Cleared"); return Task.CompletedTask; });
        }
        private async void CompareClicked(object sender, RoutedEventArgs e)
        {
            var first = EarlierBox.SelectedItem as HistoryEntry; var second = LaterBox.SelectedItem as HistoryEntry;
            if(first == null || second == null) { StatusText.Text = T("ChooseHistory"); return; }
            var folder = OutputFolderTextBox.Text;
            await RunAsync("Saving", async token => { var files = await _service.ExportHistoryAsync(first.Path, second.Path, folder, token); if(_lifetime.IsCancellationRequested) return; Result(files.First()); StatusText.Text = string.Format(T("Saved"), files.Count, Path.GetFileName(files.First())); });
        }
        private void PruneClicked(object sender, RoutedEventArgs e)
        {
            if(_preview == null) return;
            if(MessageBox.Show(this, T("PruneConfirm"), T("History"), MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) != MessageBoxResult.Yes) return;
            try { _service.Store.Prune(_preview.Document.User, 30); UpdateHistory(); }
            catch { StatusText.Text = string.Format(T("Failed"), T("Folder")); }
        }
        private async void HistoryBaselineClicked(object sender, RoutedEventArgs e)
        {
            var entry = HistoryList.SelectedItem as HistoryEntry;
            if(_preview == null || entry == null || !entry.IsValid) { StatusText.Text = T("ChooseHistory"); return; }
            await RunAsync("Saving", token => {
                token.ThrowIfCancellationRequested();
                _service.Store.UseAsBaseline(entry.Path, _preview.Document.User);
                var baseline = _service.Store.Baseline(_preview.Document.User);
                _preview.BaselineIssue = baseline.CompleteCounts ? null : "LegacyCountsUnknown";
                _preview.Changes = baseline.CompleteCounts ? CollectionExportService.Compare(baseline.Document, _preview.Document) : null;
                _preview.BaselineChecksum = baseline.CompleteCounts ? baseline.Checksum : null;
                DisplayPreview(); StatusText.Text = T("BaselineSaved"); return Task.CompletedTask;
            });
        }
        private void Result(string file) { _lastFile = file; ResultActions.Visibility = file == null ? Visibility.Collapsed : Visibility.Visible; }
        private void LaunchFolder(string path)
        {
            try { if(Directory.Exists(path)) Process.Start(new ProcessStartInfo("explorer.exe", "\"" + path + "\"") { UseShellExecute = true }); }
            catch { StatusText.Text = string.Format(T("Failed"), T("Folder")); }
        }
        private void OpenFolderClicked(object sender, RoutedEventArgs e) { if(_lastFile != null) LaunchFolder(Path.GetDirectoryName(_lastFile)); }
        private void ShowFileClicked(object sender, RoutedEventArgs e)
        {
            try { if(_lastFile != null && File.Exists(_lastFile)) Process.Start(new ProcessStartInfo("explorer.exe", "/select,\"" + _lastFile + "\"") { UseShellExecute = true }); }
            catch { StatusText.Text = string.Format(T("Failed"), T("Folder")); }
        }
        private void CopyPathClicked(object sender, RoutedEventArgs e) { try { if(_lastFile != null) { Clipboard.SetText(_lastFile); StatusText.Text = T("Copied"); } } catch { } }
        private void HistoryFolderClicked(object sender, RoutedEventArgs e) { if(_preview != null) LaunchFolder(Path.Combine(Path.GetDirectoryName(_service.Store.BaselinePath(_preview.Document.User)), "history")); }
    }
}
