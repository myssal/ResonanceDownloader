using System.CommandLine;
using System.CommandLine.Binding;
using System.CommandLine.Parsing;

namespace ResonanceDownloader;

public class AppOptions
    {
        public string Output { get; set; } = "";
        public string Version { get; set; } = "";
        public string FilterFile { get; set; } = "";
        public bool CompareToBase { get; set; } = false;
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
        private readonly Option<bool> _compareToBaseOption;
        private readonly Option<string> _presetNameOption;
        private readonly Option<bool> _downloadCompressedJabOption;
        private readonly Option<string> _regionOption;
        private readonly Option<bool> _serverInfoOption;
        private readonly Option<string> _platformOption;

        public AppOptionsBinder(
            Option<string> outputPathOption,
            Option<string> versionOption,
            Option<string> filterFileOption,
            Option<bool> compareToBaseOption,
            Option<string> presetNameOption,
            Option<bool> downloadCompressedJabOption,
            Option<string> regionOption,
            Option<bool> serverInfoOption,
            Option<string> platformOption)
        {
            _outputPathOption = outputPathOption;
            _versionOption = versionOption;
            _filterFileOption = filterFileOption;
            _compareToBaseOption = compareToBaseOption;
            _presetNameOption = presetNameOption;
            _downloadCompressedJabOption = downloadCompressedJabOption;
            _regionOption = regionOption;
            _serverInfoOption = serverInfoOption;
            _platformOption = platformOption;
        }

        protected override AppOptions GetBoundValue(BindingContext context)
        {
            
            return new AppOptions
            {
                Output = context.ParseResult.GetValueForOption(_outputPathOption) ?? "",
                Version = context.ParseResult.GetValueForOption(_versionOption) ?? "",
                FilterFile = context.ParseResult.GetValueForOption(_filterFileOption) ?? "filters.json",
                CompareToBase = context.ParseResult.GetValueForOption(_compareToBaseOption),
                PresetName = context.ParseResult.GetValueForOption(_presetNameOption) ?? "",
                DownloadCompressedJab = context.ParseResult.GetValueForOption(_downloadCompressedJabOption),
                Region = context.ParseResult.GetValueForOption(_regionOption) ?? "ReleaseB_CN",
                ServerInfo = context.ParseResult.GetValueForOption(_serverInfoOption),
                Platform = context.ParseResult.GetValueForOption(_platformOption) ?? "StandaloneWindows64"
            };
        }
    }