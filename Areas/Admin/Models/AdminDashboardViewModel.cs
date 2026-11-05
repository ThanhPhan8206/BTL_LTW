using System.Collections.Generic;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDashboardViewModel
    {
        // 4 KPI Cards
        public int ChoDuyetCount { get; set; }
        public int YeuCauDoiLichCount { get; set; }
        public int DonHomNayCount { get; set; }
        public decimal DoanhThuHomNay { get; set; }

        // Additional stats
        public int TongSanHoatDong { get; set; }
        public int TongKhachHang { get; set; }

        // Chart Data for 7 days
        public List<AdminRevenueChartItemViewModel> RevenueChartData { get; set; } = new();

        // 5 recent pending bookings
        public List<AdminBookingItemViewModel> RecentPendingBookings { get; set; } = new();
    }
}

