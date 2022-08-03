# Readme for the ExtraFiles folder

This folder is mostly a repo of files needed to generate various labels in various languages. The XLSM/XLSX files are as follows:

**objectMapping.xlsm**
Excel Macro file; This creates the objectmapping table in SQLite. It's generally bound to the tags the app handles so no need to update it.

**english.xlsx**
Standard Excel file; This is the basis for most of the labels in the app in English and if anyone wants to 'make' a new language, this is the one to copy and modify. At the time of writing this messageboxes are not part of the logic and that's a TODO for later.

**languageCreator.xlsm**
Excel Macro file; used to generate the SQLite table for the given language. It directly inserts into the main table - basically it's an Excel-to-SQLite package.