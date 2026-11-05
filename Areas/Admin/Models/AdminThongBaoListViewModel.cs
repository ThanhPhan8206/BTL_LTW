using System.Collections.Generic;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminThongBaoListViewModel
    {
        public List<AdminThongBaoItemViewModel> Items { get; set; } = new();

        public string? Search { get; set; }
        public string? LoaiThongBao { get; set; }

        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
        public int TotalItems { get; set; }

        public int TongThongBao { get; set; }
        public int SoThongBaoSuKien { get; set; }
        public int SoThongBaoHeThong { get; set; }
        public int SoThongBaoKhuyenMai { get; set; }

        public List<NguoiDung> DanhSachKhachHang { get; set; } = new();
    }
}

