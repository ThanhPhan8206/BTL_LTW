using System;
using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminHoaDonDetailViewModel
    {
        public string MaHoaDon { get; set; } = string.Empty;
        public DateTime? NgayXuat { get; set; }

        public int DatSanId { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public string KhungGioDisplay { get; set; } = string.Empty;
        public DateOnly NgayDat { get; set; }

        public decimal TienSan { get; set; }
        public List<AdminDatSanDichVuItemViewModel> DichVus { get; set; } = new();
        public decimal TongTienDichVu { get; set; }
        public decimal TongTien { get; set; }
        public decimal DaThanhToan { get; set; }
        public decimal ConLai => Math.Max(0, TongTien - DaThanhToan);
        public string TrangThaiDon { get; set; } = string.Empty;
    }
}

