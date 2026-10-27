using System;
using System.Collections.Generic;

namespace QLSanBong.Models.Entities;

public partial class ThietLapGium
{
    public int Id { get; set; }

    public int SanBongId { get; set; }

    public int KhungGioId { get; set; }

    public string NgayTrongTuan { get; set; } = null!;

    public decimal GiaThue { get; set; }

    public virtual KhungGio KhungGio { get; set; } = null!;

    public virtual SanBong SanBong { get; set; } = null!;
}
