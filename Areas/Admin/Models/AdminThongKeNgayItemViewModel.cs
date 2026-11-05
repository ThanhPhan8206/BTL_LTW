using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminThongKeNgayItemViewModel
    {
        public DateOnly Ngay { get; set; }
        public int SoDon { get; set; }
        public int SoDonHuy { get; set; }
        public decimal DoanhThu { get; set; }
        public double TyLeHuy => SoDon > 0 ? Math.Round((double)SoDonHuy / SoDon * 100, 1) : 0;
    }
}

