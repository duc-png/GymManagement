using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class MaintenanceHistory
{
    public int Id { get; set; }

    public int? EquipmentId { get; set; }

    public DateTime? LogDate { get; set; }

    public string LogType { get; set; } = null!;

    public string Description { get; set; } = null!;

    public decimal? Cost { get; set; }

    public string? PerformedBy { get; set; }

    public string? Notes { get; set; }

    public virtual Equipment? Equipment { get; set; }
}
