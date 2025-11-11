[ENG](README_ENG.md)
<h1 align="center">jellyfin-plugin-shikimori</h1>

# О плагине

Этот плагин загружает метаданные с сайта [Shikimori](https://shikimori.one)

Плагин поддерживает загрузку метадаты для аниме фильмов и сериалов. Загружает
рейтинг, описание, постеры, жанры, студии.

Для лучшего поиска можно указать в названии медиафайла или корневой папки тэг вида `[shikimori-12345]`,
где число - это id с сайта Shikimori.

# Установка

1. Зайти в Администрирование -> Панель управления -> Плагины -> Управление репозиториями -> Новый репозиторий.
2. Ввести название, в поле "URL репозитория" ввести:
```
https://raw.githubusercontent.com/te9c/jellyfin-plugin-shikimori/main/manifest.json
```
3. Установить плагин через каталог.

# Сборка

1. Установить [.NET9 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/9.0)
2. Склонировать этот репозиторий
3. Скомпилировать плагин следующей командой:
```bash
dotnet publish --configuration Release --output bin
```
4. Создать директорию `shikimori` в папке с плагинами джелифина (Она находится в [основной директории jellyfin](https://jellyfin.org/docs/general/administration/configuration/#data-directory))
5. Переместить dll `Jellyfin.Plugin.Shikimori.dll` и `Newtonsoft.Json.dll` из директории `bin` в `shikimori`, созданной в предыдущем шаге.
