
# Welcome to GeoTagNinja

GeoTagNinja is an open-source photo geotagger GUI for Windows. GTN uses [exifTool](https://exifTool.org/) to read and write EXIF info. This also sets the limitations and capabilities. If exifTool can read/write a file, chances are so can GTN.
There is a "short" (15 mins) capabilities demo on [Youtube](https://youtu.be/ulP1ZG7mH-I) if you feel like watching it. It's from the original (Aug 2022) release but it still covers the main feaures more or less. It's only gotten better since...

## Download & Install (Windows 7+ only)

- Download the .msi file from [Releases](https://github.com/nemethviktor/GeoTagNinja/releases) - Find the newest release (topmost, easy) then click on Assets if they are not showing. 
	- There "tends to be" a dev(elopment) version stored [on my public Google Drive](https://drive.google.com/file/d/18iI77SIdrIv-joOtyT0-MzqOVtB5OgM0/view?usp=share_link). This is not really a maintained location and by definition the version is likely to be messier than the published ones. It may even happen that this version is actually older than the published one (I don't always upload here but only when someone opens a ticket and I'd like them to test a change)
- As of 20221202 I've removed the built-in webView2 installer because it was more of a pain in the backside than benefit. If the app breaks complaining about the lack of webView2, it's available [here](https://go.microsoft.com/fwlink/p/?LinkId=2124703)
- The app is unsigned. This is because a certificate costs in the vicinity of £250 per year but the app is free and I don't particularly feel like splurging out on this at the moment. 
	- Due to the lack of a signed certificate, when installing SmartScreen will complain that the app is unsafe. SmartScreen is meant to be called StupidScreen but MS made a typo there. 
	- Just run the installer. As the code is public and people may compile on their own, everyone is welcome to ascertain the app is safe should they feel like.

## Things to Note, Usage

- Build 8361 [20221122]+: There is now a hold when the user enters a folder - it is kept on until the folder completes load. This is a bit annoying perhaps but is needed because otherwise people can start issuing write-commands before the read-process finishes and that can result in files being written the wrong info.
- This is mentioned in Known Issues briefly but just to reiterate: if you have issues with file data not showing please try renaming/moving your files to a "simple" folder like "C:\temp" and make sure your file names don't contain umlauts, special characters and anything "odd". This is a limitation of exifTool.
- Without being too technical, there are two main ways of handling RAW images via C# (the language GTN is written in)
	- There are native libraries such as DCRAW/libRAW. The advantage of that is there are native libraries and they are very fast but come with limited file support, e.g. at least as of the time of the initial GTN version's date (summer 2022) CR3s aren't/weren't supported and neither are some others. 
	- Alternatively there is exifTool, which is what GTN uses for everything. ExifTool supports a lot more extensions but it has to be called externally in certain cases. In the more recent version there is a keep-alive exifTool running in the background, so this should be less of an issue.
- I have tried to test reasonably thoroughly but bugs probably remain. Do make backups of your files if you feel that's the right thing to do.
- As per usual I don't accept any liability for damage and/or any other inconvenience you may suffer while/from using this app, no warranties and/or guarantees. The script is open source and everyone is welcome to read it. 
- I'm kinda hoping this won't come up a problem but don't bash me about country names please. I'm thinking disputed country names and areas here. They are ISO standards, the API returns those values, that's it. Being from the UK I do think that the long name of "United Kingdom of Great Britain and Northern Ireland (the)" is a bit lengthy but it is what it is (and it's relatively rarely disputed but for example Crimea does return Ukraine as a value, regardless of what's been going on there since 2014; again, this isn't a statement either way, it's the API reply). 
	- On this particular note above --> the script won't change existing country details in files unless you explicitly edit them. So it may happen that your existing file has the CountryCode GBR and the Country "United Kingdom", that won't be changed unless you do a "Get From Web" because the CountryCode-to-Country matching only runs in that case. That's a feature, not a bug.
- Altitude: in some cases the API returns 32K or negative 32K as an altitude. Those are being blanked out.
- Pressing Get From Web in Edit mode will always query the lat/long data for the file as in the main listview (the main page, in less geeky terms.). This is a feature. The assumption is that you aren't going to change coordinates manually by typing stuff in and _then_ query data. It's a lot more likely you'll change stuff using the map.
- **Time Zones** (in the Edit File) are left as blank on open regardless of what the stored value is. There is no Exif Tag for TZ but only "Offset", which is something like "+01:00". As there is no indication for neither TZ nor DST per se I can't ascertain that  "+01:00" was in fact say BST rather than CET, one being DST the other not. Either adjust manually or pull from web - the combination of coordinates + createDate would decisively inform the program of the real TZ value.
- See my comment above re: the app being unsigned and SmartScreen getting derpy once in a while. On top of that...
	- If you have Avast running or some other nightmare that tries to capture iframes within apps the app will most likely crash sooner rather than later, at least on the first run. I *think* it should be okay afterwards.
	- I've seen once (and only once) ESET being silly about the app. Tbh no idea as to why. While I'd say the source code is open for public viewing and building it is probably clearer to just say: the app isn't tracking or recording your data and isn't doing anything that's not strictly related to its function. If I ever were to include any tracking (no such plans for the forseeable future), it'd be entirely anonomymised anyway.
- Build 8646 [20230903]+: I've finally gotten around to displaying the **GPSImgDirection** on the map. The logic is, as not to clutter the map the direction "line" will only show when only one image (with GPSImgDirection) is selected; if more than one such image is selected then the lines don't show.
	- The way this is calculated requires a "distance" so that the target coordinate pair ("the image direction") can be calculated. This is defaulted to 10km for the line and 1km for the triangle and is unlikely to change at this point.
- Build 8651 [20230908]+: The other thing I've gotten around to was adding some method of displaying the **Destination** in images. WebView2 seems to be buggy about this and see below for details - also suggestions on fix welcome.
- Build 8658 [20230915]+: There is now some support for **Dark Mode**. This is very rudimentary and undoubtedly is not beautiful, however there is no proper API support in .NET Framework 4.8.x for "real" (read: Windows 11 style) Dark Mode and at the moment this might or might not even make it into .NET Framework 5 (see [here](https://github.com/dotnet/winforms/issues/7641) for anyone wanting to dig into the techy parts.)
    - Borders don't draw proper "dark" lines - so there's a discrepancy around the edges. From what I gather as an outcome of the above (lack of support) this is somewhere between a f...kload of work to manually code or just impossible.
	- In particular the Manage Favourites Form in Dark Mode is an eyesore. If someone feels like rewriting the `ChangeTheme` in `HelperControlThemeManager`, please do.
    - Point being Dark Mode is an option so that the app doesn't burn your eyes if you are the night owl type, rather than to be pretty, apologies (unlikely this will change unless Microsoft makes progress on the API or I rewrite the whole app in WPF, which isn't likely at the moment).

### A Particular Note on Working with Adobe Bridge (ACR) and RAW files > Saving as JPGs or Other Formats.

Basically there appear to be two schools of thoughts, one propagating that RAW files themselves shouldn't ever be edited, whereas the other is less specific on this matter. 
GTN allows to set a "Process the original image" on a per-extension basis. If set so then the updates will go into the RAW files as well as (subject to further settings) an XMP file. When not set then only the XMP file will be edited, leaving the RAW file intact. This latter (do-not-process-the-raw-file) can cause some issues:
- Current versions of GTN are coded so that data in XMP files take precedence over RAW files when there is conflicting information. The logic is that discrepancies should only happen if the user does not overwrite the RAW file and therefore whatever is in the XMP is created (and has been modified) on purpose and thus is the will of the user.
- When using Bridge (and/or ACR) as a workflow tool, it has its on quirks, one being that GPS coordinates are read from the RAW file, not the XMP file, even if there is a discrepancy. To make matters worse, any updates to the GPS coords within Bridge are saved in the XMP file but upon refresh (F5) the values revert to those in the RAW file. One can say that is a feature of sorts. 
	- This becomes a particular problem because if a user has the following workflow: download RAW from Camera > process & change coords in GTN (only write XMP) > save as JPG in Bridge then the JPG files will likely contain the wrong coordinates (those from the RAW file). _There is nothing I can do about this_. FWIW I checked and GeoSetter has the same thing, which is sensible given that this is ultimately an external factor. Amusingly enough a quick googling shows that users have reported this issue as early as 2017 but, while acknowledged, it's been ignored by Adobe, certainly as late as Bridge v2022 (haven't tried newer ones).
	- The only way to get around this is to process the RAW image (edit/overwrite as above) within GTN

### Collection Mode (Hooking up GTN with Jeffrey Frield's LightRoom Classic Plugin "Run Any Command"

As of Build 8475 [20230316] onwards we now have a `CollectionMode`. Details of this are TODO for me but the initial logic/usage is as follows:

 1. Download (and install) the plugin from http://regex.info/blog/lightroom-goodies/run-any-command - there are installation notes in Jeffrey's website, if you are having issues with this part, please nudge him, not me.
 2. Within LRC under "File | Plug-in Extras | jf Run Any Command | Configure..." ->  Configure the plugin as such: 
`"c:\Program Files\GeoTagNinja\GeoTagNinja.exe" --collection={MANIFEST}` (where {MANIFEST} is a literal string, ie just type that in with the curly brackets).
 3. Launch the Custom Command. This should trigger GTN in CollectionMode.
 4. Edit and interact with your files as usual. **Don't forget to save!** 
 5. When all's done, close GTN. At the moment it doesn't support being called twice with an updated file list. That may come later.

Things to note:
- Technically this function also works if you provide a txt file path outside of LRC, such as `"c:\Program Files\GeoTagNinja\GeoTagNinja.exe" --collection=C:\tmp\filelist.txt` 
- If you specify a txt file that doesn't exist, or there are no viable files in it then the app will just run normally (in "normal mode" that is.)
- Lines in the txt file shouldn't have trailing spaces.
- When you're in `CollectionMode` it's generally possible to do anything the app otherwise does apart from changing folders. 

## Building & Testing

If you want to build the project probably use Visual Studio - I used v2022 Community. Instead of downloading the source code as zip please pull from Git, you can do that via VS if you want. 
There are 2 parts to the project. One is the "main" the other is the installer. You'll generally have problems w/ the installer bcs it hasn't been pushed to git so it's going to be missing that half.
For the "main" project you should be okay without anything separate. It has worked ok for me on a blank VM when pulled from Git. Just build and F5/run.

There is currently no preset release cycle. I don't expect one to happen in a systematic way. There is a `development` branch as mentioned above for people that like to live dangerously.

## Pull Requests

I'm generally happy for anyone competent to add pull requests but I don't always sync the commits as they come with GitHub until there is an actual release - it's therefore possible that my local commits are lightyears away from the public ones. If you'd like to do a pull request, drop a message/open a ticket first please and we can sync the details.

## ToDos

- N/A

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
- Destinations: See below.

### Desinations/Possible Bug in WebView2

TL;DR: the arrows are missing. 
Longer: Hypothetically the idea with Destinations is that if there are groups of images that have GPSDestLat/Long defined then the app draws a path on the map for each of these. Assume the following:
- You have N groups of images (N>0) where GPSDestLat/Long is defined and is the same within each group
- Each group has C (-> C>1) count of images where the GPSLat/Long is different. Basically you have a bunch of photos from a path walked/driven/etc and you want to map them.
- The script parses these N groups, separates them and puts them independently on the map _with a bunch of arrows_. 
	- When viewing the HTML file out of GTN and open in Edge or Chrome there are the appropriate number of grouped paths and arrows show between the individual images, aka it works as expected.
	- When viewing the same thing within GTN the arrows are missing. Upon inspection it is found that `Uncaught TypeError: Cannot read properties of undefined (reading 'arrowHead') ...` - so basically the arrowHead of the polylineDecorator breaks the WebView2 JS engine, something that isn't a problem in "real" Chrome or Edge. 

- For those better versed in JS I've put a try/catch block around this but I still think there should be some way around the issue so any suggestions here pls shout.

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