using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class EquipmentService
{
    public async Task<List<Equipment>> GetAsync()
    {
        using var db = new GymManagementDbContext();
        return await db.Equipments.AsNoTracking().OrderBy(x => x.EquipmentName).ToListAsync();
    }

    public async Task<string?> SaveAsync(int? id, string code, string name, string type, string? location, DateOnly purchaseDate, string role)
    {
        if (role != UserRoles.Admin) return "Chỉ Admin được thêm hoặc sửa thiết bị.";
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(type)) return "Mã, tên và loại thiết bị là bắt buộc.";
        using var db = new GymManagementDbContext();
        if (await db.Equipments.AnyAsync(x => x.EquipmentCode == code && x.Id != id)) return "Mã thiết bị đã tồn tại.";
        Equipment? equipment = id == null ? null : await db.Equipments.FindAsync(id.Value);
        if (equipment == null)
        {
            equipment = new Equipment { EquipmentCode = code.Trim(), EquipmentName = name.Trim(), EquipmentType = type.Trim(), Location = location?.Trim(), PurchaseDate = purchaseDate, Status = "Operational" };
            db.Equipments.Add(equipment);
        }
        else
        {
            equipment.EquipmentCode = code.Trim(); equipment.EquipmentName = name.Trim(); equipment.EquipmentType = type.Trim(); equipment.Location = location?.Trim(); equipment.PurchaseDate = purchaseDate;
        }
        await db.SaveChangesAsync(); return null;
    }

    public async Task<string?> ReportIssueAsync(int equipmentId, string description, string role)
    {
        if (role is not (UserRoles.Admin or UserRoles.Receptionist)) return "Bạn không có quyền báo hỏng thiết bị.";
        if (string.IsNullOrWhiteSpace(description)) return "Vui lòng nhập mô tả sự cố.";
        using var db = new GymManagementDbContext(); await using var tx = await db.Database.BeginTransactionAsync();
        var equipment = await db.Equipments.FindAsync(equipmentId); if (equipment == null) return "Không tìm thấy thiết bị.";
        if (equipment.Status == "Broken") return "Thiết bị này đã được báo hỏng.";
        if (equipment.Status == "UnderMaintenance") return "Thiết bị này đang trong quá trình bảo trì.";
        equipment.Status = "Broken";
        db.MaintenanceHistories.Add(new MaintenanceHistory { EquipmentId = equipmentId, LogDate = DateTime.Now, LogType = "IssueReport", Description = description.Trim(), PerformedBy = role });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
    }

    public async Task<string?> StartMaintenanceAsync(int equipmentId, string role)
    {
        if (role != UserRoles.Admin) return "Chỉ Admin được bắt đầu quy trình sửa chữa.";
        using var db = new GymManagementDbContext(); var equipment = await db.Equipments.FindAsync(equipmentId); if (equipment == null) return "Không tìm thấy thiết bị.";
        if (equipment.Status == "UnderMaintenance") return "Thiết bị này đang được bảo trì.";
        if (equipment.Status != "Broken") return "Chỉ có thể bảo trì thiết bị đã được báo hỏng.";
        equipment.Status = "UnderMaintenance"; await db.SaveChangesAsync(); return null;
    }

    public async Task<string?> CompleteRepairAsync(int equipmentId, decimal cost, string performedBy, string notes, string role)
    {
        if (role != UserRoles.Admin) return "Chỉ Admin được hoàn tất sửa chữa.";
        using var db = new GymManagementDbContext(); await using var tx = await db.Database.BeginTransactionAsync();
        var equipment = await db.Equipments.FindAsync(equipmentId); if (equipment == null) return "Không tìm thấy thiết bị.";
        if (equipment.Status != "UnderMaintenance") return "Chỉ có thể xác nhận sửa xong thiết bị đang được bảo trì.";
        equipment.Status = "Operational";
        db.MaintenanceHistories.Add(new MaintenanceHistory { EquipmentId = equipmentId, LogDate = DateTime.Now, LogType = "Repair", Description = "Hoàn tất sửa chữa", Cost = cost, PerformedBy = performedBy, Notes = notes });
        await db.SaveChangesAsync(); await tx.CommitAsync(); return null;
    }
}
