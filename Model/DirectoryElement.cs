#nullable enable
using GeoTagNinja.Helpers;
using GeoTagNinja.View.ListView;
using NLog;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Forms;
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
        Thumbnail = Thumbnail;
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

    // We need a non generics super class that can be referenced
    // independent of the concrete values type (generic).
    private class AttributeValueContainer
    {
        internal Type _myValueType;
        internal IDictionary _valueDict;

        public Type MyValueType => _myValueType;

        public IDictionary ValueDict => _valueDict;
    }

    private class AttributeValuesString : AttributeValueContainer
    {
        public AttributeValuesString(string? initialValue = null,
                                     AttributeVersion initialVersion =
                                         AttributeVersion.Original,
                                     bool isMarkedForDeletion = false)
        {
            _myValueType = typeof(string);
            _valueDict = new Dictionary<AttributeVersion, Tuple<string, bool>>();
            if (initialValue != null)
            {
                _valueDict[key: initialVersion] =
                    new Tuple<string, bool>(item1: initialValue,
                        item2: isMarkedForDeletion);
            }
        }
    }

    private class AttributeValuesInt : AttributeValueContainer
    {
        public AttributeValuesInt(int? initialValue = null,
                                  AttributeVersion initialVersion =
                                      AttributeVersion.Original,
                                  bool isMarkedForDeletion = false)
        {
            _myValueType = typeof(int);
            _valueDict = new Dictionary<AttributeVersion, Tuple<int, bool>>();
            if (initialValue != null)
            {
                _valueDict[key: initialVersion] =
                    new Tuple<int, bool>(item1: (int)initialValue,
                        item2: isMarkedForDeletion);
            }
        }
    }

    private class AttributeValuesDouble : AttributeValueContainer
    {
        public AttributeValuesDouble(double? initialValue = null,
                                     AttributeVersion initialVersion =
                                         AttributeVersion.Original,
                                     bool isMarkedForDeletion = false)
        {
            _myValueType = typeof(double);
            _valueDict = new Dictionary<AttributeVersion, Tuple<double, bool>>();
            if (initialValue != null)
            {
                _valueDict[key: initialVersion] =
                    new Tuple<double, bool>(item1: (double)initialValue,
                        item2: isMarkedForDeletion);
            }
        }
    }

    private class AttributeValuesDateTime : AttributeValueContainer
    {
        public AttributeValuesDateTime(DateTime? initialValue = null,
                                       AttributeVersion initialVersion =
                                           AttributeVersion.Original,
                                       bool isMarkedForDeletion = false)
        {
            _myValueType = typeof(DateTime);
            _valueDict = new Dictionary<AttributeVersion, Tuple<DateTime, bool>>();
            if (initialValue != null)
            {
                _valueDict[key: initialVersion] =
                    new Tuple<DateTime, bool>(item1: (DateTime)initialValue,
                        item2: isMarkedForDeletion);
            }
        }
    }

    #endregion

    #region Private variables

    private readonly IDictionary<ElementAttribute, AttributeValueContainer> _Attributes;

    private readonly List<ElementAttribute> _ignoreElementAttributes =
    [
        ElementAttribute.Coordinates,
        ElementAttribute.DestCoordinates,
        ElementAttribute.TakenDateDaysShift,
        ElementAttribute.TakenDateHoursShift,
        ElementAttribute.TakenDateMinutesShift,
        ElementAttribute.TakenDateSecondsShift,
        ElementAttribute.CreateDateDaysShift,
        ElementAttribute.CreateDateHoursShift,
        ElementAttribute.CreateDateMinutesShift,
        ElementAttribute.CreateDateSecondsShift,
        ElementAttribute.RemoveAllGPS,
        ElementAttribute.GUID,
        ElementAttribute.Folder
    ];

    private string _Folder;

    #endregion

    #region Properties

    /// <summary>
    ///     The element type (get only)
    /// </summary>
    public ElementType Type { get; }

    public string FileNameWithPath { get; }

    public string Folder
    {
        get => Type == ElementType.File ? Path.GetDirectoryName(path: FileNameWithPath) ?? string.Empty : string.Empty;
        set => _Folder = value;
    }

    /// <summary>
    ///     The element name (get only)
    /// </summary>
    public string ItemNameWithoutPath { get; }

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
            FrmMainApp frmMainAppInstance = (FrmMainApp)Application.OpenForms[name: "FrmMainApp"];

            if (frmMainAppInstance.listViewDisplayMode == FrmMainApp.ListViewDisplayMode.LargeIcons)
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
    ///     Checks the given generatedValue container for which version to return
    ///     depending on the version requested.
    /// </summary>
    private AttributeVersion? CheckWhichVersion(AttributeValueContainer avc,
                                                AttributeVersion? versionRequested)
    {
        // no need for else-if because the 'return' terminates the loop
        if (((versionRequested == null) |
             (versionRequested == AttributeVersion.Stage3ReadyToWrite)) &
            avc.ValueDict.Contains(key: AttributeVersion.Stage3ReadyToWrite))
        {
            return AttributeVersion.Stage3ReadyToWrite;
        }

        if (((versionRequested == null) |
             (versionRequested ==
              AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue)) &
            avc.ValueDict.Contains(key: AttributeVersion
               .Stage2EditFormReadyToSaveAndMoveToWriteQueue))
        {
            return AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue;
        }

        if (((versionRequested == null) |
             (versionRequested == AttributeVersion.Stage1EditFormIntraTabTransferQueue)) &
            avc.ValueDict.Contains(key: AttributeVersion
               .Stage1EditFormIntraTabTransferQueue))
        {
            return AttributeVersion.Stage1EditFormIntraTabTransferQueue;
        }

        if (((versionRequested == null) |
             (versionRequested == AttributeVersion.Original)) &
            avc.ValueDict.Contains(key: AttributeVersion.Original))
        {
            return AttributeVersion.Original;
        }

        // Version not found
        return null;
    }

    /// <summary>
    ///     Checks if a generatedValue exists for a particular AttributeValueContainer & version combination
    /// </summary>
    /// <param name="avc">The AttributeValueContainer to check</param>
    /// <param name="version">The version to look for</param>
    /// <returns></returns>
    private bool HasSpecificAttributeWithVersion(AttributeValueContainer avc,
                                                 AttributeVersion version)
    {
        AttributeVersion? versionCheck =
            CheckWhichVersion(avc: avc, versionRequested: version);
        if (versionCheck == null)
        {
            return false;
        }

        // Retrieve and return generatedValue

        return true;
    }

    /// <summary>
    ///     Checks if a generatedValue exists for a particular attrib & version combination
    /// </summary>
    /// <param name="attribute">The attribute to check</param>
    /// <param name="version">The version to look for</param>
    /// <returns></returns>
    public bool HasSpecificAttributeWithVersion(ElementAttribute attribute,
                                                AttributeVersion version)
    {
        if (!_Attributes.ContainsKey(key: attribute))
        {
            return false;
        }

        AttributeValueContainer avc = _Attributes[key: attribute];
        return HasSpecificAttributeWithVersion(avc: avc, version: version);
    }

    /// <summary>
    ///     Checks if there is _any_ data for a particular ElementAttribute
    /// </summary>
    /// <param name="attribute">The attribute to check</param>
    /// <returns></returns>
    public bool HasSpecificAttributeWithAnyVersion(ElementAttribute attribute)
    {
        if (!_Attributes.ContainsKey(key: attribute))
        {
            return false;
        }

        AttributeValueContainer avc = _Attributes[key: attribute];
        foreach (AttributeVersion attributeVersion in (AttributeVersion[])Enum.GetValues(
                     enumType: typeof(AttributeVersion)))
        {
            if (HasSpecificAttributeWithVersion(avc: avc, version: attributeVersion))
            {
                return true;
            }

            ;
        }

        return false;
    }

    /// <summary>
    ///     Informs if the particular tag is marked for removal
    /// </summary>
    /// <param name="attribute"></param>
    /// <param name="version"></param>
    /// <returns></returns>
    public bool IsMarkedForDeletion(ElementAttribute attribute,
                                    AttributeVersion version)
    {
        // bit unsure of this but the second half is defo needed because if there is no generatedValue to a particular attribute (it's been deleted or never existed) then
        // adding a new generatedValue on top will trigger this part to run and would break when adding to the Stage1/2 sets
        if (!_Attributes.ContainsKey(key: attribute) ||
            !HasSpecificAttributeWithVersion(attribute: attribute, version: version))
        {
            return false;
        }

        AttributeValueContainer avc = _Attributes[key: attribute];
        Type attributeType = avc.MyValueType;

        if (attributeType == typeof(string))
        {
            IDictionary<AttributeVersion, Tuple<string, bool>> strDict =
                (IDictionary<AttributeVersion, Tuple<string, bool>>)avc.ValueDict;

            return strDict[key: version]
               .Item2;
        }

        if (attributeType == typeof(int))
        {
            IDictionary<AttributeVersion, Tuple<int, bool>> intDict =
                (IDictionary<AttributeVersion, Tuple<int, bool>>)avc.ValueDict;
            return intDict[key: version]
               .Item2;
        }

        if (attributeType == typeof(double))
        {
            IDictionary<AttributeVersion, Tuple<double, bool>> doubleDict =
                (IDictionary<AttributeVersion, Tuple<double, bool>>)avc.ValueDict;

            return doubleDict[key: version]
               .Item2;
        }

        if (attributeType == typeof(DateTime))
        {
            IDictionary<AttributeVersion, Tuple<DateTime, bool>> DateTimeDict =
                (IDictionary<AttributeVersion, Tuple<DateTime, bool>>)avc.ValueDict;

            return DateTimeDict[key: version]
               .Item2;
        }

        // else
        // Should not get to here
        throw new ArgumentException(
            message:
            $"Failed to retrieve attribute '{GetElementAttributesName(attributeToFind: attribute)}' of type '{attributeType.Name}");
    }

    /// <summary>
    ///     Returns an attribute in string format. If it is a number,
    ///     a localized conversion is done.
    /// </summary>
    /// <param name="attribute">The attribute to return the generatedValue for</param>
    /// <param name="version">The version to return or null if latest version</param>
    /// <param name="notFoundValue">The generatedValue to return if no suitable generatedValue was found</param>
    /// <param name="nowSavingExif">Indicates whether this is when the file is being saved.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public string GetAttributeValueAsString(ElementAttribute attribute,
                                          AttributeVersion? version = null,
                                          string? notFoundValue = null,
                                          bool nowSavingExif = false)
    {
        if (!_Attributes.ContainsKey(key: attribute))
        {
#pragma warning disable CS8603 // Possible null reference return.
            return notFoundValue;

        }

        AttributeValueContainer avc = _Attributes[key: attribute];
        Type attributeType = avc.MyValueType;

        AttributeVersion? versionCheck =
            CheckWhichVersion(avc: avc, versionRequested: version);
        if (versionCheck == null)
        {
            return notFoundValue;
        }
#pragma warning restore CS8603 // Possible null reference return.

        AttributeVersion versionToReturn = (AttributeVersion)versionCheck;

        // Retrieve and return generatedValue
        // no need for else-if because the 'return' terminates the loop

        // the logic with the if (strDict[key: versionToReturn].Item2) {return FrmMainApp.NullStringEquivalentGeneric;}
        // ... is that if something is marked as "to remove" then we want to show a blank generatedValue.

        if (attributeType == typeof(string))
        {
            IDictionary<AttributeVersion, Tuple<string, bool>> strDict =
                (IDictionary<AttributeVersion, Tuple<string, bool>>)avc.ValueDict;
            Tuple<string, bool> intValue = strDict[key: versionToReturn];
            return strDict[key: versionToReturn]
               .Item2
                ? FrmMainApp.NullStringEquivalentGeneric
                : intValue.Item1;
        }

        if (attributeType == typeof(int))
        {
            IDictionary<AttributeVersion, Tuple<int, bool>> intDict =
                (IDictionary<AttributeVersion, Tuple<int, bool>>)avc.ValueDict;
            Tuple<int, bool> intValue = intDict[key: versionToReturn];
            return intDict[key: versionToReturn]
               .Item2
                ? FrmMainApp.NullStringEquivalentGeneric
                : intValue.Item1.ToString(provider: CultureInfo.InvariantCulture);
        }

        if (attributeType == typeof(double))
        {
            IDictionary<AttributeVersion, Tuple<double, bool>> doubleDict =
                (IDictionary<AttributeVersion, Tuple<double, bool>>)avc.ValueDict;
            Tuple<double, bool> doubleValue = doubleDict[key: versionToReturn];
            return doubleDict[key: versionToReturn]
               .Item2
                ? FrmMainApp.NullStringEquivalentGeneric
                : doubleValue.Item1.ToString(provider: CultureInfo.InvariantCulture);
        }

        if (attributeType == typeof(DateTime))
        {
            IDictionary<AttributeVersion, Tuple<DateTime, bool>> dateTimeDict =
                (IDictionary<AttributeVersion, Tuple<DateTime, bool>>)avc.ValueDict;
            Tuple<DateTime, bool> dateTimeValue = dateTimeDict[key: versionToReturn];
            if (dateTimeDict[key: versionToReturn]
               .Item2)
            {
                return FrmMainApp.NullStringEquivalentGeneric;
            }

            string formattedDateTime =
                dateTimeValue.Item1.ToString(format: "yyyy-MM-dd HH:mm:ss");

            return !nowSavingExif
                ? dateTimeValue.Item1.ToString(provider: CultureInfo.CurrentCulture)
                : formattedDateTime;
        }

        // else
        // Should not get to here
        throw new ArgumentException(
            message:
            $"Failed to retrieve attribute '{GetElementAttributesName(attributeToFind: attribute)}' of type '{attributeType.Name}' by requesting its generatedValue with type 'string' due to conversion issues.");
    }

    public AttributeVersion? GetMaxAttributeVersion(ElementAttribute attribute)

    {
        List<AttributeVersion> relevantAttributeVersions =
        [
            // DO NOT reorder!
            AttributeVersion.Stage3ReadyToWrite,
            AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue,
            AttributeVersion.Stage1EditFormIntraTabTransferQueue,
            AttributeVersion.Original
        ];
        foreach (AttributeVersion attributeVersion in relevantAttributeVersions)
        {
            if (HasSpecificAttributeWithVersion(attribute: attribute,
                    version: attributeVersion))
            {
                if (!IsMarkedForDeletion(attribute: attribute, version: attributeVersion))
                {
                    return attributeVersion;
                }

                // if it's marked for deletion then we don't want to return the Original because the assumption is that the generatedValue is being dropped.
                return null;
            }
        }

        return null;
    }

    /// <summary>
    ///     Returns the generatedValue of an attribute as the given generic.
    ///     If the attribute has a different type than the generic, an exception is thrown.
    ///     (The attribute type is taken from SourcesAndAttributes.GetAttributeType().
    ///     If the version needed is given only that version is checked for and returned.
    ///     If no version is given, the latest version (ie. modified) is returned if
    ///     it exists, otherwise original.
    ///     Item1 is the actual generatedValue we want. Item2 is the bool flag for the deletion mark.
    /// </summary>
    /// <param name="attribute">The attribute to return the generatedValue for</param>
    /// <param name="version">The version to return or null if latest version</param>
    /// <param name="notFoundValue">The generatedValue to return if no suitable generatedValue was found</param>
    /// <returns></returns>
    public T? GetAttributeValue<T>(ElementAttribute attribute,
                                   AttributeVersion? version,
                                   T? notFoundValue = null)
        where T : struct
    {
        if (!_Attributes.ContainsKey(key: attribute))
        {
            return notFoundValue;
        }

        AttributeValueContainer avc = _Attributes[key: attribute];

        // First check for matching type
        // requesting a string is always allowed
        Type requestType = typeof(T);
        Type attributeType = avc.MyValueType;
        if ((requestType != attributeType) & (requestType != typeof(string)))
        {
            throw new ArgumentException(
                message:
                $"Failed to retrieve attribute '{GetElementAttributesName(attributeToFind: attribute)}' of type '{attributeType.Name}' due to requesting with incompatible return type '{requestType.Name}'.");
        }

        AttributeVersion? versionCheck =
            CheckWhichVersion(avc: avc, versionRequested: version);
        if (versionCheck == null)
        {
            return notFoundValue;
        }

        AttributeVersion versionToReturn = (AttributeVersion)versionCheck;

        // Retrieve and return generatedValue
        if (attributeType == typeof(int))
        {
            IDictionary<AttributeVersion, Tuple<int, bool>> intDict =
                (IDictionary<AttributeVersion, Tuple<int, bool>>)avc.ValueDict;
            int intValue = intDict[key: versionToReturn]
               .Item1;
            if (requestType == typeof(int))
            {
                return (T)Convert.ChangeType(value: intValue, conversionType: typeof(T));
            }
        }
        else if (attributeType == typeof(double))
        {
            IDictionary<AttributeVersion, Tuple<double, bool>> doubleDict =
                (IDictionary<AttributeVersion, Tuple<double, bool>>)avc.ValueDict;
            double doubleValue = doubleDict[key: versionToReturn]
               .Item1;
            if (requestType == typeof(double))
            {
                return (T)Convert.ChangeType(value: doubleValue,
                    conversionType: typeof(T));
            }
        }
        else if (attributeType == typeof(DateTime))
        {
            IDictionary<AttributeVersion, Tuple<DateTime, bool>> DateTimeDict =
                (IDictionary<AttributeVersion, Tuple<DateTime, bool>>)avc.ValueDict;
            DateTime DateTimeValue = DateTimeDict[key: versionToReturn]
               .Item1;
            if (requestType == typeof(DateTime))
            {
                return (T)Convert.ChangeType(value: DateTimeValue,
                    conversionType: typeof(T));
            }
        }

        // Should not get to here
        throw new ArgumentException(
            message:
            $"Failed to retrieve attribute '{GetElementAttributesName(attributeToFind: attribute)}' of type '{attributeType.Name}' by requesting its generatedValue with type '{requestType.Name}' due to conversion issues.");
    }

    /// <summary>
    ///     Sets the generatedValue for the given attribute - without needing to specify the Type.
    /// </summary>
    /// <param name="attribute">The attribute to set the generatedValue for</param>
    /// <param name="value">The generatedValue to set (as string)</param>
    /// <param name="version">The version to set it with </param>
    /// <param name="isMarkedForDeletion">Whether this attribute is set for deletion/removal</param>
    public void SetAttributeValueAnyType(ElementAttribute attribute,
                                         string value,
                                         AttributeVersion version,
                                         bool isMarkedForDeletion)
    {
        Type typeOfAttribute = GetElementAttributesType(attributeToFind: attribute);
        IConvertible writeValueConvertible;
        if (typeOfAttribute == typeof(string))
        {
            SetAttributeValue(attribute: attribute,
                value: value,
                version: version,
                isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (typeOfAttribute == typeof(double))
        {
            writeValueConvertible =
                HelperGenericTypeOperations.TryParseNullableDouble(val: value) ??
                FrmMainApp.NullDoubleEquivalent;
            SetAttributeValue(attribute: attribute,
                value: writeValueConvertible,
                version: version,
                isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (typeOfAttribute == typeof(int))
        {
            writeValueConvertible =
                HelperGenericTypeOperations.TryParseNullableInt(val: value) ??
                FrmMainApp.NullIntEquivalent;
            SetAttributeValue(attribute: attribute,
                value: writeValueConvertible,
                version: version,
                isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (typeOfAttribute == typeof(DateTime))
        {
            writeValueConvertible =
                HelperGenericTypeOperations.TryParseNullableDateTime(val: value) ??
                FrmMainApp.NullDateTimeEquivalent;
            SetAttributeValue(attribute: attribute,
                value: writeValueConvertible,
                version: version,
                isMarkedForDeletion: isMarkedForDeletion);
        }
        else
        {
            throw new ArgumentException(
                message: $"Trying to get attribute name of unknown attribute with generatedValue {attribute}");
        }
    }

    /// <summary>
    ///     Sets the generatedValue for the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to set the generatedValue for</param>
    /// <param name="value">The generatedValue to set (as string)</param>
    /// <param name="version">The version to set it with </param>
    /// <param name="isMarkedForDeletion">Whether this attribute is set for deletion/removal</param>
    public void SetAttributeValue(ElementAttribute attribute,
                                  IConvertible value,
                                  AttributeVersion version,
                                  bool isMarkedForDeletion)
    {
        Type attributeType = GetElementAttributesType(attributeToFind: attribute);
        if (!isMarkedForDeletion &&
            value != null)
        {
            if (attributeType != value.GetType())
            {
                throw new ArgumentException(
                    message:
                    $"Error, while trying to set the attribute {GetElementAttributesName(attributeToFind: attribute)} of item '{ItemNameWithoutPath}'. The type '{value.GetType().Name}' of the generatedValue to set  does not contain match the expected type '{attributeType.Name}'.");
            }
        }

        // update attribute if it exists       
        if (_Attributes.ContainsKey(key: attribute))
        {
            bool setMarkedForDeletion = value == null ||
                                        string.IsNullOrWhiteSpace(
                                            value: value.ToString());

            _Attributes[key: attribute]
                       .ValueDict[key: version] = attributeType == typeof(double)
                ? setMarkedForDeletion
                    ? new Tuple<double, bool>(
                        item1: FrmMainApp.NullDoubleEquivalent,
                        item2: isMarkedForDeletion)
                    : new Tuple<double, bool>(item1: (double)value,
                            item2: isMarkedForDeletion)
                : attributeType == typeof(int)
                    ? setMarkedForDeletion
                    ? new Tuple<int, bool>(
                        item1: FrmMainApp.NullIntEquivalent, item2: isMarkedForDeletion)
                    : new Tuple<int, bool>(item1: (int)value,
                            item2: isMarkedForDeletion)
                    : attributeType == typeof(DateTime)
                    ? setMarkedForDeletion
                    ? new Tuple<DateTime, bool>(
                        item1: FrmMainApp.NullDateTimeEquivalent,
                        item2: isMarkedForDeletion)
                    : new Tuple<DateTime, bool>(item1: (DateTime)value,
                            item2: isMarkedForDeletion)
                    : attributeType == typeof(string)
                    ? (object)(setMarkedForDeletion
                    ? new Tuple<string, bool>(
                        item1: FrmMainApp.NullStringEquivalentGeneric,
                        item2: isMarkedForDeletion)
                    : new Tuple<string, bool>(item1: value.ToString(),
                            item2: isMarkedForDeletion))
                    : throw new ArgumentException(
                    message:
                    $"Trying to get attribute name of unknown attribute with generatedValue {attribute}");

            return;
        }

        // add new attribute if doesn't exist. this can happen when the file/attrib has been cleaned in the past or was empty to start with
        AttributeValueContainer avc;

        // regarding settings defaults: this is bit of a safety thing here. Value can be NULL when user presses "clear all" on something that already doesn't have a generatedValue and/or is marked for deletion
        // in that case dummy values are acceptable because they won't be actually recorded anyway.

        if (attributeType == typeof(double))
        {
            value ??= FrmMainApp.NullDoubleEquivalent;
            avc = new AttributeValuesDouble(initialValue: (double)value,
                initialVersion: version,
                isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (attributeType == typeof(int))
        {
            value ??= FrmMainApp.NullIntEquivalent;
            avc = new AttributeValuesInt(initialValue: (int)value,
                initialVersion: version,
                isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (attributeType == typeof(DateTime))
        {
            value ??= FrmMainApp.NullDateTimeEquivalent;
            avc = new AttributeValuesDateTime(initialValue: (DateTime)value,
                initialVersion: version,
                isMarkedForDeletion: isMarkedForDeletion);
        }
        else
        {
            value ??= FrmMainApp.NullStringEquivalentGeneric;
            avc = new AttributeValuesString(initialValue: (string)value,
                initialVersion: version,
                isMarkedForDeletion: isMarkedForDeletion);
        }

        _Attributes[key: attribute] = avc;
    }

    public void RemoveAttributeValue(ElementAttribute attribute,
                                     AttributeVersion version)
    {
        try
        {
            if (_Attributes.ContainsKey(key: attribute))
            {
                _Attributes[key: attribute]
                   .ValueDict.Remove(key: version);
            }
        }
        catch (Exception)
        {
            // ignore
        }
    }

    #endregion

    #region Members for Parsing attribute values out of a tag list

    /// <summary>
    ///     Searches the given tag list to yield the generatedValue for the given attribute.
    ///     Hereby the TagsToAttributesIn list is used to determine which
    ///     tags are taken in which priority for the attribute.
    /// </summary>
    /// <param name="attribute">The attribute to find the generatedValue for</param>
    /// <param name="tags">The tag list to parse</param>
    /// <returns>A touple (name of tag chosen, generatedValue)</returns>
    private (string, string) GetDataPointFromTags(ElementAttribute attribute,
                                                  IDictionary<string, string> tags)
    {
        Log.Trace(message:
            $"Starting to parse dict for attribute: {GetElementAttributesName(attributeToFind: attribute)}");

        if (!_ignoreElementAttributes.Contains(item: attribute))
        {
            List<string> tagsWithAttributesIn =
                GetElementAttributesIn(attributeToFind: attribute);
            for (int i = 0; i < tagsWithAttributesIn.Count; i++)
            {
                if (tags.ContainsKey(key: tagsWithAttributesIn[index: i]
                       .ToUpper()))
                {
                    Log.Trace(
                        message:
                        $"Parse dict for attribute: '{GetElementAttributesName(attributeToFind: attribute)}' yielded generatedValue '{tags[key: tagsWithAttributesIn[index: i].ToUpper()]}'");
                    return (tagsWithAttributesIn[index: i], tags[
                                key: tagsWithAttributesIn[index: i]
                                   .ToUpper()]);
                }
            }
        }

        Log.Trace(
            message:
            $"Parse dict for attribute: '{GetElementAttributesName(attributeToFind: attribute)}' yielded no generatedValue.");
        return (null, null);
    }

    /// <summary>
    ///     Handles retrieving the generatedValue for the given attribute into the
    ///     temporary generatedValue list.
    ///     If required (due to dependencies for conversion), another generatedValue
    ///     retrieval is triggered. All results are put into the temporary
    ///     lists.
    ///     Transformations are done if needed - they are kept separately in TagsToModelValueTransformations.
    /// </summary>
    /// <param name="attribute">The attribute to retrieve the generatedValue for</param>
    /// <param name="parsedValues">The list of already parsed values (in case another generatedValue is needed)</param>
    /// <param name="parsedFails">The list of attributes, parsing failed for</param>
    /// <param name="tags">The tags provided to retrieve the generatedValue from</param>
    /// <param name="callDepth">The recursive call depth to allow for tracking of loops</param>
    /// <returns>True if parsing succeeded (resulting values are put into the passed lists).</returns>
    private bool ParseAttribute(ElementAttribute attribute,
                                IDictionary<ElementAttribute, IConvertible> parsedValues,
                                List<ElementAttribute> parsedFails,
                                IDictionary<string, string> tags,
                                int callDepth)
    {
        Log.Trace(
            message:
            $"Parse attribute '{GetElementAttributesName(attributeToFind: attribute)}' at depth {callDepth}...");
        if (parsedFails.Contains(item: attribute))
        {
            return false;
        }

        if (callDepth > 10)
        {
            throw new InvalidOperationException(
                message:
                $"Reached max call depth of '{callDepth}' while parsing attribute '{GetElementAttributesName(attributeToFind: attribute)}'.");
        }

        callDepth++;
        (string chosenTag, string parseResultStr) =
            GetDataPointFromTags(attribute: attribute, tags: tags);

        #region Create a history

        // TakenDate & CreateDate have to be sent into their
        // respective tables for querying later if user chooses time-shift.
        // TODO: replace logic with AttributeVersion concept
        try
        {
            if (parseResultStr != null)
            {
                switch (attribute)
                {
                    case ElementAttribute.TakenDate:
                        if (parseResultStr.Contains(value: "0000"))
                        {
                            return false;
                        }

                        break;

                    case ElementAttribute.CreateDate:
                        if (parseResultStr.Contains(value: "0000"))
                        {
                            return false;
                        }

                        break;
                }
                // Not adding the xmp here because the current code logic would pull a "unified" data point.
            }
        }
        catch
        {
            Log.Error(
                message:
                $"Parse attribute failed '{GetElementAttributesName(attributeToFind: attribute)}' at depth {callDepth}...");
            return false; // be triple sure here.
        }

        #endregion

        // If needed, transform the attribute
        IConvertible? resTyped = null;
        try
        {
            switch (attribute)
            {
                case ElementAttribute.GPSAltitude:
                    resTyped =
                        TagsToModelValueTransformations.T2M_GPSAltitude(
                            parseResult: parseResultStr);
                    break;

                case ElementAttribute.GPSAltitudeRef:
                    resTyped =
                        TagsToModelValueTransformations.T2M_AltitudeRef(
                            parseResult: parseResultStr);
                    break;

                case ElementAttribute.GPSLatitude:
                case ElementAttribute.GPSDestLatitude:
                case ElementAttribute.GPSLongitude:
                case ElementAttribute.GPSDestLongitude:
                    resTyped = TagsToModelValueTransformations.T2M_GPSLatLong(
                        attribute: attribute,
                        parseResult: parseResultStr,
                        parsed_Values: parsedValues,
                        ParseMissingAttribute: delegate (ElementAttribute atrb)
                        {
                            return ParseAttribute(attribute: atrb,
                                parsedValues: parsedValues,
                                parsedFails: parsedFails,
                                tags: tags,
                                callDepth: callDepth);
                        });
                    break;

                case ElementAttribute.GPSImgDirection:
                    resTyped =
                        TagsToModelValueTransformations.T2M_GPSImgDirection(
                            parseResult: parseResultStr);
                    break;
                case ElementAttribute.GPSImgDirectionRef:
                    resTyped =
                        TagsToModelValueTransformations.T2M_GPSImgDirectionRef(
                            parseResult: parseResultStr);
                    break;

                case ElementAttribute.ExposureTime:
                    resTyped =
                        TagsToModelValueTransformations.T2M_ExposureTime(
                            parseResult: parseResultStr);
                    break;

                case ElementAttribute.Fnumber:
                case ElementAttribute.FocalLength:
                case ElementAttribute.FocalLengthIn35mmFormat:
                    resTyped =
                        TagsToModelValueTransformations.T2M_F_FocalLength(
                            attribute: attribute, parseResult: parseResultStr);
                    break;
                case ElementAttribute.GPSDOP:
                    resTyped =
                        TagsToModelValueTransformations.T2M_GPSDOP(
                             parseResult: parseResultStr);
                    break;
                case ElementAttribute.ISO:
                    resTyped =
                        TagsToModelValueTransformations.T2M_F_ISO(
                            attribute: attribute, parseResult: parseResultStr);
                    break;

                case ElementAttribute.TakenDate:
                case ElementAttribute.CreateDate:
                    resTyped =
                        TagsToModelValueTransformations.T2M_TakenCreatedDate(
                            parseResult: parseResultStr);
                    break;

                default:

                    Type typeOfAttribute =
                        GetElementAttributesType(attributeToFind: attribute);
                    resTyped = typeOfAttribute == typeof(string)
                        ? parseResultStr
                        : typeOfAttribute == typeof(double)
                            ? HelperGenericTypeOperations.TryParseNullableDouble(
                                val: parseResultStr) ??
                            FrmMainApp.NullDoubleEquivalent
                            : typeOfAttribute == typeof(int)
                            ? HelperGenericTypeOperations
                               .TryParseNullableInt(val: parseResultStr) ??
                            FrmMainApp.NullIntEquivalent
                            : typeOfAttribute == typeof(DateTime)
                            ? (IConvertible)(HelperGenericTypeOperations.TryParseNullableDateTime(
                                val: parseResultStr) ??
                            FrmMainApp.NullDateTimeEquivalent)
                            : throw new ArgumentException(
                            message:
                            $"Trying to get attribute name of unknown attribute with generatedValue {attribute}");

                    break;
            }
        }
        catch
        {
            Log.Error(
                message:
                $"Parse error for attribute '{attribute}': parseResultStr: {parseResultStr}, parsedValues: {parsedValues}.");
        }

        // Add it to the lists
        if (resTyped == null)
        {
            parsedFails.Add(item: attribute);
            return false;
        }

        parsedValues[key: attribute] = resTyped;
        return true;
    }

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

        IEnumerable<ElementAttribute> possibleAttributes =
            (IEnumerable<ElementAttribute>)Enum.GetValues(enumType: typeof(ElementAttribute));

        // Create an upper-case capitalised version for case-insensitive lookup
        IDictionary<string, string> tags = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> kvp in dictTagsIn)
        {
            tags[key: kvp.Key.ToUpper()] = kvp.Value;
        }

        // 2. Clear the deck so the parser starts with a clean slate
        _Attributes?.Clear();

        // 3. Parse everything into the temporary store first
        IDictionary<ElementAttribute, IConvertible> parsedValues =
            new Dictionary<ElementAttribute, IConvertible>();
        List<ElementAttribute> parsedFails = [];

        foreach (ElementAttribute attribute in possibleAttributes)
        {
            if (!parsedValues.ContainsKey(key: attribute))
            {
                _ = ParseAttribute(
                    attribute: attribute,
                    parsedValues: parsedValues,
                    parsedFails: parsedFails,
                    tags: tags,
                    callDepth: 0);
            }
        }

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
    /// Determines the ImageList key for the element based on its type and hydration status.
    /// </summary>
    /// <returns>A string key to be used in the ListView ImageList.</returns>
    public string GetImageKey()
    {
        if (Type != ElementType.File)
        {
            // Use the Enum name (e.g., "SubDirectory", "Drive") as the key for system icons.
            return Type.ToString();
        }

        // For files, only return the unique GUID if a thumbnail has actually been generated.
        // This prevents files from "borrowing" the previous icon if they aren't ready.
        return (Thumbnail != null) ? GetAttributeValueAsString(attribute: ElementAttribute.GUID) : string.Empty;
    }

    /// <summary>
    /// Generates the thumbnail or assigns the system icon to the Thumbnail property.
    /// This should be called immediately for non-files and during hydration for files.
    /// </summary>
    public void GenerateThumbnailIfRequired()
    {
        // 1. Exit if thumbnails are disabled or already generated
        if (!HelperVariables.UserSettingShowThumbnails || Thumbnail != null)
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
        Image? generatedValue = null;
        try
        {
            // this only works for the basic file types
            HelperExifReadGetImagePreviews.UseWindowsImageHandlerToCreateThumbnail(
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
                    HelperExifReadGetImagePreviews.UseExifToolToGeneratePreviewsOrThumbnails(
                        fileNameWithPath: fileNameWithPath,
                        initiator: HelperExifReadGetImagePreviews.Initiator.FrmMainAppListViewThumbnail,
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
                        HelperExifReadGetImagePreviews.UseLibRawToGenerateThumbnail(
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
                HelperExifReadGetImagePreviews.UseLibRawToGenerateFullImage(
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
                HelperExifReadGetImagePreviews.UseMagickImageToGeneratePreview
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
}