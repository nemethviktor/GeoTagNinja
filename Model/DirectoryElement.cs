#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using GeoTagNinja.Helpers;
using NLog;
using static GeoTagNinja.Model.SourcesAndAttributes;

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

    internal static readonly Logger Logger = LogManager.GetCurrentClassLogger();


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
        UniqueID = Guid.NewGuid();
        FileNameWithPath = fileNameWithPath;
        Extension = Path.GetExtension(path: FileNameWithPath)
            .Replace(oldValue: ".", newValue: "");

        _Attributes = new Dictionary<ElementAttribute, AttributeValueContainer>();
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
    internal class AttributeValueContainer
    {
        internal Type _myValueType;
        internal IDictionary _valueDict;

        public Type MyValueType => _myValueType;

        public IDictionary ValueDict => _valueDict;
    }

    internal class AttributeValuesString : AttributeValueContainer
    {
        public AttributeValuesString(string initialValue = null,
                                     AttributeVersion initialVersion = AttributeVersion.Original,
                                     bool isMarkedForDeletion = false)
        {
            _myValueType = typeof(string);
            _valueDict = new Dictionary<AttributeVersion, Tuple<string, bool>>();
            if (initialValue != null)
            {
                _valueDict[key: initialVersion] = new Tuple<string, bool>(item1: initialValue, item2: isMarkedForDeletion);
            }
        }
    }

    internal class AttributeValuesInt : AttributeValueContainer
    {
        public AttributeValuesInt(int? initialValue = null,
                                  AttributeVersion initialVersion = AttributeVersion.Original,
                                  bool isMarkedForDeletion = false)
        {
            _myValueType = typeof(int);
            _valueDict = new Dictionary<AttributeVersion, Tuple<int, bool>>();
            if (initialValue != null)
            {
                _valueDict[key: initialVersion] = new Tuple<int, bool>(item1: (int)initialValue, item2: isMarkedForDeletion);
            }
        }
    }

    internal class AttributeValuesDouble : AttributeValueContainer
    {
        public AttributeValuesDouble(double? initialValue = null,
                                     AttributeVersion initialVersion = AttributeVersion.Original,
                                     bool isMarkedForDeletion = false)
        {
            _myValueType = typeof(double);
            _valueDict = new Dictionary<AttributeVersion, Tuple<double, bool>>();
            if (initialValue != null)
            {
                _valueDict[key: initialVersion] = new Tuple<double, bool>(item1: (double)initialValue, item2: isMarkedForDeletion);
            }
        }
    }

    internal class AttributeValuesDateTime : AttributeValueContainer
    {
        public AttributeValuesDateTime(DateTime? initialValue = null,
                                       AttributeVersion initialVersion = AttributeVersion.Original,
                                       bool isMarkedForDeletion = false)
        {
            _myValueType = typeof(DateTime);
            _valueDict = new Dictionary<AttributeVersion, Tuple<DateTime, bool>>();
            if (initialValue != null)
            {
                _valueDict[key: initialVersion] = new Tuple<DateTime, bool>(item1: (DateTime)initialValue, item2: isMarkedForDeletion);
            }
        }
    }

    #endregion

    #region Private variables

    private string _DisplayName;

    private readonly IDictionary<ElementAttribute, AttributeValueContainer> _Attributes;

    #endregion

    #region Properties

    /// <summary>
    ///     The element type (get only)
    /// </summary>
    public ElementType Type { get; }

    /// <summary>
    ///     The UniqueID (get only)
    /// </summary>
    public Guid UniqueID { get; }

    /// <summary>
    ///     The fully qualified path incl. its name (get only)
    /// </summary>
    public string FileNameWithPath { get; }

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
        get
        {
            if (_DisplayName == null)
            {
                return ItemNameWithoutPath;
            }

            return _DisplayName;
        }
        set => _DisplayName = value;
    }

    /// <summary>
    ///     The extension (get only)
    /// </summary>
    public string Extension { get; }

    /// <summary>
    ///     The sidecar file associated with this directory element.
    /// </summary>
    public string SidecarFile { set; get; }

    #endregion


    #region Members for attribute setting and retrieval

    /// <summary>
    /// Checks if this DE has changed attributes that should be saved.
    /// </summary>
    /// <param name="whichAttributeVersion"></param>
    /// <returns>boolean</returns>
    public bool HasDirtyAttributes(DirectoryElement.AttributeVersion whichAttributeVersion = AttributeVersion.Stage3ReadyToWrite)
    {
        foreach (AttributeValueContainer avc in this._Attributes.Values)
        {
            if (HasSpecificAttributeWithVersion(avc: avc,
                                                version: whichAttributeVersion))
                return true;
        }
        return false;
    }


    /// <summary>
    ///     Checks the given value container for which version to return
    ///     depending on the version requested.
    /// </summary>
    private AttributeVersion? CheckWhichVersion(AttributeValueContainer avc,
                                                AttributeVersion? versionRequested)
    {
        // no need for else-if because the 'return' terminates the loop
        if (((versionRequested == null) | (versionRequested == AttributeVersion.Stage3ReadyToWrite)) & avc.ValueDict.Contains(key: AttributeVersion.Stage3ReadyToWrite))
        {
            return AttributeVersion.Stage3ReadyToWrite;
        }

        if (((versionRequested == null) | (versionRequested == AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue)) & avc.ValueDict.Contains(key: AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue))
        {
            return AttributeVersion.Stage2EditFormReadyToSaveAndMoveToWriteQueue;
        }

        if (((versionRequested == null) | (versionRequested == AttributeVersion.Stage1EditFormIntraTabTransferQueue)) & avc.ValueDict.Contains(key: AttributeVersion.Stage1EditFormIntraTabTransferQueue))
        {
            return AttributeVersion.Stage1EditFormIntraTabTransferQueue;
        }

        if (((versionRequested == null) | (versionRequested == AttributeVersion.Original)) & avc.ValueDict.Contains(key: AttributeVersion.Original))
        {
            return AttributeVersion.Original;
        }

        // Version not found
        return null;
    }


    /// <summary>
    ///     Checks if a value exists for a particular AttributeValueContainer & version combination
    /// </summary>
    /// <param name="avc">The AttributeValueContainer to check</param>
    /// <param name="version">The version to look for</param>
    /// <returns></returns>
    private bool HasSpecificAttributeWithVersion(AttributeValueContainer avc,
                                                AttributeVersion version)
    {
        Type attributeType = avc.MyValueType;

        AttributeVersion? versionCheck = CheckWhichVersion(avc: avc, versionRequested: version);
        if (versionCheck == null)
        {
            return false;
        }

        AttributeVersion versionToReturn = (AttributeVersion)versionCheck;

        // Retrieve and return value

        return true;
    }


    /// <summary>
    ///     Checks if a value exists for a particular attrib & version combination
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
        return HasSpecificAttributeWithVersion(avc, version);
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
        // bit unsure of this but the second half is defo needed because if there is no value to a particular attribute (it's been deleted or never existed) then
        // adding a new value on top will trigger this part to run and would break when adding to the Stage1/2 sets
        if (!_Attributes.ContainsKey(key: attribute) || !HasSpecificAttributeWithVersion(attribute: attribute, version: version))
        {
            return false;
        }

        AttributeValueContainer avc = _Attributes[key: attribute];
        Type attributeType = avc.MyValueType;

        if (attributeType == typeof(string))
        {
            IDictionary<AttributeVersion, Tuple<string, bool>> strDict = (IDictionary<AttributeVersion, Tuple<string, bool>>)avc.ValueDict;
            Tuple<string, bool> intValue = strDict[key: version];
            return strDict[key: version]
                .Item2;
        }

        if (attributeType == typeof(int))
        {
            IDictionary<AttributeVersion, Tuple<int, bool>> intDict = (IDictionary<AttributeVersion, Tuple<int, bool>>)avc.ValueDict;
            Tuple<int, bool> intValue = intDict[key: version];
            return intDict[key: version]
                .Item2;
        }

        if (attributeType == typeof(double))
        {
            IDictionary<AttributeVersion, Tuple<double, bool>> doubleDict = (IDictionary<AttributeVersion, Tuple<double, bool>>)avc.ValueDict;
            Tuple<double, bool> doubleValue = doubleDict[key: version];
            return doubleDict[key: version]
                .Item2;
        }

        if (attributeType == typeof(DateTime))
        {
            IDictionary<AttributeVersion, Tuple<DateTime, bool>> DateTimeDict = (IDictionary<AttributeVersion, Tuple<DateTime, bool>>)avc.ValueDict;
            Tuple<DateTime, bool> doubleValue = DateTimeDict[key: version];
            return DateTimeDict[key: version]
                .Item2;
        }

        // else
        // Should not get to here
        throw new ArgumentException(message: $"Failed to retrieve attribute '{GetAttributeName(attribute: attribute)}" +
                                             $"' of type '{attributeType.Name}");
    }

    /// <summary>
    ///     Returns an attribute in string format. If it is a number,
    ///     a localized conversion is done.
    /// </summary>
    /// <param name="attribute">The attribute to return the value for</param>
    /// <param name="version">The version to return or null if latest version</param>
    /// <param name="notFoundValue">The value to return if no suitable value was found</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException"></exception>
    public string GetAttributeValueString(ElementAttribute attribute,
                                          AttributeVersion? version = null,
                                          string? notFoundValue = null)
    {
        if (!_Attributes.ContainsKey(key: attribute))
        {
            return notFoundValue;
        }

        AttributeValueContainer avc = _Attributes[key: attribute];
        Type attributeType = avc.MyValueType;

        AttributeVersion? versionCheck = CheckWhichVersion(avc: avc, versionRequested: version);
        if (versionCheck == null)
        {
            return notFoundValue;
        }

        AttributeVersion versionToReturn = (AttributeVersion)versionCheck;

        // Retrieve and return value
        // no need for else-if because the 'return' terminates the loop

        // the logic with the if (strDict[key: versionToReturn].Item2) {return FrmMainApp.NullStringEquivalentGeneric;}
        // ... is that if something is marked as "to remove" then we want to show a blank value.

        if (attributeType == typeof(string))
        {
            IDictionary<AttributeVersion, Tuple<string, bool>> strDict = (IDictionary<AttributeVersion, Tuple<string, bool>>)avc.ValueDict;
            Tuple<string, bool> intValue = strDict[key: versionToReturn];
            if (strDict[key: versionToReturn]
                .Item2)
            {
                return FrmMainApp.NullStringEquivalentGeneric;
            }

            return intValue.Item1;
        }

        if (attributeType == typeof(int))
        {
            IDictionary<AttributeVersion, Tuple<int, bool>> intDict = (IDictionary<AttributeVersion, Tuple<int, bool>>)avc.ValueDict;
            Tuple<int, bool> intValue = intDict[key: versionToReturn];
            if (intDict[key: versionToReturn]
                .Item2)
            {
                return FrmMainApp.NullStringEquivalentGeneric;
            }

            return intValue.Item1.ToString(provider: CultureInfo.InvariantCulture);
        }

        if (attributeType == typeof(double))
        {
            IDictionary<AttributeVersion, Tuple<double, bool>> doubleDict = (IDictionary<AttributeVersion, Tuple<double, bool>>)avc.ValueDict;
            Tuple<double, bool> doubleValue = doubleDict[key: versionToReturn];
            if (doubleDict[key: versionToReturn]
                .Item2)
            {
                return FrmMainApp.NullStringEquivalentGeneric;
            }

            return doubleValue.Item1.ToString(provider: CultureInfo.InvariantCulture);
        }

        if (attributeType == typeof(DateTime))
        {
            IDictionary<AttributeVersion, Tuple<DateTime, bool>> dateTimeDict = (IDictionary<AttributeVersion, Tuple<DateTime, bool>>)avc.ValueDict;
            Tuple<DateTime, bool> dateTimeValue = dateTimeDict[key: versionToReturn];
            if (dateTimeDict[key: versionToReturn]
                .Item2)
            {
                return FrmMainApp.NullStringEquivalentGeneric;
            }

            return dateTimeValue.Item1.ToString(provider: CultureInfo.CurrentUICulture);
        }

        // else
        // Should not get to here
        throw new ArgumentException(message: $"Failed to retrieve attribute '{GetAttributeName(attribute: attribute)}" +
                                             $"' of type '{attributeType.Name}" +
                                             "' by requesting its value with type 'string' due to conversion issues.");
    }


    /// <summary>
    ///     Returns the value of an attribute as the given generic.
    ///     If the attribute has a different type than the generic, an exception is thrown.
    ///     (The attribute type is taken from SourcesAndAttributes.GetAttributeType().
    ///     If the version needed is given only that version is checked for and returned.
    ///     If no version is given, the latest version (ie. modified) is returned if
    ///     it exists, otherwise original.
    ///     Item1 is the actual value we want. Item2 is the bool flag for the deletion mark.
    /// </summary>
    /// <param name="attribute">The attribute to return the value for</param>
    /// <param name="version">The version to return or null if latest version</param>
    /// <param name="notFoundValue">The value to return if no suitable value was found</param>
    /// <returns></returns>
    public T? GetAttributeValue<T>(ElementAttribute attribute,
                                   AttributeVersion version,
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
            throw new ArgumentException(message: $"Failed to retrieve attribute '{GetAttributeName(attribute: attribute)}" +
                                                 $"' of type '{attributeType.Name}" +
                                                 $"' due to requesting with incompatible return type '{requestType.Name}'.");
        }

        AttributeVersion? versionCheck = CheckWhichVersion(avc: avc, versionRequested: version);
        if (versionCheck == null)
        {
            return notFoundValue;
        }

        AttributeVersion versionToReturn = (AttributeVersion)versionCheck;

        // Retrieve and return value
        if (attributeType == typeof(int))
        {
            IDictionary<AttributeVersion, Tuple<int, bool>> intDict = (IDictionary<AttributeVersion, Tuple<int, bool>>)avc.ValueDict;
            int intValue = intDict[key: versionToReturn]
                .Item1;
            if (requestType == typeof(int))
            {
                return (T)Convert.ChangeType(value: intValue, conversionType: typeof(T));
            }
        }
        else if (attributeType == typeof(double))
        {
            IDictionary<AttributeVersion, Tuple<double, bool>> doubleDict = (IDictionary<AttributeVersion, Tuple<double, bool>>)avc.ValueDict;
            double doubleValue = doubleDict[key: versionToReturn]
                .Item1;
            if (requestType == typeof(double))
            {
                return (T)Convert.ChangeType(value: doubleValue, conversionType: typeof(T));
            }
        }
        else if (attributeType == typeof(DateTime))
        {
            IDictionary<AttributeVersion, Tuple<DateTime, bool>> DateTimeDict = (IDictionary<AttributeVersion, Tuple<DateTime, bool>>)avc.ValueDict;
            DateTime DateTimeValue = DateTimeDict[key: versionToReturn]
                .Item1;
            if (requestType == typeof(DateTime))
            {
                return (T)Convert.ChangeType(value: DateTimeValue, conversionType: typeof(T));
            }
        }

        // Should not get to here
        throw new ArgumentException(message: $"Failed to retrieve attribute '{GetAttributeName(attribute: attribute)}" +
                                             $"' of type '{attributeType.Name}" +
                                             $"' by requesting its value with type '{requestType.Name}' due to conversion issues.");
    }

    /// <summary>
    ///     Sets the value for the given attribute - without needing to specify the Type.
    /// </summary>
    /// <param name="attribute">The attribute to set the value for</param>
    /// <param name="value">The value to set (as string)</param>
    /// <param name="version">The version to set it with </param>
    /// <param name="isMarkedForDeletion">Whether this attribute is set for deletion/removal</param>
    public void SetAttributeValueAnyType(ElementAttribute attribute,
                                         string value,
                                         AttributeVersion version,
                                         bool isMarkedForDeletion)
    {
        Type typeOfAttribute = GetAttributeType(attribute: attribute);
        IConvertible writeValueConvertible = null;

        if (typeOfAttribute == typeof(string))
        {
            SetAttributeValue(attribute: attribute,
                              value: value,
                              version: version,
                              isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (typeOfAttribute == typeof(double))
        {
            writeValueConvertible = HelperGenericTypeOperations.TryParseNullableDouble(val: value) ?? FrmMainApp.NullDoubleEquivalent;
            SetAttributeValue(attribute: attribute,
                              value: writeValueConvertible,
                              version: version,
                              isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (typeOfAttribute == typeof(int))
        {
            writeValueConvertible = HelperGenericTypeOperations.TryParseNullableInt(val: value) ?? FrmMainApp.NullIntEquivalent;
            SetAttributeValue(attribute: attribute,
                              value: writeValueConvertible,
                              version: version,
                              isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (typeOfAttribute == typeof(DateTime))
        {
            writeValueConvertible = HelperGenericTypeOperations.TryParseNullableDateTime(val: value) ?? FrmMainApp.NullDateTimeEquivalent;
            SetAttributeValue(attribute: attribute,
                              value: writeValueConvertible,
                              version: version,
                              isMarkedForDeletion: isMarkedForDeletion);
        }
        else
        {
            throw new ArgumentException(message: "Trying to get attribute name of unknown attribute with value " + attribute);
        }
    }

    /// <summary>
    ///     Sets the value for the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to set the value for</param>
    /// <param name="value">The value to set (as string)</param>
    /// <param name="version">The version to set it with </param>
    /// <param name="isMarkedForDeletion">Whether this attribute is set for deletion/removal</param>
    public void SetAttributeValue(ElementAttribute attribute,
                                  IConvertible value,
                                  AttributeVersion version,
                                  bool isMarkedForDeletion)
    {
        Type attributeType = GetAttributeType(attribute: attribute);
        if (!isMarkedForDeletion && value != null)
        {
            if (attributeType != value.GetType())
            {
                throw new ArgumentException(message: $"Error, while trying to set the attribute {GetAttributeName(attribute: attribute)}" +
                                                     $" of item '{ItemNameWithoutPath}'. The type '{value.GetType().Name}' of the value to set  does not contain " +
                                                     $"match the expected type '{attributeType.Name}'.");
            }
        }

        // update attribute if it exists       
        if (_Attributes.ContainsKey(key: attribute))
        {
            bool setMarkedForDeletion = value == null || string.IsNullOrWhiteSpace(value: value.ToString());

            if (attributeType == typeof(double))
            {
                if (setMarkedForDeletion)
                {
                    _Attributes[key: attribute]
                        .ValueDict[key: version] = new Tuple<double, bool>(item1: FrmMainApp.NullDoubleEquivalent, item2: isMarkedForDeletion);
                }
                else
                {
                    _Attributes[key: attribute]
                        .ValueDict[key: version] = new Tuple<double, bool>(item1: (double)value, item2: isMarkedForDeletion);
                }
            }
            else if (attributeType == typeof(int))
            {
                if (setMarkedForDeletion)
                {
                    _Attributes[key: attribute]
                        .ValueDict[key: version] = new Tuple<int, bool>(item1: FrmMainApp.NullIntEquivalent, item2: isMarkedForDeletion);
                }
                else
                {
                    _Attributes[key: attribute]
                        .ValueDict[key: version] = new Tuple<int, bool>(item1: (int)value, item2: isMarkedForDeletion);
                }
            }
            else if (attributeType == typeof(DateTime))
            {
                if (setMarkedForDeletion)
                {
                    _Attributes[key: attribute]
                        .ValueDict[key: version] = new Tuple<DateTime, bool>(item1: FrmMainApp.NullDateTimeEquivalent, item2: isMarkedForDeletion);
                }
                else
                {
                    _Attributes[key: attribute]
                        .ValueDict[key: version] = new Tuple<DateTime, bool>(item1: (DateTime)value, item2: isMarkedForDeletion);
                }
            }
            else if (attributeType == typeof(string))
            {
                if (setMarkedForDeletion)
                {
                    _Attributes[key: attribute]
                        .ValueDict[key: version] = new Tuple<string, bool>(item1: FrmMainApp.NullStringEquivalentGeneric, item2: isMarkedForDeletion);
                }
                else
                {
                    _Attributes[key: attribute]
                        .ValueDict[key: version] = new Tuple<string, bool>(item1: value.ToString(), item2: isMarkedForDeletion);
                }
            }
            else
            {
                throw new ArgumentException(message: "Trying to get attribute name of unknown attribute with value " + attribute);
            }

            return;
        }

        // add new attribute if doesn't exist. this can happen when the file/attrib has been cleaned in the past or was empty to start with
        AttributeValueContainer avc;

        // regarding settings defaults: this is bit of a safety thing here. Value can be NULL when user presses "clear all" on something that already doesn't have a value and/or is marked for deletion
        // in that case dummy values are acceptable because they won't be actually recorded anyway.

        if (attributeType == typeof(double))
        {
            value ??= FrmMainApp.NullDoubleEquivalent;
            avc = new AttributeValuesDouble(initialValue: (double)value, initialVersion: version, isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (attributeType == typeof(int))
        {
            value ??= FrmMainApp.NullIntEquivalent;
            avc = new AttributeValuesInt(initialValue: (int)value, initialVersion: version, isMarkedForDeletion: isMarkedForDeletion);
        }
        else if (attributeType == typeof(DateTime))
        {
            value ??= FrmMainApp.NullDateTimeEquivalent;
            avc = new AttributeValuesDateTime(initialValue: (DateTime)value, initialVersion: version, isMarkedForDeletion: isMarkedForDeletion);
        }
        else
        {
            value ??= FrmMainApp.NullStringEquivalentGeneric;
            avc = new AttributeValuesString(initialValue: (string)value, initialVersion: version, isMarkedForDeletion: isMarkedForDeletion);
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
        catch (Exception e)
        {
            // ignore
        }
    }

    #endregion

    #region Members for Parsing attribute values out of a tag list

    /// <summary>
    ///     Searches the given tag list to yield the value for the given attribute.
    ///     Hereby the TagsToAttributesOrder list is used to determine which
    ///     tags are taken in which priority for the attribute.
    /// </summary>
    /// <param name="attribute">The attribute to find the value for</param>
    /// <param name="tags">The tag list to parse</param>
    /// <returns>A touple (name of tag chosen, value)</returns>
    private (string, string) GetDataPointFromTags(ElementAttribute attribute,
                                                  IDictionary<string, string> tags)
    {
        Logger.Trace(message: "Starting to parse dict for attribute: " + GetAttributeName(attribute: attribute));
        List<ElementAttribute> ignoreElementAttributes = new()
        {
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
            ElementAttribute.RemoveAllGPS
        };

        if (!ignoreElementAttributes.Contains(item: attribute))
        {
            if (!TagsToAttributesOrder.ContainsKey(key: attribute))
            {
                throw new ArgumentException(message: "Error, while trying to parse the dictionary of item '" +
                                                     $"{ItemNameWithoutPath}' for attribute '{GetAttributeName(attribute: attribute)}" +
                                                     "': The TagsToAttributesOrder does not contain a definition of which tags to use for this attribute.");
            }

            List<string> orderedTags = TagsToAttributesOrder[key: attribute];
            for (int i = 0; i < orderedTags.Count; i++)
            {
                if (tags.ContainsKey(key: orderedTags[index: i]
                                         .ToUpper()))
                {
                    Logger.Trace(message: $"Parse dict for attribute: '{GetAttributeName(attribute: attribute)}" +
                                          $"' yielded value '{tags[key: orderedTags[index: i].ToUpper()]}'");
                    return (orderedTags[index: i], tags[key: orderedTags[index: i]
                                                            .ToUpper()]);
                }
            }
        }

        Logger.Trace(message: $"Parse dict for attribute: '{GetAttributeName(attribute: attribute)}" +
                              "' yielded no value.");
        return (null, null);
    }

    /// <summary>
    ///     Handles retrieving the value for the given attribute into the
    ///     temporary value list.
    ///     If required (due to dependencies for conversion), another value
    ///     retrieval is triggered. All results are put into the temporary
    ///     lists.
    ///     Transformations are done if needed - they are kept separately in TagsToModelValueTransformations.
    /// </summary>
    /// <param name="attribute">The attribute to retrieve the value for</param>
    /// <param name="parsedValues">The list of already parsed values (in case another value is needed)</param>
    /// <param name="parsedFails">The list of attributes, parsing failed for</param>
    /// <param name="tags">The tags provided to retrieve the value from</param>
    /// <param name="callDepth">The recursive call depth to allow for tracking of loops</param>
    /// <returns>True if parsing succeeded (resulting values are put into the passed lists).</returns>
    private bool ParseAttribute(ElementAttribute attribute,
                                IDictionary<ElementAttribute, IConvertible> parsedValues,
                                List<ElementAttribute> parsedFails,
                                IDictionary<string, string> tags,
                                int callDepth)
    {
        Logger.Trace(message: $"Parse attribute '{GetAttributeName(attribute: attribute)}' at depth {callDepth.ToString()}...");
        if (parsedFails.Contains(item: attribute))
        {
            return false;
        }

        if (callDepth > 10)
        {
            throw new InvalidOperationException(message: $"Reached max call depth of '{callDepth.ToString()}" +
                                                         $"' while parsing attribute '{GetAttributeName(attribute: attribute)}'.");
        }

        callDepth++;
        (string chosenTag, string parseResult) = GetDataPointFromTags(attribute: attribute, tags: tags);

        #region Create a history

        // TakenDate & CreateDate have to be sent into their
        // respective tables for querying later if user chooses time-shift.
        // TODO: replace logic with AttributeVersion concept
        try
        {
            if (parseResult != null)
            {
                switch (attribute)
                {
                    case ElementAttribute.TakenDate:
                        if (parseResult.Contains("0000"))
                        {
                            return false;
                        }

                        FrmMainApp.OriginalTakenDateDict[key: ItemNameWithoutPath] =
                            DateTime.Parse(s: parseResult, provider: CultureInfo.CurrentUICulture)
                                .ToString(provider: CultureInfo.CurrentUICulture);
                        break;

                    case ElementAttribute.CreateDate:
                        if (parseResult.Contains("0000"))
                        {
                            return false;
                        }

                        FrmMainApp.OriginalCreateDateDict[key: ItemNameWithoutPath] =
                            DateTime.Parse(s: parseResult, provider: CultureInfo.CurrentUICulture)
                                .ToString(provider: CultureInfo.CurrentUICulture);
                        break;
                }
                // Not adding the xmp here because the current code logic would pull a "unified" data point.

                // Add to list of file attributes seen
                // TODO: Understand where this is used and check how the model can support this
                DataRow dr = FrmMainApp.DtFileDataSeenInThisSession.NewRow();
                dr[columnName: "fileNameWithPath"] = FileNameWithPath;
                dr[columnName: "settingId"] = GetAttributeName(attribute: attribute);
                dr[columnName: "settingValue"] = parseResult;
                FrmMainApp.DtFileDataSeenInThisSession.Rows.Add(row: dr);
            }
        }
        catch
        {
            Logger.Error(message: $"Parse attribute failed '{GetAttributeName(attribute: attribute)}' at depth {callDepth.ToString()}...");
            return false; // be triple sure here.
        }

        #endregion

        // If needed, transform the attribute
        IConvertible resTyped = null;
        switch (attribute)
        {
            case ElementAttribute.GPSAltitude:
                resTyped = TagsToModelValueTransformations.T2M_GPSAltitude(parseResult: parseResult);
                break;

            case ElementAttribute.GPSAltitudeRef:
                resTyped = TagsToModelValueTransformations.T2M_AltitudeRef(parseResult: parseResult);
                break;

            case ElementAttribute.GPSLatitude:
            case ElementAttribute.GPSDestLatitude:
            case ElementAttribute.GPSLongitude:
            case ElementAttribute.GPSDestLongitude:
                resTyped = TagsToModelValueTransformations.T2M_GPSLatLong(attribute: attribute,
                                                                          parseResult: parseResult,
                                                                          parsed_Values: parsedValues,
                                                                          ParseMissingAttribute: delegate(
                                                                              ElementAttribute atrb)
                                                                          {
                                                                              return ParseAttribute(attribute: atrb,
                                                                                                    parsedValues: parsedValues,
                                                                                                    parsedFails: parsedFails,
                                                                                                    tags: tags,
                                                                                                    callDepth: callDepth);
                                                                          });
                break;

            case ElementAttribute.ExposureTime:
                resTyped = TagsToModelValueTransformations.T2M_ExposureTime(parseResult: parseResult);
                break;

            case ElementAttribute.Fnumber:
            case ElementAttribute.FocalLength:
            case ElementAttribute.FocalLengthIn35mmFormat:
                resTyped = TagsToModelValueTransformations.T2M_F_FocalLength(attribute: attribute, parseResult: parseResult);
                break;

            case ElementAttribute.ISO:
                resTyped = TagsToModelValueTransformations.T2M_F_ISO(attribute: attribute, parseResult: parseResult);
                break;

            case ElementAttribute.TakenDate:
            case ElementAttribute.CreateDate:
                resTyped = TagsToModelValueTransformations.T2M_TakenCreatedDate(parseResult: parseResult);
                break;

            default:
                // Just take the string
                resTyped = parseResult;
                break;
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
        Logger.Trace(message: $"Parse dict for item '{ItemNameWithoutPath}'...");
        IEnumerable<ElementAttribute> possibleAttributes = (IEnumerable<ElementAttribute>)Enum.GetValues(enumType: typeof(ElementAttribute));

        // Create an upper-case capitalised version
        IDictionary<string, string> tags = new Dictionary<string, string>();
        foreach (KeyValuePair<string, string> kvp in dictTagsIn)
        {
            tags[key: kvp.Key.ToUpper()] = kvp.Value;
        }

        // First, clear previous values
        _Attributes.Clear();

        // Temporary store for parsed values.
        // Allows recursive parsing if one attribute requires another one
        // to be already parsed without publishing the results already.
        IDictionary<ElementAttribute, IConvertible> parsedValues = new Dictionary<ElementAttribute, IConvertible>();
        // List of missing attributes (must be separate, as IConvertible
        // is not nullable
        List<ElementAttribute> parsedFails = new();

        // Parse values into temp. store
        foreach (ElementAttribute attribute in possibleAttributes)
        {
            // If it was already parsed due to another attrib needing it, do not parse again
            if (!parsedValues.ContainsKey(key: attribute))
            {
                ParseAttribute(attribute: attribute,
                               parsedValues: parsedValues,
                               parsedFails: parsedFails,
                               tags: tags,
                               callDepth: 0);
            }
        }

        // Add all parsed attributes to the published list
        foreach (ElementAttribute attribute in parsedValues.Keys)
        {
            SetAttributeValue(attribute: attribute,
                              value: parsedValues[key: attribute],
                              version: AttributeVersion.Original, isMarkedForDeletion: false);
        }

        Logger.Trace(message: $"Parse dict for item '{ItemNameWithoutPath}' - OK");
    }

    #endregion
}