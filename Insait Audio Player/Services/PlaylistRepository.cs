using System;
using System.IO;
using Insait_Audio_Player.Models;
using Microsoft.Data.Sqlite;

namespace Insait_Audio_Player.Services;

public sealed class PlaylistRepository
{
    private const int StateDocumentId = 1;
    private const string HealthyDatabaseMarker = "ok";
    private readonly string _databasePath;
    private readonly AesTextProtector _protector;

    public PlaylistRepository(AesTextProtector protector)
    {
        _protector = protector;
        _databasePath = Path.Combine(AppDataPaths.DataDirectory, "playlist.sqlite");
    }

    public PlaylistState Load()
    {
        try
        {
            using var connection = OpenHealthyConnection();

            using var stateCommand = connection.CreateCommand();
            stateCommand.CommandText = @"
SELECT current_index,
       volume,
       shuffle_enabled,
       repeat_enabled,
       playback_rate,
       current_position_ms
FROM playlist_state
WHERE state_id = $stateId;";
            stateCommand.Parameters.AddWithValue("$stateId", StateDocumentId);

            using var stateReader = stateCommand.ExecuteReader();
            if (!stateReader.Read())
            {
                return new PlaylistState();
            }

            var state = new PlaylistState
            {
                CurrentIndex = stateReader.IsDBNull(0) ? -1 : stateReader.GetInt32(0),
                Volume = stateReader.IsDBNull(1) ? 75 : stateReader.GetDouble(1),
                ShuffleEnabled = !stateReader.IsDBNull(2) && stateReader.GetInt64(2) != 0,
                RepeatEnabled = stateReader.IsDBNull(3) || stateReader.GetInt64(3) != 0,
                PlaybackRate = stateReader.IsDBNull(4) ? 1.0 : stateReader.GetDouble(4),
                CurrentPositionMilliseconds = stateReader.IsDBNull(5) ? 0 : stateReader.GetInt64(5)
            };

            stateReader.Close();

            using var tracksCommand = connection.CreateCommand();
            tracksCommand.CommandText = @"
SELECT title_cipher,
       artist_cipher,
       album_cipher,
       file_path_cipher,
       duration_ticks
FROM playlist_tracks
WHERE state_id = $stateId
ORDER BY track_order;";
            tracksCommand.Parameters.AddWithValue("$stateId", StateDocumentId);

            using var tracksReader = tracksCommand.ExecuteReader();
            while (tracksReader.Read())
            {
                state.Tracks.Add(new PlaylistTrackState
                {
                    Title = _protector.Decrypt(tracksReader.IsDBNull(0) ? string.Empty : tracksReader.GetString(0)),
                    Artist = _protector.Decrypt(tracksReader.IsDBNull(1) ? string.Empty : tracksReader.GetString(1)),
                    Album = _protector.Decrypt(tracksReader.IsDBNull(2) ? string.Empty : tracksReader.GetString(2)),
                    FilePath = _protector.Decrypt(tracksReader.IsDBNull(3) ? string.Empty : tracksReader.GetString(3)),
                    DurationTicks = tracksReader.IsDBNull(4) ? 0 : tracksReader.GetInt64(4)
                });
            }

            return state;
        }
        catch
        {
            RecreateDatabase();
            using var recreatedConnection = OpenHealthyConnection();
            return new PlaylistState();
        }
    }

    public void Save(PlaylistState state)
    {
        try
        {
            SaveCore(state);
        }
        catch
        {
            RecreateDatabase();
            SaveCore(state);
        }
    }

    private void SaveCore(PlaylistState state)
    {
        using var connection = OpenHealthyConnection();
        using var transaction = connection.BeginTransaction();

        using (var upsertStateCommand = connection.CreateCommand())
        {
            upsertStateCommand.Transaction = transaction;
            upsertStateCommand.CommandText = @"
INSERT INTO playlist_state (
    state_id,
    current_index,
    volume,
    shuffle_enabled,
    repeat_enabled,
    playback_rate,
    current_position_ms)
VALUES (
    $stateId,
    $currentIndex,
    $volume,
    $shuffleEnabled,
    $repeatEnabled,
    $playbackRate,
    $currentPositionMs)
ON CONFLICT(state_id) DO UPDATE SET
    current_index = excluded.current_index,
    volume = excluded.volume,
    shuffle_enabled = excluded.shuffle_enabled,
    repeat_enabled = excluded.repeat_enabled,
    playback_rate = excluded.playback_rate,
    current_position_ms = excluded.current_position_ms;";
            upsertStateCommand.Parameters.AddWithValue("$stateId", StateDocumentId);
            upsertStateCommand.Parameters.AddWithValue("$currentIndex", state.CurrentIndex);
            upsertStateCommand.Parameters.AddWithValue("$volume", state.Volume);
            upsertStateCommand.Parameters.AddWithValue("$shuffleEnabled", state.ShuffleEnabled ? 1 : 0);
            upsertStateCommand.Parameters.AddWithValue("$repeatEnabled", state.RepeatEnabled ? 1 : 0);
            upsertStateCommand.Parameters.AddWithValue("$playbackRate", state.PlaybackRate);
            upsertStateCommand.Parameters.AddWithValue("$currentPositionMs", state.CurrentPositionMilliseconds);
            upsertStateCommand.ExecuteNonQuery();
        }

        using (var deleteTracksCommand = connection.CreateCommand())
        {
            deleteTracksCommand.Transaction = transaction;
            deleteTracksCommand.CommandText = "DELETE FROM playlist_tracks WHERE state_id = $stateId;";
            deleteTracksCommand.Parameters.AddWithValue("$stateId", StateDocumentId);
            deleteTracksCommand.ExecuteNonQuery();
        }

        for (var i = 0; i < state.Tracks.Count; i++)
        {
            var track = state.Tracks[i];
            using var insertTrackCommand = connection.CreateCommand();
            insertTrackCommand.Transaction = transaction;
            insertTrackCommand.CommandText = @"
INSERT INTO playlist_tracks (
    state_id,
    track_order,
    title_cipher,
    artist_cipher,
    album_cipher,
    file_path_cipher,
    duration_ticks)
VALUES (
    $stateId,
    $trackOrder,
    $title,
    $artist,
    $album,
    $filePath,
    $durationTicks);";
            insertTrackCommand.Parameters.AddWithValue("$stateId", StateDocumentId);
            insertTrackCommand.Parameters.AddWithValue("$trackOrder", i);
            insertTrackCommand.Parameters.AddWithValue("$title", _protector.Encrypt(track.Title));
            insertTrackCommand.Parameters.AddWithValue("$artist", _protector.Encrypt(track.Artist));
            insertTrackCommand.Parameters.AddWithValue("$album", _protector.Encrypt(track.Album));
            insertTrackCommand.Parameters.AddWithValue("$filePath", _protector.Encrypt(track.FilePath));
            insertTrackCommand.Parameters.AddWithValue("$durationTicks", track.DurationTicks);
            insertTrackCommand.ExecuteNonQuery();
        }

        transaction.Commit();
    }

    private SqliteConnection OpenHealthyConnection()
    {
        SqliteConnection? connection = null;
        try
        {
            connection = OpenRawConnection();
            EnsureDatabaseIsHealthy(connection);
            EnsureSchema(connection);
            return connection;
        }
        catch
        {
            // Dispose the failed connection before clearing pools so that
            // the WAL/SHM file handles are released on Windows prior to deletion.
            connection?.Dispose();
            RecreateDatabase();

            var recreatedConnection = OpenRawConnection();
            EnsureDatabaseIsHealthy(recreatedConnection);
            EnsureSchema(recreatedConnection);
            return recreatedConnection;
        }
    }

    private SqliteConnection OpenRawConnection()
    {
        var connection = new SqliteConnection(new SqliteConnectionStringBuilder
        {
            DataSource = _databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        }.ToString());

        connection.Open();
        using var pragmaCommand = connection.CreateCommand();
        pragmaCommand.CommandText = @"
PRAGMA foreign_keys = ON;
PRAGMA journal_mode = WAL;";
        pragmaCommand.ExecuteNonQuery();
        return connection;
    }

    private static void EnsureDatabaseIsHealthy(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA quick_check(1);";
        var result = command.ExecuteScalar()?.ToString();

        if (!string.Equals(result, HealthyDatabaseMarker, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("SQLite database integrity check failed.");
        }
    }

    private static void EnsureSchema(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS playlist_state (
    state_id INTEGER NOT NULL PRIMARY KEY CHECK (state_id = 1),
    current_index INTEGER NOT NULL,
    volume REAL NOT NULL,
    shuffle_enabled INTEGER NOT NULL,
    repeat_enabled INTEGER NOT NULL,
    playback_rate REAL NOT NULL,
    current_position_ms INTEGER NOT NULL
);

CREATE TABLE IF NOT EXISTS playlist_tracks (
    state_id INTEGER NOT NULL,
    track_order INTEGER NOT NULL,
    title_cipher TEXT NOT NULL,
    artist_cipher TEXT NOT NULL,
    album_cipher TEXT NOT NULL,
    file_path_cipher TEXT NOT NULL,
    duration_ticks INTEGER NOT NULL,
    PRIMARY KEY (state_id, track_order),
    FOREIGN KEY (state_id) REFERENCES playlist_state(state_id) ON DELETE CASCADE
);";
        command.ExecuteNonQuery();
    }

    private void RecreateDatabase()
    {
        SqliteConnection.ClearAllPools();
        BackupCorruptedDatabaseIfPossible();

        DeleteFileIfExists(_databasePath);
        DeleteSidecarFile("-wal");
        DeleteSidecarFile("-shm");

        using var connection = OpenRawConnection();
        EnsureSchema(connection);
    }

    private void BackupCorruptedDatabaseIfPossible()
    {
        try
        {
            if (File.Exists(_databasePath))
            {
                var corruptedPath = Path.Combine(
                    Path.GetDirectoryName(_databasePath) ?? string.Empty,
                    $"playlist_corrupt_{DateTime.UtcNow:yyyyMMddHHmmss}_{Guid.NewGuid():N}.sqlite");

                File.Move(_databasePath, corruptedPath, overwrite: false);
            }
        }
        catch
        {
            // ignored, fallback delete will be attempted next
        }
    }

    private static void DeleteFileIfExists(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch
        {
            // ignored
        }
    }

    private void DeleteSidecarFile(string suffix)
    {
        var sidecarPath = _databasePath + suffix;

        try
        {
            if (File.Exists(sidecarPath))
            {
                File.Delete(sidecarPath);
            }
        }
        catch
        {
            // ignored
        }
    }
}

