// ============================================================
//  ApiResponseDto.cs
//  The universal API response envelope.
//  Every endpoint wraps its payload in this type so the
//  client always has a consistent contract to parse.
//
//  Success:  { status: true,  message: "...", data: {...} }
//  Failure:  { status: false, message: "Error reason",  data: null }
// ============================================================

namespace Calcifer.Api.DTOs
{
    public class ApiResponseDto<T>
    {
        public bool Status { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }

        // ── Static factory helpers ────────────────────────────────

        public static ApiResponseDto<T> Ok(T data, string message = "Success") =>
            new() { Status = true, Message = message, Data = data };

        public static ApiResponseDto<T> Fail(string message) =>
            new() { Status = false, Message = message };
    }
}