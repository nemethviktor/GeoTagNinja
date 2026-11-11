using GeoTagNinja.Helpers.FileSystem;
using NLog;
using System;
using System.Collections.Generic;
using System.Data.SQLite;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Named Batch Rename setups ("presets") kept in the settings database, so a naming scheme (folder name, a
///     separator, a sequence number...) can be used again with one click.
/// </summary>
internal static class BatchRenamePresets
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static SQLiteConnection Open()
    {
        SQLiteConnection connection = new(connectionString: $"Data Source={HelperVariables.SettingsDatabaseFilePath}");
        connection.Open();

        // a database created before this table existed gets it the first time it is needed
        using SQLiteCommand create = new(
            commandText: DatabaseSchema.GetCreateTableStatement(tableName: DatabaseSchema.TableNameBatchRenamePresets),
            connection: connection);
        _ = create.ExecuteNonQuery();
        return connection;
    }

    /// <summary>The names of all saved presets, alphabetically.</summary>
    internal static IReadOnlyList<string> Names()
    {
        List<string> names = [];
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: "SELECT presetName FROM BatchRenamePresets ORDER BY presetName COLLATE NOCASE;",
                connection: connection);
            using SQLiteDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                names.Add(item: reader.GetString(i: 0));
            }
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: "Could not list the batch rename presets");
        }

        return names;
    }

    /// <summary>One preset's settings; null if there is no such preset or it cannot be read.</summary>
    internal static BatchRenameSettings Read(string presetName)
    {
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: "SELECT presetJson FROM BatchRenamePresets WHERE presetName = @presetName;",
                connection: connection);
            _ = command.Parameters.AddWithValue(parameterName: "@presetName", value: presetName);
            object json = command.ExecuteScalar();
            return json is string text
                ? BatchRenameSettings.FromJson(json: text)
                : null;
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: $"Could not read the batch rename preset '{presetName}'");
            return null;
        }
    }

    /// <summary>Saves a preset, replacing any preset of the same name.</summary>
    /// <returns>False if it could not be saved.</returns>
    internal static bool Write(string presetName,
                               BatchRenameSettings settings)
    {
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: "INSERT OR REPLACE INTO BatchRenamePresets (presetName, presetJson) VALUES (@presetName, @presetJson);",
                connection: connection);
            _ = command.Parameters.AddWithValue(parameterName: "@presetName", value: presetName);
            _ = command.Parameters.AddWithValue(parameterName: "@presetJson", value: settings.ToJson());
            _ = command.ExecuteNonQuery();
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex, message: $"Could not save the batch rename preset '{presetName}'");
            return false;
        }
    }

    /// <summary>Deletes a preset.</summary>
    internal static void Delete(string presetName)
    {
        try
        {
            using SQLiteConnection connection = Open();
            using SQLiteCommand command = new(
                commandText: "DELETE FROM BatchRenamePresets WHERE presetName = @presetName;",
                connection: connection);
            _ = command.Parameters.AddWithValue(parameterName: "@presetName", value: presetName);
            _ = command.ExecuteNonQuery();
        }
        catch (Exception ex)
        {
            Log.Warn(exception: ex, message: $"Could not delete the batch rename preset '{presetName}'");
        }
    }
}
