using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class User
{
    public int Id { get; set; }

    public string Username { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string PhoneNumber { get; set; } = null!;

    public string? Avatar { get; set; }

    public string Role { get; set; } = null!;

    public string? Status { get; set; }

    public string? Specialty { get; set; }

    public string? Ptstatus { get; set; }

    public decimal? PthourlyRate { get; set; }

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();

    public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();

    public virtual ICollection<Ptbooking> Ptbookings { get; set; } = new List<Ptbooking>();

    public virtual ICollection<Ptmedium> Ptmedia { get; set; } = new List<Ptmedium>();
}
