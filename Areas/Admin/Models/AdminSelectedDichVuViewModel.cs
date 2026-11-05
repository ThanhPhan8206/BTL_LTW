namespace QLSanBong.Areas.Admin.Models
{
    public class AdminSelectedDichVuViewModel
    {
        public int DichVuId { get; set; }
        public string TenDichVu { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public string DonViTinh { get; set; } = string.Empty;
        public int SoLuong { get; set; }
    }
}

