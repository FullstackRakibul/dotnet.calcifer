using Calcifer.Api.DbContexts;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.Services
{
  /// <summary>
  /// Service for system health monitoring and status reporting.
  /// Provides overall system status and performance metrics.
  /// </summary>
  public class SystemStatusService : ISystemStatusService
  {
    private readonly CalciferAppDbContext _dbContext;
    private readonly ILogWriter _logger;
    private readonly DateTime _appStartTime;

    public SystemStatusService(
      CalciferAppDbContext dbContext,
      ILogWriter logger)
    {
      _dbContext = dbContext;
      _logger = logger;
      _appStartTime = DateTime.UtcNow;
    }

    public async Task<SystemHealthDto> GetSystemStatusAsync(CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get system status",
          "SystemStatus",
          "Retrieving system health and metrics",
          _logger.GetCorrelationId());

        var dbHealthy = await CheckDatabaseHealthAsync();
        var cacheHealthy = await CheckCacheHealthAsync();
        var uptime = GetUptime();
        var activeUsers = await GetActiveUsersCountAsync();
        var activeSessions = await GetActiveSessionsCountAsync();

        var metrics = new Dictionary<string, object>
        {
          ["DatabaseConnected"] = dbHealthy,
          ["CacheAvailable"] = cacheHealthy,
          ["Timestamp"] = DateTime.UtcNow,
          ["Environment"] = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production"
        };

        var status = new SystemHealthDto(
          DatabaseHealthy: dbHealthy,
          CacheHealthy: cacheHealthy,
          SystemUptime: uptime,
          ActiveUsers: activeUsers,
          ActiveSessions: activeSessions,
          TotalRequests: 0,  // Can be tracked via middleware
          AverageResponseTime: 0,  // Can be tracked via middleware
          LastHealthCheck: DateTime.UtcNow.ToString("O"),
          AdditionalMetrics: metrics
        );


		return status;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get system status", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public string GetUptime()
    {
      var uptime = DateTime.UtcNow - _appStartTime;
      return $"{uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";
    }

    public async Task<bool> CheckDatabaseHealthAsync()
    {
      try
      {
        // Try to execute a simple query
        var canConnect = await _dbContext.Database.CanConnectAsync();
        return canConnect;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Database health check failed", ex, _logger.GetCorrelationId());
        return false;
      }
    }

    public async Task<bool> CheckCacheHealthAsync()
    {
      try
      {
        // Placeholder for cache health check
        // Implement with your caching solution (Redis, MemoryCache, etc.)
        await _logger.LogValidationAsync(
          "Cache health check",
          "Skipped",
          "Cache health check not implemented",
          _logger.GetCorrelationId());

        return true;  // Assume healthy if no exception
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Cache health check failed", ex, _logger.GetCorrelationId());
        return false;
      }
    }

    /// <summary>
    /// Get count of active users (logged in within last 24 hours)
    /// </summary>
    private async Task<int> GetActiveUsersCountAsync()
    {
      try
      {
        var oneDayAgo = DateTime.UtcNow.AddHours(-24);
        var activeUsers = await _dbContext.Users
          .Where(u => u.LastLogin.HasValue && u.LastLogin >= oneDayAgo)
          .CountAsync();

        return activeUsers;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get active users count", ex, _logger.GetCorrelationId());
        return 0;
      }
    }

    /// <summary>
    /// Get count of active sessions
    /// </summary>
    private async Task<int> GetActiveSessionsCountAsync()
    {
      try
      {
        var now = DateTime.UtcNow;
        var activeSessions = await _dbContext.UserRefreshTokens
          .Where(rt => rt.ExpiryTime > now)
          .CountAsync();

        return activeSessions;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get active sessions count", ex, _logger.GetCorrelationId());
        return 0;
      }
    }
  }
}
