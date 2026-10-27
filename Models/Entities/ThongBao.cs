using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class ThongBao
{
    public int Id { get; set; }

    public int NguoiDungId { get; set; }

    public string TieuDe { get; set; } = null!;

    public string NoiDung { get; set; } = null!;

    public string LoaiThongBao { get; set; } = null!;

    public bool? DaDoc { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual NguoiDung NguoiDung { get; set; } = null!;
}
