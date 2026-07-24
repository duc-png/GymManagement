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

    public string PaymentStatus { get; set; } = null!;

    public DateTime? ConfirmedDate { get; set; }

    public int? ConfirmedByUserId { get; set; }

    public string PaymentMethodDisplay => PaymentMethod switch
    {
        "Cash" => "Tiền mặt tại quầy",
        "Transfer" => "Chuyển khoản",
        "Card" => "Thẻ",
        "MoMo" => "MoMo",
        _ => PaymentMethod
    };

    public string PaymentStatusDisplay => PaymentStatus switch
    {
        "PendingCash" => "Chờ thanh toán tại quầy",
        "PendingTransfer" => "Chờ xác nhận chuyển khoản",
        "Paid" => "Đã thanh toán",
        "Rejected" => "Đã từ chối",
        _ => PaymentStatus
    };

    public virtual ICollection<InvoiceDetail> InvoiceDetails { get; set; } = new List<InvoiceDetail>();

    public virtual Member? Member { get; set; }

    public virtual User? User { get; set; }
}
