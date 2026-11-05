using System;

namespace QLSanBong.Models.ViewModels
{
    public class KhachHangAdminItemViewModel
    {
        public int Id { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? AnhDaiDienUrl { get; set; }
        public string TrangThai { get; set; } = "Hoạt động";
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        public int TongDonDat { get; set; }
        public decimal TongChiTieu { get; set; }
    }
}

