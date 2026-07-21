using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class PackageTemplate
{
    public int Id { get; set; }

    public string PackageName { get; set; } = null!;

    public decimal Price { get; set; }

    public int DurationMonths { get; set; }

    public bool? HasPt { get; set; }

    public int? PtminutesPerSession { get; set; }

    public int? PtSessions { get; set; }

    public virtual ICollection<MemberPackage> MemberPackages { get; set; } = new List<MemberPackage>();
}
