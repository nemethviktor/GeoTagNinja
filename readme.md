# Welcome to GeoTagNinja

GeoTagNinja is an open-source geotagger for Windows. It's a lightweight an open-source (written from scratch) reminiscent of GeoSetter.
GTN uses [exifTool](https://exifTool.org/) to read and write EXIF info. This also sets the limitations and capabilities. If exifTool can read/write a file, chances are so can GTN.
There is a "short" (15 mins) capabilities demo on [Youtube](https://youtu.be/ulP1ZG7mH-I) if you feel like watching it. It's from the original (Aug 2022) release but it still covers the main feaures more or less. It's only gotten better since...

## Download & Install (Windows 7+ only)

- Download the .msi file from [Releases](https://github.com/nemethviktor/GeoTagNinja/releases) - Find the newest release (topmost, easy) then click on Assets if they are not showing. 
	- There "tends to be" a dev(elopment) version stored [on my public Google Drive](https://drive.google.com/file/d/18iI77SIdrIv-joOtyT0-MzqOVtB5OgM0/view?usp=share_link). This is not really a maintained location and by definition the version is likely to be messier than the published ones. It may even happen that this version is actually older than the published one (I don't always upload here but only when someone opens a ticket and I'd like them to test a change)
- As of 20221202 I've removed the built-in webView2 installer because it was more of a pain in the backside than benefit. If the app breaks complaining about the lack of webView2, it's available [here](https://go.microsoft.com/fwlink/p/?LinkId=2124703)
- The app is unsigned. This is because a certificate costs in the vicinity of £250 per year but the app is free and I don't particular feel like splurging out on this at the moment. 
	- Due to the lack of a signed certificate, when installing SmartScreen will complain that the app is unsafe. SmartScreen is meant to be called StupidScreen but MS made a typo there. 
	- Just run the installer. As the code is public and people may compile on their own, everyone is welcome to ascertain the app is safe should they feel like.

## Things to Note, Usage

- Build 8361 [20221122]+: There is now a hold when the user enters a folder - it is kept on until the folder completes load. This is a bit annoying perhaps but is needed because otherwise people can start issuing write-commands before the read-process finishes and that can result in files being written the wrong info.
- This is mentioned in Known Issues briefly but just to reiterate: if you have issues with file data not showing please try renaming/moving your files to a "simple" folder like "C:\temp" and make sure your file names don't contain umlauts, special characters and anything "odd". This is a limitation of exifTool.
- GPX/Track File Import is very experimental atm. Do report bugs please. This functionality is based on [this](https://exiftool.org/geotag.html) exifTool feature so please read as to what it can and can't do (mostly re: file types etc.).
- Without being too technical, there are two main ways of handling RAW images via C# (the language GTN is written in)
	- There are native libraries such as DCRAW/libRAW. The advantage of that is there are native libraries and they are very fast but come with limited file support, e.g. at least as of the time of the initial GTN version's date (summer 2022) CR3s aren't/weren't supported and neither are some others. 
	- Alternatively there is exifTool, which is what GTN uses for everything. ExifTool supports a lot more extensions but it has to be called externally each time the user interacts with a file or folder. The most visible disadvantage of this is that it takes a second (or two or three or five) to load up exifTool and the app may appear non-responsive when entering a folder.
- I have tried to test reasonably thoroughly but bugs probably remain. Do make backups of your files if you feel that's the right thing to do.
- As per usual I don't accept any liability for damage and/or any other inconvenience you may suffer while/from using this app, no warranties and/or guarantees. The script is open source and everyone is welcome to read it. 
- I'm kinda hoping this won't come up a problem but don't bash me about country names please. I'm thinking disputed country names and areas here. They are ISO standards, the API returns those values, that's it. Being from the UK I do think that the long name of "United Kingdom of Great Britain and Northern Ireland (the)" is a bit lengthy but it is what it is (and it's relatively rarely disputed but for example Crimea does return Ukraine as a value, regardless of what's been going on there since 2014; again, this isn't a statement either way, it's the API reply). 
	- On this particular note above --> the script won't change existing country details in files unless you explicitly edit them. So it may happen that your existing file has the CountryCode GBR and the Country "United Kingdom", that won't be changed unless you do a "Get From Web" because the CountryCode-to-Country matching only runs in that case. That's a feature, not a bug.
- Altitude: in some cases the API returns 32K or negative 32K as an altitude. Those are being blanked out.
- Pressing Get From Web in Edit mode will always query the lat/long data for the file as in the main listview (the main page, in less geeky terms.). This is a feature. The assumption is that you aren't going to change coordinates manually by typing stuff in and _then_ query data. It's a lot more likely you'll change stuff using the map.
- Time Zones (in the Edit File) are left as blank on open regardless of what the stored value is. There is no Exif Tag for TZ but only "Offset", which is something like "+01:00". As there is no indication for neither TZ nor DST per se I can't ascertain that  "+01:00" was in fact say BST rather than CET, one being DST the other not. Either adjust manually or pull from web - the combination of coordinates + createDate would decisively inform the program of the real TZ value.
- See my comment above re: the app being unsigned and SmartScreen getting derpy once in a while. On top of that...
	- If you have Avast running or some other nightmare that tries to capture iframes within apps the app will most likely crash sooner rather than later, at least on the first run. I *think* it should be okay afterwards.
	- I've seen once (and only once) ESET being silly about the app. Tbh no idea as to why. While I'd say the source code is open for public viewing and building it is probably clearer to just say: the app isn't tracking or recording your data and isn't doing anything that's not strictly related to its function. If I ever were to include any tracking (no such plans for the forseeable future), it'd be entirely anonomymised anyway.

## Building & Testing

If you want to build the project probably use Visual Studio - I used v2022 Community. Instead of downloading the source code as zip please pull from Git, you can do that via VS if you want. 
There are 2 parts to the project. One is the "main" the other is the installer. You'll generally have problems w/ the installer bcs it hasn't been pushed to git so it's going to be missing that half.
For the "main" project you should be okay without anything separate. It has worked ok for me on a blank VM when pulled from Git. Just build and F5/run.

There is currently no preset release cycle. I don't expect one to happen in a systematic way. There is a `development` branch as mentioned above for people that like to live dangerously.

## Pull Requests

I'm generally happy for anyone competent to add pull requests but I don't always sync the commits as they come with GitHub until there is an actual release - it's therefore possible that my local commits are lightyears away from the public ones. If you'd like to do a pull request, drop a message/open a ticket first please and we can sync the details.

## ToDos

- [later] Destination stuff untested / not showing cones on map / generally likely not working.

## Known Issues

- There is a likelyhood that the app will struggle to read file data if your files are kept in folders with accent marks (umlauts, non-standard English characters) in the path and/or filename. This is a limitation of exifTool + cmd. If you encounter a problem, move your files to something like "C:\temp" and see if it works better.
	- On top of that above exifTool has a hard limit of 260 characters for file names including paths. Anything beyond that will fail. Again, rename your files or temporarily move them to C:\temp if this is an issue. Unicode (e.g. most Chinese and other Asian) characters have an up to 4-byte size per character. This is to say you'll run into errors with these files more often than not.
	- Alternatively you can enable [this](https://stackoverflow.com/questions/56419639/what-does-beta-use-unicode-utf-8-for-worldwide-language-support-actually-do) feature if you're running v1903 Windows 10 or later but it may have some unwanted effects so keep an eye out for changes caused by it. 
- The API has its rather odd ways of dealing with the "City" tag [not the least because there isn't such a tag in the API].
	- For a detailed rundown on the above see the code if interested + an overall discussion [here](https://github.com/nemethviktor/GeoTagNinja/issues/38)
	- As of 20230105 there is now a "Custom Rules" section in `Settings` where users can amend their own rules and [there is](https://github.com/nemethviktor/GeoTagNinja/wiki/Settings-&-Custom-Rules) a wiki on the overall rundown of how this works.
- Pressing Get From Web either in Edit mode or on the map will always set the affected file to write-queue even if the values don't actually change.
- If user zooms "too far out" on Map they will get odd longitude values. The code handles this internally but map feedback is what it is.
- I didn't really manage to test this but for Nikon D5 the camera outputs NEF files with a built-in "Rating=0" tag. This becomes an issue in Adobe Bridge if an XMP is created and then the NEF file is subsequently ownerwritten by GTN because Bridge would ignore the Rating value in the XMP file going forward. For this reason Rating is always sent back to the RAW files if they are saved. I don't think this would be a problem but more of a heads-up that there are some oddities like this. Btw this force-save-Rating isn't limited to Nikon camera saves in GTN.
- If you change Time Offset (time zone) _and nothing else at all_ then you'll get a warning that "nothing has changed" in the xmp sidecar file. This is not a bug. There is no XMP tag for OffsetTime.

## Possible Issues & Solutions

- You get an "unauthorized" error when pulling data from the GeoNames API:
	- I've been told that by default the API usage is disabled for (some?) new accounts. In that case you have to activate the free web service ---> After registering and clicking the link in the activation email, you still need to activate your account for using web services. To do so, go to your "Manage Account" page [ [http://www.geonames.org/manageaccount](http://www.geonames.org/manageaccount) ] make sure your email shows up correctly. From here, click the "Free Webservice" activation link.
	- Make sure you have provided a valid username and password for GeoNames in the `Settings`. For username _do not_ use your email address but just the username itself. If you think you've done everything correctly, do the following:
		- If you need screenshots for the below visit [this](https://github.com/nemethviktor/GeoTagNinja/issues/13#issuecomment-1305805110) ticket reply.
		- Download & install the newest sqlitebrowse from [here](https://download.sqlitebrowser.org/) (any version will do)
		- Open sqlitebrowser and then in it open the file C:\Users\YOURUSERNAME\AppData\Roaming\GeoTagNinja\database.sqlite
		- Go to Browse Data
		- Pick the Settings table
		- In the filter for the SettingID type in "geo" (or just find the value on the screenshot)
		- Check that the username and password is what you'd expect to find (check for extraneous spaces pls). If it isn't, open a ticket.
	- If everything looks as expected then try the following:
		- In your browser go to http://api.geonames.org/findNearbyPlaceNameJSON?formatted=true&lat=47.3&lng=9&username=YOURUSERNAME&password=YOURPASSWORD (replace the username and pwd as required)
		- You should get a JSON formatted API reply back that visibly looks like a description of a place (likely called Churzegg or some such). If you don't know what that is then have a look [here](https://github.com/nemethviktor/GeoTagNinja/issues/13#issuecomment-1305859987)
		- If you instead get a reply that it's an invalid user or user account not enabled to use the free webservice then do as instructed/logical.

## When reporting bugs please specify

- The OS's version,
- The OS's language (incl region such as English/UK or English/USA),
- Full path of your photo if the bug is related to reading or writing data,
- Possibly upload the picture somewhere if not sensitive so I can test.

## System Requirements

- Windows 7+ is needed. 
- SQLite is running x86 but fwiw the app isn't really memory-hungry so it will do. Chances are if you're still on a 1st-gen Intel Core you're not on Windows 7. Hopefully.
- You'll need an ArcGIS API key to use the map search functionality. Register for free [here](https://developers.arcgis.com/)
- You'll need a geoNames username and password to use toponomy search. Register for free [here](https://www.geonames.org/)
- WebView2 is required but should come with your OS most likely. If not, get it from [here](https://go.microsoft.com/fwlink/p/?LinkId=2124703)