namespace Calcifer.Api.Rbac.Entities
{
  /// <summary>
  /// Audit Log entity for tracking all administrative and user actions.
  /// Records who did what, when, where (IP), and the outcome.
  /// </summary>
  public class AuditLog
  {
    /// <summary>
    /// Primary key
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// When the action occurred (UTC)
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// ID of the user performing the action
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// User's display name (denormalized from ApplicationUser for historical purposes)
    /// </summary>
    public string? UserName { get; set; }

    /// <summary>
    /// User's email (denormalized for historical purposes)
    /// </summary>
    public string? UserEmail { get; set; }

    /// <summary>
    /// Module where the action occurred (e.g., "RBAC", "Auth", "License")
    /// </summary>
    public string? Module { get; set; }

    /// <summary>
    /// Resource being acted upon (e.g., "Role", "User", "Permission")
    /// </summary>
    public string? Resource { get; set; }

    /// <summary>
    /// ID of the resource being acted upon
    /// </summary>
    public string? ResourceId { get; set; }

    /// <summary>
    /// Action performed (e.g., "Create", "Update", "Delete", "Login", "AccessDenied")
    /// </summary>
    public string? Action { get; set; }

    /// <summary>
    /// Detailed description of what happened
    /// </summary>
    public string? Details { get; set; }

    /// <summary>
    /// Status of the action: "Success", "Failed", "PartialSuccess"
    /// </summary>
    public string? Status { get; set; }

    /// <summary>
    /// IP address of the request
    /// </summary>
    public string? IpAddress { get; set; }

    /// <summary>
    /// Previous value (for updates), stored as JSON or text
    /// </summary>
    public string? OldValue { get; set; }

    /// <summary>
    /// New value (for updates), stored as JSON or text
    /// </summary>
    public string? NewValue { get; set; }

    /// <summary>
    /// Correlation ID for tracing related audit events
    /// </summary>
    public string? CorrelationId { get; set; }
  }
}
