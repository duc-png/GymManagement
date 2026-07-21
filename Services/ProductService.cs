using GymManagement.Models;
using Microsoft.EntityFrameworkCore;

namespace GymManagement.Services;

public class ProductService
{
    public async Task<List<Product>> GetAsync()
    {
        using var db = new GymManagementDbContext();
        return await db.Products.AsNoTracking().OrderBy(x => x.ProductName).ToListAsync();
    }

    public async Task<string?> SaveAsync(int? id, string name, decimal price, int stockQuantity, string role)
    {
        if (role != UserRoles.Admin) return "Chỉ Admin được thêm hoặc sửa sản phẩm.";
        if (string.IsNullOrWhiteSpace(name)) return "Tên sản phẩm là bắt buộc.";
        if (price <= 0) return "Giá sản phẩm phải lớn hơn 0.";
        if (stockQuantity < 0) return "Số lượng tồn kho không được nhỏ hơn 0.";

        using var db = new GymManagementDbContext();
        if (await db.Products.AnyAsync(x => x.ProductName == name.Trim() && x.Id != id))
            return "Tên sản phẩm đã tồn tại.";

        if (id == null)
        {
            db.Products.Add(new Product
            {
                ProductName = name.Trim(),
                Price = price,
                StockQuantity = stockQuantity
            });
        }
        else
        {
            var product = await db.Products.FindAsync(id.Value);
            if (product == null) return "Không tìm thấy sản phẩm.";
            product.ProductName = name.Trim();
            product.Price = price;
            product.StockQuantity = stockQuantity;
        }

        await db.SaveChangesAsync();
        return null;
    }

    public async Task<string?> DeleteAsync(int id, string role)
    {
        if (role != UserRoles.Admin) return "Chỉ Admin được xóa sản phẩm.";
        using var db = new GymManagementDbContext();
        var product = await db.Products.FindAsync(id);
        if (product == null) return "Không tìm thấy sản phẩm.";
        db.Products.Remove(product);
        await db.SaveChangesAsync();
        return null;
    }
}
