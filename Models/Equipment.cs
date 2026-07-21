using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class Equipment
{
    public int Id { get; set; }

    public string EquipmentCode { get; set; } = null!;

    public string EquipmentName { get; set; } = null!;

    public string EquipmentType { get; set; } = null!;

    public string? Location { get; set; }

    public DateOnly PurchaseDate { get; set; }

    public string? Status { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<MaintenanceHistory> MaintenanceHistories { get; set; } = new List<MaintenanceHistory>();
}
