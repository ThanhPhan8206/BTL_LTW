namespace QLSanBong.Areas.KhachHang.Models
{
    public class BookingHistoryViewModel
    {
        public List<BookingHistoryItemModel> DanhSachDonDat { get; set; } = new();

        public int TongSoDon => DanhSachDonDat.Count;
        public int SoDonChoDuyet => DanhSachDonDat.Count(d => d.TrangThaiDatSan.Contains("Chờ duyệt") || d.TrangThaiDatSan.Contains("Pending"));
        public int SoDonDaDuyet => DanhSachDonDat.Count(d => d.TrangThaiDatSan.Contains("Đã duyệt") || d.TrangThaiDatSan.Contains("Xác nhận") || d.TrangThaiDatSan.Contains("Hoạt động"));
        public int SoDonDaHuy => DanhSachDonDat.Count(d => d.TrangThaiDatSan.Contains("Hủy") || d.TrangThaiDatSan.Contains("Từ chối"));

        public string? FilterTrangThai { get; set; }
    }
}

