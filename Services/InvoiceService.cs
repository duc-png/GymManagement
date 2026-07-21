using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class InvoiceService
{
    public async Task<List<Invoice>> GetMemberPurchaseHistoryAsync(int userId)
    {
        using var db = new GymManagementDbContext();
        return await db.Invoices.AsNoTracking()
            .Where(x => x.Member != null && x.Member.UserId == userId)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();
    }
}
