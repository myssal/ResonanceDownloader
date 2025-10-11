<div align="center">
  <h1>Resonance Downloader</h1>
  <img src="/resources/header.png" alt="Header image" width="40%">
</div>

Asset downloader for [Resonance Solstice](https://store.steampowered.com/app/3037160/Resonance_Solstice).

## Usage:
```
Description:
  Resonance Solstice Assets Downloader

Usage:
  ResonanceDownloader [options]

Options:
  -o, --output <output>                Specify output folder for download contents.
  -ver, --game-version <game-version>  Specify game version. If not set, version will be fetched from server.
  -f, --filter-file <filter-file>      Specify filter JSON file path. If omitted, defaults to filters.json.
  --compare-to-base                    Filter updated asset bundles from previous versions. Specify previous version or
                                       use option with empty argument to use second most lastest game version patch.
                                       [default: False]
  --preset <preset>                    Specify which preset filter to use from filter file.
  -cjab, --download-compressed-jab     Download compressed JAB files. [default: False]
  -r, --region <region>                Specify server region (default: ReleaseB_CN). [default: ReleaseB_CN]
  --server-info                        Show server and platform info list.
  -p, --platform <platform>            Specify platform (default: StandaloneWindows64). [default: StandaloneWindows64]
  --version                            Show version information
  -?, -h, --help                       Show help and usage information
```

**Server options:**
```
======================================
Available Servers
======================================
CN:
- Release: ReleaseB_CN (default)
- Debug:   ReleaseB_DBG
[ GLB ]
- Release: ReleaseB_GLB
- Debug:   ReleaseB_GLB_DBG
[ JP ]
- Release: ReleaseB_JP
- Debug:   ReleaseB_JP_DBG
[ KR ]
- Release: ReleaseB_KR
- Debug:   ReleaseB_KR_DBG
======================================
Available Platforms
======================================
 PC:       StandaloneWindows64 or PC
 Android:  Android
 iOS:      IOS
======================================
```
## To do:
- Improve filter.
- ~~Improve download bundle speed.~~
## Output structure:
```
root folder 
|
+--region_platform
  |
  +-- game_version
      |
      +-- metadata
      |   |
      |   +-- desc.json
      |   +-- [index_ReleaseB.txt]
      +-- bundles
      |   |
      |   +-- [jab]
      |   |   |
      |   |   +-- [compressed jab files]
      |   +-- raw                         
      |   |   |
      |   |   +-- .unity3d/ .ab files
      |
      +-- extracted
          |
          +-- extracted assets
```

## Credits:
- [formagGinoo](github.com/formagGinoo): for `desc.bin` and `.jab` parser [tool](https://github.com/formagGinoo/ResonanceTools).