using System;
using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminThongKeViewModel
    {
        public DateTime TuNgay { get; set; }
        public DateTime DenNgay { get; set; }

        // KPI Thống kê
        public decimal TongDoanhThu { get; set; }
        public int TongDonDat { get; set; }
        public int DonHoanThanh { get; set; }
        public int DonChoDuyet { get; set; }
        public int DonDaHuy { get; set; }
        public double TyLeHuy { get; set; }

        public decimal TongTienMat { get; set; }
        public decimal TongChuyenKhoan { get; set; }

        // Dữ liệu cho Chart.js
        public List<string> DoanhThuTheoNgayLabels { get; set; } = new();
        public List<decimal> DoanhThuTheoNgayValues { get; set; } = new();
        public List<int> SoDonTheoNgayValues { get; set; } = new();

        public List<string> LoaiSanLabels { get; set; } = new();
        public List<int> LoaiSanValues { get; set; } = new();

        public List<string> KhungGioLabels { get; set; } = new();
        public List<int> KhungGioValues { get; set; } = new();

        public List<string> TrangThaiDonLabels { get; set; } = new();
        public List<int> TrangThaiDonValues { get; set; } = new();

        // Bảng chi tiết từng ngày
        public List<AdminThongKeNgayItemViewModel> BangTongHopNgay { get; set; } = new();
    }
}

