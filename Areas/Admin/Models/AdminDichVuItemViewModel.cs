namespace QLSanBong.Areas.Admin.Models
{
    public class AdminDichVuItemViewModel
    {
        public int Id { get; set; }
        public string TenDichVu { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal GiaDichVu { get; set; }
        public bool DaNgungBan { get; set; }
        public int SoLuotSuDung { get; set; }
    }
}

