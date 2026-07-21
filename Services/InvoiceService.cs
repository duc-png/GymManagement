using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class InvoiceService
{
    public async Task<Invoice?> GetInvoiceAsync(int invoiceId)
    {
        using var db = new GymManagementDbContext();
        return await db.Invoices.AsNoTracking()
            .Include(x => x.Member)
            .Include(x => x.User)
            .Include(x => x.InvoiceDetails)
            .SingleOrDefaultAsync(x => x.Id == invoiceId);
    }

    public async Task<List<Invoice>> GetMemberPurchaseHistoryAsync(int userId)
    {
        using var db = new GymManagementDbContext();
        return await db.Invoices.AsNoTracking()
            .Where(x => x.Member != null && x.Member.UserId == userId)
            .OrderByDescending(x => x.CreatedDate)
            .ToListAsync();
    }
}
