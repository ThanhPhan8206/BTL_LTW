using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.KhachHang.Models
{
    public class CustomerHomeViewModel
    {
        public int UserId { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? AnhDaiDienUrl { get; set; }

        public int TongSoCaDaDat { get; set; }
        public int SoCaSapDienRa { get; set; }
        public int SoDonChoDuyet { get; set; }
        public int SoThongBaoChuaDoc { get; set; }
        public int UuDaiHienCo { get; set; }

        // Các thuộc tính phục vụ bộ lọc tìm kiếm sân trống thông minh
        public DateTime NgayLoc { get; set; } = DateTime.Today;
        public string? LoaiSanLoc { get; set; }
        public decimal? GiaToiDa { get; set; }
        public int? KhungGioLocId { get; set; }

        public List<CustomerPitchDisplayModel> DanhSachSan { get; set; } = new();
        public List<DatSan> LichDatGanDay { get; set; } = new();
        public List<ThongBao> ThongBaoGanDay { get; set; } = new();
        public List<NotificationItemViewModel> DanhSachThongBaoMoi { get; set; } = new();
        public List<KhungGio> DanhSachKhungGio { get; set; } = new();
    }
}
