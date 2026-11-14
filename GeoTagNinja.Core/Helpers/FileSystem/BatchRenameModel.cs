using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;

namespace GeoTagNinja.Helpers.FileSystem;

/// <summary>What one piece of the new file name is made of (the entries of Adobe Bridge's "New Filenames" drop-down).</summary>
internal enum BatchRenameKind
{
    /// <summary>Literal text.</summary>
    Text,

    /// <summary>Only changes the case of the extension; the extension itself is always kept.</summary>
    NewExtension,

    /// <summary>The file's current name, without its extension.</summary>
    CurrentFilename,

    /// <summary>The name the file had when it was first renamed (XMP <c>PreservedFileName</c>), else the current name.</summary>
    PreservedFilename,

    /// <summary>1, 2, 3 ... (zero-padded) in the order the files are listed.</summary>
    SequenceNumber,

    /// <summary>a, b, c ... z, aa, ab ... in the order the files are listed.</summary>
    SequenceLetter,

    /// <summary>One of the file's dates, in a format chosen by the user.</summary>
    DateTime,

    /// <summary>The value of one of the file's metadata fields.</summary>
    Metadata,

    /// <summary>The name of the folder the file is in.</summary>
    FolderName,

    /// <summary>The file's current (or preserved) name with a find-and-replace applied.</summary>
    StringSubstitution
}

/// <summary>How the letters of a piece of the name are cased.</summary>
internal enum BatchRenameCase
{
    Original,
    Lower,
    Upper
}

/// <summary>Which date a <see cref="BatchRenameKind.DateTime" /> piece shows.</summary>
internal enum BatchRenameDateSource
{
    DateTaken,
    DateCreated,
    FileCreated,
    FileModified,
    Today,
    Yesterday
}

/// <summary>Which part of a file name a <see cref="BatchRenameKind.CurrentFilename" /> or <c>PreservedFilename</c> piece shows.</summary>
internal enum BatchRenameFilePart
{
    /// <summary>The name without its extension.</summary>
    Name,

    /// <summary>The whole name, with the extension.</summary>
    NameAndExtension,

    /// <summary>Just the extension, without the full stop.</summary>
    Extension,

    /// <summary>The digits the name ends with (<c>0806</c> of <c>DSC0806</c>); nothing if it does not end in digits.</summary>
    NumberSuffix
}

/// <summary>What a <see cref="BatchRenameKind.StringSubstitution" /> piece searches in.</summary>
internal enum BatchRenameSubstituteSource
{
    OriginalFilename,
    PreservedFilename
}

/// <summary>Where the renamed files end up.</summary>
internal enum BatchRenameDestination
{
    /// <summary>Renamed where they are.</summary>
    SameFolder,

    /// <summary>Renamed and moved to another folder.</summary>
    MoveToFolder,

    /// <summary>Copied to another folder under the new name; the originals stay as they are.</summary>
    CopyToFolder
}

/// <summary>One piece of the new name, with the settings that kind of piece needs (the others are ignored).</summary>
internal sealed class BatchRenameComponent
{
    /// <summary>What the piece is made of.</summary>
    public BatchRenameKind Kind { get; set; }

    /// <summary>
    ///     <see cref="BatchRenameKind.Text" />: the text. <see cref="BatchRenameKind.DateTime" />: the format, written
    ///     with <c>YYYY YY MM DD hh mm ss</c> as in Adobe Bridge (e.g. <c>YYYYMMDD</c>).
    ///     <see cref="BatchRenameKind.Metadata" />: the attribute's name.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary><see cref="BatchRenameKind.CurrentFilename" /> and <c>PreservedFilename</c>: which part of the name.</summary>
    public BatchRenameFilePart Part { get; set; }

    /// <summary>The case of the piece (names, letters and the extension).</summary>
    public BatchRenameCase Case { get; set; }

    /// <summary><see cref="BatchRenameKind.SequenceNumber" />: how many digits at least (zero-padded).</summary>
    public int Digits { get; set; } = 3;

    /// <summary><see cref="BatchRenameKind.SequenceNumber" />: the number given to the first file.</summary>
    public int Start { get; set; } = 1;

    /// <summary><see cref="BatchRenameKind.DateTime" />: which date.</summary>
    public BatchRenameDateSource DateSource { get; set; }

    /// <summary><see cref="BatchRenameKind.StringSubstitution" />: what to search in.</summary>
    public BatchRenameSubstituteSource SubstituteSource { get; set; }

    /// <summary><see cref="BatchRenameKind.StringSubstitution" />: what to look for.</summary>
    public string Find { get; set; } = string.Empty;

    /// <summary><see cref="BatchRenameKind.StringSubstitution" />: what to put instead.</summary>
    public string Replace { get; set; } = string.Empty;

    /// <summary><see cref="BatchRenameKind.StringSubstitution" />: upper and lower case match.</summary>
    public bool IgnoreCase { get; set; }

    /// <summary><see cref="BatchRenameKind.StringSubstitution" />: every occurrence, not just the first.</summary>
    public bool ReplaceAll { get; set; } = true;

    /// <summary><see cref="BatchRenameKind.StringSubstitution" />: <see cref="Find" /> is a regular expression.</summary>
    public bool UseRegex { get; set; }
}

/// <summary>Everything the Batch Rename dialog collects: this is what a preset stores.</summary>
internal sealed class BatchRenameSettings
{
    /// <summary>The pieces of the new name, left to right.</summary>
    public List<BatchRenameComponent> Components { get; set; } = [];

    /// <summary>Where the renamed files go.</summary>
    public BatchRenameDestination Destination { get; set; }

    /// <summary>The folder for <see cref="BatchRenameDestination.MoveToFolder" /> and <c>CopyToFolder</c>.</summary>
    public string DestinationFolder { get; set; } = string.Empty;

    /// <summary>Also keep names legal on macOS (no leading full stop).</summary>
    public bool MacCompatible { get; set; }

    /// <summary>Also keep names legal on Unix (no leading full stop).</summary>
    public bool UnixCompatible { get; set; }

    private static readonly JsonSerializerSettings JsonOptions = new()
    {
        Converters = { new StringEnumConverter() },
        Formatting = Formatting.None
    };

    /// <summary>The settings as text, for storing as a preset.</summary>
    internal string ToJson()
    {
        return JsonConvert.SerializeObject(value: this, settings: JsonOptions);
    }

    /// <summary>Reads settings stored by <see cref="ToJson" />; null if the text is not usable.</summary>
    internal static BatchRenameSettings FromJson(string json)
    {
        try
        {
            BatchRenameSettings settings = JsonConvert.DeserializeObject<BatchRenameSettings>(value: json, settings: JsonOptions);
            settings?.Components.RemoveAll(match: component => component == null);
            return settings;
        }
        catch (Exception ex) when (ex is JsonException or ArgumentException)
        {
            return null;
        }
    }
}
