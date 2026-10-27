using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class LichBaoTri
{
    public int Id { get; set; }

    public int SanBongId { get; set; }

    public DateTime ThoiGianBatDau { get; set; }

    public DateTime ThoiGianKetThuc { get; set; }

    public string? GhiChu { get; set; }

    public virtual SanBong SanBong { get; set; } = null!;
}
