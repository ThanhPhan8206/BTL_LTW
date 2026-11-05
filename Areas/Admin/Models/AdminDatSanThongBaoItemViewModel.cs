using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDatSanThongBaoItemViewModel
    {
        public int Id { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public string LoaiThongBao { get; set; } = string.Empty;
        public DateTime? NgayTao { get; set; }
    }
}

