using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace GeoTagNinja.Model;

/// <summary>
///     An immutable, ordered list of text items - the CLR value of a multi-value metadata tag such as
///     <c>Keywords</c> or <c>SupplementalCategories</c>, where every item must reach the file as its own entry.
/// </summary>
/// <remarks>
///     <para>
///         It implements <see cref="IConvertible" /> only so it can live in the model beside the scalar types
///         (<c>AttributeValues&lt;T&gt;</c> is constrained to <see cref="IConvertible" />). The only conversion it
///         supports is to <see cref="string" />, which is the canonical text form: the items joined by
///         <see cref="Separator" />, or <see cref="NullEquivalents.StringGeneric" /> when the list is empty, so that
///         a blank list looks like every other blank string to the form and the file list.
///     </para>
///     <para>
///         Items are trimmed, blanks are dropped and duplicates are removed with an <b>ordinal</b>,
///         case-sensitive comparison (never culture-sensitive: a Turkish <c>I</c> must not merge two keywords).
///         First occurrence wins, so the order the user typed is kept. An item cannot contain a line break, because
///         the line break is the separator of the canonical text form.
///     </para>
/// </remarks>
internal sealed class TagList : IConvertible, IEquatable<TagList>, IReadOnlyList<string>
{
    /// <summary>The separator of the canonical text form. Items cannot contain it.</summary>
    internal const char Separator = '\n';

    /// <summary>The list with no items.</summary>
    internal static readonly TagList Empty = new(items: []);

    private readonly string[] _items;

    private TagList(string[] items)
    {
        _items = items;
    }

    /// <summary>Builds a list from items, normalising them as described in the type remarks.</summary>
    /// <param name="items">The raw items; null entries and blanks are dropped.</param>
    internal static TagList From(IEnumerable<string?>? items)
    {
        if (items == null)
        {
            return Empty;
        }

        List<string> cleaned = [];
        HashSet<string> seen = new(comparer: StringComparer.Ordinal);

        foreach (string? raw in items)
        {
            // A line break inside an item would split it in two on the way back through the text form, so treat
            // it as a separator now rather than discover that later.
            foreach (string part in (raw ?? string.Empty).Split(separator: ['\r', '\n'],
                         options: StringSplitOptions.RemoveEmptyEntries))
            {
                string item = part.Trim();
                if (item.Length > 0 &&
                    seen.Add(item: item))
                {
                    cleaned.Add(item: item);
                }
            }
        }

        return cleaned.Count == 0
            ? Empty
            : new TagList(items: cleaned.ToArray());
    }

    /// <summary>
    ///     Parses the canonical text form (line-separated items, or the blank sentinel) back into a list. This is the
    ///     inverse of <see cref="ToString()" />.
    /// </summary>
    internal static TagList Parse(string? text)
    {
        if (string.IsNullOrWhiteSpace(value: text) ||
            text.Trim() == NullEquivalents.StringGeneric)
        {
            return Empty;
        }

        return From(items: text.Split(separator: ['\r', '\n'], options: StringSplitOptions.RemoveEmptyEntries));
    }

    /// <summary>
    ///     Parses the text ExifTool prints for a multi-value tag when run with <c>-struct -args</c>, which is how the
    ///     read pass runs it: a bracketed list, <c>[one,two,three]</c>, in which a literal <c>|</c>, <c>,</c>, <c>[</c>
    ///     or <c>]</c> inside an item is escaped with a leading <c>|</c>. Single-item lists are bracketed too.
    /// </summary>
    /// <remarks>
    ///     Text that is not bracketed (a tag that was stored as a plain string, or output from a run without
    ///     <c>-struct</c>) is taken as one item per line, or as a single item - commas are deliberately not guessed
    ///     at, since a keyword may contain one and nothing in that output says which commas are separators.
    /// </remarks>
    internal static TagList ParseExifToolOutput(string? text)
    {
        if (string.IsNullOrWhiteSpace(value: text))
        {
            return Empty;
        }

        string trimmed = text.Trim();
        if (trimmed.Length < 2 ||
            trimmed[index: 0] != '[' ||
            trimmed[index: trimmed.Length - 1] != ']' ||
            IsEscaped(text: trimmed, index: trimmed.Length - 1))
        {
            return From(items: trimmed.Split(separator: ['\r', '\n'], options: StringSplitOptions.RemoveEmptyEntries));
        }

        List<string> items = [];
        System.Text.StringBuilder current = new();
        for (int i = 1; i < trimmed.Length - 1; i++)
        {
            char c = trimmed[index: i];
            if (c == '|' &&
                i + 1 < trimmed.Length - 1)
            {
                _ = current.Append(value: trimmed[index: ++i]);
            }
            else if (c == ',')
            {
                items.Add(item: current.ToString());
                _ = current.Clear();
            }
            else
            {
                _ = current.Append(value: c);
            }
        }

        items.Add(item: current.ToString());
        return From(items: items);
    }

    /// <summary>Whether the character at <paramref name="index" /> is preceded by an odd number of escape bars.</summary>
    private static bool IsEscaped(string text,
                                  int index)
    {
        int bars = 0;
        for (int i = index - 1; i >= 0 && text[index: i] == '|'; i--)
        {
            bars++;
        }

        return bars % 2 == 1;
    }

    /// <summary>Whether the list has no items.</summary>
    internal bool IsEmpty => _items.Length == 0;

    /// <summary>A copy of this list with an item added at the end, unless it is blank or already there.</summary>
    internal TagList With(string? item)
    {
        return From(items: _items.Concat(second: [item]));
    }

    /// <summary>A copy of this list without the given item (ordinal match).</summary>
    internal TagList Without(string item)
    {
        return From(items: _items.Where(predicate: existing => !string.Equals(a: existing, b: item, comparisonType: StringComparison.Ordinal)));
    }

    /// <summary>The items joined with <paramref name="separator" />, for showing in a single cell.</summary>
    internal string Join(string separator)
    {
        return string.Join(separator: separator, value: _items);
    }

    #region IReadOnlyList

    public int Count => _items.Length;

    public string this[int index] => _items[index];

    public IEnumerator<string> GetEnumerator()
    {
        return ((IEnumerable<string>)_items).GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    #endregion

    #region Equality and text

    public bool Equals(TagList? other)
    {
        return other != null &&
               _items.SequenceEqual(second: other._items, comparer: StringComparer.Ordinal);
    }

    public override bool Equals(object? obj)
    {
        return Equals(other: obj as TagList);
    }

    public override int GetHashCode()
    {
        int hash = 17;
        foreach (string item in _items)
        {
            hash = unchecked((hash * 31) + StringComparer.Ordinal.GetHashCode(obj: item));
        }

        return hash;
    }

    /// <summary>The canonical text form: items joined by <see cref="Separator" />, or the blank sentinel.</summary>
    public override string ToString()
    {
        return _items.Length == 0
            ? NullEquivalents.StringGeneric
            : string.Join(separator: Separator.ToString(), value: _items);
    }

    #endregion

    #region IConvertible - string only

    public TypeCode GetTypeCode()
    {
        return TypeCode.Object;
    }

    public string ToString(IFormatProvider? provider)
    {
        return ToString();
    }

    public object ToType(Type conversionType,
                         IFormatProvider? provider)
    {
        if (conversionType == typeof(string))
        {
            return ToString();
        }

        if (conversionType == typeof(TagList))
        {
            return this;
        }

        throw new InvalidCastException(message: $"A {nameof(TagList)} cannot be converted to {conversionType.Name}.");
    }

    private static InvalidCastException NotSupported(string target)
    {
        return new InvalidCastException(message: $"A {nameof(TagList)} cannot be converted to {target}.");
    }

    public bool ToBoolean(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Boolean));
    }

    public byte ToByte(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Byte));
    }

    public char ToChar(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Char));
    }

    public DateTime ToDateTime(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(DateTime));
    }

    public decimal ToDecimal(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Decimal));
    }

    public double ToDouble(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Double));
    }

    public short ToInt16(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Int16));
    }

    public int ToInt32(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Int32));
    }

    public long ToInt64(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Int64));
    }

    public sbyte ToSByte(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(SByte));
    }

    public float ToSingle(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(Single));
    }

    public ushort ToUInt16(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(UInt16));
    }

    public uint ToUInt32(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(UInt32));
    }

    public ulong ToUInt64(IFormatProvider? provider)
    {
        throw NotSupported(target: nameof(UInt64));
    }

    #endregion
}
