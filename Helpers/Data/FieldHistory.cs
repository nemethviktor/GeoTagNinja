using NLog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Linq;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Remembers what was last typed into the free-text fields of the Edit Data form (artist, credit, keywords, ...),
///     so the field's drop-down and its "Most Recently Used Items" menu can offer it again.
/// </summary>
/// <remarks>
///     Nothing here is essential, so a database problem is logged and swallowed rather than allowed to get in the way
///     of saving the user's edit.
/// </remarks>
internal static class FieldHistory
{
    /// <summary>How many values are kept per field; the oldest go first.</summary>
    internal const int MaxPerAttribute = 20;

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static SQLiteConnection Open()
    {
        SQLiteConnection connection = new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        connection.Open();

        // a database created before this table existed gets it the first time it is needed
        using SQLiteCommand create = new(
            commandText: DatabaseSchema.GetCreateTableStatement(tableName: DatabaseSchema.TableNameFieldHistory),
            connection: connection);
        _ = create.ExecuteNonQuery();
        return connection;
    }

    /// <summary>The remembered values of a field, most recently used first.</summary>
    /// <param name="attributeName">The attribute's name, as in <c>SourcesAndAttributes</c>.</param>
    internal static IReadOnlyList<string> Read(string attributeName)
    {
        List<string> values = [];
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: """
                             SELECT fieldValue FROM FieldHistory
                             WHERE attributeName = @attributeName
                             ORDER BY lastUsed DESC
                             LIMIT @limit;
                             """,
                connection: connection);
            _ = command.Parameters.AddWithValue(parameterName: "@attributeName", value: attributeName);
            _ = command.Parameters.AddWithValue(parameterName: "@limit", value: MaxPerAttribute);

            using SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                values.Add(item: reader.GetString(i: 0));
            }
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: $"Could not read the history of '{attributeName}'");
        }

        return values;
    }

    /// <summary>Removes one remembered value, e.g. a typo or a test entry, so it is no longer offered.</summary>
    /// <param name="attributeName">The attribute's name, as in <c>SourcesAndAttributes</c>.</param>
    /// <param name="value">The value to forget, exactly as listed.</param>
    internal static void Forget(string attributeName,
                                string value)
    {
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: "DELETE FROM FieldHistory WHERE attributeName = @attributeName AND fieldValue = @fieldValue;",
                connection: connection);
            _ = command.Parameters.AddWithValue(parameterName: "@attributeName", value: attributeName);
            _ = command.Parameters.AddWithValue(parameterName: "@fieldValue", value: value);
            _ = command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: $"Could not forget a value of '{attributeName}'");
        }
    }

    /// <summary>Notes values as just used, so they head the field's list. Blank values and the blank marker are ignored.</summary>
    /// <param name="attributeName">The attribute's name, as in <c>SourcesAndAttributes</c>.</param>
    /// <param name="values">The values, in the order they should rank (the last one becomes the newest).</param>
    internal static void Remember(string attributeName,
                                  IEnumerable<string> values)
    {
        string[] usable = values.Select(selector: value => value?.Trim())
                                .Where(predicate: value => !string.IsNullOrEmpty(value: value) &&
                                                           value != Model.NullEquivalents.StringGeneric)
                                .ToArray();
        if (usable.Length == 0)
        {
            return;
        }

        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteTransaction transaction = connection.BeginTransaction();

            long ticks = DateTime.UtcNow.Ticks;
            foreach (string value in usable)
            {
                using SQLiteCommand upsert = new(
                    commandText: """
                                 INSERT OR REPLACE INTO FieldHistory (attributeName, fieldValue, lastUsed)
                                 VALUES (@attributeName, @fieldValue, @lastUsed);
                                 """,
                    connection: connection,
                    transaction: transaction);
                _ = upsert.Parameters.AddWithValue(parameterName: "@attributeName", value: attributeName);
                _ = upsert.Parameters.AddWithValue(parameterName: "@fieldValue", value: value);
                _ = upsert.Parameters.AddWithValue(parameterName: "@lastUsed", value: ticks++);
                _ = upsert.ExecuteNonQuery();
            }

            using SQLiteCommand trim = new(
                commandText: """
                             DELETE FROM FieldHistory
                             WHERE attributeName = @attributeName
                               AND fieldValue NOT IN (
                                   SELECT fieldValue FROM FieldHistory
                                   WHERE attributeName = @attributeName
                                   ORDER BY lastUsed DESC
                                   LIMIT @limit);
                             """,
                connection: connection,
                transaction: transaction);
            _ = trim.Parameters.AddWithValue(parameterName: "@attributeName", value: attributeName);
            _ = trim.Parameters.AddWithValue(parameterName: "@limit", value: MaxPerAttribute);
            _ = trim.ExecuteNonQuery();

            transaction.Commit();
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: $"Could not save the history of '{attributeName}'");
        }
    }
}
