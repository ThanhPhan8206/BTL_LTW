using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class YeuCauDoiLich
{
    public int Id { get; set; }

    public int DatSanId { get; set; }

    public int SanBongMoiId { get; set; }

    public int KhungGioMoiId { get; set; }

    public DateOnly NgayDatMoi { get; set; }

    public string TrangThai { get; set; } = null!;

    public string? LyDo { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual DatSan DatSan { get; set; } = null!;

    public virtual KhungGio KhungGioMoi { get; set; } = null!;

    public virtual SanBong SanBongMoi { get; set; } = null!;
}
