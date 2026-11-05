using System.Collections.Generic;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminThietLapGiaMatrixViewModel
    {
        public string SelectedNgayTrongTuan { get; set; } = "Ngày thường";
        public int? SelectedSanBongId { get; set; }

        public List<SanBong> DanhSachSan { get; set; } = new();
        public List<KhungGio> DanhSachKhungGio { get; set; } = new();
        public List<string> DanhSachNgayTrongTuan { get; set; } = new() { "Ngày thường", "Cuối tuần", "Ngày lễ" };

        // Dictionary lookup: key = $"{sanBongId}_{khungGioId}_{ngayTrongTuan}", value = decimal GiaThue
        public Dictionary<string, decimal> PriceMatrix { get; set; } = new();
    }
}

