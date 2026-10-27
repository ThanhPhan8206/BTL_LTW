using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class SanBong
{
    public int Id { get; set; }

    public string TenSan { get; set; } = null!;

    public string LoaiSan { get; set; } = null!;

    public string? MoTa { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime? NgayTao { get; set; }

    public string? AnhSan { get; set; }

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();

    public virtual ICollection<LichBaoTri> LichBaoTris { get; set; } = new List<LichBaoTri>();

    public virtual ICollection<ThietLapGium> ThietLapGia { get; set; } = new List<ThietLapGium>();

    public virtual ICollection<YeuCauDoiLich> YeuCauDoiLiches { get; set; } = new List<YeuCauDoiLich>();
}
