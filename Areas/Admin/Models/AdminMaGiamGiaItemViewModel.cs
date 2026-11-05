using System;

namespace QLSanBong.Areas.Admin.Models
{
    public class AdminMaGiamGiaItemViewModel
    {
        public int Id { get; set; }
        public string MaCode { get; set; } = string.Empty;
        public int? PhanTramGiam { get; set; }
        public decimal? SoTienGiam { get; set; }
        public DateTime HieuLucTu { get; set; }
        public DateTime HieuLucDen { get; set; }
        public decimal? GiaTriDonToiThieu { get; set; }

        public string TrangThaiHienTai
        {
            get
            {
                var now = DateTime.Now;
                if (now < HieuLucTu) return "Chưa bắt đầu";
                if (now > HieuLucDen) return "Hết hiệu lực";
                return "Còn hiệu lực";
            }
        }
    }
}

