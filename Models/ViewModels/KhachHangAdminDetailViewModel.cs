using System;
using System.Collections.Generic;

namespace QLSanBong.Models.ViewModels
{
    public class KhachHangAdminDetailViewModel
    {
        public int Id { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? AnhDaiDienUrl { get; set; }
        public string TrangThai { get; set; } = "Hoạt động";
        public DateTime? NgayTao { get; set; }
        public DateTime? NgayCapNhat { get; set; }

        // 4 chỉ số thống kê cá nhân
        public int TongDonDat { get; set; }
        public int DonHoanThanh { get; set; }
        public int DonDaHuy { get; set; }
        public decimal TongChiTieu { get; set; }

        // Danh sách lịch sử đặt sân
        public List<KhachHangLichSuDatSanItemViewModel> LichSuDatSan { get; set; } = new();
    }
}

