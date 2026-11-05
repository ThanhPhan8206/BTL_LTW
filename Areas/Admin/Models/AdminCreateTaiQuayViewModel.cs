using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminCreateTaiQuayViewModel
    {
        public int? NguoiDungId { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập họ tên khách hàng")]
        public string TenKhachHang { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        public string SoDienThoai { get; set; } = string.Empty;

        public string? Email { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn sân bóng")]
        public int SanBongId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ca thi đấu")]
        public int KhungGioId { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày đặt sân")]
        public DateOnly NgayDat { get; set; } = DateOnly.FromDateTime(DateTime.Today);

        public string PhuongThucThanhToan { get; set; } = "Tiền mặt";

        public decimal SoTienThanhToan { get; set; }

        public string? GhiChu { get; set; }

        // Danh sách dịch vụ kèm số lượng
        public List<AdminSelectedDichVuViewModel> DichVus { get; set; } = new();

        // Danh sách chọn lựa cho form
        public List<NguoiDung> DanhSachKhachHang { get; set; } = new();
        public List<SanBong> DanhSachSan { get; set; } = new();
        public List<KhungGio> DanhSachKhungGio { get; set; } = new();
    }
}

