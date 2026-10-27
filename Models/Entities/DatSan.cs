using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class DatSan
{
    public int Id { get; set; }

    public int NguoiDungId { get; set; }

    public int SanBongId { get; set; }

    public int KhungGioId { get; set; }

    public DateOnly NgayDat { get; set; }

    public decimal TongTien { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime? NgayTao { get; set; }

    public virtual ICollection<ChiTietDichVuDat> ChiTietDichVuDats { get; set; } = new List<ChiTietDichVuDat>();

    public virtual DanhGium? DanhGium { get; set; }

    public virtual HoaDon? HoaDon { get; set; }

    public virtual KhungGio KhungGio { get; set; } = null!;

    public virtual NguoiDung NguoiDung { get; set; } = null!;

    public virtual SanBong SanBong { get; set; } = null!;

    public virtual ICollection<ThanhToan> ThanhToans { get; set; } = new List<ThanhToan>();

    public virtual ICollection<YeuCauDoiLich> YeuCauDoiLiches { get; set; } = new List<YeuCauDoiLich>();
}
