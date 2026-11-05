using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Areas.KhachHang.Models;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.KhachHang.Controllers
{
    [Area("KhachHang")]
    [Authorize]
    public class HomeController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public HomeController(QLSanBongDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            DateTime? ngayLoc,
            string? loaiSanLoc,
            decimal? giaToiDa,
            int? khungGioLocId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (!int.TryParse(userIdStr, out int userId))
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var user = await _context.NguoiDungs
                .FirstOrDefaultAsync(u => u.Id == userId);

            // Tự động lưu thông tin vào Session để sẵn sàng Auto-fill ở form đặt sân các bước tiếp theo
            if (user != null)
            {
                HttpContext.Session.SetString("Customer_UserId", user.Id.ToString());
                HttpContext.Session.SetString("Customer_HoTen", user.HoTen);
                HttpContext.Session.SetString("Customer_Email", user.Email);
                HttpContext.Session.SetString("Customer_SoDienThoai", user.SoDienThoai);
                if (!string.IsNullOrEmpty(user.AnhDaiDienUrl))
                {
                    HttpContext.Session.SetString("Customer_AvatarUrl", user.AnhDaiDienUrl);
                }
            }

            // Chuẩn hóa ngày lọc (Mặc định là ngày hôm nay nếu người dùng chưa chọn)
            DateTime filterDate = (ngayLoc.HasValue && ngayLoc.Value.Date >= DateTime.Today)
                ? ngayLoc.Value.Date
                : DateTime.Today;
            DateOnly targetDate = DateOnly.FromDateTime(filterDate);

            // Danh sách toàn bộ khung giờ trong ngày
            var allTimeSlots = await _context.KhungGios
                .OrderBy(k => k.GioBatDau)
                .ToListAsync();

            KhungGio? selectedSlot = khungGioLocId.HasValue
                ? allTimeSlots.FirstOrDefault(k => k.Id == khungGioLocId.Value)
                : null;

            // ===============================================================
            // LOGIC LỌC THÔNG MINH:
            // 1. Loại trừ các sân nằm trong bảng dbo.LichBaoTri
            // 2. Loại trừ các sân đã kín lịch trong dbo.DatSan theo ngày và ca đá
            // ===============================================================
            var maintenancePitchIds = new List<int>();
            if (selectedSlot != null)
            {
                DateTime slotStartDateTime = filterDate.Add(selectedSlot.GioBatDau.ToTimeSpan());
                DateTime slotEndDateTime = filterDate.Add(selectedSlot.GioKetThuc.ToTimeSpan());

                maintenancePitchIds = await _context.LichBaoTris
                    .Where(m => m.ThoiGianBatDau < slotEndDateTime && m.ThoiGianKetThuc > slotStartDateTime)
                    .Select(m => m.SanBongId)
                    .Distinct()
                    .ToListAsync();
            }
            else
            {
                DateTime dayStart = filterDate;
                DateTime dayEnd = filterDate.AddDays(1);

                maintenancePitchIds = await _context.LichBaoTris
                    .Where(m => m.ThoiGianBatDau <= dayStart && m.ThoiGianKetThuc >= dayEnd)
                    .Select(m => m.SanBongId)
                    .Distinct()
                    .ToListAsync();
            }

            var bookedPitchIds = new List<int>();
            if (selectedSlot != null)
            {
                // Loại trừ sân đã được đặt trong đúng khung giờ đó
                bookedPitchIds = await _context.DatSans
                    .Where(d => d.NgayDat == targetDate &&
                                d.KhungGioId == selectedSlot.Id &&
                                d.TrangThai != "Đã hủy" &&
                                d.TrangThai != "Từ chối" &&
                                d.TrangThai != "Cancelled")
                    .Select(d => d.SanBongId)
                    .Distinct()
                    .ToListAsync();
            }
            else if (allTimeSlots.Any())
            {
                // Nếu chưa chọn khung giờ cụ thể, kiểm tra xem sân nào đã kín toàn bộ tất cả các ca trong ngày
                int totalSlotsCount = allTimeSlots.Count;
                bookedPitchIds = await _context.DatSans
                    .Where(d => d.NgayDat == targetDate &&
                                d.TrangThai != "Đã hủy" &&
                                d.TrangThai != "Từ chối" &&
                                d.TrangThai != "Cancelled")
                    .GroupBy(d => d.SanBongId)
                    .Where(g => g.Select(x => x.KhungGioId).Distinct().Count() >= totalSlotsCount)
                    .Select(g => g.Key)
                    .ToListAsync();
            }

            var excludedPitchIds = maintenancePitchIds
                .Union(bookedPitchIds)
                .Distinct()
                .ToHashSet();

            // Truy vấn danh sách sân từ dbo.SanBong kết hợp dbo.ThietLapGia
            var query = _context.SanBongs
                .Include(s => s.ThietLapGia)
                .ThenInclude(t => t.KhungGio)
                .Where(s => s.TrangThai == "Hoạt động" || s.TrangThai == "Sẵn sàng" || s.TrangThai == "Active")
                .AsQueryable();

            // Loại trừ các sân không khả dụng do bảo trì hoặc đã kín lịch
            if (excludedPitchIds.Any())
            {
                query = query.Where(s => !excludedPitchIds.Contains(s.Id));
            }

            // Lọc theo loại sân ("5", "7", "11")
            if (!string.IsNullOrWhiteSpace(loaiSanLoc) && loaiSanLoc != "Tất cả")
            {
                query = query.Where(s => s.LoaiSan.Contains(loaiSanLoc));
            }

            var dbPitches = await query.ToListAsync();
            var pitchDisplayList = new List<CustomerPitchDisplayModel>();

            if (dbPitches.Any())
            {
                foreach (var p in dbPitches)
                {
                    decimal pitchPrice = 0;
                    if (selectedSlot != null)
                    {
                        var slotPrice = p.ThietLapGia.FirstOrDefault(t => t.KhungGioId == selectedSlot.Id);
                        pitchPrice = slotPrice != null ? slotPrice.GiaThue : (p.ThietLapGia.Any() ? p.ThietLapGia.Min(t => t.GiaThue) : (p.LoaiSan.Contains("7") ? 450000m : 280000m));
                    }
                    else
                    {
                        pitchPrice = p.ThietLapGia.Any() ? p.ThietLapGia.Min(t => t.GiaThue) : (p.LoaiSan.Contains("7") ? 450000m : 280000m);
                    }

                    // Lọc theo giá tối đa nếu người dùng nhập
                    if (giaToiDa.HasValue && giaToiDa.Value > 0 && pitchPrice > giaToiDa.Value)
                    {
                        continue;
                    }

                    string img = !string.IsNullOrWhiteSpace(p.AnhSan)
                        ? p.AnhSan
                        : (p.LoaiSan.Contains("7") ? "/uploads/sanbong/san-7-so-1.jpg" : "/uploads/sanbong/san-5-so-1.jpg");

                    pitchDisplayList.Add(new CustomerPitchDisplayModel
                    {
                        Id = p.Id,
                        TenSan = p.TenSan,
                        LoaiSan = p.LoaiSan,
                        MoTa = p.MoTa ?? "Mặt sân cỏ nhân tạo đạt chuẩn thi đấu sinh viên UTC, dàn đèn LED cao cấp chống chói.",
                        TrangThai = p.TrangThai,
                        AnhSan = img,
                        GiaThamKhao = pitchPrice,
                        GiaTheoKhungGio = selectedSlot != null ? pitchPrice : null,
                        DacDiem = p.LoaiSan.Contains("7") ? "Sân 7 người tiêu chuẩn thi đấu Giải" : (p.LoaiSan.Contains("11") ? "Sân 11 người mặt cỏ chuẩn Quốc gia" : "Sân 5 người cỏ mềm êm chân"),
                        IsAvailable = true
                    });
                }
            }
            else
            {
                // Fallback 3 sân mẫu gắn đúng ảnh trong wwwroot/uploads/sanbong/ nếu CSDL chưa có dữ liệu mẫu
                var fallbackList = new List<CustomerPitchDisplayModel>
                {
                    new CustomerPitchDisplayModel
                    {
                        Id = 1,
                        TenSan = "Sân 5A - Mặt Cỏ FIFA",
                        LoaiSan = "Sân 5 người",
                        MoTa = "Mặt cỏ sợi đệm cao su êm ái, hệ thống chiếu sáng ban đêm 6 cột đèn LED, lưới bao mới 100%.",
                        TrangThai = "Sẵn sàng",
                        AnhSan = "/uploads/sanbong/san-5-so-1.jpg",
                        GiaThamKhao = 250000,
                        DacDiem = "Phù hợp thi đấu giao hữu chi đoàn lớp",
                        IsAvailable = true
                    },
                    new CustomerPitchDisplayModel
                    {
                        Id = 2,
                        TenSan = "Sân 5B - Khán Đài Thoáng",
                        LoaiSan = "Sân 5 người",
                        MoTa = "Vị trí sát khu kỹ thuật và khu nước giải khát, khán đài mini dành cho cổ động viên chi đoàn.",
                        TrangThai = "Sẵn sàng",
                        AnhSan = "/uploads/sanbong/san-5-so-2.jpg",
                        GiaThamKhao = 280000,
                        DacDiem = "Khu vực trung tâm sôi động nhất",
                        IsAvailable = true
                    },
                    new CustomerPitchDisplayModel
                    {
                        Id = 3,
                        TenSan = "Sân 7A - Tiêu Chuẩn Liên Đoàn",
                        LoaiSan = "Sân 7 người",
                        MoTa = "Kích thước tiêu chuẩn 30m x 50m, phù hợp cho các giải đấu Hội đồng Trường, Liên chi đoàn & Khoa viện.",
                        TrangThai = "Sẵn sàng",
                        AnhSan = "/uploads/sanbong/san-7-so-1.jpg",
                        GiaThamKhao = 480000,
                        DacDiem = "Đường piste rộng, bảng điểm điện tử",
                        IsAvailable = true
                    }
                };

                // Áp dụng bộ lọc lên danh sách fallback
                pitchDisplayList = fallbackList
                    .Where(p => string.IsNullOrWhiteSpace(loaiSanLoc) || loaiSanLoc == "Tất cả" || p.LoaiSan.Contains(loaiSanLoc))
                    .Where(p => !giaToiDa.HasValue || giaToiDa.Value <= 0 || p.GiaThamKhao <= giaToiDa.Value)
                    .ToList();
            }

            // Lấy điểm đánh giá thực tế từ bảng dbo.DanhGia cho từng sân bóng
            var pitchRatings = await _context.DanhGia
                .GroupBy(dg => dg.SanBongId)
                .Select(g => new
                {
                    SanBongId = g.Key,
                    Count = g.Count(),
                    Avg = g.Average(x => (double)x.SoSao)
                })
                .ToDictionaryAsync(x => x.SanBongId, x => x);

            foreach (var p in pitchDisplayList)
            {
                if (pitchRatings.TryGetValue(p.Id, out var r))
                {
                    p.TongSoDanhGia = r.Count;
                    p.DiemDanhGiaTrungBinh = Math.Round(r.Avg, 1);
                }
            }

            // Thống kê cá nhân của sinh viên
            var totalBookings = await _context.DatSans
                .CountAsync(d => d.NguoiDungId == userId);

            var upcomingBookings = await _context.DatSans
                .CountAsync(d => d.NguoiDungId == userId && d.NgayDat >= DateOnly.FromDateTime(DateTime.Today) && d.TrangThai != "Đã hủy");

            var pendingBookings = await _context.DatSans
                .CountAsync(d => d.NguoiDungId == userId && (d.TrangThai == "Chờ duyệt" || d.TrangThai == "Pending"));

            var unreadNotifs = await _context.ThongBaos
                .CountAsync(t => t.NguoiDungId == userId && t.DaDoc != true);

            var activeCoupons = await _context.MaGiamGia
                .CountAsync(m => m.HieuLucDen >= DateTime.Now);
            if (activeCoupons == 0)
            {
                activeCoupons = 2; // Ưu đãi sinh viên mặc định
            }

            // Danh sách 3-5 lịch đặt sân gần nhất của người dùng
            var recentBookings = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Where(d => d.NguoiDungId == userId)
                .OrderByDescending(d => d.NgayDat)
                .ThenByDescending(d => d.Id)
                .Take(5)
                .ToListAsync();

            // Danh sách thông báo mới nhất
            var recentNotifs = await _context.ThongBaos
                .Where(t => t.NguoiDungId == userId)
                .OrderByDescending(t => t.NgayTao)
                .Take(5)
                .ToListAsync();

            var notifModels = recentNotifs.Select(t => new NotificationItemViewModel
            {
                Id = t.Id,
                TieuDe = t.TieuDe,
                NoiDung = t.NoiDung,
                LoaiThongBao = t.LoaiThongBao,
                DaDoc = t.DaDoc == true,
                NgayTao = t.NgayTao ?? DateTime.Now
            }).ToList();

            var viewModel = new CustomerHomeViewModel
            {
                UserId = userId,
                HoTen = user?.HoTen ?? User.Identity?.Name ?? "Sinh viên",
                Email = user?.Email ?? "",
                SoDienThoai = user?.SoDienThoai ?? "",
                AnhDaiDienUrl = user?.AnhDaiDienUrl,
                TongSoCaDaDat = totalBookings,
                SoCaSapDienRa = upcomingBookings,
                SoDonChoDuyet = pendingBookings,
                SoThongBaoChuaDoc = unreadNotifs,
                UuDaiHienCo = activeCoupons,

                NgayLoc = filterDate,
                LoaiSanLoc = loaiSanLoc,
                GiaToiDa = giaToiDa,
                KhungGioLocId = khungGioLocId,

                DanhSachSan = pitchDisplayList,
                LichDatGanDay = recentBookings,
                ThongBaoGanDay = recentNotifs,
                DanhSachThongBaoMoi = notifModels,
                DanhSachKhungGio = allTimeSlots
            };

            return View(viewModel);
        }
    }
}
