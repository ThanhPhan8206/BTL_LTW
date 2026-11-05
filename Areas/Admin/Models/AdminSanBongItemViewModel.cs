using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminSanBongItemViewModel
    {
        public int Id { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public string TrangThai { get; set; } = "Sẵn sàng";
        public DateTime? NgayTao { get; set; }
        public string? AnhSan { get; set; }

        public int TongDonDat { get; set; }
        public bool DangBaoTri { get; set; }
        public string? ThongTinBaoTriHienTai { get; set; }
        public bool CoTheXoa { get; set; }
    }
}

