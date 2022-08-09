# Readme for the Resources folder

This folder is used by the app to function properly - (most) files are as follows:

**Languages\**
Folder where language SQLite files are kept

**.ExifTool_config**
Config file for ExifTool - don't modify.

**exiftool.exe**
ExifTool itself. Feel free to update.

**MicrosoftEdgeWebview2Setup.exe**
Webview2 setup - included as the installer needs it and in case you need it too.

**map.html**
HTML file the app uses for map purposes. Feel free to edit your own.

**AppIcon.ico**
Hopefully obvious

**geoTagNinja_square.ico**
Hopefully obvious

**readme.md**
This file

**isoCountryCodeMapping.sqlite**
SQLite file containing ISO codes and country codes as well as timezone data.
TZ data from https://timezonedb.com/download -- truncated to exclude dates cca +/- 20 years from now. Even that's a bit pointless as the app will only look at "current" time zones.

**objectMapping.sqlite**
Output from the XLSM file in the ExtraFiles folder.