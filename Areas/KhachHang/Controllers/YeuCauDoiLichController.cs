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
    public class YeuCauDoiLichController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public YeuCauDoiLichController(QLSanBongDbContext context)
        {
            _context = context;
        }

        // ===================================================================
        // 1. [HttpGet] DANH SÁCH YÊU CẦU ĐỔI LỊCH CỦA KHÁCH HÀNG
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            int userId = GetCurrentUserId();
            if (userId <= 0)
            {
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            var requests = await _context.YeuCauDoiLiches
                .Include(y => y.DatSan)
                    .ThenInclude(d => d.SanBong)
                .Include(y => y.DatSan)
                    .ThenInclude(d => d.KhungGio)
                .Include(y => y.SanBongMoi)
                .Include(y => y.KhungGioMoi)
                .Where(y => y.DatSan.NguoiDungId == userId)
                .OrderByDescending(y => y.NgayTao)
                .ToListAsync();

            var items = requests.Select(y => new RescheduleHistoryItemModel
            {
                YeuCauId = y.Id,
                DatSanId = y.DatSanId,
                TenSanCu = y.DatSan.SanBong?.TenSan ?? "Sân hiện tại",
                TenSanMoi = y.SanBongMoi?.TenSan ?? "Sân mới",
                NgayDatCu = y.DatSan.NgayDat,
                NgayDatMoi = y.NgayDatMoi,
                KhungGioCu = y.DatSan.KhungGio != null ? $"{y.DatSan.KhungGio.GioBatDau:HH:mm} - {y.DatSan.KhungGio.GioKetThuc:HH:mm}" : "",
                KhungGioMoi = y.KhungGioMoi != null ? $"{y.KhungGioMoi.GioBatDau:HH:mm} - {y.KhungGioMoi.GioKetThuc:HH:mm}" : "",
                LyDo = y.LyDo,
                TrangThai = y.TrangThai,
                NgayTao = y.NgayTao
            }).ToList();

            return View(items);
        }

        // ===================================================================
        // 2. [HttpGet] FORM GỬI YÊU CẦU ĐỔI LỊCH KÈM KIỂM TRA TIME-LOCK RULE
        // ===================================================================
        [HttpGet]
        public async Task<IActionResult> TaoYeuCau(int datSanId)
        {
            int userId = GetCurrentUserId();
            var datSan = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .FirstOrDefaultAsync(d => d.Id == datSanId && d.NguoiDungId == userId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân cần đổi lịch.";
                return RedirectToAction("LichSu", "DatSan");
            }

            // Kiểm tra ràng buộc Time-lock (tối thiểu 2 tiếng trước giờ bóng lăn)
            DateTime kickoffTime = datSan.NgayDat.ToDateTime(datSan.KhungGio?.GioBatDau ?? new TimeOnly(17, 30));
            TimeSpan diff = kickoffTime - DateTime.Now;
            bool isTimeLocked = diff.TotalHours < 2.0;

            var allPitches = await _context.SanBongs.Where(s => s.TrangThai == "Hoạt động" || s.TrangThai == "Sẵn sàng").ToListAsync();
            var allTimeSlots = await _context.KhungGios.OrderBy(k => k.GioBatDau).ToListAsync();

            var model = new RescheduleRequestViewModel
            {
                DatSanId = datSan.Id,
                TenSanHienTai = datSan.SanBong?.TenSan ?? "Sân bóng UTC",
                LoaiSanHienTai = datSan.SanBong?.LoaiSan ?? "Sân 5 người",
                NgayDatHienTai = datSan.NgayDat,
                KhungGioHienTai = datSan.KhungGio != null ? $"{datSan.KhungGio.GioBatDau:HH:mm} - {datSan.KhungGio.GioKetThuc:HH:mm}" : "",
                SanBongMoiId = datSan.SanBongId,
                NgayDatMoi = datSan.NgayDat.AddDays(1),
                KhungGioMoiId = datSan.KhungGioId,
                IsTimeLockPassed = isTimeLocked,
                TimeLockWarning = isTimeLocked
                    ? "ĐÃ KHÓA ĐỔI LỊCH: Thời gian tới giờ bóng lăn còn dưới 2 tiếng theo quy định Time-lock Rule. Vui lòng liên hệ Hotline BQL Sân 0988.686.868 để được giải quyết trực tiếp."
                    : null,
                DanhSachSan = allPitches,
                DanhSachKhungGio = allTimeSlots
            };

            return View(model);
        }

        // ===================================================================
        // 3. [HttpPost] XỬ LÝ LƯU YÊU CẦU ĐỔI LỊCH VÀO dbo.YeuCauDoiLich
        // ===================================================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TaoYeuCau(RescheduleRequestViewModel model)
        {
            int userId = GetCurrentUserId();
            var datSan = await _context.DatSans
                .Include(d => d.KhungGio)
                .Include(d => d.SanBong)
                .FirstOrDefaultAsync(d => d.Id == model.DatSanId && d.NguoiDungId == userId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn đặt sân.";
                return RedirectToAction("LichSu", "DatSan");
            }

            // Kiểm tra lại ràng buộc Time-lock phía Server
            DateTime kickoffTime = datSan.NgayDat.ToDateTime(datSan.KhungGio?.GioBatDau ?? new TimeOnly(17, 30));
            TimeSpan diff = kickoffTime - DateTime.Now;

            if (diff.TotalHours < 2.0)
            {
                ModelState.AddModelError(string.Empty, "Quy định Time-lock: Không thể đổi lịch khi thời gian bắt đầu còn dưới 2 tiếng! Vui lòng liên hệ Hotline BQL Sân 0988.686.868.");
            }

            // 1. Kiểm tra trùng lịch của sân mới trong ngày và khung giờ mới (bảng dbo.DatSan)
            var isConflict = await _context.DatSans.AnyAsync(d =>
                d.SanBongId == model.SanBongMoiId &&
                d.NgayDat == model.NgayDatMoi &&
                d.KhungGioId == model.KhungGioMoiId &&
                d.TrangThai != "Đã hủy" &&
                d.TrangThai != "Từ chối" &&
                d.TrangThai != "Cancelled");

            if (isConflict)
            {
                ModelState.AddModelError(string.Empty, "Sân bóng và khung giờ mới bạn chọn hiện đã có đội bóng khác giữ chỗ. Vui lòng chọn ca đá khác.");
            }

            // 2. Kiểm tra trùng lịch bảo trì mặt sân/hệ thống chiếu sáng (bảng dbo.LichBaoTri)
            var newSlot = await _context.KhungGios.FindAsync(model.KhungGioMoiId);
            if (newSlot != null)
            {
                DateTime newSlotStart = model.NgayDatMoi.ToDateTime(newSlot.GioBatDau);
                DateTime newSlotEnd = model.NgayDatMoi.ToDateTime(newSlot.GioKetThuc);

                bool isInMaintenance = await _context.LichBaoTris.AnyAsync(m =>
                    m.SanBongId == model.SanBongMoiId &&
                    m.ThoiGianBatDau < newSlotEnd &&
                    m.ThoiGianKetThuc > newSlotStart);

                if (isInMaintenance)
                {
                    ModelState.AddModelError(string.Empty, "Sân bóng đề xuất mới đang trong lịch bảo dưỡng mặt sân/hệ thống chiếu sáng vào khung giờ này. Vui lòng chọn sân hoặc ca đá khác.");
                }
            }

            if (!ModelState.IsValid)
            {
                model.DanhSachSan = await _context.SanBongs.Where(s => s.TrangThai == "Hoạt động" || s.TrangThai == "Sẵn sàng").ToListAsync();
                model.DanhSachKhungGio = await _context.KhungGios.OrderBy(k => k.GioBatDau).ToListAsync();
                return View(model);
            }

            // Lưu yêu cầu vào dbo.YeuCauDoiLich
            var yeuCau = new YeuCauDoiLich
            {
                DatSanId = datSan.Id,
                SanBongMoiId = model.SanBongMoiId,
                KhungGioMoiId = model.KhungGioMoiId,
                NgayDatMoi = model.NgayDatMoi,
                TrangThai = "Chờ duyệt",
                LyDo = model.LyDo.Trim(),
                NgayTao = DateTime.Now
            };

            _context.YeuCauDoiLiches.Add(yeuCau);

            // Gửi thông báo đến tài khoản khách hàng
            _context.ThongBaos.Add(new ThongBao
            {
                NguoiDungId = userId,
                TieuDe = $"Đã gửi yêu cầu đổi lịch cho đơn #{datSan.Id}",
                NoiDung = $"Yêu cầu đổi sang ngày {model.NgayDatMoi:dd/MM/yyyy} đang được Ban Quản Lý Sân xem xét và duyệt sớm nhất.",
                LoaiThongBao = "DoiLich",
                DaDoc = false,
                NgayTao = DateTime.Now
            });

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Gửi yêu cầu đổi lịch thành công! Ban Quản Lý Sân UTC STADIUM sẽ duyệt yêu cầu trong vòng 15-30 phút.";
            return RedirectToAction(nameof(Index));
        }

        private int GetCurrentUserId()
        {
            var sessionUid = HttpContext.Session.GetString("Customer_UserId");
            if (int.TryParse(sessionUid, out int uidFromSession) && uidFromSession > 0)
                return uidFromSession;

            var claimUid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(claimUid, out int uidFromClaim))
                return uidFromClaim;

            return 0;
        }
    }
}

