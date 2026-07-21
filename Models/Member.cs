using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class Member
{
    public int Id { get; set; }

    public string MemberCode { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public DateOnly? DateOfBirth { get; set; }

    public string? Gender { get; set; }

    public string PhoneNumber { get; set; } = null!;

    public string? Email { get; set; }

    public string? Avatar { get; set; }

    public DateOnly? RegistrationDate { get; set; }

    public int? UserId { get; set; }

    public virtual ICollection<CheckInHistory> CheckInHistories { get; set; } = new List<CheckInHistory>();

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<MemberPackage> MemberPackages { get; set; } = new List<MemberPackage>();

    public virtual ICollection<Ptbooking> Ptbookings { get; set; } = new List<Ptbooking>();

    public virtual User? User { get; set; }
}
