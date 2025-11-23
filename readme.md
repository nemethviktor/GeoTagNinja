[![Translation status](https://hosted.weblate.org/widget/geotagninja/svg-badge.svg)](https://hosted.weblate.org/engage/geotagninja/)

# Welcome to GeoTagNinja

GeoTagNinja is an open-source photo geotagger GUI for Windows. GTN uses [ExifTool](https://ExifTool.org/) to read and write EXIF info. This also sets the limitations and capabilities. If ExifTool can read/write a file, chances are so can GTN.

There is a "short" (15 mins) demo on [YouTube](https://youtu.be/ulP1ZG7mH-I) if you feel like watching it. It's from the original (Aug 2022) release but it still covers the main features more or less. It's only gotten better since...

## Download & Install (Windows 7+ x64 only)

- Download the .msi file from [Releases (latest)](https://github.com/nemethviktor/GeoTagNinja/releases/latest) - Click on Assets if they are not showing. 
- As of `Build 8371 [20221202]` I've removed the built-in webView2 installer because it was more of a pain in the backside than benefit. If the app breaks complaining about the lack of webView2, it's available [here](https://go.microsoft.com/fwlink/p/?LinkId=2124703)
- The app is unsigned. This is because a certificate costs in the vicinity of £250 per year but the app is free, and I don't particularly feel like splurging out on this at the moment.
	- Due to the lack of a signed certificate, when installing SmartScreen will complain that the app is unsafe. SmartScreen is meant to be called StupidScreen but MS made a typo there.
	- Just run the installer. As the code is public and people may compile on their own, everyone is welcome to ascertain the app is safe should they feel like.

## Features at-a-glance

- Tag the location coordinates and City/State/etc details of multiple photos with the help of a map
- Save and retrieve favourite locations
- Import Track (GPX etc) files and associate them with the photos
- Export coordinate tracks of photos to GPX
- Ability to define custom rules/logic wrt naming of places at country level

## Parameters

The app works without parameters, however you may add:
- `-f` or `--folder` (e.g. `geotagninja.exe -f "g:\My Drive\DigiPics_4K_JPG"`) will launch gtn into a specified folder assuming it exists. Trailing backslash char will be auto-removed.
- `-c` or `--collection` will launch collectionMode; read about this further down. 

`-f` and `-c` are mutually exclusive.

## Quirks and Things to Note

- `Build 8361 [20221122]`+: There is now a hold when the user enters a folder - it is kept on until the folder completes load. This is a bit annoying perhaps but is needed because otherwise people can start issuing write-commands before the read-process finishes and that can result in files being written the wrong info.
- This is mentioned in Known Issues briefly but just to reiterate: if you have issues with file data not showing please try renaming/moving your files to a "simple" folder like `C:\temp` and make sure your file names don't contain umlauts, special characters and anything "odd". This is a limitation of ExifTool.
- I have tried to test reasonably thoroughly but bugs probably remain. Do make backups of your files if you feel that's the right thing to do.
- As per usual I don't accept any liability for damage and/or any other inconvenience you may suffer while/from using this app, no warranties and/or guarantees. The script is open source, and everyone is welcome to read it.
- I'm kind of hoping this won't come up a problem but don't bash me about country names please. I'm thinking disputed country names and areas here. They are ISO standards, the API returns those values, that's it. Being from the UK I do think that the long name of `United Kingdom of Great Britain and Northern Ireland (the)` is a bit of a mouthful but it is what it is (and it's relatively rarely disputed but for example Crimea does return `Ukraine` as a value, regardless of what's been going on there since 2014; again, this isn't a statement either way, it's the API reply).
	- On this particular note above --> the script won't change existing country details in files unless you explicitly edit them. So, it may happen that your existing file has the `CountryCode` GBR and the `Country` "United Kingdom", that won't be changed unless you do a `Get From Web` because the CountryCode-to-Country matching only runs in that case. That's a feature, not a bug.
- Altitude: in some cases, the API returns `32K` or `-32K` as an altitude. Those are being blanked out.
- Pressing `Get From Web` in Edit mode will always query the lat/long data for the file as in the main listview (the main page, in less geeky terms.). This is a feature. The assumption is that you aren't going to change coordinates manually by typing stuff in and _then_ query data. It's a lot more likely you'll change stuff using the map.
- **Time Zones** (in the `Edit File Form`) are left as blank on open regardless of what the stored value is. There is no Exif Tag for TZ but only `Offset`, which is something like `+01:00`. As there is no indication for neither TZ nor DST per se I can't ascertain that `+01:00` was in fact say BST rather than CET, one being DST the other not. Either adjust manually or pull from web - the combination of coordinates + `createDate` would decisively inform the program of the real TZ value.
- See my comment above re: the app being unsigned and SmartScreen getting derpy once in a while. On top of that...
	- If you have Avast running or some other nightmare that tries to capture iframes within apps the app will most likely crash sooner rather than later, at least on the first run. I *think* it should be okay afterwards.
	- I've seen once (and only once) ESET being silly about the app. Tbh no idea as to why. While I'd say the source code is open for public viewing and building it is probably clearer to just say: the app isn't tracking or recording your data and isn't doing anything that's not strictly related to its function. If I ever were to include any tracking (no such plans for the foreseeable future), it'd be entirely anonymised anyway.
- `Build 8646 [20230903]`+: I've finally gotten around to displaying the `GPSImgDirection` on the map. The logic is, as not to clutter the map the direction "line" will only show when only one image (with GPSImgDirection) is selected; if more than one such image is selected then the lines don't show.
	- The way this is calculated requires a "distance" so that the target coordinate pair ("the image direction") can be calculated. This is defaulted to 10km for the line and 1km for the triangle and is unlikely to change at this point.
- `Build 8651 [20230908]`+: The other thing I've gotten around to was adding some method of displaying the `Destination` in images. WebView2 seems to be buggy about this and see below for details - also suggestions on fix welcome.
- `Build 8658 [20230915]`+: There is now some support for `Dark Mode`. This is a little "unbeautiful", however there is no proper API support in .NET Framework 4.8.x for "real" (read: Windows 11 style) Dark Mode.
	- Some Borders and Scrollbars don't draw proper "dark" lines/brushes - so there's a discrepancy around the edges. From what I gather as an outcome of the above (lack of support).
	- Point being Dark Mode is an option so that the app doesn't burn your eyes if you are the night owl type, rather than to be pretty, apologies.
- `Build 8829 [20240304]`+: Added a feature to Paste coordinate pairs into the GPSData (Lat/Long) of the Edit File. 
	- The logic is that a Clipboard of _only_ a pair of coordinates separated by (preferably) the Culture-invariant [aka comma] ListSeparator would be pasted into the Edit Form when pressing CTRL+V (e.g.: `56.1234, 12.5678`) <-- and nothing else. 
	- I coded this in a relatively foolproof way but try not to outsmart it.
- `Build 8831 [20240306]`+: ExifTool should now auto-update into the Roaming folder upon app shutdown. (that's `c:\Users\username\AppData\Roaming\GeoTagNinja\`)
- `Build 9095 [20241125]`+: There is now an option for `Flat Mode` that can be triggered from within the `File` menu. This will parse and load all subfolders within the currently selected folder. It's a little experimental although should work.
- `Build 9276 [20250526]`+: There is now an option for `Thumbnail View` within `Settings`. It incurs a very heavy performance hit on RAW-type files and so I don't particularly recommend it but it works. Currently there's no option to customise the thumbnail size.

### A Particular Note on Working with Adobe Bridge (ACR) and RAW files > Saving as JPGs or Other Formats.

Basically, there appear to be two schools of thoughts, one propagating that RAW files themselves shouldn't ever be edited, whereas the other is less specific on this matter.
GTN allows to set a "Process the original image" on a per-extension basis. If set so then the updates will go into the RAW files as well as (subject to further settings) an XMP file. When not set then only the XMP file will be edited, leaving the RAW file intact. This latter (do-not-process-the-raw-file) can cause some issues:
- GTN is coded so that data in XMP files take precedence over RAW files when there is conflicting information. The logic is that discrepancies should only happen if the user does not overwrite the RAW file and therefore whatever is in the XMP is created (and has been modified) on purpose and thus is the will of the user.
- When using Bridge (and/or ACR) as a workflow tool, it has its own quirks, one being that GPS coordinates are read from the RAW file, not the XMP file, even if there is a discrepancy. To make matters worse, any updates to the GPS coords within Bridge are saved in the XMP file but upon refresh (F5) the values revert to those in the RAW file. One can say that is a feature of sorts.
	- This becomes a particular problem because if a user has the following workflow: download RAW from Camera > process & change coords in GTN (only write XMP) > save as JPG in Bridge then the JPG files will likely contain the wrong coordinates (those from the RAW file). _There is nothing I can do about this_. FWIW I checked and GeoSetter has the same thing, which is sensible given that this is ultimately an external factor. Amusingly enough a quick googling shows that users have reported this issue as early as 2017 but, while acknowledged, it's been ignored by Adobe, certainly as late as Bridge v2022 (haven't tried newer ones).
	- The only way to get around this is to process the RAW image (edit/overwrite as above) within GTN

### Collection Mode (Hooking up GTN with Jeffrey Frield's LightRoom Classic Plugin "Run Any Command"

As of `Build 8475 [20230316]` onwards we now have a `CollectionMode`. The initial logic/usage is as follows:

1. Download (and install) the plugin from http://regex.info/blog/lightroom-goodies/run-any-command - there are installation notes in Jeffrey's website, if you are having issues with this part, please nudge him, not me.
2. Within LRC under "File | Plug-in Extras | jf Run Any Command | Configure..." -> Configure the plugin as such:
`"c:\Program Files\GeoTagNinja\GeoTagNinja.exe" --collection={MANIFEST}` (where {MANIFEST} is a literal string, i.e. just type that in with the curly brackets).
3. Launch the Custom Command. This should trigger GTN in CollectionMode.
4. Edit and interact with your files as usual. **Don't forget to save!**
5. When all's done, close GTN. At the moment it doesn't support being called twice with an updated file list. That may come later.

Things to note:
- Technically this function also works if you provide a txt file path outside of LRC, such as `"c:\Program Files\GeoTagNinja\GeoTagNinja.exe" --collection=C:\tmp\filelist.txt`
- If you specify a txt file that doesn't exist, or there are no viable files in it then the app will just run normally (in "normal mode" that is.)
- Lines in the txt file shouldn't have trailing spaces.
- When you're in `CollectionMode` it's generally possible to do anything, the app otherwise does apart from changing folders.

## Building & Testing

If you want to build the project, probably use Visual Studio - I used v2022 Community. Instead of downloading the source code as zip please pull from Git, you can do that via VS if you want.
There are 2 parts to the project. One is the "main" the other is the installer. You'll generally have problems w/ the installer bcs it hasn't been pushed to git so it's going to be missing that half.
For the "main" project you should be okay without anything separate. It has worked ok for me on a blank VM when pulled from Git. Just build and F5/run.

There is currently no preset release cycle. I don't expect one to happen in a systematic way.

## Translations, Contributions & Pull Requests

The project is now available for translation via [Weblate](https://hosted.weblate.org/engage/geotagninja/) - any translations and new additions (new languages) will sync to the project and be included in future builds.

As for code updates, I'm generally happy for anyone competent to add pull requests but I don't always sync the commits as they come with GitHub until there is an actual release - it's therefore possible that my local commits are lightyears away from the public ones. If you'd like to do a pull request, drop a message/open a ticket first please and we can sync the details.

### Translation Progress
[![Translation status](https://hosted.weblate.org/widget/geotagninja/multi-auto.svg)](https://hosted.weblate.org/engage/geotagninja/)

## Known Issues

- There is a likelihood that the app will struggle to read file data if your files are kept in folders with accent marks (umlauts, non-standard English, Asian, Russian, Unicode, etc. characters) in the path and/or filename. This is a limitation of ExifTool + cmd. If you encounter a problem, move your files to something like `C:\temp` and see if it works better.
	- On top of that above ExifTool has a hard limit of 260 characters for file names including paths. Anything beyond that will fail. Again, rename your files or temporarily move them to `C:\temp` if this is an issue. Unicode (e.g. most Chinese and other Asian) characters have an up to 4-byte size per character. This is to say you'll run into errors with these files more often than not.
	- Alternatively, you can enable [this](https://stackoverflow.com/questions/56419639/what-does-beta-use-unicode-utf-8-for-worldwide-language-support-actually-do) feature if you're running v1903 Windows 10 or later but it may have some unwanted effects so keep an eye out for changes caused by it.
- The API has its rather odd ways of dealing with the "City" tag [not the least because there isn't such a tag in the API].
	- For a detailed rundown on the above see the code if interested + an overall discussion [here](https://github.com/nemethviktor/GeoTagNinja/issues/38)
	- As of 20230105 there is now a "Custom Rules" section in `Settings` where users can amend their own rules and [there is](https://github.com/nemethviktor/GeoTagNinja/wiki/Settings-&-Custom-Rules) a wiki on the overall workings of this.
- Pressing Get From Web either in Edit mode or on the map will always set the affected file to write-queue even if the values don't actually change.
- If user zooms "too far out" on Map they will get odd longitude values. The code handles this internally but map feedback is what it is.
- I didn't really manage to test this but for Nikon D5 the camera outputs NEF files with a built-in "Rating=0" tag. This becomes an issue in Adobe Bridge if an XMP is created and then the NEF file is subsequently overwritten by GTN because Bridge would ignore the Rating value in the XMP file going forward. For this reason, Rating is always sent back to the RAW files if they are saved. I don't think this would be a problem but more of a heads-up that there are some oddities like this. Btw this force-save-Rating isn't limited to Nikon camera saves in GTN.
- If you change Time Offset (time zone) _and nothing else at all_ then you'll get a warning that "nothing has changed" in the xmp sidecar file. This is not a bug. There is no XMP tag for OffsetTime.
- Destinations: See below.
- IPTC Keywords & XMP Subjects: when using Adobe Bridge or certain other tools, keywords get added by that software. This can be anything whatsoever (e.g. `mum` or `I love you` or `Stratford`) either defined by the user or the software. The problem is that there most often is no indicator as to what a keyword is, e.g. no `City=` or some such -> this makes it extremely difficult (read: impossible) to sync data with keywords. See: `[XMP] Subject: England, GBR, geo:lat=51.54131500, geo:lon=-0.01036167, geotagged, Stratford, Stratford and New Town Ward, United Kingdom`
	- At the moment there's some groundwork-code in the codebase to enable some future interaction with keywords but I haven't quite worked out an efficient way around this. You'll notice that the two `geo:` are easy to capture and edit but the rest are just undefined character strings. A `remove all geodata` command therefore wouldn’t be able to identify whether say `Fxxking` is a verb or a [town in Austria](https://en.wikipedia.org/wiki/Fugging,_Upper_Austria) or perhaps `Bugyi` is a character string that refers to the Hungarian town called Bugyi, or the otherwise equivalent noun that translates as `panties`. If you think I'm entirely crazy, then read the [Wikipedia article](https://en.wikipedia.org/wiki/Bugyi) regarding that settlement.
	- This means that if your file has Keywords/Subjects and you edit the geo-data the keywords will become out of sync with the changes.
	- What's therefore likely to happen is that I'll attempt to replace existing `geo:` keywords with up-to-date values as required and ignore the rest.

### Destinations/Possible Bug in WebView2

TL; DR: the arrows are missing.
Longer: Hypothetically the idea with Destinations is that if there are groups of images that have GPSDestLat/Long defined then the app draws a path on the map for each of these. Assume the following:
- You have N groups of images (N>0) where GPSDestLat/Long is defined and is the same within each group
- Each group has C (-> C>1) count of images where the GPSLat/Long is different. Basically, you have a bunch of photos from a path walked/driven/etc, and you want to map them.
- The script parses these N groups, separates them and puts them independently on the map _with a bunch of arrows_.
	- When viewing the HTML file out of GTN and open in Edge or Chrome there are the appropriate number of grouped paths and arrows show between the individual images, aka it works as expected.
	- When viewing the same thing within GTN the arrows are missing. Upon inspection it is found that `Uncaught TypeError: Cannot read properties of undefined (reading 'arrowHead') ...` - so basically the arrowHead of the polylineDecorator breaks the WebView2 JS engine, something that isn't a problem in "real" Chrome or Edge.
- For those better versed in JS I've put a try/catch block around this, but I still think there should be some way around the issue so any suggestions here pls shout.

## Possible Issues & Solutions

- *Read/Save file process fails semi-randomly*: (yes this is a copy-paste from above but it seems not everyone's reading the _known issues_ section)
	- If your files are kept in folders with accent marks (umlauts, non-standard English, Asian, Russian, Unicode, etc. characters) in the path and/or filename. This is a limitation of ExifTool + cmd. If you encounter a problem, move your files to something like `C:\temp` and see if it works better.
	- On top of that above ExifTool has a hard limit of 260 characters for file names including paths. Anything beyond that will fail. Again, rename your files or temporarily move them to `C:\temp` if this is an issue. Unicode (e.g. most Chinese and other Asian) characters have an up to 4-byte size per character. This is to say you'll run into errors with these files more often than not.
	- Alternatively, you can enable [this](https://stackoverflow.com/questions/56419639/what-does-beta-use-unicode-utf-8-for-worldwide-language-support-actually-do) feature if you're running v1903 Windows 10 or later but it may have some unwanted effects so keep an eye out for changes caused by it.
- *OSM Street Map tiles say something like "Unauthorized"* (ie the actual map isn't showing properly but gives an ugly error): 
    - This is usually down to an attribution issue but GTN isn't directly using that services but instead via `Leaflet` so it's not something I can do much about
	- Change the map layer to something else, the issue should be transient hopefully - there is at least one other street-type maps that should work (in theory...)
- *You get an "unauthorized" error when pulling data from the GeoNames API*:
	- I've been told that by default the API usage is disabled for (some?) new accounts. In that case you have to activate the free web service ---> After registering and clicking the link in the activation email, you still need to activate your account for using web services. To do so, go to your "Manage Account" page [ [http://www.geonames.org/manageaccount](http://www.geonames.org/manageaccount) ] make sure your email shows up correctly. From here, click the "Free Webservice" activation link.
	- Make sure you have provided a valid username and password for GeoNames in the `Settings`. For username _do not_ use your email address but just the username itself. If you think you've done everything correctly, do the following:
		- If you need screenshots for the below visit [this](https://github.com/nemethviktor/GeoTagNinja/issues/13#issuecomment-1305805110) ticket reply.
		- Download & install the newest sqlitebrowser from [here](https://download.sqlitebrowser.org/) (any version will do)
		- Open sqlitebrowser and then in it open the file `C:\Users\YOURUSERNAME\AppData\Roaming\GeoTagNinja\database.sqlite`
		- Go to Browse Data
		- Pick the Settings table
		- In the filter for the SettingID type in "geo" (or just find the value on the screenshot)
		- Check that the username and password is what you'd expect to find (check for extraneous spaces pls). If it isn't, open a ticket.
	- If everything looks as expected then try the following:
		- In your browser go to http://api.geonames.org/findNearbyPlaceNameJSON?formatted=true&lat=47.3&lng=9&username=YOURUSERNAME&password=YOURPASSWORD (replace the username and pwd as required)
		- You should get a JSON formatted API reply back that visibly looks like a description of a place (likely called Churzegg or some such). If you don't know what that is then have a look [here](https://github.com/nemethviktor/GeoTagNinja/issues/13#issuecomment-1305859987)
		- If you instead get a reply that it's an invalid user or user account not enabled to use the free webservice then do as instructed/logical.

## ToDos

- IPTC Keywords & XMP Subjects alignment

## Roadmap

~~I'm hoping to eventually move away from WinForms to something visually more pleasing. Currently there's no Visual Designer for WinUI3~~ Well the original plan was to eventually move to something nicer (like WinUI3) but Microsoft keeps killing off their newer platforms and I don't want to learn an entirely different language just for the sake of this so WinForms it will be till the time protons decay. However...
The current .NET Framework 4.8 is a little obsolete. I haven't switched to 4.8.1 because that doesn't work on Win 7 and also offers nothing that's relevant for this app, but it's likely that eventually the codebase will move to some more current version of .NET.

## When reporting bugs please specify

- The OS's version,
- The OS's language (incl region such as English/UK or English/USA),
- Full path of your photo if the bug is related to reading or writing data,
- Possibly upload the picture somewhere if not sensitive so I can test.

## System Requirements

- Windows 7+ x64 is needed. As I mentioned in the Roadmap it's likely that support for pre-Win10 OSs will eventually be dropped.
- You'll need an ArcGIS API key to use the map search functionality. 
	- ArcGIS have changed their registration process as of June 2024. Register [here](https://location.arcgis.com/sign-up/)
	- You now need to specify a portal url. Just input anything you want.
	- Scroll/Go to `Developer credential creator`
	- Then `New Item`
	- Then `Developer Credentials`
	- Pick `API Key` (not OAUth)
	- Set your expiration date to as far into the future as you can (1 year)
	- Leave `Referrers` blank
	- On the next page pick `Geocoding (not stored)`
	- On the next page skip `Item Access`
	- On the next page add any title
	- Eventually click `Generate the API key and go to item details page. I am ready to copy and save the key.`
	- Save the API key when prompted. If you don't do it at the time you'll have to generate another one.
- You'll need a geoNames username and password to use toponomy search. Register for free [here](https://www.geonames.org/)
    - Copypaste from above: I've been told that by default the geoNames API usage is disabled for (some?) new accounts. In that case you have to activate the free web service ---> After registering and clicking the link in the activation email, you still need to activate your account for using web services. To do so, go to your "Manage Account" page [ [http://www.geonames.org/manageaccount](http://www.geonames.org/manageaccount) ] make sure your email shows up correctly. From here, click the "Free Webservice" activation link.
- WebView2 is required but should come with your OS most likely. If not, get it from [here](https://go.microsoft.com/fwlink/p/?LinkId=2124703)