using Calcifer.Api.DbContexts;
using Calcifer.Api.Helper.LogWriter;
using Calcifer.Api.Rbac.DTOs;
using Calcifer.Api.Rbac.Entities;
using Calcifer.Api.Rbac.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Calcifer.Api.Rbac.Services
{
  /// <summary>
  /// Service for managing organizational units and their hierarchies.
  /// Handles CRUD operations and tree structure queries.
  /// </summary>
  public class OrganizationUnitService : IOrganizationUnitService
  {
    private readonly CalciferAppDbContext _dbContext;
    private readonly ILogWriter _logger;

    public OrganizationUnitService(CalciferAppDbContext dbContext, ILogWriter logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task<List<OrgUnitDto>> GetAllUnitsAsync(CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync("Get all organizational units", "OrganizationUnit", "Retrieving all organizational units", _logger.GetCorrelationId());

        return await _dbContext.OrganizationUnits
          .Select(u => new OrgUnitDto(
            u.Id,                         // Id
            u.Code,                       // Code
            u.Name,                       // Name
            u.Description,                // Description
            u.ParentId,                   // ParentId
            u.IsActive,                   // IsActive
            u.Level,                      // Level
            u.UserRoles.Count,            // MembersCount
            u.CreatedAt,                  // CreatedDate
            u.UpdatedAt ?? u.CreatedAt,   // LastModified
            null                          // Children
          ))
          .ToListAsync(ct);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get all organizational units", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<List<OrgUnitDto>> GetUnitTreeAsync(CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync("Get organizational unit tree", "OrganizationUnit", "Building hierarchy", _logger.GetCorrelationId());

        return await _dbContext.OrganizationUnits
          .Where(u => u.ParentId == null && u.IsActive)
          .Select(u => new OrgUnitDto(
            u.Id,                         // Id
            u.Code,                       // Code
            u.Name,                       // Name
            u.Description,                // Description
            u.ParentId,                   // ParentId
            u.IsActive,                   // IsActive
            u.Level,                      // Level
            u.UserRoles.Count,            // MembersCount
            u.CreatedAt,                  // CreatedDate
            u.UpdatedAt ?? u.CreatedAt,   // LastModified
            null                          // Children
          ))
          .ToListAsync(ct);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to get organizational unit tree", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<OrgUnitDto?> GetUnitByIdAsync(int id, CancellationToken ct = default)
    {
      try
      {
        var unit = await _dbContext.OrganizationUnits
          .Include(u => u.UserRoles)
          .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (unit == null)
        {
          await _logger.LogValidationAsync("Get org unit by ID", "Not found", $"UnitId: {id}", _logger.GetCorrelationId());
          return null;
        }

        return MapToDto(unit);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync($"Failed to get organizational unit by ID: {id}", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<OrgUnitDto> CreateUnitAsync(CreateOrgUnitRequest request, string createdBy, CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync("Create organizational unit", "OrganizationUnit", $"Creating unit: {request.Name}", _logger.GetCorrelationId());

        var unit = new OrganizationUnit
        {
          Code = request.Code ?? "",
          Name = request.Name,
          Description = null,
          ParentId = request.ParentId,
          IsActive = true,
          Level = request.Level ?? (request.ParentId.HasValue ? "Team" : "Department"),
          CreatedBy = createdBy
        };

        _dbContext.OrganizationUnits.Add(unit);
        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync("Organizational unit created", "OrganizationUnit", $"UnitId: {unit.Id}, CreatedBy: {createdBy}", _logger.GetCorrelationId());

        return MapToDto(unit);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to create organizational unit", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<OrgUnitDto> UpdateUnitAsync(int id, UpdateOrgUnitRequest request, string updatedBy, CancellationToken ct = default)
    {
      try
      {
        var unit = await _dbContext.OrganizationUnits
          .Include(u => u.UserRoles)
          .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (unit == null)
        {
          await _logger.LogValidationAsync("Update organizational unit", "Not found", $"UnitId: {id}", _logger.GetCorrelationId());
          throw new Exception("Organizational unit not found");
        }

        if (!string.IsNullOrEmpty(request.Name)) unit.Name = request.Name;
        if (!string.IsNullOrEmpty(request.Code)) unit.Code = request.Code;
        if (!string.IsNullOrEmpty(request.Level)) unit.Level = request.Level;
        if (request.Description != null) unit.Description = request.Description;
        if (request.IsActive.HasValue) unit.IsActive = request.IsActive.Value;
        if (request.ParentId.HasValue) unit.ParentId = request.ParentId;

        unit.UpdatedAt = DateTime.UtcNow;
        unit.UpdatedBy = updatedBy;

        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync("Organizational unit updated", "OrganizationUnit", $"UnitId: {id}, UpdatedBy: {updatedBy}", _logger.GetCorrelationId());

        return MapToDto(unit);
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to update organizational unit", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    public async Task<bool> DeleteUnitAsync(int id, string deletedBy, CancellationToken ct = default)
    {
      try
      {
        var unit = await _dbContext.OrganizationUnits.FirstOrDefaultAsync(u => u.Id == id, ct);
        if (unit == null)
        {
          await _logger.LogValidationAsync("Delete organizational unit", "Not found", $"UnitId: {id}", _logger.GetCorrelationId());
          return false;
        }

        unit.IsActive = false;
        unit.IsDeleted = true;
        unit.DeletedAt = DateTime.UtcNow;
        unit.DeletedBy = deletedBy;

        await _dbContext.SaveChangesAsync(ct);
        await _logger.LogActionAsync("Organizational unit deleted", "OrganizationUnit", $"UnitId: {id}, DeletedBy: {deletedBy}", _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to delete organizational unit", ex, _logger.GetCorrelationId());
        throw;
      }
    }

    private static OrgUnitDto MapToDto(OrganizationUnit u) => new(
      u.Id,
      u.Code,
      u.Name,
      u.Description,
      u.ParentId,
      u.IsActive,
      u.Level,
      u.UserRoles?.Count ?? 0,
      u.CreatedAt,
      u.UpdatedAt ?? u.CreatedAt
    );
  }
}
