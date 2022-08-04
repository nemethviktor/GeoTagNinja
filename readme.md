

# Welcome to GeoTagNinja

GeoTagNinja is an open-source geotagger for Windows. It's a lightweight an open-source (written from scratch) reminiscent of GeoSetter, which hasn't been updated for a longer while and is now a little obsolete as it's using IE and other technologies that are becoming increasingly ageing.
GTN uses [exifTool](https://exifTool.org/) to read and write EXIF info. This also sets the limitations and capabilities. If exifTool can read/write a file, chances are so can GTN.
There is a "short" (15 mins) capabilities demo on [Youtube](https://youtu.be/ulP1ZG7mH-I) if you feel like watching it. Download the .msi file from [Releases](https://github.com/nemethviktor/GeoTagNinja/releases) - click on Assets if they are not showing.

## Things to Note, Usage and Testing

- This isn't GeoSetter. I've included the map/toponymy functionality because admittedly that's what I use. For the time being that's it. I am open to requests and commits/contributions from others. Admittedly I'd rather bug-hunt initially and then add in new stuff though. Probably the next step will be time zones and time-setting.
- My background is SQL and VBA development and contrary to the obvious I've never before written anything in C# or similar. This means that the code is probably a bit ugly, but it works. As for naming conventions, they've been "more or less" followed. An effort has been made. Classes are mostly okay-named and that's that. I'm aware. If someone wants to refactor the whole code, be my guest.
- Again this isn't GeoSetter and I'm not the guy that wrote it and I don't have access to the source code of it. GeoSetter (I think) used DCRAW/libRAW to read RAW files. The advantage of that is there are native libraries for C# that hook DCRAW and it's fast AF. The disadvantage is that it has a comparatively limited range of file support, e.g. CR3s aren't supported and neither are some others. GTN uses exifTool for everything, which supports a lot more extensions but it has to be called externally each time the user interacts with a file or folder. The most visible disadvantage of this is that it takes a second (or two or three or five) to load up exifTool and the app may appear non-responsive when entering a folder. For the time being this is what it is, be patient and I'm hoping someone in the coder community can make suggestions on how to deal with this. FWIW I've tried having the whole shebang as Static classes and also as non-Static classes and while I settled on the latter I think it makes f.k all difference.
- I have tried to test reasonably thoroughly but bugs probably remain.
-- Basically until the program hits version 1 (currently it's v0.x) most probably save your original files before using this. This particularly applies if you happen to be using a non-English version of Windows or if your various file paths contain non-standard characters.
- As per usual I don't accept any liability for damage and/or any other inconvenience you may suffer while/from using this app, no warranties and/or guarantees. The script is open source and everyone is welcome to read it. 
- This is detailed in the Known Issues but if you are using non-English filenames then you may want to turn on UTF support in Windows 10. This feature is not available in older Windows versions.
- During Setup the installer will trigger the webView2 installer as well. This is an Edge/Chromium-based engine that is required to show the map.
- I'm kinda hoping this won't come up a problem but don't bash me about country names please. I'm thinking disputed country names and areas here. They are ISO standards, the API returns those values, that's it. Being from the UK I do think that the long name of "United Kingdom of Great Britain and Northern Ireland (the)" is a bit lengthy but it is what it is (and it's relatively rarely disputed but for example Crimea does return Ukraine as a value, regardless of what's been going on there since 2014; again, this isn't a statement either way, it's the API reply). 
-- On this particular note above --> the script won't change existing country details in files unless you explicitly edit them. So it may happen that your existing file has the CountryCode GBR and the Country "United Kingdom", that won't be changed unless you do a "Get From Web" because the CountryCode-to-Country matching only runs in that case. That's a feature, not a bug.
- Altitude: in some cases the API returns 32K or negative 32K as an altitude. Those are being blanked out.
- Pressing Get From Web in Edit mode will always query the lat/long data for the file as in the main listview (the main page, in less geeky terms.). This is a feature. The assumption is that you aren't going to change coordinates manually by typing stuff in and _then_ query data. It's a lot more likely you'll change stuff using the map.
- If you have Avast running or some other nightmare that tries to capture iframes within apps the app will most likely crash sooner rather than later, at least on the first run. I *think* it should be okay afterwards.

## ToDos

- [TODO] Add a setting for cleaning local api data on start/exit

- [half] Add copy-paste functionality - kinda done but no choices as to what to paste.
- [half] Language support. MessageBoxes are still hard-coded. I'd personally vote for leaving this till a bit later once the app is more complete.
- [later] Destination stuff not working atm, todo.
- [later] Column reorg.
- [later] add time-shift capabilities
- [done] Get All From Web
- [done] Check webview2 is available
- [done] Add map-to-image function
- [done] Add filegroup-specific options --> ascertain default values get respected.
- [done] Investigate Overwrite. Methinks it's not working.
- [done] Correct XMP Sidecar process
- [done] Add "last location" setting
- [done] Add img preview
- [done] Cbx country dropdown not working
- [done] Cbx needs to be synced w country

## Known Issues

- exifTool has a hard limit of 260 characters for file names including paths. Anything beyond that will fail. Rename your files or temporarily move them to C:\temp if this is an issue. Unicode (e.g. most Chinese and other Asian) characters have an up to 4-byte size per character. This is to say you'll run into errors with these files more often than not.
-- Alternatively you can enable [this](https://stackoverflow.com/questions/56419639/what-does-beta-use-unicode-utf-8-for-worldwide-language-support-actually-do) feature if you're running v1903 Windows 10 or later but it may have some unwanted effects so keep an eye out for changes caused by it. 
- Multi-select doesn't show on the map. I don't know how to do it.
- For UK the API returns city/sublocation in an odd way. this has been *probably* fixed in code but do test.
- Language support has not beed tested at all and is most likely "imperfect". Most specifically messageboxes are hardcoded. This has been low priority in lieu of the likelyhood of extension of capabilities in the near future.
- Listview columns hiding/reorg capability is lacking. 
-- On a related note there is an attempt to clean up the user folder (C:\Users\username\AppData\Roaming\GeoTagNinja\) on application exit but it might fail due to a file lock. This doesn't crash the app but some files may remain, they'll be cleaned up the next time the app runs.
- Sometimes a number of cmd.exe handles remain open. Not sure why but they only consume about 800kbytes of memory each so while not great, not terrible. You can force-kill them with "taskkill /f /im cmd.exe" ; I didn't want to put that into the code as it kills _all_ cmd instances regardless of what conjured them up so it would kill any instances you're otherwise running.
- Pressing Get From Web either in Edit mode or on the map will always set the affected file to write-queue even if the values don't actually change.
- Preview images don't respect orientation.
- If user zooms "too far out" on Map they will get odd longitude values. The code handles this internally but map feedback is what it is.

## When reporting bugs please specify

- the OS's version
- the OS's language
- full path of your photo if the bug is related to exif tagging
- possibly upload the picture somewhere if not sensitive so i can test


## System Requirements

- Windows 7+ is needed. The WebView2 installer runs along Setup. Should you need separately, it's available [here](https://go.microsoft.com/fwlink/p/?LinkId=2124703) - the installer should take care of it tho'.
- sqlite is running x86 but fwiw the app isn't really memory-hungry so it will do. Chances are if you're still on a 1st-gen Intel Core you're not on Windows 7. Hopefully.
- You'll need an ArcGIS API key to use the map search functionality. Register for free [here](https://developers.arcgis.com/)
- You'll need a geoNames username and password to use toponomy search. Register for free [here](https://www.geonames.org/)