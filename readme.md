# Welcome to GeoTagNinja

GeoTagNinja is an open-source geotagger for Windows. It's a lightweight an open-source (written from scratch) reminiscent of GeoSetter, which hasn't been updated for a longer while and is now a little obsolete as it's using IE and other technologies that are becoming increasingly ageing.
GTN uses [exifTool](https://exifTool.org/) to read and write EXIF info. This also sets the limitations and capabilities. If exifTool can read/write a file, chances are so can GTN.
There is a "short" (15 mins) capabilities demo on [Youtube](https://youtu.be/ulP1ZG7mH-I) if you feel like watching it. Download the .msi file from [Releases](https://github.com/nemethviktor/GeoTagNinja/releases) - click on Assets if they are not showing.

## Things to Note, Usage and Testing

- This is mentioned in Known Issues briefly but just to reiterate: if you have issues with file data not showing please try renaming/moving your files to a "simple" folder like "C:\temp" and make sure your file names don't contain umlauts, special characters and anything "odd". This is a limitation of exifTool.
- This isn't GeoSetter. I've included the map/toponymy functionality because admittedly that's what I use. For the time being that's it. I am open to requests and commits/contributions from others. Admittedly I'd rather bug-hunt initially and then add in new stuff though. Probably the next step will be time zones and time-setting.
- My background is SQL and VBA development and contrary to the obvious I've never before written anything in C# or similar. This means that the code is probably a bit ugly, but it works. As for naming conventions, they've been "more or less" followed. An effort has been made. Classes are mostly okay-named and that's that. I'm aware. If someone wants to refactor the whole code, be my guest.
- Again this isn't GeoSetter and I'm not the guy that wrote it and I don't have access to the source code of it. GeoSetter (I think) used DCRAW/libRAW to read RAW files. The advantage of that is there are native libraries for C# that hook DCRAW and it's fast AF. The disadvantage is that it has a comparatively limited range of file support, e.g. CR3s aren't supported and neither are some others. GTN uses exifTool for everything, which supports a lot more extensions but it has to be called externally each time the user interacts with a file or folder. The most visible disadvantage of this is that it takes a second (or two or three or five) to load up exifTool and the app may appear non-responsive when entering a folder. For the time being this is what it is, be patient and I'm hoping someone in the coder community can make suggestions on how to deal with this. FWIW I've tried having the whole shebang as Static classes and also as non-Static classes and while I settled on the latter I think it makes f.k all difference.
- I have tried to test reasonably thoroughly but bugs probably remain.
-- Basically until the program hits version 1 (currently it's v0.x) most probably save your original files before using this. This particularly applies if you happen to be using a non-English version of Windows or if your various file paths contain non-standard characters.
- As per usual I don't accept any liability for damage and/or any other inconvenience you may suffer while/from using this app, no warranties and/or guarantees. The script is open source and everyone is welcome to read it. 
- During Setup the installer will trigger the webView2 installer as well. This is an Edge/Chromium-based engine that is required to show the map.
- I'm kinda hoping this won't come up a problem but don't bash me about country names please. I'm thinking disputed country names and areas here. They are ISO standards, the API returns those values, that's it. Being from the UK I do think that the long name of "United Kingdom of Great Britain and Northern Ireland (the)" is a bit lengthy but it is what it is (and it's relatively rarely disputed but for example Crimea does return Ukraine as a value, regardless of what's been going on there since 2014; again, this isn't a statement either way, it's the API reply). 
-- On this particular note above --> the script won't change existing country details in files unless you explicitly edit them. So it may happen that your existing file has the CountryCode GBR and the Country "United Kingdom", that won't be changed unless you do a "Get From Web" because the CountryCode-to-Country matching only runs in that case. That's a feature, not a bug.
- Altitude: in some cases the API returns 32K or negative 32K as an altitude. Those are being blanked out.
- Pressing Get From Web in Edit mode will always query the lat/long data for the file as in the main listview (the main page, in less geeky terms.). This is a feature. The assumption is that you aren't going to change coordinates manually by typing stuff in and _then_ query data. It's a lot more likely you'll change stuff using the map.
- If you have Avast running or some other nightmare that tries to capture iframes within apps the app will most likely crash sooner rather than later, at least on the first run. I *think* it should be okay afterwards.

## Building

If you want to build the project probably use Visual Studio - I used v2022 Community. Instead of downloading the source code as zip please pull from Git, you can do that via VS if you want. 
There are 2 parts to the project. One is the "main" the other is the installer. You'll generally have problems w/ the installer bcs it hasn't been pushed to git so it's going to be missing that half.
For the "main" project you should be okay without anything separate. It has worked ok for me on a blank VM when pulled from Git. Just build and F5/run.

## ToDos

- [TODO] Add a setting for cleaning local api data on start/exit

- [half] Add copy-paste functionality - kinda done but no choices as to what to paste.
- [later] Destination stuff not working atm, todo.
- [later] Column reorg.
- [later] add time-shift capabilities

## Known Issues

- There is a likelyhood that the app will struggle to read file data if your files are kept in folders with accent marks (umlauts, non-standard English characters) in the path and/or filename. This is a limitation of exifTool + cmd. If you encounter a problem, move your files to something like "C:\temp" and see if it works better.
-- On top of that above exifTool has a hard limit of 260 characters for file names including paths. Anything beyond that will fail. Again, rename your files or temporarily move them to C:\temp if this is an issue. Unicode (e.g. most Chinese and other Asian) characters have an up to 4-byte size per character. This is to say you'll run into errors with these files more often than not.
-- Alternatively you can enable [this](https://stackoverflow.com/questions/56419639/what-does-beta-use-unicode-utf-8-for-worldwide-language-support-actually-do) feature if you're running v1903 Windows 10 or later but it may have some unwanted effects so keep an eye out for changes caused by it. 
- Multi-select doesn't show on the map. I don't know how to do it.
- For UK the API returns city/sublocation in an odd way. this has been *probably* fixed in code but do test.
- Language support should generally be okay but since the app is constantly evolving at this stage I'd prob vote against spending time on adding in a new language just yet.
- Listview columns hiding/reorg capability is lacking. 
-- On a related note there is an attempt to clean up the user folder (C:\Users\username\AppData\Roaming\GeoTagNinja\) on application exit but it might fail due to a file lock. This doesn't crash the app but some files may remain, they'll be cleaned up the next time the app runs.
- Sometimes a number of cmd.exe handles remain open. Not sure why but they only consume about 800kbytes of memory each so while not great, not terrible. You can force-kill them with "taskkill /f /im cmd.exe" ; I didn't want to put that into the code as it kills _all_ cmd instances regardless of what conjured them up so it would kill any instances you're otherwise running.
- Pressing Get From Web either in Edit mode or on the map will always set the affected file to write-queue even if the values don't actually change.
- Preview images don't respect orientation.
- If user zooms "too far out" on Map they will get odd longitude values. The code handles this internally but map feedback is what it is.

## When reporting bugs please specify

- The OS's version,
- The OS's language (incl region such as English/UK or English/USA),
- Full path of your photo if the bug is related to reading or writing data,
- Possibly upload the picture somewhere if not sensitive so I can test.

## System Requirements

- Windows 7+ is needed. The WebView2 installer runs along Setup. Should you need separately, it's available [here](https://go.microsoft.com/fwlink/p/?LinkId=2124703) - the installer should take care of it tho'.
- SQLite is running x86 but fwiw the app isn't really memory-hungry so it will do. Chances are if you're still on a 1st-gen Intel Core you're not on Windows 7. Hopefully.
- You'll need an ArcGIS API key to use the map search functionality. Register for free [here](https://developers.arcgis.com/)
- You'll need a geoNames username and password to use toponomy search. Register for free [here](https://www.geonames.org/)