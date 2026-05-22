using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using HdtCollectionExporter.Models;
using HdtCollectionExporter.Services;
using HdtCollectionExporter.Settings;
using Forms = System.Windows.Forms;

namespace HdtCollectionExporter.UI
{
    public partial class ExportWindow : Window
    {
        private readonly PluginSettings _settings;
        private readonly CollectionExportService _exportService;
        private readonly Action _saveSettings;
        private readonly ExportWindowText _text;
        private bool _isExporting;

        public ExportWindow(PluginSettings settings, CollectionExportService exportService, Action saveSettings)
            : this(settings, exportService, saveSettings, ExportWindowText.English())
        {
        }

        public ExportWindow(PluginSettings settings, CollectionExportService exportService, Action saveSettings, ExportWindowText text)
        {
            if(settings == null)
                throw new ArgumentNullException("settings");
            if(exportService == null)
                throw new ArgumentNullException("exportService");
            if(saveSettings == null)
                throw new ArgumentNullException("saveSettings");

            _settings = settings;
            _exportService = exportService;
            _saveSettings = saveSettings;
            _text = text ?? ExportWindowText.English();

            InitializeComponent();
            Width = _settings.WindowWidth > 0 ? _settings.WindowWidth : Width;
            Height = _settings.WindowHeight > 0 ? _settings.WindowHeight : Height;
            ApplyText();
            LoadSettingsToUi();
        }

        private void BrowseButton_OnClick(object sender, RoutedEventArgs e)
        {
            using(var dialog = new Forms.FolderBrowserDialog())
            {
                dialog.Description = _text.FolderDialogDescription;
                dialog.SelectedPath = GetOutputFolder();
                dialog.ShowNewFolderButton = true;
                if(dialog.ShowDialog() == Forms.DialogResult.OK)
                {
                    OutputFolderTextBox.Text = dialog.SelectedPath;
                    PersistSettingsFromUi();
                }
            }
        }

        private async void ExportJsonButton_OnClick(object sender, RoutedEventArgs e)
        {
            await RunExportAsync(ExportFormat.Json);
        }

        private async void ExportCsvButton_OnClick(object sender, RoutedEventArgs e)
        {
            await RunExportAsync(ExportFormat.Csv);
        }

        private async void ExportBothButton_OnClick(object sender, RoutedEventArgs e)
        {
            await RunExportAsync(ExportFormat.Both);
        }

        private async void ExportChangesButton_OnClick(object sender, RoutedEventArgs e)
        {
            await RunChangesExportAsync();
        }

        protected override void OnClosed(EventArgs e)
        {
            PersistSettingsFromUi();
            base.OnClosed(e);
        }

        private async Task RunExportAsync(ExportFormat format)
        {
            if(_isExporting)
                return;

            PersistSettingsFromUi();
            SetBusy(true);
            SetStatus(_text.Exporting);

            try
            {
                var result = await _exportService.ExportAsync(format, BuildOptions());
                _settings.LastExportTimeUtc = result.ExportedAt.UtcDateTime;
                _settings.LastStatus = _text.SuccessPrefix + result.CardCount + _text.SuccessMiddle +
                                       string.Join(", ", result.Files.Select(Path.GetFileName));
                _saveSettings();

                SetStatus(_settings.LastStatus);
                UpdateLastExportText();
            }
            catch(CollectionUnavailableException ex)
            {
                SetError(ex.Message);
            }
            catch(UnauthorizedAccessException ex)
            {
                SetError(_text.CannotWrite + ex.Message);
            }
            catch(IOException ex)
            {
                SetError(_text.FileExportFailed + ex.Message);
            }
            catch(Exception ex)
            {
                SetError(_text.ExportFailed + ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task RunChangesExportAsync()
        {
            if(_isExporting)
                return;

            PersistSettingsFromUi();
            SetBusy(true);
            SetStatus(_text.ExportingChanges);

            try
            {
                var result = await _exportService.ExportChangesAsync(BuildOptions());
                _settings.LastExportTimeUtc = result.ExportedAt.UtcDateTime;
                _settings.LastStatus = _text.ChangesSuccessPrefix + result.ChangeCount + _text.ChangesSuccessMiddle +
                                       string.Join(", ", result.Files.Select(Path.GetFileName));
                _saveSettings();

                SetStatus(_settings.LastStatus);
                UpdateLastExportText();
            }
            catch(NoPreviousExportException)
            {
                SetError(_text.NoPreviousExport);
            }
            catch(CollectionUnavailableException ex)
            {
                SetError(ex.Message);
            }
            catch(UnauthorizedAccessException ex)
            {
                SetError(_text.CannotWrite + ex.Message);
            }
            catch(IOException ex)
            {
                SetError(_text.FileExportFailed + ex.Message);
            }
            catch(Exception ex)
            {
                SetError(_text.ExportFailed + ex.Message);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private ExportOptions BuildOptions()
        {
            return new ExportOptions
            {
                OutputFolder = GetOutputFolder(),
                IncludeCardNames = IncludeCardNamesCheckBox.IsChecked == true,
                IncludeGoldenCount = IncludeGoldenCountCheckBox.IsChecked == true,
                IncludeMetadata = IncludeMetadataCheckBox.IsChecked == true
            };
        }

        private void LoadSettingsToUi()
        {
            OutputFolderTextBox.Text = string.IsNullOrWhiteSpace(_settings.OutputFolder)
                ? PluginSettings.GetDefaultOutputFolder()
                : _settings.OutputFolder;
            IncludeCardNamesCheckBox.IsChecked = _settings.IncludeCardNames;
            IncludeGoldenCountCheckBox.IsChecked = _settings.IncludeGoldenCount;
            IncludeMetadataCheckBox.IsChecked = _settings.IncludeMetadata;
            SetStatus(string.IsNullOrWhiteSpace(_settings.LastStatus) ? _text.Ready : _settings.LastStatus);
            UpdateLastExportText();
        }

        private void ApplyText()
        {
            Title = _text.WindowTitle;
            HeaderTitleTextBlock.Text = _text.HeaderTitle;
            BrandTextBlock.Text = _text.BrandLine;
            OutputFolderLabel.Text = _text.OutputFolder;
            BrowseButton.Content = _text.Browse;
            OptionsTitleTextBlock.Text = _text.OptionsTitle;
            IncludeCardNamesCheckBox.Content = _text.IncludeCardNames;
            IncludeGoldenCountCheckBox.Content = _text.IncludeGoldenCount;
            IncludeMetadataCheckBox.Content = _text.IncludeMetadata;
            ExportJsonButton.Content = _text.ExportJson;
            ExportCsvButton.Content = _text.ExportCsv;
            ExportBothButton.Content = _text.ExportBoth;
            ExportChangesButton.Content = _text.ExportChanges;
            StatusLabelTextBlock.Text = _text.Status;
            PrivacyNoteTextBlock.Text = _text.PrivacyNote;
        }

        private void PersistSettingsFromUi()
        {
            _settings.OutputFolder = GetOutputFolder();
            _settings.IncludeCardNames = IncludeCardNamesCheckBox.IsChecked == true;
            _settings.IncludeGoldenCount = IncludeGoldenCountCheckBox.IsChecked == true;
            _settings.IncludeMetadata = IncludeMetadataCheckBox.IsChecked == true;
            _settings.WindowWidth = Width;
            _settings.WindowHeight = Height;
            _saveSettings();
        }

        private string GetOutputFolder()
        {
            var folder = OutputFolderTextBox.Text;
            return string.IsNullOrWhiteSpace(folder) ? PluginSettings.GetDefaultOutputFolder() : folder.Trim();
        }

        private void SetBusy(bool isBusy)
        {
            _isExporting = isBusy;
            ExportJsonButton.IsEnabled = !isBusy;
            ExportCsvButton.IsEnabled = !isBusy;
            ExportBothButton.IsEnabled = !isBusy;
            ExportChangesButton.IsEnabled = !isBusy;
            BrowseButton.IsEnabled = !isBusy;
        }

        private void SetStatus(string message)
        {
            StatusTextBlock.Text = message;
        }

        private void SetError(string message)
        {
            _settings.LastStatus = _text.ErrorPrefix + message;
            _saveSettings();
            SetStatus(_settings.LastStatus);
        }

        private void UpdateLastExportText()
        {
            if(_settings.LastExportTimeUtc == DateTime.MinValue)
            {
                LastExportTextBlock.Text = _text.LastExportNever;
                return;
            }

            var localTime = DateTime.SpecifyKind(_settings.LastExportTimeUtc, DateTimeKind.Utc).ToLocalTime();
            LastExportTextBlock.Text = _text.LastExportPrefix + localTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
    }
}
