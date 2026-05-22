namespace HdtCollectionExporter.UI
{
    public class ExportWindowText
    {
        public string WindowTitle { get; set; }

        public string HeaderTitle { get; set; }

        public string BrandLine { get; set; }

        public string OutputFolder { get; set; }

        public string Browse { get; set; }

        public string OptionsTitle { get; set; }

        public string IncludeCardNames { get; set; }

        public string IncludeGoldenCount { get; set; }

        public string IncludeMetadata { get; set; }

        public string ExportJson { get; set; }

        public string ExportCsv { get; set; }

        public string ExportBoth { get; set; }

        public string ExportChanges { get; set; }

        public string Status { get; set; }

        public string PrivacyNote { get; set; }

        public string FolderDialogDescription { get; set; }

        public string Ready { get; set; }

        public string Exporting { get; set; }

        public string ExportingChanges { get; set; }

        public string SuccessPrefix { get; set; }

        public string SuccessMiddle { get; set; }

        public string ChangesSuccessPrefix { get; set; }

        public string ChangesSuccessMiddle { get; set; }

        public string ErrorPrefix { get; set; }

        public string NoPreviousExport { get; set; }

        public string CannotWrite { get; set; }

        public string FileExportFailed { get; set; }

        public string ExportFailed { get; set; }

        public string LastExportNever { get; set; }

        public string LastExportPrefix { get; set; }

        public static ExportWindowText English()
        {
            return new ExportWindowText
            {
                WindowTitle = "Collection Exporter by Manacost",
                HeaderTitle = "Collection Exporter",
                BrandLine = "by Manacost",
                OutputFolder = "Output folder",
                Browse = "Browse...",
                OptionsTitle = "Export options",
                IncludeCardNames = "include card names",
                IncludeGoldenCount = "include golden count",
                IncludeMetadata = "include metadata",
                ExportJson = "Export JSON",
                ExportCsv = "Export CSV",
                ExportBoth = "Export Both",
                ExportChanges = "Export Changes",
                Status = "Status",
                PrivacyNote = "Local export by Manacost. No collection data is sent to external services.",
                FolderDialogDescription = "Select collection export folder",
                Ready = "Ready.",
                Exporting = "Exporting collection...",
                ExportingChanges = "Exporting collection changes...",
                SuccessPrefix = "Success: exported ",
                SuccessMiddle = " cards to ",
                ChangesSuccessPrefix = "Success: exported ",
                ChangesSuccessMiddle = " changes to ",
                ErrorPrefix = "Error: ",
                NoPreviousExport = "No previous export was found. Run Export Both once before exporting changes.",
                CannotWrite = "Cannot write to the output folder: ",
                FileExportFailed = "File export failed: ",
                ExportFailed = "Export failed: ",
                LastExportNever = "Last export: never",
                LastExportPrefix = "Last export: "
            };
        }

        public static ExportWindowText Russian()
        {
            return new ExportWindowText
            {
                WindowTitle = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 \u043a\u043e\u043b\u043b\u0435\u043a\u0446\u0438\u0438 \u043e\u0442 Manacost",
                HeaderTitle = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 \u043a\u043e\u043b\u043b\u0435\u043a\u0446\u0438\u0438",
                BrandLine = "\u043e\u0442 \u043a\u043e\u043c\u0430\u043d\u0434\u044b Manacost",
                OutputFolder = "\u041f\u0430\u043f\u043a\u0430 \u044d\u043a\u0441\u043f\u043e\u0440\u0442\u0430",
                Browse = "\u0412\u044b\u0431\u0440\u0430\u0442\u044c...",
                OptionsTitle = "\u041d\u0430\u0441\u0442\u0440\u043e\u0439\u043a\u0438 \u044d\u043a\u0441\u043f\u043e\u0440\u0442\u0430",
                IncludeCardNames = "\u0434\u043e\u0431\u0430\u0432\u043b\u044f\u0442\u044c \u043d\u0430\u0437\u0432\u0430\u043d\u0438\u044f \u043a\u0430\u0440\u0442",
                IncludeGoldenCount = "\u0434\u043e\u0431\u0430\u0432\u043b\u044f\u0442\u044c \u0437\u043e\u043b\u043e\u0442\u044b\u0435/\u043f\u0440\u0435\u043c\u0438\u0443\u043c \u043a\u0430\u0440\u0442\u044b",
                IncludeMetadata = "\u0434\u043e\u0431\u0430\u0432\u043b\u044f\u0442\u044c \u043c\u0435\u0442\u0430\u0434\u0430\u043d\u043d\u044b\u0435",
                ExportJson = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 JSON",
                ExportCsv = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 CSV",
                ExportBoth = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442\u0438\u0440\u043e\u0432\u0430\u0442\u044c \u043e\u0431\u0430",
                ExportChanges = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 \u0438\u0437\u043c\u0435\u043d\u0435\u043d\u0438\u0439",
                Status = "\u0421\u0442\u0430\u0442\u0443\u0441",
                PrivacyNote = "\u041b\u043e\u043a\u0430\u043b\u044c\u043d\u044b\u0439 \u044d\u043a\u0441\u043f\u043e\u0440\u0442 \u043e\u0442 Manacost. \u0414\u0430\u043d\u043d\u044b\u0435 \u043d\u0435 \u043e\u0442\u043f\u0440\u0430\u0432\u043b\u044f\u044e\u0442\u0441\u044f \u0432\u043e \u0432\u043d\u0435\u0448\u043d\u0438\u0435 \u0441\u0435\u0440\u0432\u0438\u0441\u044b.",
                FolderDialogDescription = "\u0412\u044b\u0431\u0435\u0440\u0438\u0442\u0435 \u043f\u0430\u043f\u043a\u0443 \u0434\u043b\u044f \u044d\u043a\u0441\u043f\u043e\u0440\u0442\u0430 \u043a\u043e\u043b\u043b\u0435\u043a\u0446\u0438\u0438",
                Ready = "\u0413\u043e\u0442\u043e\u0432\u043e.",
                Exporting = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 \u043a\u043e\u043b\u043b\u0435\u043a\u0446\u0438\u0438...",
                ExportingChanges = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 \u0438\u0437\u043c\u0435\u043d\u0435\u043d\u0438\u0439 \u043a\u043e\u043b\u043b\u0435\u043a\u0446\u0438\u0438...",
                SuccessPrefix = "\u0423\u0441\u043f\u0435\u0448\u043d\u043e: \u044d\u043a\u0441\u043f\u043e\u0440\u0442\u0438\u0440\u043e\u0432\u0430\u043d\u043e ",
                SuccessMiddle = " \u043a\u0430\u0440\u0442 \u0432 ",
                ChangesSuccessPrefix = "\u0423\u0441\u043f\u0435\u0448\u043d\u043e: \u044d\u043a\u0441\u043f\u043e\u0440\u0442\u0438\u0440\u043e\u0432\u0430\u043d\u043e ",
                ChangesSuccessMiddle = " \u0438\u0437\u043c\u0435\u043d\u0435\u043d\u0438\u0439 \u0432 ",
                ErrorPrefix = "\u041e\u0448\u0438\u0431\u043a\u0430: ",
                NoPreviousExport = "\u041d\u0435\u0442 \u043f\u0440\u0435\u0434\u044b\u0434\u0443\u0449\u0435\u0433\u043e \u044d\u043a\u0441\u043f\u043e\u0440\u0442\u0430. \u0421\u043d\u0430\u0447\u0430\u043b\u0430 \u0441\u0434\u0435\u043b\u0430\u0439\u0442\u0435 \u043f\u043e\u043b\u043d\u044b\u0439 \u044d\u043a\u0441\u043f\u043e\u0440\u0442.",
                CannotWrite = "\u041d\u0435\u0442 \u0434\u043e\u0441\u0442\u0443\u043f\u0430 \u043d\u0430 \u0437\u0430\u043f\u0438\u0441\u044c \u0432 \u043f\u0430\u043f\u043a\u0443: ",
                FileExportFailed = "\u041d\u0435 \u0443\u0434\u0430\u043b\u043e\u0441\u044c \u0437\u0430\u043f\u0438\u0441\u0430\u0442\u044c \u0444\u0430\u0439\u043b: ",
                ExportFailed = "\u042d\u043a\u0441\u043f\u043e\u0440\u0442 \u043d\u0435 \u0432\u044b\u043f\u043e\u043b\u043d\u0435\u043d: ",
                LastExportNever = "\u041f\u043e\u0441\u043b\u0435\u0434\u043d\u0438\u0439 \u044d\u043a\u0441\u043f\u043e\u0440\u0442: \u043d\u0438\u043a\u043e\u0433\u0434\u0430",
                LastExportPrefix = "\u041f\u043e\u0441\u043b\u0435\u0434\u043d\u0438\u0439 \u044d\u043a\u0441\u043f\u043e\u0440\u0442: "
            };
        }
    }
}
