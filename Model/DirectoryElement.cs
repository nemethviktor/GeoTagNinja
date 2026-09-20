#nullable enable
using GeoTagNinja.Helpers;
using GeoTagNinja.Helpers.Exif;
using GeoTagNinja.View.FileList;
using GeoTagNinja.View.Forms;
using NLog;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Threading.Tasks;
using static GeoTagNinja.Model.SourcesAndAttributes;

#pragma warning disable CS8618, CS9264

namespace GeoTagNinja.Model;

/// <summary>
///     An element in a folder/directory.
/// </summary>
public class DirectoryElement
{
    /// <summary>
    ///     Classification of a directory element in terms of its type.
    /// </summary>
    public enum ElementType
    {
        Drive = 0,
        SubDirectory = 1,
        ParentDirectory = 2,
        File = 3,
        MyComputer = 4,
        Unknown = 99
    }

    public bool IsDirty { get; internal set; }

    // Returns true only if the ExifTool dictionary actually contains data (other than GUID, which is indeed an attribute)
    public bool IsHydrated => _Attributes != null && _Attributes.Count > 1;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="itemNameWithoutPath">The name of the element</param>
    /// <param name="type">The ElementType of it</param>
    /// <param name="fileNameWithPath">The fully qualified path incl. its name</param>
    public DirectoryElement(string itemNameWithoutPath,
                            ElementType type,
                            string fileNameWithPath)
    {
        ItemNameWithoutPath = itemNameWithoutPath;
        Type = type;

        FileNameWithPath = fileNameWithPath;
        Extension = Path.GetExtension(path: FileNameWithPath)
                        .Replace(oldValue: ".", newValue: "");
        _Attributes = new Dictionary<ElementAttribute, AttributeValueContainer>();

        // Assign GUID at birth
        SetAttributeValue(
            attribute: ElementAttribute.GUID,
            value: Guid.NewGuid().ToString(),
            version: AttributeVersion.Original,
            isMarkedForDeletion: false
        );

        Log.Trace($"Created DirectoryElement with GUID: {GetAttributeValueAsString(ElementAttribute.GUID)}");
    }

    #region Attribute Values Support

    /// <summary>
    ///     Possible versions of an element. Initial loads receive the
    ///     original version tag, updates the modified one.
    /// </summary>
    public enum AttributeVersion
    {
        Original,
        Stage1EditFormIntraTabTransferQueue,
        Stage2EditFormReadyToSaveAndMoveToWriteQueue,
        Stage3ReadyToWrite
    }

    /// <summary>
    ///     Non-generic base for <see cref="AttributeValues{T}" />, so that the attribute dictionary can hold
    ///     containers of differing value types without itself being generic.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         Each container holds every <see cref="AttributeVersion" /> of a single attribute of a single file,
    ///         together with the per-version "user asked for this tag to be deleted" flag.
    ///     </para>
    ///     <para>
    ///         This used to be four hand-written classes (string / int / double / DateTime) whose bodies were
    ///         identical apart from the type token, each paired with a <c>if (attributeType == typeof(...))</c> ladder
    ///         at every call site. The generic subclass below replaces all of it; the type ladder now exists exactly
    ///         once, in <see cref="CreateFor" />.
    ///     </para>
    /// </remarks>
    private abstract class AttributeValueContainer
    {
        /// <summary>
        ///     The CLR type of the values held here, matching
        ///     <see cref="SourcesAndAttributes.GetElementAttributesType" /> for the owning attribute.
        /// </summary>
        public abstract Type MyValueType { get; }

        /// <summary>
        ///     The value that stands in for "nothing" for this container's type. Note that for strings this is the
        ///     display sentinel <see cref="FrmMainApp.NullStringEquivalentGeneric" /> rather than an empty string;
        ///     that is long-standing behaviour which the refactor preserves rather than quietly changes.
        /// </summary>
        public abstract IConvertible BlankValue { get; }

        /// <summary>Whether a value has been recorded for the given version.</summary>
        public abstract bool HasVersion(AttributeVersion version);

        /// <summary>Whether the given version is flagged for removal from the file on the next write.</summary>
        public abstract bool IsMarkedForDeletion(AttributeVersion version);

        /// <summary>
        ///     Returns the stored value for the given version, or <see cref="BlankValue" /> if that version is absent.
        /// </summary>
        public abstract IConvertible GetValue(AttributeVersion version);

        /// <summary>Records (or overwrites) the value for the given version.</summary>
        /// <param name="version">The version slot to write into.</param>
        /// <param name="value">
        ///     The value to store. Must already be of <see cref="MyValueType" />; callers are expected to have
        ///     substituted <see cref="BlankValue" /> for anything blank.
        /// </param>
        /// <param name="isMarkedForDeletion">Whether the tag should be removed from the file on the next write.</param>
        public abstract void SetValue(AttributeVersion version,
                                      IConvertible value,
                                      bool isMarkedForDeletion);

        /// <summary>Discards the given version, if present.</summary>
        public abstract void RemoveVersion(AttributeVersion version);

        /// <summary>
        ///     Creates an empty container for the given attribute value type.
        /// </summary>
        /// <param name="attributeType">
        ///     The CLR type the attribute is declared to hold, per
        ///     <see cref="SourcesAndAttributes.GetElementAttributesType" />.
        /// </param>
        /// <exception cref="ArgumentException">Thrown when the type is not one GeoTagNinja knows how to store.</exception>
        public static AttributeValueContainer CreateFor(Type attributeType)
        {
            if (attributeType == typeof(string))
            {
                return new AttributeValues<string>(blankValue: FrmMainApp.NullStringEquivalentGeneric);
            }

            if (attributeType == typeof(int))
            {
                return new AttributeValues<int>(blankValue: FrmMainApp.NullIntEquivalent);
            }

            if (attributeType == typeof(double))
            {
                return new AttributeValues<double>(blankValue: FrmMainApp.NullDoubleEquivalent);
            }

            if (attributeType == typeof(DateTime))
            {
                return new AttributeValues<DateTime>(blankValue: FrmMainApp.NullDateTimeEquivalent);
            }

            throw new ArgumentException(
                message: $"'{attributeType.Name}' is not a supported attribute value type.",
                paramName: nameof(attributeType));
        }
    }

    /// <summary>
    ///     Holds every recorded <see cref="AttributeVersion" /> of one attribute, strongly typed.
    /// </summary>
    /// <typeparam name="T">
    ///     The attribute's value type - one of <see cref="string" />, <see cref="int" />, <see cref="double" /> or
    ///     <see cref="DateTime" />.
    /// </typeparam>
    private sealed class AttributeValues<T> : AttributeValueContainer
        where T : IConvertible
    {
        private readonly T _blankValue;

        private readonly Dictionary<AttributeVersion, (T Value, bool IsMarkedForDeletion)> _versions = [];

        /// <param name="blankValue">The stand-in this type uses for "no value"; see <see cref="BlankValue" />.</param>
        public AttributeValues(T blankValue)
        {
            _blankValue = blankValue;
        }

        public override Type MyValueType => typeof(T);

        public override IConvertible BlankValue => _blankValue;

        public override bool HasVersion(AttributeVersion version)
        {
            return _versions.ContainsKey(key: version);
        }

        public override bool IsMarkedForDeletion(AttributeVersion version)
        {
            return _versions.TryGetValue(key: version,
                       value: out (T Value, bool IsMarkedForDeletion) entry) &&
                   entry.IsMarkedForDeletion;
        }

        public override IConvertible GetValue(AttributeVersion version)
        {
            return _versions.TryGetValue(key: version,
                value: out (T Value, bool IsMarkedForDeletion) entry)
                ? entry.Value
                : _blankValue;
        }

        public override void SetValue(AttributeVersion version,
                                      IConvertible value,
                                      bool isMarkedForDeletion)
        {
            _versions[key: version] = ((T)(object)value, isMarkedForDeletion);
        }

        public override void RemoveVersion(AttributeVersion version)
        {
            _ = _versions.Remove(key: version);
        }
    }

    #endregion

    #region Private variables

    private readonly IDictionary<ElementAttribute, AttributeValueContainer> _Attributes;

    private string _Folder;

    #endregion

    #region Properties

    /// <summary>
    ///     The element type (get only)
    /// </summary>
    public ElementType Type { get; }

    /// <summary>
    /// Whether this DE has the offline-only flag attached to it
    /// </summary>
    public bool IsCloudOffline
    {
        get => Type == ElementType.File && field;
        set => field = (Type == ElementType.File) && value;
    }

    /// <summary>
    /// Full file name including its path.
    /// </summary>
    /// <remarks>May be an absolute or relative path. Use System.IO.Path and related APIs for manipulation and
    /// validation.</remarks>
    public string FileNameWithPath { get; set; }

    public string Folder
    {
        get => Type == ElementType.File ? Path.GetDirectoryName(path: FileNameWithPath) ?? string.Empty : string.Empty;
        set => _Folder = value;
    }

    /// <summary>
    ///     The element name
    /// </summary>
    public string ItemNameWithoutPath { get; set; }

    /// <summary>
    ///     Returns the set display name (text to display). If it was not
    ///     set, it returns the ItemNameWithoutPath.
    /// </summary>
    public string DisplayName
    {
        get => field == null ? ItemNameWithoutPath : Type == ElementType.ParentDirectory ? ".." : (field);
        set;
    }

    /// <summary>
    ///     The extension (get only)
    /// </summary>
    public string Extension { get; }

    /// <summary>
    ///     The sidecar file associated with this directory element.
    /// </summary>
    public FileInfo SidecarFile { set; get; }

    /// <summary>
    ///     We attempt to generate a thumbnail through a variety of means. Initially we try Windows's own magic, then LibRaw in
    ///     a variety of ways, then exiftool, then Magick, then we hang ourselves.
    /// </summary>
    public Image Thumbnail
    {
        get;
        private set
        {
            // Thumbnails are only worth holding on to in the icon views. The main window may legitimately not be up
            // yet (folder scanning starts early) or not exist at all (tests, and the WinForms designer), so a missing
            // instance simply means "not in icon mode" rather than being an error.
            if (FrmMainApp.Instance?.listViewDisplayMode == FrmMainApp.ListViewDisplayMode.LargeIcons)
            {
                field = value;
            }
        }
    }

    /// <summary>
    ///     Via https://stackoverflow.com/a/2001462/3968494 - plus I added the ExifRotate because the incoming image's
    ///     rotation appears to be ignored by the original script.
    /// </summary>
    /// <param name="originalPath"></param>
    /// <param name="width"></param>
    /// <param name="height"></param>
    /// <returns></returns>
    private static Image GenerateFixedSizeImage(string originalPath,
                                                int width,
                                                int height)
    {
        Bitmap? bmPhoto = null;
        using (Image imgPhoto = Image.FromFile(filename: originalPath))
        {
            imgPhoto.ExifRotate();
            int sourceWidth = imgPhoto.Width;
            int sourceHeight = imgPhoto.Height;
            int sourceX = 0;
            int sourceY = 0;
            int destX = 0;
            int destY = 0;

            float nPercent = 0;
            float nPercentW = 0;
            float nPercentH = 0;

            nPercentW = width / (float)sourceWidth;
            nPercentH = height / (float)sourceHeight;
            if (nPercentH < nPercentW)
            {
                nPercent = nPercentH;
                destX = Convert.ToInt16(value: (width -
                                                (sourceWidth * nPercent)) / 2);
            }
            else
            {
                nPercent = nPercentW;
                destY = Convert.ToInt16(value: (height -
                                                (sourceHeight * nPercent)) / 2);
            }

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);

            bmPhoto = new Bitmap(width: width, height: height,
                format: PixelFormat.Format24bppRgb);
            bmPhoto.SetResolution(xDpi: imgPhoto.HorizontalResolution,
                yDpi: imgPhoto.VerticalResolution);

            Graphics grPhoto = Graphics.FromImage(image: bmPhoto);
            grPhoto.Clear(color: Color.Transparent);
            grPhoto.InterpolationMode =
                InterpolationMode.HighQualityBicubic;

            grPhoto.DrawImage(image: imgPhoto,
                destRect: new Rectangle(x: destX, y: destY, width: destWidth, height: destHeight),
                srcRect: new Rectangle(x: sourceX, y: sourceY, width: sourceWidth, height: sourceHeight),
                srcUnit: GraphicsUnit.Pixel);

            grPhoto.Dispose();
        }

        File.Delete(path: originalPath);
        bmPhoto.Save(filename: originalPath);
        return bmPhoto;
    }

    #endregion

    #region Members for attribute setting and retrieval

    /// <summary>
    ///     Checks if this DE has changed attributes that should be saved.
    /// </summary>
    /// <param name="whichAttributeVersion"></param>
    /// <returns>boolean</returns>
    public bool HasDirtyAttributes(AttributeVersion whichAttributeVersion =
                                       AttributeVersion.Stage3ReadyToWrite)
    {
        foreach (AttributeValueContainer avc in _Attributes.Values)
        {
            if (HasSpecificAttributeWithVersion(avc: avc,
                    version: whichAttributeVersion))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    ///     Versions in the order they supersede one another: the later the stage, the more authoritative it is.
    /// </summary>
    /// <remarks>
    ///     <see cref="AttributeVersion.Original" /> is what was read off the file; each later stage is a pending edit
    ///     on its way to being written back. "Give me the current value" therefore means "give me the newest stage
    ///     that has one", which is what this order encodes. Do not reorder.
    /// </remarks>
    private static readonly AttributeVersion[] VersionsNewestFirst =
    [
        AttributeVersion.Stage3ReadyToWrite,
        AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue,
        AttributeVersion.Stage1EditFormIntraTabTransferQueue,
        AttributeVersion.Original
    ];

    /// <summary>
    ///     Resolves which version of a value should actually be returned.
    /// </summary>
    /// <param name="avc">The container to inspect.</param>
    /// <param name="versionRequested">
    ///     A specific version to look for, or <see langword="null" /> to mean "whichever is newest".
    /// </param>
    /// <returns>The version to read, or <see langword="null" /> when the container holds nothing suitable.</returns>
    private AttributeVersion? CheckWhichVersion(AttributeValueContainer avc,
                                                AttributeVersion? versionRequested)
    {
        foreach (AttributeVersion version in VersionsNewestFirst)
        {
            if ((versionRequested == null || versionRequested == version) &&
                avc.HasVersion(version: version))
            {
                return version;
            }
        }

        // Version not found
        return null;
    }

    /// <summary>
    ///     Checks if a value exists for a particular container and version combination.
    /// </summary>
    /// <param name="avc">The container to check</param>
    /// <param name="version">The version to look for</param>
    private bool HasSpecificAttributeWithVersion(AttributeValueContainer avc,
                                                 AttributeVersion version)
    {
        return CheckWhichVersion(avc: avc, versionRequested: version) != null;
    }

    /// <summary>
    ///     Checks if a value exists for a particular attribute and version combination
    /// </summary>
    /// <param name="attribute">The attribute to check</param>
    /// <param name="version">The version to look for</param>
    public bool HasSpecificAttributeWithVersion(ElementAttribute attribute,
                                                AttributeVersion version)
    {
        return _Attributes.TryGetValue(key: attribute, value: out AttributeValueContainer avc) &&
               HasSpecificAttributeWithVersion(avc: avc, version: version);
    }

    /// <summary>
    ///     Checks if there is _any_ data for a particular ElementAttribute
    /// </summary>
    /// <param name="attribute">The attribute to check</param>
    public bool HasSpecificAttributeWithAnyVersion(ElementAttribute attribute)
    {
        return _Attributes.TryGetValue(key: attribute, value: out AttributeValueContainer avc) &&
               CheckWhichVersion(avc: avc, versionRequested: null) != null;
    }

    /// <summary>
    ///     Informs if the particular tag is marked for removal
    /// </summary>
    /// <param name="attribute">The attribute to check</param>
    /// <param name="version">The version to check</param>
    public bool IsMarkedForDeletion(ElementAttribute attribute,
                                    AttributeVersion version)
    {
        // The second half of the test is needed because adding a brand new value on top of an attribute that has
        // been cleaned in the past (or never existed) would otherwise look up a version that is not there.
        return _Attributes.TryGetValue(key: attribute, value: out AttributeValueContainer avc) &&
               HasSpecificAttributeWithVersion(avc: avc, version: version) &&
               avc.IsMarkedForDeletion(version: version);
    }

    /// <summary>
    ///     Returns an attribute rendered as text.
    /// </summary>
    /// <param name="attribute">The attribute to return the value for</param>
    /// <param name="version">The version to return, or null for the newest available version</param>
    /// <param name="notFoundValue">The value to return if no suitable value was found</param>
    /// <param name="context">
    ///     Why the text is wanted - see <see cref="ValueFormatContext" />. This decides whether the value is rendered
    ///     for a human (current culture) or for a machine (invariant). Getting this wrong is how dates used to end up
    ///     unparseable on non-English systems, so the parameter is deliberately explicit rather than a bare flag.
    /// </param>
    public string GetAttributeValueAsString(ElementAttribute attribute,
                                            AttributeVersion? version = null,
                                            string? notFoundValue = null,
                                            ValueFormatContext context = ValueFormatContext.Display)
    {
        if (!_Attributes.TryGetValue(key: attribute, value: out AttributeValueContainer avc))
        {
            return notFoundValue!;
        }

        AttributeVersion? versionToReturn =
            CheckWhichVersion(avc: avc, versionRequested: version);
        if (versionToReturn == null)
        {
            return notFoundValue!;
        }

        // A value flagged for removal reads as blank: the user has asked for the tag to go, so the UI should
        // already reflect that even though the old value is still in memory until the file is actually written.
        if (avc.IsMarkedForDeletion(version: (AttributeVersion)versionToReturn))
        {
            return FrmMainApp.NullStringEquivalentGeneric;
        }

        return AttributeValueFormatter.Format(
            value: avc.GetValue(version: (AttributeVersion)versionToReturn),
            context: context);
    }

    /// <summary>
    ///     Returns the newest version that holds a usable value for the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to inspect.</param>
    /// <returns>
    ///     The newest version holding a value, or <see langword="null" /> when there is none - including the case
    ///     where the newest version is flagged for deletion, since the value is on its way out and falling back to
    ///     an older version would resurrect it.
    /// </returns>
    public AttributeVersion? GetMaxAttributeVersion(ElementAttribute attribute)
    {
        foreach (AttributeVersion attributeVersion in VersionsNewestFirst)
        {
            if (HasSpecificAttributeWithVersion(attribute: attribute,
                    version: attributeVersion))
            {
                return IsMarkedForDeletion(attribute: attribute, version: attributeVersion)
                    ? null
                    : attributeVersion;
            }
        }

        return null;
    }

    /// <summary>
    ///     Returns the value of an attribute as the requested value type.
    /// </summary>
    /// <remarks>
    ///     The attribute type comes from <see cref="SourcesAndAttributes.GetElementAttributesType" />; asking for a
    ///     different one is a programming error rather than a runtime condition, hence the exception.
    ///     If no version is given, the newest one that holds a value is returned.
    /// </remarks>
    /// <param name="attribute">The attribute to return the value for</param>
    /// <param name="version">The version to return or null for the newest available version</param>
    /// <param name="notFoundValue">The value to return if no suitable value was found</param>
    /// <exception cref="ArgumentException">Thrown when the requested type does not match the attribute type.</exception>
    public T? GetAttributeValue<T>(ElementAttribute attribute,
                                   AttributeVersion? version,
                                   T? notFoundValue = null)
        where T : struct
    {
        if (!_Attributes.TryGetValue(key: attribute, value: out AttributeValueContainer avc))
        {
            return notFoundValue;
        }

        Type requestType = typeof(T);
        Type attributeType = avc.MyValueType;
        if (requestType != attributeType)
        {
            throw new ArgumentException(
                message:
                $"Failed to retrieve attribute {GetElementAttributesName(attributeToFind: attribute)} of type {attributeType.Name} due to requesting with incompatible return type {requestType.Name}.");
        }

        AttributeVersion? versionToReturn =
            CheckWhichVersion(avc: avc, versionRequested: version);

        return versionToReturn == null
            ? notFoundValue
            : (T)avc.GetValue(version: (AttributeVersion)versionToReturn);
    }

    /// <summary>
    ///     Sets the value for the given attribute from text, converting it to the declared type of the attribute.
    /// </summary>
    /// <param name="attribute">The attribute to set the value for</param>
    /// <param name="value">The value to set, as text</param>
    /// <param name="version">The version to set it with</param>
    /// <param name="isMarkedForDeletion">Whether this attribute is set for deletion/removal</param>
    /// <param name="context">
    ///     Where the text came from. Defaults to <see cref="ValueFormatContext.Display" /> because most callers are
    ///     WinForms controls; that context also accepts invariant text, so machine-generated values still parse.
    /// </param>
    public void SetAttributeValueAnyType(ElementAttribute attribute,
                                         string value,
                                         AttributeVersion version,
                                         bool isMarkedForDeletion,
                                         ValueFormatContext context = ValueFormatContext.Display)
    {
        Type typeOfAttribute = GetElementAttributesType(attributeToFind: attribute);

        if (typeOfAttribute == typeof(string))
        {
            SetAttributeValue(attribute: attribute,
                value: value,
                version: version,
                isMarkedForDeletion: isMarkedForDeletion);
            return;
        }

        // Text that cannot be parsed is stored as the blank value for the type rather than rejected, matching
        // long-standing behaviour: the write path decides what to emit from the deletion flag, not from the value.
        _ = AttributeValueFormatter.TryParse(value: value,
            targetType: typeOfAttribute,
            context: context,
            result: out IConvertible? parsedValue);

        SetAttributeValue(attribute: attribute,
            value: parsedValue ??
                   AttributeValueContainer.CreateFor(attributeType: typeOfAttribute)
                                          .BlankValue,
            version: version,
            isMarkedForDeletion: isMarkedForDeletion);
    }

    /// <summary>
    ///     Sets the value for the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to set the value for</param>
    /// <param name="value">The value to set; must match the declared type of the attribute</param>
    /// <param name="version">The version to set it with</param>
    /// <param name="isMarkedForDeletion">Whether this attribute is set for deletion/removal</param>
    /// <exception cref="ArgumentException">Thrown when the type of the value does not match the attribute.</exception>
    public void SetAttributeValue(ElementAttribute attribute,
                                  IConvertible value,
                                  AttributeVersion version,
                                  bool isMarkedForDeletion)
    {
        Type attributeType = GetElementAttributesType(attributeToFind: attribute);

        if (!isMarkedForDeletion &&
            value != null &&
            attributeType != value.GetType())
        {
            throw new ArgumentException(
                message:
                $"Error while trying to set the attribute {GetElementAttributesName(attributeToFind: attribute)} of item {ItemNameWithoutPath}. The type {value.GetType().Name} of the value to set does not match the expected type {attributeType.Name}.");
        }

        if (_Attributes.TryGetValue(key: attribute, value: out AttributeValueContainer existingContainer))
        {
            // An existing attribute that is being set to nothing keeps its slot but holds the blank sentinel, so
            // that the write path can still see that this tag was touched via the version and the deletion flag.
            bool valueIsBlank = value == null ||
                                string.IsNullOrWhiteSpace(value: value.ToString());

            existingContainer.SetValue(version: version,
                value: valueIsBlank
                    ? existingContainer.BlankValue
                    : value,
                isMarkedForDeletion: isMarkedForDeletion);
            return;
        }

        // Adding an attribute that was not there before. This happens when the tag was cleaned out in the past or
        // was never present; a dummy value is acceptable because a blank is not written back anyway.
        AttributeValueContainer newContainer =
            AttributeValueContainer.CreateFor(attributeType: attributeType);

        newContainer.SetValue(version: version,
            value: value ?? newContainer.BlankValue,
            isMarkedForDeletion: isMarkedForDeletion);

        _Attributes[key: attribute] = newContainer;
    }

    /// <summary>
    ///     Discards a single version of an attribute, typically to drop a pending edit once it has been written.
    /// </summary>
    /// <param name="attribute">The attribute to modify.</param>
    /// <param name="version">The version to discard. Absent versions are ignored.</param>
    public void RemoveAttributeValue(ElementAttribute attribute,
                                     AttributeVersion version)
    {
        if (_Attributes.TryGetValue(key: attribute, value: out AttributeValueContainer avc))
        {
            avc.RemoveVersion(version: version);
        }
    }

    /// <summary>
    ///     The value that stands in for "nothing" for the given attribute - what reading an attribute that was never
    ///     set gives back.
    /// </summary>
    /// <remarks>
    ///     Exposed because the readers need to store a blank without owning a <see cref="DirectoryElement" /> yet, and
    ///     because the sentinels are not all the obvious ones: for strings it is
    ///     <see cref="FrmMainApp.NullStringEquivalentGeneric" />, not the empty string.
    /// </remarks>
    /// <param name="attribute">The attribute whose blank value is wanted.</param>
    public static IConvertible BlankValueFor(ElementAttribute attribute)
    {
        return AttributeValueContainer
              .CreateFor(attributeType: GetElementAttributesType(attributeToFind: attribute))
              .BlankValue;
    }

    #endregion

    #region Members for Parsing attribute values out of a tag list

    /// <summary>
    ///     Parses all attrbites of this DirectoryElement from the given tag list.
    ///     The list of attributes of this DE is cleared beforehand. Then, the
    ///     values are retrieved and finally (after all values are retrieved into
    ///     a temporary list) the values are put into the "public attribute list".
    /// </summary>
    /// <param name="dictTagsIn">The tags to parse</param>
    public void ParseAttributesFromExifToolOutput(IDictionary<string, string> dictTagsIn)
    {
        Log.Trace(message: $"Parse dict for item '{ItemNameWithoutPath}'...");

        // 1. Rescue the GUID as a raw string using the existing helper
        // This prevents "Russian Doll" nesting of containers
        string currentGuid = GetAttributeValueAsString(attribute: ElementAttribute.GUID);

        // 2. Clear the deck so the parser starts with a clean slate
        _Attributes?.Clear();

        // 3. Parse everything into the temporary store first. The tag-to-typed-value work lives in
        // ExifTagSetParser because the track-file overlay has to do exactly the same thing to the sidecar it
        // gets back from ExifTool.
        IDictionary<ElementAttribute, IConvertible> parsedValues =
            ExifTagSetParser.ParseTagSet(dictTagsIn: dictTagsIn);

        // 4. Restore the GUID first (Identity)
        if (!string.IsNullOrEmpty(value: currentGuid))
        {
            SetAttributeValue(
                attribute: ElementAttribute.GUID,
                value: currentGuid,
                version: AttributeVersion.Original,
                isMarkedForDeletion: false);
        }
        else
        {
            // If it somehow vanished, generate a new one now
            SetAttributeValue(
                attribute: ElementAttribute.GUID,
                value: Guid.NewGuid().ToString(),
                version: AttributeVersion.Original,
                isMarkedForDeletion: false);
        }

        // 5. Add all newly parsed attributes (Metadata)
        foreach (ElementAttribute attribute in parsedValues.Keys)
        {
            // Don't overwrite the GUID we just restored unless the parser actually found a new one
            if (attribute == ElementAttribute.GUID)
            {
                continue;
            }

            SetAttributeValue(
                attribute: attribute,
                value: parsedValues[key: attribute],
                version: AttributeVersion.Original,
                isMarkedForDeletion: false);
        }

        Log.Trace(message: $"Parse dict for item '{ItemNameWithoutPath}' - OK");
    }

    #endregion

    #region Other Methods

    /// <summary>
    /// Generates the thumbnail or assigns the system icon to the Thumbnail property.
    /// This should be called immediately for non-files and during hydration for files.
    /// </summary>
    public void GenerateThumbnailIfRequired()
    {
        // 1. Exit if thumbnails are disabled or already generated or is cloud-offline
        if (!HelperVariables.UserSettingShowThumbnails || Thumbnail != null || IsCloudOffline)
        {
            return;
        }

        try
        {
            Image? generatedValue = null;
            string generatedFileName = Path.Combine(path1: HelperVariables.UserDataFolderPath,
                    path2: $"{ItemNameWithoutPath}_small_thumbnail.jpg");

            if (Type == ElementType.File)
            {
                // Expensive file extraction (only done in background hydration)
                generatedValue = ExtractFileThumbnail(fileNameWithPath: FileNameWithPath, generatedFileName: generatedFileName);
            }
            else
            {
                // Cheap system icon assignment (can be done during discovery)
                string iconLookupValue = Type switch
                {
                    ElementType.SubDirectory => "Folder.png",
                    ElementType.ParentDirectory => "Parentfolder.png",
                    ElementType.MyComputer => "Computer.png",
                    ElementType.Drive => GetDriveIconName(path: FileNameWithPath),
                    _ => "Unknown.png"
                };

                string fullPath = Path.Combine(
                    path1: AppDomain.CurrentDomain.BaseDirectory,
                    path2: "images",
                    path3: iconLookupValue);

                if (File.Exists(path: fullPath))
                {
                    generatedValue = Image.FromFile(filename: fullPath);
                }
            }

            Thumbnail = generatedValue;
        }
        catch (Exception ex)
        {
            Log.Error(ex, message: $"Thumbnail generation failed for {ItemNameWithoutPath}");
        }
    }

    private Image? ExtractFileThumbnail(string fileNameWithPath, string generatedFileName)
    {
        Log.Info(message: $"Thumbnail generation started for {fileNameWithPath}");

        Image? generatedValue = null;
        try
        {
            // this only works for the basic file types
            ReadGetImagePreviews.UseWindowsImageHandlerToCreateThumbnail(
                fileNameIn: fileNameWithPath,
                fileNameOut: generatedFileName,
                maxWidth: FileListView.ThumbnailSize,
                maxHeight: FileListView.ThumbnailSize);
        }
        catch
        {
            //
        }

        if (!File.Exists(path: generatedFileName))
        {
            try
            {
                // Exiftool is acceptable speed and works most of the time but outputs a large file
                Task task =
                    ReadGetImagePreviews.UseExifToolToGeneratePreviewsOrThumbnails(
                        fileNameWithPath: fileNameWithPath,
                        initiator: ReadGetImagePreviews.Initiator.FrmMainAppListViewThumbnail,
                        addSmallThumbnailToFileName: true
                    );
            }

            catch

            {
                //
            }
        }

        if (!File.Exists(path: generatedFileName))
        {
            try
            {
                // This is so fucking stupid it hurts my brain.
                // Libraw is the fastest but doesn't work w/ my D5 files
                // Basically i've found that some files have a zero-index thumbnail but others have a 1-index.
                // ... maybe even more. So bump this to high heavens. (= 4)
                for (int i = 0; i < 4; i++)
                {
                    // yes i know this line duplicates the above for 0-index but alas.
                    if (!File.Exists(path: generatedFileName))
                    {
                        ReadGetImagePreviews.UseLibRawToGenerateThumbnail(
                            originalImagePath: fileNameWithPath,
                            generatedJpegPath: generatedFileName,
                            thumbnailIndex: i);
                    }
                }
            }
            catch
            {
                //
            }
        }

        // This just sucks altogether.
        if (!File.Exists(path: generatedFileName))
        {
            try
            {
                ReadGetImagePreviews.UseLibRawToGenerateFullImage(
                    originalImagePath: fileNameWithPath,
                    generatedJpegPath: generatedFileName,
                    imgWidth: FileListView.ThumbnailSize,
                    imgHeight: FileListView.ThumbnailSize);
            }
            catch
            {
                //
            }
        }

        // And if we've still not succeeded try Magick
        if (!File.Exists(path: generatedFileName))
        {
            try
            {
                ReadGetImagePreviews.UseMagickImageToGeneratePreview
                (
                    originalImagePath: fileNameWithPath,
                    generatedJpegPath: generatedFileName,
                    imgWidth: FileListView.ThumbnailSize,
                    imgHeight: FileListView.ThumbnailSize);
            }
            catch
            {
                // sod it
            }
        }

        if (File.Exists(path: generatedFileName))
        {
            generatedValue = GenerateFixedSizeImage(originalPath: generatedFileName,
                width: FileListView.ThumbnailSize, height: FileListView.ThumbnailSize);
        }

        Log.Info(message: $"Thumbnail generation finished for {fileNameWithPath}");

        return generatedValue;
    }

    /// <summary>
    /// Gets the icon file name for the drive that contains the specified path.
    /// </summary>
    /// <remarks>Determines the drive type via System.IO.DriveInfo.DriveType and maps common DriveType values
    /// to specific icon file names (Removable, Fixed, Network, CDRom). Unknown or other drive types map to
    /// 'Otherdrive.png'.</remarks>
    /// <param name="path">The path to a file or directory used to determine the drive.</param>
    /// <returns>The file name of an icon that represents the drive type (for example 'Harddrive.png', 'Removabledrive.png',
    /// 'Networkdrive.png', 'CDdrive.png'), or 'Otherdrive.png' for unrecognized types.</returns>
    private string GetDriveIconName(string path)
    {
        DriveInfo di = new(driveName: FileNameWithPath);
        DriveType driveType = di.DriveType;
        return driveType switch
        {
            // If I have to fish for these again I'll hang myself.
            // Tutorial as to how to get the files -> https://www.tenforums.com/tutorials/128170-extract-icon-file-windows.html
            // Also comment after some interaction with Gemini:
            // While it is possible to drag these out of Windows but it interferes heavily with the HDPI scaling capability of the app overall and thefore it's more pain in the ass to do it than just leave it as-is. Essentially triggering the relevant libraries will force the app to ignore the HDPI scaling and things will have be semi-manually set for size everywhere, including the icons on the main form etc, which isn't worth the effort.
            DriveType.Removable => "Removabledrive.png",
            DriveType.Fixed => "Harddrive.png",
            DriveType.Network => "Networkdrive.png",
            DriveType.CDRom => "CDdrive.png",
            _ => "Otherdrive.png"
        };
    }

    #endregion

    #region File Operations

    /// <summary>
    ///     Updates internal paths and the sidecar FileInfo. 
    ///     Ensures the sidecar follows the base name pattern (e.g., file.xmp).
    /// </summary>
    /// <param name="newPath">The new absolute path of the primary file.</param>
    public void UpdatePathAfterRename(string newPath)
    {
        FileNameWithPath = newPath;
        ItemNameWithoutPath = Path.GetFileName(path: newPath);

        // Refresh sidecar to [NewBaseName].xmp
        if (SidecarFile != null)
        {
            string? dir = Path.GetDirectoryName(path: newPath);
            string baseName = Path.GetFileNameWithoutExtension(path: newPath);
            string xmpPath = Path.Combine(path1: dir ?? "", path2: baseName + ".xmp");

            SidecarFile = new FileInfo(fileName: xmpPath);
        }
    }

    #endregion
}