# Readme for the ExtraFiles folder

This folder is mostly a repo of files needed to generate various labels in various languages. The XLSM/XLSX files are as follows:

**objectMapping.xlsm**
Excel Macro file; This creates the objectmapping table in SQLite. It's generally bound to the tags the app handles so no need to update it.

**languageCreator.xlsm**
Excel Macro file; used to generate the SQLite table for the given language. It directly inserts into the main table - basically it's an Excel-to-SQLite package. If you want to edit/add a language then add the columns to each sheet after the last one (like as of v0.6 there is English followed by French so just add (say) German and then the translations.  

Eventually the logic might be changed to get C# to read direct from an XLSX file but for now this is easier.