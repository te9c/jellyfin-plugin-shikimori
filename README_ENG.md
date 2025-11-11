[RU](README.md)
<h1 align="center">jellyfin-plugin-shikimori</h1>

# About

This plugin adds the metadata provider for [Shikimori](https://shikimori.one)

# Installation

1. Go to Jellyfin's dashboard -> Plugins -> Repositories.
2. Add new repository with url: `https://raw.githubusercontent.com/te9c/jellyfin-plugin-shikimori/main/manifest.json`.
3. Install Shikimori plugin from the catalogue.

# Build

1. Make sure that you have [.NET9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0) installed
2. Clone this repository
3. Build plugin with following command
```bash
dotnet publish --configuration Release --output bin
```
4. Create folder `shikimori` in jellyfin's plugins directory (it's located in [data directory](https://jellyfin.org/docs/general/administration/configuration/#data-directory))
5. Move dll's `Jellyfin.Plugin.Shikimori.dll` and `Newtonsoft.Json.dll` from the `bin` directory to the `shikimori` directory created in previous step.

# Releasing

The best way to release is to use
[JPRM](https://github.com/oddstr13/jellyfin-plugin-repository-manager), which
can package the plugin and add it to the `manifest.json`.
