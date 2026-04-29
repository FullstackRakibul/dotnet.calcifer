using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Calcifer.Api.Helper.LogWriter
{
  /// <summary>
  /// Dynamic Log Writer Helper - Centralized logging for all application operations
  /// Writes to: /logs folder in project base directory with DateTime-stamped files
  /// Format: Text-based logs with timestamps, correlation IDs, and operation details
  /// </summary>
  public interface ILogWriter
  {
    Task LogAsync(LogEntry logEntry);
    Task LogActionAsync(string action, string module, string detail, string? correlationId = null);
    Task LogValidationAsync(string validation, string result, string? details = null, string? correlationId = null);
    Task LogErrorAsync(string error, Exception? exception = null, string? correlationId = null);
    Task LogResponseAsync(string endpoint, string method, int statusCode, string responseBody, string? correlationId = null);
    string GetCorrelationId();
  }

  public class LogEntry
  {
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string CorrelationId { get; set; } = Guid.NewGuid().ToString();
    public string LogType { get; set; } // Action, Validation, Error, Response, Try, Failed
    public string Module { get; set; }
    public string? Endpoint { get; set; }
    public string? Method { get; set; }
    public string Message { get; set; }
    public string? Detail { get; set; }
    public int? StatusCode { get; set; }
    public string? IpAddress { get; set; }
    public string? UserId { get; set; }
    public Exception? Exception { get; set; }
    public string? StackTrace { get; set; }
  }

  public class DynamicLogWriter : ILogWriter
  {
    private readonly string _logDirectory;
    private readonly string _baseDirectory;
    private string _currentCorrelationId;

    public DynamicLogWriter(string? baseDirectory = null)
    {
      _baseDirectory = baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory;
      _logDirectory = Path.Combine(_baseDirectory, "logs");
      EnsureLogDirectoryExists();
      _currentCorrelationId = Guid.NewGuid().ToString();
    }

    /// <summary>
    /// Ensure logs directory exists, create if not present
    /// </summary>
    private void EnsureLogDirectoryExists()
    {
      if (!Directory.Exists(_logDirectory))
      {
        Directory.CreateDirectory(_logDirectory);
      }
    }

    /// <summary>
    /// Get or set correlation ID for distributed tracing
    /// </summary>
    public string GetCorrelationId() => _currentCorrelationId;

    /// <summary>
    /// Core logging method - writes to file-based logs
    /// File naming: YYYY-MM-DD_LogType.txt (e.g., 2026-04-28_Action.txt)
    /// </summary>
    public async Task LogAsync(LogEntry logEntry)
    {
      try
      {
        var fileName = $"{DateTime.UtcNow:yyyy-MM-dd}_{logEntry.LogType}.txt";
        var filePath = Path.Combine(_logDirectory, fileName);

        var logContent = FormatLogEntry(logEntry);

        await File.AppendAllTextAsync(filePath, logContent, Encoding.UTF8);
      }
      catch (Exception ex)
      {
        // Fallback: write to console if file logging fails
        Console.WriteLine($"[LogWriter Error] {ex.Message}");
      }
    }

    /// <summary>
    /// Log action (e.g., "User Login", "Permission Check", "License Validation")
    /// </summary>
    public async Task LogActionAsync(string action, string module, string detail, string? correlationId = null)
    {
      var entry = new LogEntry
      {
        LogType = "Action",
        Module = module,
        Message = action,
        Detail = detail,
        CorrelationId = correlationId ?? _currentCorrelationId
      };

      await LogAsync(entry);
    }

    /// <summary>
    /// Log validation (e.g., "Email Validation Passed", "RBAC Permission Denied")
    /// </summary>
    public async Task LogValidationAsync(string validation, string result, string? details = null, string? correlationId = null)
    {
      var entry = new LogEntry
      {
        LogType = "Validation",
        Module = "System",
        Message = validation,
        Detail = $"Result: {result}. {details}",
        CorrelationId = correlationId ?? _currentCorrelationId
      };

      await LogAsync(entry);
    }

    /// <summary>
    /// Log errors with full exception details (stack trace, message)
    /// </summary>
    public async Task LogErrorAsync(string error, Exception? exception = null, string? correlationId = null)
    {
      var entry = new LogEntry
      {
        LogType = "Error",
        Module = "System",
        Message = error,
        Exception = exception,
        StackTrace = exception?.StackTrace,
        Detail = exception?.InnerException?.Message,
        CorrelationId = correlationId ?? _currentCorrelationId
      };

      await LogAsync(entry);
    }

    /// <summary>
    /// Log HTTP responses (endpoint, method, status code, response body)
    /// </summary>
    public async Task LogResponseAsync(string endpoint, string method, int statusCode, string responseBody, string? correlationId = null)
    {
      var entry = new LogEntry
      {
        LogType = "Response",
        Module = "HTTP",
        Endpoint = endpoint,
        Method = method,
        StatusCode = statusCode,
        Message = $"{method} {endpoint} -> {statusCode}",
        Detail = responseBody,
        CorrelationId = correlationId ?? _currentCorrelationId
      };

      await LogAsync(entry);
    }

    /// <summary>
    /// Format log entry into readable text format
    /// </summary>
    private string FormatLogEntry(LogEntry logEntry)
    {
      var sb = new StringBuilder();
      sb.AppendLine($"═══════════════════════════════════════════════════════════");
      sb.AppendLine($"[{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{logEntry.LogType.ToUpper()}]");
      sb.AppendLine($"CorrelationId: {logEntry.CorrelationId}");
      sb.AppendLine($"Module: {logEntry.Module}");

      if (!string.IsNullOrEmpty(logEntry.Endpoint))
        sb.AppendLine($"Endpoint: {logEntry.Method} {logEntry.Endpoint}");

      if (logEntry.StatusCode.HasValue)
        sb.AppendLine($"Status Code: {logEntry.StatusCode}");

      if (!string.IsNullOrEmpty(logEntry.IpAddress))
        sb.AppendLine($"IP Address: {logEntry.IpAddress}");

      if (!string.IsNullOrEmpty(logEntry.UserId))
        sb.AppendLine($"User ID: {logEntry.UserId}");

      sb.AppendLine($"Message: {logEntry.Message}");

      if (!string.IsNullOrEmpty(logEntry.Detail))
        sb.AppendLine($"Detail: {logEntry.Detail}");

      if (logEntry.Exception != null)
      {
        sb.AppendLine($"Exception: {logEntry.Exception.GetType().Name}");
        sb.AppendLine($"Error: {logEntry.Exception.Message}");
        sb.AppendLine($"StackTrace: {logEntry.StackTrace}");
      }

      sb.AppendLine();

      return sb.ToString();
    }
  }

  /// <summary>
  /// Extension methods for convenient logging throughout the application
  /// </summary>
  public static class LogWriterExtensions
  {
    /// <summary>
    /// Register ILogWriter as singleton in DependencyInversion.cs:
    /// services.AddSingleton<ILogWriter>(new DynamicLogWriter());
    /// </summary>
    public static void AddDynamicLogWriter(this IServiceCollection services)
    {
      services.AddSingleton<ILogWriter>(sp =>
      {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        return new DynamicLogWriter(baseDir);
      });
    }
  }
}
