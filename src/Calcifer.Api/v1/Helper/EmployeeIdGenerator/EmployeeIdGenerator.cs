// ============================================================
//  EmployeeIdGenerator.cs
//  Generates unique Employee IDs with prefix format.
//  Format: EMP-{YYYYMMDD}-{RandomNumber}
// ============================================================

namespace Calcifer.Api.Helper.EmployeeIdGenerator
{
  public static class EmployeeIdGenerator
  {
    private static readonly Random _random = new Random();
    private static readonly object _lockObject = new object();

    /// <summary>
    /// Generates a unique Employee ID with format: EMP-{YYYYMMDD}-{RandomNumber}
    /// Example: EMP-20260510-7834
    /// </summary>
    /// <param name="prefix">Custom prefix (default: "WGO")</param>
    /// <returns>Generated Employee ID</returns>
    public static string GenerateEmployeeId(string prefix = "WGO")
    {
      lock (_lockObject)
      {
        var dateString = DateTime.UtcNow.ToString("yyyyMMdd");
        var randomNumber = _random.Next(1000, 9999);
        return $"{prefix}-{dateString}-{randomNumber}";
      }
    }

    /// <summary>
    /// Generates a unique Employee ID using GUID-based approach
    /// Format: EMP-{ShortGuid}
    /// Example: EMP-a3f2b9c1d4e5
    /// </summary>
    /// <param name="prefix">Custom prefix (default: "WGO")</param>
    /// <returns>Generated Employee ID</returns>
    // public static Guid GenerateEmployeeIdGuid(string prefix = "WGO")
    // {
    //   var guid = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
    //   return $"{prefix}-{guid}";
    // }

    public static string GenerateEmployeeIdGuidFormatted(string prefix = "WGO")
    {
      var guid = Guid.NewGuid().ToString("N").Substring(0, 12).ToUpper();
      return $"{prefix}-{guid}";
    }
    public static Guid GenerateEmployeeIdGuidOnly(string prefix = "WGO")
    {
      return Guid.NewGuid();
    }
    /// <summary>
    /// Generates a unique Employee ID using timestamp approach
    /// Format: EMP-{UnixTimestamp}-{RandomNumber}
    /// Example: EMP-1715378550-4521
    /// </summary>
    /// <param name="prefix">Custom prefix (default: "WGO")</param>
    /// <returns>Generated Employee ID</returns>
    public static string GenerateEmployeeIdTimestamp(string prefix = "WGO")
    {
      lock (_lockObject)
      {
        var timestamp = ((long)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalSeconds);
        var randomNumber = _random.Next(1000, 9999);
        return $"{prefix}-{timestamp}-{randomNumber}";
      }
    }
  }
}
