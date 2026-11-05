using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminRevenueChartItemViewModel
    {
        public DateTime Date { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int BookingCount { get; set; }
    }
}

