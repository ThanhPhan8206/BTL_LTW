using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminKhungGioItemViewModel
    {
        public int Id { get; set; }
        public TimeOnly GioBatDau { get; set; }
        public TimeOnly GioKetThuc { get; set; }
        public string KhungGioDisplay => $"{GioBatDau:HH:mm} - {GioKetThuc:HH:mm}";
        public int TongDonDat { get; set; }
        public bool CoTheXoa { get; set; }
    }
}

