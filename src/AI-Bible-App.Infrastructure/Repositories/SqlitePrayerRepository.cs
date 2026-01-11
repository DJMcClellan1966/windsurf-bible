using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// SQLite-based prayer repository for better querying and scalability.
/// Replaces JSON file storage with proper database for prayer history.
/// </summary>
public class SqlitePrayerRepository : IPrayerRepository, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqlitePrayerRepository> _logger;
    private bool _initialized;

    public SqlitePrayerRepository(ILogger<SqlitePrayerRepository> logger, string? databasePath = null)
    {
        _logger = logger;

        var dbPath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AI-Bible-App",
            "data",
            "prayer_history.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";

        _logger.LogInformation("SqlitePrayerRepository initialized with database: {Path}", dbPath);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS Prayers (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT,
                    Content TEXT NOT NULL,
                    Topic TEXT,
                    CreatedAt TEXT NOT NULL,
                    Tags TEXT
                );

                CREATE TABLE IF NOT EXISTS SavedPrayers (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Topic TEXT,
                    CharacterId TEXT,
                    CreatedAt TEXT NOT NULL,
                    LastPrayedAt TEXT,
                    IsFavorite INTEGER DEFAULT 0,
                    Tags TEXT
                );

                CREATE INDEX IF NOT EXISTS idx_prayers_user ON Prayers(UserId);
                CREATE INDEX IF NOT EXISTS idx_prayers_topic ON Prayers(Topic);
                CREATE INDEX IF NOT EXISTS idx_prayers_created ON Prayers(CreatedAt);
                CREATE INDEX IF NOT EXISTS idx_saved_user ON SavedPrayers(UserId);
                CREATE INDEX IF NOT EXISTS idx_saved_favorite ON SavedPrayers(IsFavorite);
            ";

            await connection.ExecuteAsync(createTablesSql);
            _initialized = true;
            _logger.LogDebug("SQLite prayer database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SQLite prayer database");
            throw;
        }
    }

    public async Task<Prayer> GetPrayerAsync(string prayerId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var dto = await connection.QueryFirstOrDefaultAsync<PrayerDto>(
                "SELECT * FROM Prayers WHERE Id = @Id",
                new { Id = prayerId });

            if (dto == null)
                throw new KeyNotFoundException($"Prayer {prayerId} not found");

            return MapToPrayer(dto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prayer {PrayerId}", prayerId);
            throw;
        }
    }

    public async Task<List<Prayer>> GetAllPrayersAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var prayers = await connection.QueryAsync<PrayerDto>(
                "SELECT * FROM Prayers ORDER BY CreatedAt DESC");

            return prayers.Select(MapToPrayer).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all prayers");
            throw;
        }
    }

    public async Task<List<SavedPrayer>> GetAllForUserAsync(string userId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var prayers = await connection.QueryAsync<SavedPrayerDto>(
                "SELECT * FROM SavedPrayers WHERE UserId = @UserId ORDER BY CreatedAt DESC",
                new { UserId = userId });

            return prayers.Select(MapToSavedPrayer).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prayers for user {UserId}", userId);
            throw;
        }
    }

    public async Task<List<Prayer>> GetPrayersByTopicAsync(string topic)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var prayers = await connection.QueryAsync<PrayerDto>(
                "SELECT * FROM Prayers WHERE Topic LIKE @Topic ORDER BY CreatedAt DESC",
                new { Topic = $"%{topic}%" });

            return prayers.Select(MapToPrayer).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prayers by topic '{Topic}'", topic);
            throw;
        }
    }

    public async Task SavePrayerAsync(Prayer prayer)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO Prayers (Id, UserId, Content, Topic, CreatedAt, Tags)
                VALUES (@Id, @UserId, @Content, @Topic, @CreatedAt, @Tags)
                ON CONFLICT(Id) DO UPDATE SET
                    Content = @Content,
                    Topic = @Topic,
                    Tags = @Tags
            ";

            await connection.ExecuteAsync(sql, new
            {
                prayer.Id,
                prayer.UserId,
                prayer.Content,
                prayer.Topic,
                CreatedAt = prayer.CreatedAt.ToString("O"),
                Tags = JsonSerializer.Serialize(prayer.Tags)
            });

            _logger.LogDebug("Saved prayer {PrayerId}", prayer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving prayer {PrayerId}", prayer.Id);
            throw;
        }
    }

    public async Task SaveAsync(SavedPrayer prayer)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sql = @"
                INSERT INTO SavedPrayers (Id, UserId, Content, Topic, CharacterId, CreatedAt, LastPrayedAt, IsFavorite, Tags)
                VALUES (@Id, @UserId, @Content, @Topic, @CharacterId, @CreatedAt, @LastPrayedAt, @IsFavorite, @Tags)
                ON CONFLICT(Id) DO UPDATE SET
                    Content = @Content,
                    Topic = @Topic,
                    LastPrayedAt = @LastPrayedAt,
                    IsFavorite = @IsFavorite,
                    Tags = @Tags
            ";

            await connection.ExecuteAsync(sql, new
            {
                prayer.Id,
                prayer.UserId,
                prayer.Content,
                prayer.Topic,
                prayer.CharacterId,
                CreatedAt = prayer.CreatedAt.ToString("O"),
                LastPrayedAt = prayer.LastPrayedAt?.ToString("O"),
                IsFavorite = prayer.IsFavorite ? 1 : 0,
                Tags = JsonSerializer.Serialize(prayer.Tags)
            });

            _logger.LogDebug("Saved user prayer {PrayerId}", prayer.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving user prayer {PrayerId}", prayer.Id);
            throw;
        }
    }

    public async Task DeletePrayerAsync(string prayerId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Delete from both tables (could be in either)
            await connection.ExecuteAsync("DELETE FROM Prayers WHERE Id = @Id", new { Id = prayerId });
            await connection.ExecuteAsync("DELETE FROM SavedPrayers WHERE Id = @Id", new { Id = prayerId });

            _logger.LogInformation("Deleted prayer {PrayerId}", prayerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting prayer {PrayerId}", prayerId);
            throw;
        }
    }

    /// <summary>
    /// Get favorite prayers for quick access.
    /// </summary>
    public async Task<List<SavedPrayer>> GetFavoritePrayersAsync(string userId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var prayers = await connection.QueryAsync<SavedPrayerDto>(
                "SELECT * FROM SavedPrayers WHERE UserId = @UserId AND IsFavorite = 1 ORDER BY LastPrayedAt DESC",
                new { UserId = userId });

            return prayers.Select(MapToSavedPrayer).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving favorite prayers for user {UserId}", userId);
            throw;
        }
    }

    /// <summary>
    /// Search prayers by content.
    /// </summary>
    public async Task<List<Prayer>> SearchPrayersAsync(string searchTerm, int maxResults = 50)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var prayers = await connection.QueryAsync<PrayerDto>(@"
                SELECT * FROM Prayers 
                WHERE Content LIKE @SearchTerm OR Topic LIKE @SearchTerm
                ORDER BY CreatedAt DESC
                LIMIT @MaxResults",
                new { SearchTerm = $"%{searchTerm}%", MaxResults = maxResults });

            return prayers.Select(MapToPrayer).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching prayers for '{SearchTerm}'", searchTerm);
            throw;
        }
    }

    /// <summary>
    /// Update the last prayed timestamp.
    /// </summary>
    public async Task MarkAsPrayedAsync(string prayerId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            await connection.ExecuteAsync(
                "UPDATE SavedPrayers SET LastPrayedAt = @Now WHERE Id = @Id",
                new { Id = prayerId, Now = DateTime.UtcNow.ToString("O") });

            _logger.LogDebug("Marked prayer {PrayerId} as prayed", prayerId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error marking prayer {PrayerId} as prayed", prayerId);
            throw;
        }
    }

    /// <summary>
    /// Get prayer statistics.
    /// </summary>
    public async Task<PrayerStatistics> GetStatisticsAsync(string? userId = null)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var whereClause = userId != null ? "WHERE UserId = @UserId" : "";
            var param = userId != null ? new { UserId = userId } : null;

            var stats = await connection.QueryFirstAsync<dynamic>($@"
                SELECT 
                    COUNT(*) as TotalPrayers,
                    SUM(CASE WHEN IsFavorite = 1 THEN 1 ELSE 0 END) as FavoriteCount
                FROM SavedPrayers {whereClause}", param);

            var topicStats = await connection.QueryAsync<dynamic>($@"
                SELECT Topic, COUNT(*) as Count
                FROM SavedPrayers {whereClause}
                WHERE Topic IS NOT NULL AND Topic != ''
                GROUP BY Topic
                ORDER BY Count DESC
                LIMIT 10", param);

            return new PrayerStatistics
            {
                TotalPrayers = (int)(long)stats.TotalPrayers,
                FavoriteCount = (int)(long)(stats.FavoriteCount ?? 0),
                PrayersByTopic = topicStats.ToDictionary(
                    x => (string)x.Topic,
                    x => (int)(long)x.Count)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving prayer statistics");
            throw;
        }
    }

    private static Prayer MapToPrayer(PrayerDto dto)
    {
        return new Prayer
        {
            Id = dto.Id,
            UserId = dto.UserId,
            Content = dto.Content,
            Topic = dto.Topic ?? "",
            CreatedAt = DateTime.Parse(dto.CreatedAt),
            Tags = string.IsNullOrEmpty(dto.Tags) 
                ? new List<string>() 
                : JsonSerializer.Deserialize<List<string>>(dto.Tags) ?? new List<string>()
        };
    }

    private static SavedPrayer MapToSavedPrayer(SavedPrayerDto dto)
    {
        return new SavedPrayer
        {
            Id = dto.Id,
            UserId = dto.UserId,
            Content = dto.Content,
            Topic = dto.Topic ?? "",
            CharacterId = dto.CharacterId,
            CreatedAt = DateTime.Parse(dto.CreatedAt),
            LastPrayedAt = string.IsNullOrEmpty(dto.LastPrayedAt) ? null : DateTime.Parse(dto.LastPrayedAt),
            IsFavorite = dto.IsFavorite == 1,
            Tags = string.IsNullOrEmpty(dto.Tags)
                ? new List<string>()
                : JsonSerializer.Deserialize<List<string>>(dto.Tags) ?? new List<string>()
        };
    }

    public void Dispose()
    {
        // SQLite connections are pooled, nothing to dispose
    }

    // DTOs for Dapper mapping
    private class PrayerDto
    {
        public string Id { get; set; } = "";
        public string? UserId { get; set; }
        public string Content { get; set; } = "";
        public string? Topic { get; set; }
        public string CreatedAt { get; set; } = "";
        public string? Tags { get; set; }
    }

    private class SavedPrayerDto
    {
        public string Id { get; set; } = "";
        public string UserId { get; set; } = "";
        public string Content { get; set; } = "";
        public string? Topic { get; set; }
        public string? CharacterId { get; set; }
        public string CreatedAt { get; set; } = "";
        public string? LastPrayedAt { get; set; }
        public int IsFavorite { get; set; }
        public string? Tags { get; set; }
    }
}

/// <summary>
/// Prayer statistics for analytics dashboard.
/// </summary>
public class PrayerStatistics
{
    public int TotalPrayers { get; set; }
    public int FavoriteCount { get; set; }
    public Dictionary<string, int> PrayersByTopic { get; set; } = new();
}
