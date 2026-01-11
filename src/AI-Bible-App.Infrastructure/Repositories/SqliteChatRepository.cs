using AI_Bible_App.Core.Interfaces;
using AI_Bible_App.Core.Models;
using Dapper;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AI_Bible_App.Infrastructure.Repositories;

/// <summary>
/// SQLite-based chat repository for better querying and scalability.
/// Replaces JSON file storage with proper database for chat history.
/// </summary>
public class SqliteChatRepository : IChatRepository, IDisposable
{
    private readonly string _connectionString;
    private readonly ILogger<SqliteChatRepository> _logger;
    private bool _initialized;

    public SqliteChatRepository(ILogger<SqliteChatRepository> logger, string? databasePath = null)
    {
        _logger = logger;
        
        var dbPath = databasePath ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "AI-Bible-App",
            "data",
            "chat_history.db");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        _connectionString = $"Data Source={dbPath}";
        
        _logger.LogInformation("SqliteChatRepository initialized with database: {Path}", dbPath);
    }

    private async Task EnsureInitializedAsync()
    {
        if (_initialized) return;

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Create tables if they don't exist
            var createTablesSql = @"
                CREATE TABLE IF NOT EXISTS ChatSessions (
                    Id TEXT PRIMARY KEY,
                    UserId TEXT,
                    CharacterId TEXT NOT NULL,
                    SessionType INTEGER DEFAULT 0,
                    StartedAt TEXT NOT NULL,
                    EndedAt TEXT,
                    MessageCount INTEGER DEFAULT 0
                );

                CREATE TABLE IF NOT EXISTS ChatMessages (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    SessionId TEXT NOT NULL,
                    Role TEXT NOT NULL,
                    Content TEXT NOT NULL,
                    Timestamp TEXT NOT NULL,
                    FOREIGN KEY (SessionId) REFERENCES ChatSessions(Id) ON DELETE CASCADE
                );

                CREATE INDEX IF NOT EXISTS idx_sessions_user ON ChatSessions(UserId);
                CREATE INDEX IF NOT EXISTS idx_sessions_character ON ChatSessions(CharacterId);
                CREATE INDEX IF NOT EXISTS idx_sessions_started ON ChatSessions(StartedAt);
                CREATE INDEX IF NOT EXISTS idx_messages_session ON ChatMessages(SessionId);
            ";

            await connection.ExecuteAsync(createTablesSql);
            _initialized = true;
            _logger.LogDebug("SQLite chat database initialized successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize SQLite chat database");
            throw;
        }
    }

    public async Task<ChatSession> GetSessionAsync(string sessionId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var session = await connection.QueryFirstOrDefaultAsync<ChatSessionDto>(
                "SELECT * FROM ChatSessions WHERE Id = @Id",
                new { Id = sessionId });

            if (session == null)
                throw new KeyNotFoundException($"Session {sessionId} not found");

            var messages = await connection.QueryAsync<ChatMessageDto>(
                "SELECT * FROM ChatMessages WHERE SessionId = @SessionId ORDER BY Timestamp",
                new { SessionId = sessionId });

            return MapToSession(session, messages.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<List<ChatSession>> GetAllSessionsAsync()
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sessions = await connection.QueryAsync<ChatSessionDto>(
                "SELECT * FROM ChatSessions ORDER BY StartedAt DESC");

            var result = new List<ChatSession>();
            foreach (var session in sessions)
            {
                var messages = await connection.QueryAsync<ChatMessageDto>(
                    "SELECT * FROM ChatMessages WHERE SessionId = @SessionId ORDER BY Timestamp",
                    new { SessionId = session.Id });

                result.Add(MapToSession(session, messages.ToList()));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving all chat sessions");
            throw;
        }
    }

    public async Task<List<ChatSession>> GetAllSessionsForUserAsync(string userId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sessions = await connection.QueryAsync<ChatSessionDto>(
                "SELECT * FROM ChatSessions WHERE UserId = @UserId ORDER BY StartedAt DESC",
                new { UserId = userId });

            var result = new List<ChatSession>();
            foreach (var session in sessions)
            {
                var messages = await connection.QueryAsync<ChatMessageDto>(
                    "SELECT * FROM ChatMessages WHERE SessionId = @SessionId ORDER BY Timestamp",
                    new { SessionId = session.Id });

                result.Add(MapToSession(session, messages.ToList()));
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat sessions for user {UserId}", userId);
            throw;
        }
    }

    public async Task SaveSessionAsync(ChatSession session)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            using var transaction = await connection.BeginTransactionAsync();

            try
            {
                // Upsert session
                var upsertSessionSql = @"
                    INSERT INTO ChatSessions (Id, UserId, CharacterId, SessionType, StartedAt, EndedAt, MessageCount)
                    VALUES (@Id, @UserId, @CharacterId, @SessionType, @StartedAt, @EndedAt, @MessageCount)
                    ON CONFLICT(Id) DO UPDATE SET
                        EndedAt = @EndedAt,
                        MessageCount = @MessageCount
                ";

                await connection.ExecuteAsync(upsertSessionSql, new
                {
                    session.Id,
                    session.UserId,
                    session.CharacterId,
                    SessionType = (int)session.SessionType,
                    StartedAt = session.StartedAt.ToString("O"),
                    EndedAt = session.EndedAt?.ToString("O"),
                    MessageCount = session.Messages.Count
                }, transaction);

                // Delete existing messages and re-insert
                await connection.ExecuteAsync(
                    "DELETE FROM ChatMessages WHERE SessionId = @SessionId",
                    new { SessionId = session.Id },
                    transaction);

                foreach (var msg in session.Messages)
                {
                    await connection.ExecuteAsync(@"
                        INSERT INTO ChatMessages (SessionId, Role, Content, Timestamp)
                        VALUES (@SessionId, @Role, @Content, @Timestamp)",
                        new
                        {
                            SessionId = session.Id,
                            msg.Role,
                            msg.Content,
                            Timestamp = msg.Timestamp.ToString("O")
                        },
                        transaction);
                }

                await transaction.CommitAsync();
                _logger.LogDebug("Saved chat session {SessionId} with {Count} messages", session.Id, session.Messages.Count);
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving chat session {SessionId}", session.Id);
            throw;
        }
    }

    public async Task DeleteSessionAsync(string sessionId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            // Messages will be cascade deleted due to FK
            await connection.ExecuteAsync(
                "DELETE FROM ChatSessions WHERE Id = @Id",
                new { Id = sessionId });

            _logger.LogInformation("Deleted chat session {SessionId}", sessionId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting chat session {SessionId}", sessionId);
            throw;
        }
    }

    public async Task<ChatSession?> GetLatestSessionForCharacterAsync(string characterId)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var session = await connection.QueryFirstOrDefaultAsync<ChatSessionDto>(
                "SELECT * FROM ChatSessions WHERE CharacterId = @CharacterId ORDER BY StartedAt DESC LIMIT 1",
                new { CharacterId = characterId });

            if (session == null) return null;

            var messages = await connection.QueryAsync<ChatMessageDto>(
                "SELECT * FROM ChatMessages WHERE SessionId = @SessionId ORDER BY Timestamp",
                new { SessionId = session.Id });

            return MapToSession(session, messages.ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving latest session for character {CharacterId}", characterId);
            throw;
        }
    }

    /// <summary>
    /// Search chat history by content (full-text search).
    /// </summary>
    public async Task<List<ChatSession>> SearchSessionsAsync(string searchTerm, int maxResults = 50)
    {
        await EnsureInitializedAsync();

        try
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();

            var sessionIds = await connection.QueryAsync<string>(@"
                SELECT DISTINCT s.Id FROM ChatSessions s
                INNER JOIN ChatMessages m ON s.Id = m.SessionId
                WHERE m.Content LIKE @SearchTerm
                ORDER BY s.StartedAt DESC
                LIMIT @MaxResults",
                new { SearchTerm = $"%{searchTerm}%", MaxResults = maxResults });

            var result = new List<ChatSession>();
            foreach (var sessionId in sessionIds)
            {
                var session = await GetSessionAsync(sessionId);
                result.Add(session);
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error searching chat sessions for '{SearchTerm}'", searchTerm);
            throw;
        }
    }

    /// <summary>
    /// Get chat statistics for analytics.
    /// </summary>
    public async Task<ChatStatistics> GetStatisticsAsync(string? userId = null)
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
                    COUNT(*) as TotalSessions,
                    COALESCE(SUM(MessageCount), 0) as TotalMessages,
                    COALESCE(AVG(MessageCount), 0) as AvgMessagesPerSession
                FROM ChatSessions {whereClause}", param);

            var characterStats = await connection.QueryAsync<dynamic>($@"
                SELECT CharacterId, COUNT(*) as SessionCount
                FROM ChatSessions {whereClause}
                GROUP BY CharacterId
                ORDER BY SessionCount DESC", param);

            return new ChatStatistics
            {
                TotalSessions = (int)(long)stats.TotalSessions,
                TotalMessages = (int)(long)stats.TotalMessages,
                AverageMessagesPerSession = (double)stats.AvgMessagesPerSession,
                SessionsByCharacter = characterStats.ToDictionary(
                    x => (string)x.CharacterId,
                    x => (int)(long)x.SessionCount)
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving chat statistics");
            throw;
        }
    }

    private static ChatSession MapToSession(ChatSessionDto dto, List<ChatMessageDto> messageDtos)
    {
        return new ChatSession
        {
            Id = dto.Id,
            UserId = dto.UserId,
            CharacterId = dto.CharacterId,
            SessionType = (ChatSessionType)dto.SessionType,
            StartedAt = DateTime.Parse(dto.StartedAt),
            EndedAt = string.IsNullOrEmpty(dto.EndedAt) ? null : DateTime.Parse(dto.EndedAt),
            Messages = messageDtos.Select(m => new ChatMessage
            {
                Role = m.Role,
                Content = m.Content,
                Timestamp = DateTime.Parse(m.Timestamp)
            }).ToList()
        };
    }

    public void Dispose()
    {
        // SQLite connections are pooled, nothing to dispose
    }

    // DTOs for Dapper mapping
    private class ChatSessionDto
    {
        public string Id { get; set; } = "";
        public string? UserId { get; set; }
        public string CharacterId { get; set; } = "";
        public int SessionType { get; set; }
        public string StartedAt { get; set; } = "";
        public string? EndedAt { get; set; }
        public int MessageCount { get; set; }
    }

    private class ChatMessageDto
    {
        public int Id { get; set; }
        public string SessionId { get; set; } = "";
        public string Role { get; set; } = "";
        public string Content { get; set; } = "";
        public string Timestamp { get; set; } = "";
    }
}

/// <summary>
/// Chat statistics for analytics dashboard.
/// </summary>
public class ChatStatistics
{
    public int TotalSessions { get; set; }
    public int TotalMessages { get; set; }
    public double AverageMessagesPerSession { get; set; }
    public Dictionary<string, int> SessionsByCharacter { get; set; } = new();
}
