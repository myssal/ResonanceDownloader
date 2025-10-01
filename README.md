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
  ResonanceDownloader <output> [options]

Arguments:
  <output>  Specify output folder for download contents.

Options:
  -gv, --game-version <game-version>  Specify game version. If not set, the version will be fetched from server.
  -f, --filter-file <filter-file>     Specify filter json file path. If specify with no input, default input will be
                                      filters.json. [default: filters.json]
  -p, --preset <preset>               Specify which preset filter to use from filter file. Only valid if --filter-file
                                      or -f is specified.
  --version                           Show version information
  -?, -h, --help                      Show help and usage information
```

## Output structure:
```
root folder
|
+-- game_version
    |
    +-- metadata
    |   |
    |   +-- desc.json
    +-- bundles
    |   |
    |   +-- jab
    |   |   |
    |   |   +-- compressed jab files
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