using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class MemberPackageService
{
    public async Task<List<MemberPackage>> GetMyPackagesAsync(int userId)
    {
        using var db = new GymManagementDbContext();
        return await db.MemberPackages.AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.PackageTemplate)
            .Where(x => x.Member != null && x.Member.UserId == userId)
            .OrderByDescending(x => x.StartDate)
            .ToListAsync();
    }
}
