using System.ComponentModel.DataAnnotations;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.KhachHang.Models
{
    public class BookingCreateViewModel
    {
        [Required]
        public int SanBongId { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public string LoaiSan { get; set; } = string.Empty;
        public string AnhSan { get; set; } = "/uploads/sanbong/san-5-so-1.jpg";

        [Required(ErrorMessage = "Vui lòng chọn ngày thi đấu.")]
        [Display(Name = "Ngày đặt sân")]
        public DateOnly NgayDat { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        [Required(ErrorMessage = "Vui lòng chọn khung giờ thi đấu.")]
        [Range(1, int.MaxValue, ErrorMessage = "Vui lòng chọn khung giờ thi đấu.")]
        [Display(Name = "Khung giờ thi đấu")]
        public int KhungGioId { get; set; }
        public string KhungGioDisplay { get; set; } = string.Empty;

        public decimal GiaTienSan { get; set; }

        // Thông tin người đặt sân (Auto-fill)
        public string HoTenNguoiDat { get; set; } = string.Empty;
        public string SoDienThoaiNguoiDat { get; set; } = string.Empty;
        public string EmailNguoiDat { get; set; } = string.Empty;

        // Ghi chú cho ban quản lý sân
        [Display(Name = "Ghi chú đặc biệt (Chi đoàn / Yêu cầu)")]
        [StringLength(500, ErrorMessage = "Ghi chú không được dài quá 500 ký tự.")]
        public string? GhiChu { get; set; }

        // Danh sách dịch vụ đi kèm (nước uống, bóng, áo bib...)
        public List<ServiceSelectionItemModel> DanhSachDichVu { get; set; } = new();

        // Mã giảm giá / Voucher
        [Display(Name = "Mã Voucher / Giảm giá")]
        public string? MaVoucher { get; set; }
        public decimal SoTienGiam { get; set; } = 0;
        public string? ThongBaoVoucher { get; set; }

        // Hình thức thanh toán: "DatCoc" (30% hoặc 50%) hoặc "ToanBo" (100%)
        [Required]
        [Display(Name = "Hình thức thanh toán")]
        public string HinhThucThanhToan { get; set; } = "DatCoc";

        public decimal PhanTramDatCoc { get; set; } = 30; // 30% giá trị đơn
        public decimal SoTienDatCoc { get; set; }
        public decimal TongTienDichVu { get; set; }
        public decimal TongTienTamTinh { get; set; }
        public decimal TongTienThanhToan { get; set; }
        public decimal SoTienPhaiTraNgay { get; set; }

        // Danh sách hỗ trợ dropdowns
        public List<KhungGio> DanhSachKhungGio { get; set; } = new();
        public List<SanBong> DanhSachSanCungLoai { get; set; } = new();
    }
}

