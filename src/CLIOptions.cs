using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;

namespace ResonanceDownloader;

public class AppOptions
    {
        public string Output { get; set; } = "";
        public string Version { get; set; } = "";
        public string FilterFile { get; set; } = "";
        public string PreviousVersionCompare { get; set; } = "";
        public string PresetName { get; set; } = "";
        public bool DownloadCompressedJab { get; set; } = false;
        public string Region { get; set; } = "ReleaseB_CN";
        public bool ServerInfo { get; set; } = false;
        public string Platform { get; set; } = "StandaloneWindows64";
    }
    
    public class AppOptionsBinder : BinderBase<AppOptions>
    {
        private readonly Option<string> _outputPathOption;
        private readonly Option<string> _versionOption;
        private readonly Option<string> _filterFileOption;
        private readonly Option<string> _previousVersionCompareOption;
        private readonly Option<string> _presetNameOption;
        private readonly Option<bool> _downloadCompressedJabOption;
        private readonly Option<string> _regionOption;
        private readonly Option<bool> _serverInfoOption;
        private readonly Option<string> _platformOption;

        public AppOptionsBinder(
            Option<string> outputPathOption,
            Option<string> versionOption,
            Option<string> filterFileOption,
            Option<string> previousVersionCompareOption,
            Option<string> presetNameOption,
            Option<bool> downloadCompressedJabOption,
            Option<string> regionOption,
            Option<bool> serverInfoOption,
            Option<string> platformOption)
        {
            _outputPathOption = outputPathOption;
            _versionOption = versionOption;
            _filterFileOption = filterFileOption;
            _previousVersionCompareOption = previousVersionCompareOption;
            _presetNameOption = presetNameOption;
            _downloadCompressedJabOption = downloadCompressedJabOption;
            _regionOption = regionOption;
            _serverInfoOption = serverInfoOption;
            _platformOption = platformOption;
        }

        protected override AppOptions GetBoundValue(BindingContext context)
        {
            var result = context.ParseResult;

            string? pvcValue = null;

            if (result.HasOption(_previousVersionCompareOption))
            {
                pvcValue = result.GetValueForOption(_previousVersionCompareOption);

                // handle 3 cases for --previous-version-compare option:
                // if use -pvc [version], pass that version to class
                // if use -pvc alone, still pass to class and automatic determine later
                // no -pvc use, do not compare version
                if (pvcValue is null)
                    pvcValue = ""; 
            }
            
            return new AppOptions
            {
                Output = context.ParseResult.GetValueForOption(_outputPathOption) ?? "",
                Version = context.ParseResult.GetValueForOption(_versionOption) ?? "",
                FilterFile = context.ParseResult.GetValueForOption(_filterFileOption) ?? "filters.json",
                PreviousVersionCompare = pvcValue,
                PresetName = context.ParseResult.GetValueForOption(_presetNameOption) ?? "",
                DownloadCompressedJab = context.ParseResult.GetValueForOption(_downloadCompressedJabOption),
                Region = context.ParseResult.GetValueForOption(_regionOption) ?? "ReleaseB_CN",
                ServerInfo = context.ParseResult.GetValueForOption(_serverInfoOption),
                Platform = context.ParseResult.GetValueForOption(_platformOption) ?? "StandaloneWindows64"
            };
        }
    }