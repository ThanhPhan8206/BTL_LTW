using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class NguoiDung
{
    public int Id { get; set; }

    public int QuyenHanId { get; set; }

    public string HoTen { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string SoDienThoai { get; set; } = null!;

    public string MatKhauMaHoa { get; set; } = null!;

    public string? AnhDaiDienUrl { get; set; }

    public string TrangThai { get; set; } = null!;

    public DateTime? NgayTao { get; set; }

    public DateTime? NgayCapNhat { get; set; }

    public virtual ICollection<DanhGium> DanhGia { get; set; } = new List<DanhGium>();

    public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();

    public virtual QuyenHan QuyenHan { get; set; } = null!;

    public virtual ICollection<ThongBao> ThongBaos { get; set; } = new List<ThongBao>();
}
