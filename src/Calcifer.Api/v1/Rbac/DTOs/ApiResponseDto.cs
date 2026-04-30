namespace Calcifer.Api.Rbac.DTOs
{
  /// <summary>
  /// Standard API Response wrapper for all endpoints.
  /// Ensures consistent response structure across all APIs.
  /// </summary>
  public record ApiResponseDto<T>(
    bool Status,
    string Message,
    T? Data
  );

  /// <summary>
  /// Helper class for creating standardized API responses
  /// </summary>
  public static class ApiResponse
  {
    public static ApiResponseDto<T> Success<T>(T data, string message = "Operation successful")
    {
      return new ApiResponseDto<T>(Status: true, Message: message, Data: data);
    }

    public static ApiResponseDto<object> Success(string message = "Operation successful")
    {
      return new ApiResponseDto<object>(Status: true, Message: message, Data: null);
    }

    public static ApiResponseDto<T> Error<T>(string message = "Operation failed", T? data = default)
    {
      return new ApiResponseDto<T>(Status: false, Message: message, Data: data);
    }

    public static ApiResponseDto<object> Error(string message = "Operation failed")
    {
      return new ApiResponseDto<object>(Status: false, Message: message, Data: null);
    }
  }
}
