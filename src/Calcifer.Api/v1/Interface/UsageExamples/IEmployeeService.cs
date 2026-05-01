// ============================================================
//  IEmployeeService.cs  (usage example)
//  Stub interface referenced by the EmployeeController
//  usage example. Replace with a real implementation when the
//  HCM module is built.
// ============================================================

namespace Calcifer.Api.Interface.UsageExamples
{
    public interface IEmployeeService
    {
        Task<object?> GetByUserIdAsync(string userId);
        Task<IEnumerable<object>> GetAllAsync();
        Task<object> CreateAsync(CreateEmployeeDto dto);
        Task DeleteAsync(int id);
    }

    public class CreateEmployeeDto
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Department { get; set; }
    }
}
