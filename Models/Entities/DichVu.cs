using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class DichVu
{
    public int Id { get; set; }

    public string TenDichVu { get; set; } = null!;

    public decimal GiaDichVu { get; set; }

    public string DonViTinh { get; set; } = null!;

    public virtual ICollection<ChiTietDichVuDat> ChiTietDichVuDats { get; set; } = new List<ChiTietDichVuDat>();
}
