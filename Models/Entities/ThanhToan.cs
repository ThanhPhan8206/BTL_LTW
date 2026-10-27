using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class ThanhToan
{
    public int Id { get; set; }

    public int DatSanId { get; set; }

    public decimal SoTien { get; set; }

    public string PhuongThucThanhToan { get; set; } = null!;

    public string LoaiThanhToan { get; set; } = null!;

    public string TrangThai { get; set; } = null!;

    public string? MaGiaoDich { get; set; }

    public DateTime? ThoiGianThanhToan { get; set; }

    public virtual DatSan DatSan { get; set; } = null!;
}
