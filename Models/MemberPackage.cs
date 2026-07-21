using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class MemberPackage
{
    public int Id { get; set; }

    public int? MemberId { get; set; }

    public int? PackageTemplateId { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public int? RemainingPtsessions { get; set; }

    public string? Status { get; set; }

    public virtual Member? Member { get; set; }

    public virtual PackageTemplate? PackageTemplate { get; set; }

    public virtual ICollection<Ptbooking> Ptbookings { get; set; } = new List<Ptbooking>();
}
