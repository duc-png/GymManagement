using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class PtService
{
    public async Task<List<User>> GetPortfolioAsync()
    {
        using var db = new GymManagementDbContext();
        return await db.Users.AsNoTracking()
            .Include(x => x.Ptmedia)
            .Where(x => x.Role == UserRoles.Pt)
            .OrderBy(x => x.FullName)
            .ToListAsync();
    }
}
