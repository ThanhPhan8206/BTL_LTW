using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class ChiTietDichVuDat
{
    public int Id { get; set; }

    public int DatSanId { get; set; }

    public int DichVuId { get; set; }

    public int SoLuong { get; set; }

    public decimal DonGia { get; set; }

    public virtual DatSan DatSan { get; set; } = null!;

    public virtual DichVu DichVu { get; set; } = null!;
}
