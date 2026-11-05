namespace QLSanBong.Areas.KhachHang.Models
{
    public class ServiceSelectionItemModel
    {
        public int DichVuId { get; set; }
        public string TenDichVu { get; set; } = string.Empty;
        public decimal GiaDichVu { get; set; }
        public string DonViTinh { get; set; } = "Lượt";
        public int SoLuong { get; set; } = 0;
        public bool IsSelected { get; set; } = false;
        public decimal ThanhTien => IsSelected ? SoLuong * GiaDichVu : 0;
    }
}

