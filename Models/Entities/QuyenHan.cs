using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class QuyenHan
{
    public int Id { get; set; }

    public string TenQuyen { get; set; } = null!;

    public virtual ICollection<NguoiDung> NguoiDungs { get; set; } = new List<NguoiDung>();
}
