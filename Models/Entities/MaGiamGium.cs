using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class MaGiamGium
{
    public int Id { get; set; }

    public string MaCode { get; set; } = null!;

    public int? PhanTramGiam { get; set; }

    public decimal? SoTienGiam { get; set; }

    public DateTime HieuLucTu { get; set; }

    public DateTime HieuLucDen { get; set; }

    public decimal? GiaTriDonToiThieu { get; set; }
}
