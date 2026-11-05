using System;
using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDatSanDetailViewModel
    {
        public int Id { get; set; }
        public DateOnly NgayDat { get; set; }
        public decimal TongTien { get; set; }
        public decimal DaThanhToan { get; set; }
        public decimal ConLai => Math.Max(0, TongTien - DaThanhToan);
        public string TrangThai { get; set; } = string.Empty;
        public DateTime? NgayTao { get; set; }

        // Khách hàng
        public int NguoiDungId { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        // Sân bóng & Khung giờ
        public int SanBongId { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public string? AnhSan { get; set; }
        public int KhungGioId { get; set; }
        public string KhungGioDisplay { get; set; } = string.Empty;

        // Hóa đơn
        public bool DaXuatHoaDon { get; set; }
        public string? MaHoaDon { get; set; }
        public DateTime? NgayXuatHoaDon { get; set; }

        // Chi tiết dịch vụ đi kèm
        public List<AdminDatSanDichVuItemViewModel> DanhSachDichVu { get; set; } = new();

        // Lịch sử thanh toán
        public List<AdminDatSanThanhToanItemViewModel> DanhSachThanhToan { get; set; } = new();

        // Lịch sử thông báo liên quan
        public List<AdminDatSanThongBaoItemViewModel> DanhSachThongBao { get; set; } = new();
    }
}

