using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class CheckInHistory
{
    public int Id { get; set; }

    public int? MemberId { get; set; }

    public DateTime? CheckInTime { get; set; }

    public DateTime? CheckOutTime { get; set; }

    public virtual Member? Member { get; set; }
}
