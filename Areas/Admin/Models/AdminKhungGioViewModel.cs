using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminKhungGioViewModel
    {
        public List<AdminKhungGioItemViewModel> Items { get; set; } = new();

        public int TotalSlots => Items.Count;
        public int TotalBookings => Items.Count > 0 ? 0 : 0; // Calculated dynamically in Controller
    }
}

