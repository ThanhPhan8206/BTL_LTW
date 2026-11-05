namespace QLSanBong.Areas.Admin.Models
{
    public class AdminPriceCellViewModel
    {
        public int SanBongId { get; set; }
        public string TenSan { get; set; } = string.Empty;
        public int KhungGioId { get; set; }
        public string KhungGioDisplay { get; set; } = string.Empty;
        public string NgayTrongTuan { get; set; } = "Ngày thường";
        public decimal GiaThue { get; set; }
        public int? ThietLapGiaId { get; set; }
    }
}

