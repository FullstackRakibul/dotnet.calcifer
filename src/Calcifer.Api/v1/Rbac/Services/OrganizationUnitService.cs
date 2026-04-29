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

    public OrganizationUnitService(
      CalciferAppDbContext dbContext,
      ILogWriter logger)
    {
      _dbContext = dbContext;
      _logger = logger;
    }

    public async Task<List<OrgUnitDto>> GetAllUnitsAsync(CancellationToken ct = default)
    {
      try
      {
        await _logger.LogActionAsync(
          "Get all organizational units",
          "OrganizationUnit",
          "Retrieving all organizational units",
          _logger.GetCorrelationId());

        var units = await _dbContext.OrganizationUnits
          .Select(u => new OrgUnitDto(
            Id: u.Id,
            Code: u.Code,
            Name: u.Name,
            Description: u.Description,
            ParentId: u.ParentId,
            IsActive: u.IsActive,
            Level: u.Level,
            MembersCount: u.Members.Count,
            CreatedDate: u.CreatedDate,
            LastModified: u.LastModified
          ))
          .ToListAsync(ct);

        return units;
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
        await _logger.LogActionAsync(
          "Get organizational unit tree",
          "OrganizationUnit",
          "Building organizational unit hierarchy",
          _logger.GetCorrelationId());

        var rootUnits = await _dbContext.OrganizationUnits
          .Where(u => u.ParentId == null && u.IsActive)
          .Select(u => new OrgUnitDto(
            Id: u.Id,
            Code: u.Code,
            Name: u.Name,
            Description: u.Description,
            ParentId: u.ParentId,
            IsActive: u.IsActive,
            Level: u.Level,
            MembersCount: u.Members.Count,
            CreatedDate: u.CreatedDate,
            LastModified: u.LastModified
          ))
          .ToListAsync(ct);

        return rootUnits;
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
          .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (unit == null)
        {
          await _logger.LogValidationAsync(
            "Get organizational unit by ID",
            "Not found",
            $"UnitId: {id}",
            _logger.GetCorrelationId());
          return null;
        }

        await _logger.LogActionAsync(
          "Get organizational unit",
          "OrganizationUnit",
          $"Retrieved unit: {unit.Name}",
          _logger.GetCorrelationId());

        return new OrgUnitDto(
          Id: unit.Id,
          Code: unit.Code,
          Name: unit.Name,
          Description: unit.Description,
          ParentId: unit.ParentId,
          IsActive: unit.IsActive,
          Level: unit.Level,
          MembersCount: unit.Members.Count,
          CreatedDate: unit.CreatedDate,
          LastModified: unit.LastModified
        );
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
        await _logger.LogActionAsync(
          "Create organizational unit",
          "OrganizationUnit",
          $"Creating unit: {request.Name}",
          _logger.GetCorrelationId());

        var unit = new OrganizationUnit
        {
          Code = request.Code,
          Name = request.Name,
          Description = request.Description,
          ParentId = request.ParentId,
          IsActive = true,
          Level = request.ParentId.HasValue ? 2 : 1,
          CreatedDate = DateTime.UtcNow,
          LastModified = DateTime.UtcNow
        };

        _dbContext.OrganizationUnits.Add(unit);
        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Organizational unit created",
          "OrganizationUnit",
          $"UnitId: {unit.Id}, CreatedBy: {createdBy}",
          _logger.GetCorrelationId());

        return new OrgUnitDto(
          Id: unit.Id,
          Code: unit.Code,
          Name: unit.Name,
          Description: unit.Description,
          ParentId: unit.ParentId,
          IsActive: unit.IsActive,
          Level: unit.Level,
          MembersCount: 0,
          CreatedDate: unit.CreatedDate,
          LastModified: unit.LastModified
        );
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
          .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (unit == null)
        {
          await _logger.LogValidationAsync(
            "Update organizational unit",
            "Not found",
            $"UnitId: {id}",
            _logger.GetCorrelationId());
          throw new Exception("Organizational unit not found");
        }

        if (!string.IsNullOrEmpty(request.Name))
          unit.Name = request.Name;

        if (request.Description != null)
          unit.Description = request.Description;

        if (request.IsActive.HasValue)
          unit.IsActive = request.IsActive.Value;

        unit.LastModified = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Organizational unit updated",
          "OrganizationUnit",
          $"UnitId: {id}, UpdatedBy: {updatedBy}",
          _logger.GetCorrelationId());

        return new OrgUnitDto(
          Id: unit.Id,
          Code: unit.Code,
          Name: unit.Name,
          Description: unit.Description,
          ParentId: unit.ParentId,
          IsActive: unit.IsActive,
          Level: unit.Level,
          MembersCount: unit.Members.Count,
          CreatedDate: unit.CreatedDate,
          LastModified: unit.LastModified
        );
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
        var unit = await _dbContext.OrganizationUnits
          .FirstOrDefaultAsync(u => u.Id == id, ct);

        if (unit == null)
        {
          await _logger.LogValidationAsync(
            "Delete organizational unit",
            "Not found",
            $"UnitId: {id}",
            _logger.GetCorrelationId());
          return false;
        }

        // Soft delete - mark as inactive
        unit.IsActive = false;
        unit.LastModified = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        await _logger.LogActionAsync(
          "Organizational unit deleted",
          "OrganizationUnit",
          $"UnitId: {id}, DeletedBy: {deletedBy}",
          _logger.GetCorrelationId());

        return true;
      }
      catch (Exception ex)
      {
        await _logger.LogErrorAsync("Failed to delete organizational unit", ex, _logger.GetCorrelationId());
        throw;
      }
    }
  }
}
