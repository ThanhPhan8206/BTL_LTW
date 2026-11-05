using System;

namespace QLSanBong.Models.ViewModels
{
    public class KhachHangLichSuDatSanItemViewModel
    {
        public int Id { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public string KhungGioDisplay { get; set; } = string.Empty;
        public DateOnly NgayDat { get; set; }
        public decimal TongTien { get; set; }
        public decimal DaThanhToan { get; set; }
        public string TrangThai { get; set; } = "Chờ duyệt";
        public DateTime? NgayTao { get; set; }
    }
}

