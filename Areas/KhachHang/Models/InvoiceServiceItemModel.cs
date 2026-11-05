namespace QLSanBong.Areas.KhachHang.Models
{
    public class InvoiceServiceItemModel
    {
        public string TenDichVu { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = "Lượt";
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien => SoLuong * DonGia;
    }
}

