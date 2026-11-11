using GeoTagNinja.Helpers.FileSystem;
using GeoTagNinja.Helpers.Localisation;
using GeoTagNinja.Model;
using GeoTagNinja.View.FileList;
using GeoTagNinja.View.Forms;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GeoTagNinja.Tests;

/// <summary>
///     Batch Rename's text lives in Strings.resx so it can be translated. Its drop-down captions are looked up by keys
///     built from enum names, so a new enum member without an entry would show the raw key; these catch that.
/// </summary>
[TestFixture]
public class BatchRenameTextTests
{
    private static IEnumerable<string> DerivedKeys()
    {
        foreach (BatchRenameKind value in Enum.GetValues(enumType: typeof(BatchRenameKind)))
        {
            yield return $"lbl_BatchRename_Kind_{value}";
        }

        foreach (BatchRenameCase value in Enum.GetValues(enumType: typeof(BatchRenameCase)))
        {
            yield return $"lbl_BatchRename_Case_{value}";
        }

        foreach (BatchRenameFilePart value in Enum.GetValues(enumType: typeof(BatchRenameFilePart)))
        {
            yield return $"lbl_BatchRename_Part_{value}";
        }

        foreach (BatchRenameDateSource value in Enum.GetValues(enumType: typeof(BatchRenameDateSource)))
        {
            yield return $"lbl_BatchRename_DateSource_{value}";
        }

        foreach (BatchRenameSubstituteSource value in Enum.GetValues(enumType: typeof(BatchRenameSubstituteSource)))
        {
            yield return $"lbl_BatchRename_SubstituteSource_{value}";
        }

        for (int digits = 1; digits <= FrmBatchRename.MaxDigitsOffered; digits++)
        {
            yield return $"lbl_BatchRename_Digits_{digits}";
        }

        foreach (SourcesAndAttributes.ElementAttribute attribute in FileListView.BatchRenameFields)
        {
            yield return $"lbl_BatchRename_Field_{SourcesAndAttributes.GetElementAttributesName(attributeToFind: attribute)}";
        }
    }

    [TestCaseSource(nameof(DerivedKeys))]
    public void EveryDropDownCaption_HasAnEnglishEntry(string key)
    {
        Assert.That(actual: BatchRenameText.Exists(key: key), expression: Is.True, message: $"{key} is missing from Strings.resx");
    }

    [Test]
    public void EveryKindAndDate_IsOfferedInItsDropDown()
    {
        Assert.That(actual: FrmBatchRename.KindOrder, expression: Is.EquivalentTo(expected: Enum.GetValues(enumType: typeof(BatchRenameKind)).Cast<BatchRenameKind>()));
        Assert.That(actual: FrmBatchRename.DateSourceOrder, expression: Is.EquivalentTo(expected: Enum.GetValues(enumType: typeof(BatchRenameDateSource)).Cast<BatchRenameDateSource>()));
    }

    [Test]
    public void FormatArguments_AreFilledIn_AndAMissingKeyShowsAsItself()
    {
        Assert.That(actual: BatchRenameText.Get(key: "mbx_BatchRename_ManyFiles", 7), expression: Is.EqualTo(expected: "7 files will be processed"));
        Assert.That(actual: BatchRenameText.Get(key: "no_such_key"), expression: Is.EqualTo(expected: "no_such_key"));
    }
}
