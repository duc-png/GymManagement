using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class PackageService
{
    public async Task<(List<Member> Members, List<PackageTemplate> Packages, List<MemberPackage> Assignments)> GetAssignmentDataAsync()
    {
        using var db = new GymManagementDbContext();
        var members = await db.Members
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .ToListAsync();
        var packages = await db.PackageTemplates
            .AsNoTracking()
            .OrderBy(x => x.PackageName)
            .ToListAsync();
        var assignments = await db.MemberPackages
            .AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.PackageTemplate)
            .OrderByDescending(x => x.Id)
            .ToListAsync();
        return (members, packages, assignments);
    }

    public async Task<string?> AssignAsync(int memberId, int packageId)
    {
        using var db = new GymManagementDbContext();
        var package = await db.PackageTemplates.FindAsync(packageId);
        if (package == null) return "Package was not found.";

        var startDate = DateOnly.FromDateTime(DateTime.Today);
        if (await db.MemberPackages.AnyAsync(x => x.MemberId == memberId && x.Status == "Active" && x.EndDate >= startDate))
            return "This member already has an active package.";

        db.MemberPackages.Add(new MemberPackage
        {
            MemberId = memberId,
            PackageTemplateId = packageId,
            StartDate = startDate,
            EndDate = startDate.AddMonths(package.DurationMonths),
            RemainingPtsessions = package.HasPt == true ? package.PtSessions ?? 0 : 0,
            Status = "Active"
        });
        await db.SaveChangesAsync();
        return null;
    }
}
