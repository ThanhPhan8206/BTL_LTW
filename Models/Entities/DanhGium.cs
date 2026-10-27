using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class DanhGium
{
    public int Id { get; set; }

    public int DatSanId { get; set; }

    public int NguoiDungId { get; set; }

    public int SanBongId { get; set; }

    public int SoSao { get; set; }

    public string? BinhLuan { get; set; }

    public DateTime? NgayTao { get; set; }

    public virtual DatSan DatSan { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual SanBong SanBong { get; set; } = null!;
}
