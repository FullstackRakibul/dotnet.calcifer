using Calcifer.Api.DbContexts;
using Calcifer.Api.DbContexts.AuthModels;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.Services
{
  /// <summary>
  /// Service for managing active user sessions.
  /// Tracks session tokens and handles session revocation.
  /// </summary>
  public class ActiveSessionService : IActiveSessionService
  {
    private readonly CalciferAppDbContext _dbContext;
    private readonly ILogWriter _logger;

    public ActiveSessionService(
      CalciferAppDbContext dbContext,
      ILogWriter logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task<List<ActiveSessionDto>> GetActiveSessionsAsync()
    {
      try
      {
        await _logger.LogActionAsync(
          "Get active sessions",
          "Session",
          "Retrieving all active sessions",
          _logger.GetCorrelationId());

        var now = DateTime.UtcNow;

        // Query from UserRefreshTokens table (assumed to exist)
        // If using JWT with Redis, query Redis keys for active tokens
        var sessions = await _dbContext.Users
          .Where(u => u.RefreshTokens.Any(rt => rt.ExpiryTime > now))
          .SelectMany(u => u.RefreshTokens.Where(rt => rt.ExpiryTime > now),
            (user, token) => new ActiveSessionDto(
              token.Id,                   // Id
              user.Id,                    // UserId
              user.UserName ?? "",        // UserName
              user.Email ?? "",           // UserEmail
              token.IpAddress ?? "",      // IpAddress
              null,                       // Location
              token.DeviceName,           // Device
              token.CreatedAt,            // LoginTime
              token.UpdatedAt,            // LastActivityTime
              token.ExpiryTime > now      // IsActive
            ))
          .ToListAsync();

        return sessions;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get active sessions", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<ActiveSessionDto?> GetSessionByIdAsync(string sessionId)
    {
      try
      {
        var now = DateTime.UtcNow;

        var session = await _dbContext.UserRefreshTokens
          .Where(rt => rt.Id == sessionId && rt.ExpiryTime > now)
          .Select(rt => new ActiveSessionDto(
            rt.Id,                      // Id
            rt.UserId,                  // UserId
            rt.User.UserName ?? "",     // UserName
            rt.User.Email ?? "",        // UserEmail
            rt.IpAddress ?? "",         // IpAddress
            null,                       // Location
            rt.DeviceName,              // Device
            rt.CreatedAt,               // LoginTime
            rt.UpdatedAt,               // LastActivityTime
            rt.ExpiryTime > now         // IsActive
          ))
          .FirstOrDefaultAsync();

        if (session == null)
        {
          await _logger.LogValidationAsync(
            "Get session by ID",
            "Not found",
            $"SessionId: {sessionId}",
            _logger.GetCorrelationId());
        }

        return session;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed to get session by ID: {sessionId}", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<bool> RevokeSessionAsync(string sessionId, string revokedBy)
    {
      try
      {
        var session = await _dbContext.UserRefreshTokens.FindAsync(sessionId);
        if (session == null)
        {
          await _logger.LogValidationAsync(
            "Revoke session",
            "Not found",
            $"SessionId: {sessionId}",
            _logger.GetCorrelationId());
          return false;
        }

        // Mark token as revoked by setting expiry to past date
        session.ExpiryTime = DateTime.UtcNow.AddSeconds(-1);
        session.UpdatedAt = DateTime.UtcNow;

        _dbContext.UserRefreshTokens.Update(session);
        await _dbContext.SaveChangesAsync();

        await _logger.LogActionAsync(
          "Session revoked",
          "Session",
          $"SessionId: {sessionId}, RevokedBy: {revokedBy}",
          _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to revoke session", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<int> RevokeAllSessionsExceptCurrentAsync(string? currentSessionId, string revokedBy)
    {
      try
      {
        var now = DateTime.UtcNow;

        // Get all active sessions except the current one
        var sessionsToRevoke = _dbContext.UserRefreshTokens
          .Where(rt => rt.ExpiryTime > now && (currentSessionId == null || rt.Id != currentSessionId))
          .ToList();

        // Mark all as revoked
        foreach (var session in sessionsToRevoke)
        {
          session.ExpiryTime = DateTime.UtcNow.AddSeconds(-1);
          session.UpdatedAt = DateTime.UtcNow;
        }

        _dbContext.UserRefreshTokens.UpdateRange(sessionsToRevoke);
        await _dbContext.SaveChangesAsync();

        await _logger.LogActionAsync(
          "Revoke all sessions",
          "Session",
          $"Count: {sessionsToRevoke.Count}, ExceptSessionId: {currentSessionId}, RevokedBy: {revokedBy}",
          _logger.GetCorrelationId());

        return sessionsToRevoke.Count;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to revoke all sessions", ex, _logger.GetCorrelationId());
        throw;
      }
    }
  }
}
