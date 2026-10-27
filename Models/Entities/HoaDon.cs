using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class HoaDon
{
    public int Id { get; set; }

    public int DatSanId { get; set; }

    public string MaHoaDon { get; set; } = null!;

    public DateTime? NgayXuat { get; set; }

    public virtual DatSan DatSan { get; set; } = null!;
}
