using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class KhungGio
{
    public int Id { get; set; }

    public TimeOnly GioBatDau { get; set; }

    public TimeOnly GioKetThuc { get; set; }

    public virtual ICollection<DatSan> DatSans { get; set; } = new List<DatSan>();

    public virtual ICollection<ThietLapGium> ThietLapGia { get; set; } = new List<ThietLapGium>();

    public virtual ICollection<YeuCauDoiLich> YeuCauDoiLiches { get; set; } = new List<YeuCauDoiLich>();
}
