using System.Collections.Generic;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminMaGiamGiaListViewModel
    {
        public List<AdminMaGiamGiaItemViewModel> Items { get; set; } = new();

        public string? Search { get; set; }
        public string? TrangThai { get; set; }

        public int TongVoucher { get; set; }
        public int ConHieuLuc { get; set; }
        public int HetHieuLuc { get; set; }

        public List<NguoiDung> DanhSachKhachHang { get; set; } = new();
    }
}

