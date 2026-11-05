using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminThongBaoItemViewModel
    {
        public int Id { get; set; }
        public int NguoiDungId { get; set; }

        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;

        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiThongBao { get; set; } = string.Empty;
        public bool DaDoc { get; set; }
        public DateTime? NgayTao { get; set; }
    }
}

