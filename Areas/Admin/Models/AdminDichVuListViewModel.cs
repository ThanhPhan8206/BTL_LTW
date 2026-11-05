using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDichVuListViewModel
    {
        public List<AdminDichVuItemViewModel> Items { get; set; } = new();

        public string? Search { get; set; }

        public int TongDichVu { get; set; }
        public int DangKinhDoanh { get; set; }
        public int NgungBan { get; set; }
    }
}

