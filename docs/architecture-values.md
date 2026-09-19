# How a value travels through GeoTagNinja

This note describes the path a single piece of metadata takes from the file on disk to the screen and
back again, and the rules that keep it intact on the way. It exists because the single most common
class of bug in this application has been a value that was correct in memory but wrong once it had
been turned into text - usually a date, usually only on a non-English machine.

## The pipeline

```
   file on disk
        |  exiftool -args   (ExifTool.GetProperties)
        v
   raw tag strings          e.g. "EXIF:DateTimeOriginal" -> "2018-06-22 19:32:53"
        |  DirectoryElement.ParseAttributesFromExifToolOutput
        |    -> TagsToModelValueTransformations (per-attribute clean-up)
        |    -> AttributeValueFormatter.TryParse
        v
   typed value in memory    DateTime / double / int / string, held per AttributeVersion
        |  AttributeValueFormatter.Format(value, context)
        v
   text at the edges        list view cell / text box / clipboard / ExifTool argument file
```

The important property is the middle box. Between parsing and writing, a value is **always** the CLR
type that `SourcesAndAttributes.GetElementAttributesType` says it is. It is never a string that
happens to look like a number, and never a string that happens to look like a date.

## Versions

`DirectoryElement` stores up to four versions of every attribute, ordered by authority:

| Version | Meaning |
| --- | --- |
| `Original` | What was read off the file. |
| `Stage1EditFormIntraTabTransferQueue` | An edit in progress inside the edit dialog. |
| `Stage2EditFormReadyToSaveAndMoveToWriteQueue` | An edit the dialog has accepted. |
| `Stage3ReadyToWrite` | An edit queued for writing back to the file. |

"The current value" means "the newest version that has one". That ordering lives in exactly one place,
`DirectoryElement.VersionsNewestFirst`; do not re-encode it anywhere else.

A version can additionally be flagged *marked for deletion*, meaning the tag should be removed from
the file on the next write. Such a value reads back as blank, so the UI reflects the pending removal
straight away.

## The three formatting contexts

Whenever a typed value becomes text, the caller must say why, using `ValueFormatContext`:

| Context | Used for | Rendering |
| --- | --- | --- |
| `Display` | Anything a human reads or types: list view cells, text boxes, labels. | Current culture. |
| `RoundTrip` | Text this application will parse again itself: the copy/paste pool, moving a value between versions, caches, `DataTable` cells. | Invariant. |
| `ExifTool` | Tag values handed to ExifTool as `-Tag=Value`. | Invariant, canonical. |

`AttributeValueFormatter` is the only place these rules are implemented. If a value comes out wrong,
that is the file to open.

### Why this matters

The obvious failure is a date rendered as `22.06.2018 19:32:53` on a German machine, stashed in the
copy/paste pool, and then parsed back on the assumption that it was invariant. The subtler failure is
the reverse: the application used to parse ExifTool output with `DateTime.TryParse` against the
*current* culture. That only worked because the bundled `Resources/.ExifTool_config` rewrites
ExifTool's native `YYYY:MM:DD hh:mm:ss` into `%Y-%m-%d %H:%M:%S`. Whenever that Perl config was not
picked up - or the user's culture used a non-Gregorian calendar, as `ar-SA` does - every timestamp in
the application silently went blank, with no error anywhere.

`AttributeValueFormatter` accepts both layouts explicitly, so correctness no longer depends on a
config file being found.

## Numbers are deliberately not localised

Doubles and ints are rendered invariantly in *all three* contexts, `Display` included. Coordinates,
altitudes and f-numbers are round-tripped through the map's JavaScript bridge, `NumericUpDown`
controls and the SQLite settings store, none of which expect a comma as the decimal separator.

Showing `47,4979` to a Hungarian user instead of `47.4979` is a reasonable thing to want, but it is a
product decision with consequences across the map bridge and every numeric input control, not a
formatting tweak. If it is taken on, `AttributeValueFormatter.FormatDouble` is where it belongs - the
`context` parameter is already threaded through for exactly that purpose.

## Known rough edges

These are pre-existing and deliberately left alone so far, recorded here so they are not mistaken for
intended design:

- **The blank sentinel for strings is `"-"`**, not the empty string (`FrmMainApp.NullStringEquivalentGeneric`).
  That is a *display* sentinel being used as a *data* value, so a string attribute that is blanked but
  not flagged for deletion can reach the write path as a literal `-`.
- **`SetAttributeValue` treats a blank differently on create and on update.** Creating an attribute
  with `""` stores `""`; updating an existing one to `""` stores the sentinel. Behaviour was preserved
  rather than unified, because unifying it changes what gets written to files.
- **Two parallel read pipelines.** `ReadExifData.ExifGetStandardisedDataPointFromExifAsString` is a
  string-based re-implementation of the transformations in `TagsToModelValueTransformations`, used by
  the track-file path. The two must be kept in step by hand until they are merged.
