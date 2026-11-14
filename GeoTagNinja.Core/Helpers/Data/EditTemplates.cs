using NLog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Named sets of Edit Data values ("templates") kept in the settings database, so a set of credits, keywords and
///     location names can be put onto any photo in one go.
/// </summary>
/// <remarks>
///     Values are stored as the invariant text of <see cref="Model.ValueFormatContext.RoundTrip" />, keyed by attribute
///     name, so a template means the same thing in every culture.
/// </remarks>
internal static class EditTemplates
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static SQLiteConnection Open()
    {
        SQLiteConnection connection = new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        connection.Open();

        // a database created before this table existed gets it the first time it is needed
        using SQLiteCommand create = new(
            commandText: DatabaseSchema.GetCreateTableStatement(tableName: DatabaseSchema.TableNameEditTemplates),
            connection: connection);
        _ = create.ExecuteNonQuery();
        return connection;
    }

    /// <summary>The names of all saved templates, alphabetically.</summary>
    internal static IReadOnlyList<string> Names()
    {
        List<string> names = [];
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: "SELECT DISTINCT templateName FROM EditTemplates ORDER BY templateName COLLATE NOCASE;",
                connection: connection);
            using SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                names.Add(item: reader.GetString(i: 0));
            }
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: "Could not list the edit templates");
        }

        return names;
    }

    /// <summary>The values of one template, by attribute name. Empty if there is no such template.</summary>
    internal static Dictionary<string, string> Read(string templateName)
    {
        Dictionary<string, string> values = new(comparer: StringComparer.Ordinal);
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: "SELECT attributeName, fieldValue FROM EditTemplates WHERE templateName = @templateName;",
                connection: connection);
            _ = command.Parameters.AddWithValue(parameterName: "@templateName", value: templateName);
            using SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                values[key: reader.GetString(i: 0)] = reader.GetString(i: 1);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: $"Could not read the edit template '{templateName}'");
        }

        return values;
    }

    /// <summary>Saves a template, replacing any template of the same name.</summary>
    /// <returns>False if it could not be saved.</returns>
    internal static bool Write(string templateName,
                               IReadOnlyDictionary<string, string> values)
    {
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteTransaction transaction = connection.BeginTransaction();

            using (SQLiteCommand delete = new(
                       commandText: "DELETE FROM EditTemplates WHERE templateName = @templateName;",
                       connection: connection,
                       transaction: transaction))
            {
                _ = delete.Parameters.AddWithValue(parameterName: "@templateName", value: templateName);
                _ = delete.ExecuteNonQuery();
            }

            foreach (KeyValuePair<string, string> pair in values)
            {
                using SQLiteCommand insert = new(
                    commandText: """
                                 INSERT INTO EditTemplates (templateName, attributeName, fieldValue)
                                 VALUES (@templateName, @attributeName, @fieldValue);
                                 """,
                    connection: connection,
                    transaction: transaction);
                _ = insert.Parameters.AddWithValue(parameterName: "@templateName", value: templateName);
                _ = insert.Parameters.AddWithValue(parameterName: "@attributeName", value: pair.Key);
                _ = insert.Parameters.AddWithValue(parameterName: "@fieldValue", value: pair.Value);
                _ = insert.ExecuteNonQuery();
            }

            transaction.Commit();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex, message: $"Could not save the edit template '{templateName}'");
            return false;
        }
    }

    /// <summary>Deletes a template.</summary>
    internal static void Delete(string templateName)
    {
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: "DELETE FROM EditTemplates WHERE templateName = @templateName;",
                connection: connection);
            _ = command.Parameters.AddWithValue(parameterName: "@templateName", value: templateName);
            _ = command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: $"Could not delete the edit template '{templateName}'");
        }
    }
}
