using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class Feedback
{
    public int Id { get; set; }

    public int? MemberId { get; set; }

    public string FeedbackType { get; set; } = null!;

    public int? TargetPtid { get; set; }

    public int? EquipmentId { get; set; }

    public int? RatingStars { get; set; }

    public string Comment { get; set; } = null!;

    public DateTime? SubmittedDate { get; set; }

    public virtual Equipment? Equipment { get; set; }

    public virtual Member? Member { get; set; }

    public virtual User? TargetPt { get; set; }
}
