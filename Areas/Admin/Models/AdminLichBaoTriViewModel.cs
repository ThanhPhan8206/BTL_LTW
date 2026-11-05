using System;
using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminLichBaoTriViewModel
    {
        public List<AdminLichBaoTriItemViewModel> DanhSachBaoTri { get; set; } = new();

        public int? SelectedSanBongId { get; set; }

        public List<AdminSanBongItemViewModel> DanhSachSan { get; set; } = new();

        // Form fields to add new maintenance
        public int SanBongId { get; set; }
        public DateTime ThoiGianBatDau { get; set; } = DateTime.Now.AddHours(1);
        public DateTime ThoiGianKetThuc { get; set; } = DateTime.Now.AddHours(4);
        public string? GhiChu { get; set; }

        // Conflicting bookings list if blocked
        public List<AdminConflictBookingItemViewModel> ConflictingBookings { get; set; } = new();
    }
}

