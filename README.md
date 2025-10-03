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
  output, -o <output>                 Specify output folder for download contents.
  -gv, --game-version <game-version>  Specify game version. If not set, the version will be fetched from server.
  -f, --filter-file <filter-file>     Specify filter json file path. If specify with no input, default input will be
                                      filters.json.
  -pr, --preset <preset>              Specify which preset filter to use from filter file. Only valid if --filter-file
                                      or -f is specified.
  -cjab, --download-compressed-jab    Specify to download compressed jab files or not. [default: False]
  -r, --region <region>               Specify server region and type. Default is CN Release. For full server option,
                                      use --server-info or -svi. [default: ReleaseB_CN]
  -svi, --server-info                 Servers info list.
  -p, --platform <platform>           Specify platform. Default is PC (StandaloneWindows64). For full platform option,
                                      use --server-info or -svi. [default: StandaloneWindows64]
  --version                           Show version information
  -?, -h, --help                      Show help and usage information
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
- Improve download bundle speed.
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