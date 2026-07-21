using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class Invoice
{
    public int Id { get; set; }

    public string InvoiceCode { get; set; } = null!;

    public int? UserId { get; set; }

    public int? MemberId { get; set; }

    public DateTime? CreatedDate { get; set; }

    public decimal TotalAmount { get; set; }

    public int? DiscountPercent { get; set; }

    public decimal FinalAmount { get; set; }

    public string PaymentMethod { get; set; } = null!;

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual Member? Member { get; set; }

    public virtual User? User { get; set; }
}
