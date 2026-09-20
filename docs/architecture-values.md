# How a value travels through GeoTagNinja

This note describes the path a single piece of metadata takes from the file on disk to the screen and
back again, and the rules that keep it intact on the way. It exists because the single most common
class of bug in this application has been a value that was correct in memory but wrong once it had
been turned into text - usually a date, usually only on a non-English machine.

## The pipeline

```
   file on disk                              track file (.gpx)
        |  exiftool -args                         |  exiftool -geotag
        |  (ExifTool.GetProperties)               v
        |                                    sidecar .xmp -> DataTable
        |                                         |  ExifTagSetParser.TagsFromExifToolDataTable
        v                                         v
   raw tag strings          e.g. "EXIF:DateTimeOriginal" -> "2018-06-22 19:32:53"
        |  ExifTagSetParser.ParseTagSet
        |    -> TagsToModelValueTransformations.TransformTagValue (per-attribute clean-up)
        |    -> AttributeValueFormatter.TryParse
        v
   typed value in memory    DateTime / double / int / string, held per AttributeVersion
        |  AttributeValueFormatter.Format(value, context)
        v
   text at the edges        list view cell / text box / clipboard / ExifTool argument file
```

Both sources converge before the transformations, not after: whether a coordinate came out of a photo or out
of a GPX track, exactly one piece of code decides what it means.

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

## Altitude is held in display units, never stored in them

`GPSAltitude` is the one attribute whose in-memory value is not simply what the file said. The EXIF tag
is defined in metres and only metres, so the file is always metres - but the model holds whatever unit
the user asked to see. There are exactly two places that know this, and they are mirror images:

- **Read** - `TagsToModelValueTransformations.T2M_Altitude` multiplies by `MetreToFeet` when
  `UserSettingUseImperial` is set.
- **Write** - `WriteSaveToFile` divides by `MetreToFeet` under the same condition, immediately before
  the ExifTool argument is built.

Nothing converts at display time; the list view and the altitude box render the model value as-is, and
`lbl_Feet_Abbr` is only the unit label printed next to it. So an altitude that enters the model already
in metres while imperial is selected is wrong twice over: shown as metres under a "ft" label, then
divided by 3.28084 on the way back out, writing roughly a third of the real altitude into a tag that is
still declared in metres.

That is precisely what the track-file path used to do, which is what the merge below fixed.

## Known rough edges

These are pre-existing and deliberately left alone so far, recorded here so they are not mistaken for
intended design:

- **The blank sentinel for strings is `"-"`**, not the empty string (`FrmMainApp.NullStringEquivalentGeneric`).
  That is a *display* sentinel being used as a *data* value, so a string attribute that is blanked but
  not flagged for deletion can reach the write path as a literal `-`.
- **`SetAttributeValue` treats a blank differently on create and on update.** Creating an attribute
  with `""` stores `""`; updating an existing one to `""` stores the sentinel. Behaviour was preserved
  rather than unified, because unifying it changes what gets written to files.
- **`GPSHPositioningError` is declared as text** while `GPSDOP`, which it is derived from, is a double.
  So the one value on that screen that is arithmetic on another value is the one held as a string.
- **The GeoNames altitude does not follow the unit convention above.** `ReadExifData` takes the API's
  `srtm3` value, which is metres, and puts it into the model unconverted. With imperial units selected
  that is the same mistake the track path used to make: the write path will still divide it by
  `MetreToFeet`. Untouched here because it is a different pipeline (web lookup, not file read) and
  fixing it belongs with that code.

## The read pipelines were merged

There used to be two. `ReadExifData.ExifGetStandardisedDataPointFromExifAsString` was a string-based
re-implementation of the transformations, used only by the track-file path, and the note here said the two
"must be kept in step by hand". They had not been: the string one read rationals (`43/10`) where the typed
one did not, and skipped the metric-to-imperial altitude conversion the typed one applied, so overlaying a
track onto a photo put metres into a model that was expected to hold feet.

`ExifTagSetParser` is now the single reader. The track-file path flattens its sidecar into the same tag
dictionary a photo produces and runs it through the same
`TagsToModelValueTransformations.TransformTagValue`, which is also the only place that says which clean-up
belongs to which attribute. `ReadPipelineParityTests` asserts the two entry points agree, so the split
cannot quietly reopen.

The merge settled three disagreements in favour of the better-behaved side, which does change behaviour:

| Value | Was | Now |
| --- | --- | --- |
| Altitude from a track | Metres in the model even with imperial selected, so shown under a "ft" label and then divided again on write | Converted on read like any other altitude, so the model is in display units and the tag is written in metres |
| `GPSDOP` from a photo | Rationals unreadable, attribute left unset | `43/10` reads as `4.3`, as it always did for tracks |
| `GPSHPositioningError` from a photo | Only what the file carried | Falls back to `GPSDOP * 3`, as it always did for tracks |
