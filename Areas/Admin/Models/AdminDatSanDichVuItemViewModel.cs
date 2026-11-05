namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDatSanDichVuItemViewModel
    {
        public int DichVuId { get; set; }
        public string TenDichVu { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
    }
}

