using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class InvoiceDetail
{
    public int Id { get; set; }

    public int? InvoiceId { get; set; }

    public string ItemType { get; set; } = null!;

    public int ItemId { get; set; }

    public string ItemName { get; set; } = null!;

    public int Quantity { get; set; }

    public decimal UnitPrice { get; set; }

    public decimal TotalPrice { get; set; }

    public virtual Invoice? Invoice { get; set; }
}
