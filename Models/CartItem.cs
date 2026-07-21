namespace GymManagement.Models;

public partial class CartItem
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string ItemType { get; set; } = null!;
    public int ItemId { get; set; }
    public string ItemName { get; set; } = null!;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public DateTime CreatedDate { get; set; }
}
