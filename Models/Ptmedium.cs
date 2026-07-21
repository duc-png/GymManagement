using System;
using System.Collections.Generic;

namespace GymManagement.Models;

public partial class Ptmedium
{
    public int Id { get; set; }

    public int? Ptid { get; set; }

    public string MediaType { get; set; } = null!;

    public string MediaUrl { get; set; } = null!;

    public string? Caption { get; set; }

    public DateTime? UploadedDate { get; set; }

    public virtual User? Pt { get; set; }
}
