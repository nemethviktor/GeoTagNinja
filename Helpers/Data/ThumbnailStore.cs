using GeoTagNinja.View.Forms;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace GeoTagNinja.Helpers.Data;

/// <summary>
///     Persistent (SQLite) tier of the thumbnail cache, so a folder opened in an earlier session doesn't have to be
///     thumbnailed again. It lives in its own file (not the settings database) so it can never bloat or endanger the
///     settings and can simply be deleted. Keys are those of <see cref="FileKeyedCache{T}" /> (path + mtime + length +
///     size), so a changed file just misses and is eventually pruned.
/// </summary>
/// <remarks>
///     <para>
///         Reads are synchronous and tiny (one blob). Writes are queued and flushed in one transaction shortly afterwards,
///         so generating thousands of thumbnails costs a handful of commits, not thousands.
///     </para>
///     <para>
///         Lifecycle: <see cref="Shutdown" /> (called when the app closes, and again from <c>ProcessExit</c> as a safety
///         net) flushes, checkpoints the write-ahead log and closes the database, leaving a single tidy file and removing
///         the "running" marker. If the marker is still there at the next start the previous session did not exit cleanly,
///         so the database is integrity-checked before use and rebuilt from scratch if it is damaged. It is a cache:
///         losing it costs time, never data.
///     </para>
///     <para>Every failure is swallowed: the worst outcome is a cache miss. After an unrecoverable failure the store is off for the session.</para>
/// </remarks>
internal static class ThumbnailStore
{
    private const int FlushDelayMilliseconds = 500;
    private const long MaxTotalBytes = 256L * 1024 * 1024;
    private static readonly TimeSpan MaxAge = TimeSpan.FromDays(value: 90);

    private static readonly object Gate = new();
    private static readonly ConcurrentQueue<KeyValuePair<string, byte[]>> PendingWrites = new();
    private static readonly ConcurrentQueue<string> PendingTouches = new();
    private static Timer? _flushTimer;
    private static SQLiteConnection? _connection;
    private static bool _disabled;
    private static bool _shutDown;
    private static bool _exitHookRegistered;

    /// <summary>Where the database lives. Settable so tests and tools can point it elsewhere before first use.</summary>
    internal static string? DatabasePathOverride { get; set; }

    private static string DatabasePath => DatabasePathOverride ??
                                          Path.Combine(path1: HelperVariables.UserDataFolderPath, path2: "thumbnailcache.sqlite");

    /// <summary>
    ///     Exists while a session has the database open and is removed on a clean <see cref="Shutdown" />; finding it at
    ///     startup therefore means the previous session crashed or was killed.
    /// </summary>
    private static string MarkerPath => DatabasePath + ".running";

    /// <summary>The stored JPEG for a key, or null.</summary>
    internal static byte[]? TryGet(string key)
    {
        lock (Gate)
        {
            SQLiteConnection? connection = Connection();
            if (connection == null)
            {
                return null;
            }

            try
            {
                using SQLiteCommand command = connection.CreateCommand();
                command.CommandText = "SELECT jpeg FROM thumbs WHERE key = @key";
                _ = command.Parameters.AddWithValue(parameterName: "@key", value: key);
                byte[]? jpeg = command.ExecuteScalar() as byte[];
                if (jpeg != null)
                {
                    PendingTouches.Enqueue(item: key);
                    ScheduleFlush();
                }

                return jpeg;
            }
            catch (Exception ex)
            {
                Disable(ex: ex);
                return null;
            }
        }
    }

    /// <summary>Queues a thumbnail for storage.</summary>
    internal static void Add(string key,
                             byte[] jpeg)
    {
        if (_disabled ||
            _shutDown)
        {
            return;
        }

        PendingWrites.Enqueue(item: new KeyValuePair<string, byte[]>(key: key, value: jpeg));
        ScheduleFlush();
    }

    /// <summary>Writes everything queued, now.</summary>
    internal static void Flush()
    {
        lock (Gate)
        {
            FlushLocked();
        }
    }

    /// <summary>
    ///     Flushes what is queued, checkpoints the write-ahead log, closes the database and removes the running marker.
    ///     The store stays off afterwards (nothing reopens it during shutdown). Safe to call more than once.
    /// </summary>
    internal static void Shutdown()
    {
        lock (Gate)
        {
            if (_shutDown)
            {
                return;
            }

            try
            {
                _flushTimer?.Dispose();
                _flushTimer = null;

                FlushLocked();

                if (_connection != null)
                {
                    try
                    {
                        using SQLiteCommand command = _connection.CreateCommand();
                        command.CommandText = "PRAGMA wal_checkpoint(TRUNCATE)";
                        _ = command.ExecuteNonQuery();
                    }
                    catch
                    {
                        // closing the connection will checkpoint anyway
                    }

                    _connection.Dispose();
                    _connection = null;
                }

                // Only a connection that was opened wrote a marker, and only a clean close earns its removal.
                if (!_disabled)
                {
                    DeleteQuietly(path: MarkerPath);
                }
            }
            catch
            {
                // shutting down: nothing useful left to do with an error
            }
            finally
            {
                _shutDown = true;
            }
        }
    }

    /// <summary>Caller holds <see cref="Gate" />.</summary>
    private static void FlushLocked()
    {
        if (PendingWrites.IsEmpty &&
            PendingTouches.IsEmpty)
        {
            return;
        }

        SQLiteConnection? connection = Connection();
        if (connection == null)
        {
            // store unavailable: drop what was queued
            while (PendingWrites.TryDequeue(result: out _))
            {
            }

            while (PendingTouches.TryDequeue(result: out _))
            {
            }

            return;
        }

        try
        {
            long now = DateTime.UtcNow.Ticks;
            using SQLiteTransaction transaction = connection.BeginTransaction();

            while (PendingWrites.TryDequeue(result: out KeyValuePair<string, byte[]> write))
            {
                using SQLiteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "INSERT OR REPLACE INTO thumbs (key, jpeg, lastUsed) VALUES (@key, @jpeg, @now)";
                _ = command.Parameters.AddWithValue(parameterName: "@key", value: write.Key);
                _ = command.Parameters.AddWithValue(parameterName: "@jpeg", value: write.Value);
                _ = command.Parameters.AddWithValue(parameterName: "@now", value: now);
                _ = command.ExecuteNonQuery();
            }

            while (PendingTouches.TryDequeue(result: out string? touched))
            {
                using SQLiteCommand command = connection.CreateCommand();
                command.Transaction = transaction;
                command.CommandText = "UPDATE thumbs SET lastUsed = @now WHERE key = @key";
                _ = command.Parameters.AddWithValue(parameterName: "@key", value: touched);
                _ = command.Parameters.AddWithValue(parameterName: "@now", value: now);
                _ = command.ExecuteNonQuery();
            }

            transaction.Commit();
        }
        catch (Exception ex)
        {
            Disable(ex: ex);
        }
    }

    private static void ScheduleFlush()
    {
        if (_shutDown)
        {
            return;
        }

        _flushTimer ??= new Timer(callback: _ => Flush(), state: null, dueTime: Timeout.Infinite, period: Timeout.Infinite);
        _ = _flushTimer.Change(dueTime: FlushDelayMilliseconds, period: Timeout.Infinite);
    }

    /// <summary>
    ///     The open connection, opening it first if needed. If opening or checking fails the database is thrown away and
    ///     rebuilt once; if that fails too the store is switched off. Caller holds <see cref="Gate" />.
    /// </summary>
    private static SQLiteConnection? Connection()
    {
        if (_disabled ||
            _shutDown)
        {
            return null;
        }

        if (_connection != null)
        {
            return _connection;
        }

        try
        {
            _connection = OpenAndPrepare();
        }
        catch (Exception first)
        {
            FrmMainApp.Log?.Warn(exception: first, message: "Thumbnail cache database unusable; rebuilding it");
            DeleteDatabaseFiles();

            try
            {
                _connection = OpenAndPrepare();
            }
            catch (Exception second)
            {
                Disable(ex: second);
                return null;
            }
        }

        RegisterExitHook();
        return _connection;
    }

    private static SQLiteConnection OpenAndPrepare()
    {
        bool previousRunWasDirty = PreviousRunWasDirty();
        _ = Directory.CreateDirectory(path: Path.GetDirectoryName(path: DatabasePath)!);

        SQLiteConnection connection = new(connectionString: $"Data Source={DatabasePath};Version=3;Journal Mode=WAL;Synchronous=Normal");
        try
        {
            connection.Open();

            if (previousRunWasDirty)
            {
                // The write-ahead log of a crashed session is replayed automatically on open; this checks what is left.
                // (Throws, or returns something other than "ok", if the file is damaged.)
                FrmMainApp.Log?.Info(message: "Previous session did not exit cleanly; checking the thumbnail cache");
                using SQLiteCommand check = connection.CreateCommand();
                check.CommandText = "PRAGMA quick_check";
                string? result = check.ExecuteScalar() as string;
                if (!string.Equals(a: result, b: "ok", comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidDataException(message: $"Thumbnail cache failed its integrity check: {result}");
                }
            }

            using (SQLiteCommand command = connection.CreateCommand())
            {
                // auto_vacuum only takes effect on a new, empty database; it lets the file shrink after pruning.
                command.CommandText = "PRAGMA auto_vacuum = INCREMENTAL;" +
                                      "CREATE TABLE IF NOT EXISTS thumbs (key TEXT PRIMARY KEY NOT NULL, jpeg BLOB NOT NULL, lastUsed INTEGER NOT NULL);" +
                                      "CREATE INDEX IF NOT EXISTS idx_thumbs_lastUsed ON thumbs (lastUsed);";
                _ = command.ExecuteNonQuery();
            }

            Prune(connection: connection);

            // From here until a clean Shutdown the database counts as "in use".
            File.WriteAllText(path: MarkerPath, contents: Process.GetCurrentProcess().Id.ToString());
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    /// <summary>
    ///     True if a marker from an earlier session is still there and no other live copy of the app owns it.
    /// </summary>
    private static bool PreviousRunWasDirty()
    {
        try
        {
            if (!File.Exists(path: MarkerPath))
            {
                return false;
            }

            if (int.TryParse(s: File.ReadAllText(path: MarkerPath).Trim(), result: out int pid) &&
                pid != Process.GetCurrentProcess().Id)
            {
                try
                {
                    using Process other = Process.GetProcessById(processId: pid);
                    if (!other.HasExited &&
                        other.ProcessName.StartsWith(value: Process.GetCurrentProcess().ProcessName, comparisonType: StringComparison.OrdinalIgnoreCase))
                    {
                        // another running instance has the database open: that is not a crash
                        return false;
                    }
                }
                catch (ArgumentException)
                {
                    // no such process: the owner is gone without having cleaned up
                }
            }

            return true;
        }
        catch
        {
            // can't tell; being careful costs only a check
            return true;
        }
    }

    /// <summary>Drops entries unused for <see cref="MaxAge" />, then the least recently used until under the size cap.</summary>
    private static void Prune(SQLiteConnection connection)
    {
        bool deleted = false;

        using (SQLiteCommand command = connection.CreateCommand())
        {
            command.CommandText = "DELETE FROM thumbs WHERE lastUsed < @cutoff";
            _ = command.Parameters.AddWithValue(parameterName: "@cutoff", value: (DateTime.UtcNow - MaxAge).Ticks);
            deleted |= command.ExecuteNonQuery() > 0;
        }

        for (int pass = 0; pass < 20; pass++)
        {
            long total;
            long count;
            using (SQLiteCommand command = connection.CreateCommand())
            {
                command.CommandText = "SELECT IFNULL(SUM(LENGTH(jpeg)), 0), COUNT(*) FROM thumbs";
                using SQLiteDataReader reader = command.ExecuteReader();
                _ = reader.Read();
                total = reader.GetInt64(i: 0);
                count = reader.GetInt64(i: 1);
            }

            if (total <= MaxTotalBytes ||
                count == 0)
            {
                break;
            }

            using SQLiteCommand delete = connection.CreateCommand();
            delete.CommandText = "DELETE FROM thumbs WHERE key IN (SELECT key FROM thumbs ORDER BY lastUsed ASC LIMIT @n)";
            _ = delete.Parameters.AddWithValue(parameterName: "@n", value: Math.Max(val1: 1, val2: count / 10));
            _ = delete.ExecuteNonQuery();
            deleted = true;
        }

        if (deleted)
        {
            // hand the freed pages back so the file actually shrinks
            using SQLiteCommand vacuum = connection.CreateCommand();
            vacuum.CommandText = "PRAGMA incremental_vacuum; PRAGMA wal_checkpoint(TRUNCATE);";
            _ = vacuum.ExecuteNonQuery();
        }
    }

    private static void RegisterExitHook()
    {
        if (_exitHookRegistered)
        {
            return;
        }

        _exitHookRegistered = true;
        // Normal closing calls Shutdown itself; this covers exits that bypass it (Environment.Exit, updater restart, ...).
        AppDomain.CurrentDomain.ProcessExit += (_,
                                                _) => Shutdown();
    }

    private static void DeleteDatabaseFiles()
    {
        DeleteQuietly(path: DatabasePath);
        DeleteQuietly(path: DatabasePath + "-wal");
        DeleteQuietly(path: DatabasePath + "-shm");
        DeleteQuietly(path: MarkerPath);
    }

    private static void DeleteQuietly(string path)
    {
        try
        {
            File.Delete(path: path);
        }
        catch
        {
            // best effort
        }
    }

    private static void Disable(Exception ex)
    {
        if (!_disabled)
        {
            _disabled = true;
            FrmMainApp.Log?.Warn(exception: ex, message: "Persistent thumbnail cache disabled for this session");
        }
    }
}
