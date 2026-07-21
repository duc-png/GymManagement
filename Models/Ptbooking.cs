using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class Ptbooking
{
    public int Id { get; set; }

    public int? MemberId { get; set; }

    public int? Ptid { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public string? Status { get; set; }

    public virtual Member? Member { get; set; }

    public virtual User? Pt { get; set; }
}
