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
    public class DanhGiaController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public DanhGiaController(QLSanBongDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var uidClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (int.TryParse(uidClaim, out int uid))
            {
                return uid;
            }
            throw new UnauthorizedAccessException("Không xác định được danh tính người dùng.");
        }

        /// <summary>
        /// Màn hình viết đánh giá sân bóng kèm Ràng Buộc Nghiệp Vụ Chặt Chẽ:
        /// 1. Chỉ người đặt đơn mới có quyền đánh giá.
        /// 2. Đơn đặt sân phải có trạng thái "Hoàn thành" (Verified Booking Only).
        /// 3. Mỗi đơn đặt sân chỉ được đánh giá duy nhất 1 lần.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> VietDanhGia(int datSanId)
        {
            var userId = GetCurrentUserId();

            // 1. Kiểm tra đơn đặt sân có tồn tại và thuộc về user không
            var datSan = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .FirstOrDefaultAsync(d => d.Id == datSanId && d.NguoiDungId == userId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy thông tin đơn đặt sân hoặc bạn không có quyền truy cập.";
                return RedirectToAction("LichSu", "DatSan");
            }

            // 2. RÀNG BUỘC CHẶT CHẼ: Verified Booking Only
            // Chỉ những đơn đặt sân ĐÃ chuyển sang trạng thái "Hoàn thành" mới được viết đánh giá
            if (!string.Equals(datSan.TrangThai, "Hoàn thành", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = $"Bạn chỉ có thể viết đánh giá cho các đơn đặt sân đã kết thúc và chuyển sang trạng thái 'Hoàn thành' (Verified Booking). Trạng thái hiện tại của đơn #{datSan.Id}: {datSan.TrangThai}.";
                return RedirectToAction("LichSu", "DatSan");
            }

            // 3. RÀNG BUỘC 1 LẦN DUY NHẤT: Kiểm tra đơn đã từng đánh giá chưa
            var daDanhGia = await _context.DanhGia.AnyAsync(dg => dg.DatSanId == datSanId);
            if (daDanhGia)
            {
                TempData["WarningMessage"] = "Đơn đặt sân này đã được bạn gửi đánh giá trước đó. Mỗi đơn chỉ được đánh giá 1 lần duy nhất.";
                return RedirectToAction("LichSu", "DatSan");
            }

            var model = new CreateReviewViewModel
            {
                DatSanId = datSan.Id,
                SanBongId = datSan.SanBongId,
                TenSan = datSan.SanBong?.TenSan ?? "Sân bóng UTC",
                LoaiSan = datSan.SanBong?.LoaiSan ?? "Sân bóng",
                AnhSan = string.IsNullOrWhiteSpace(datSan.SanBong?.AnhSan) ? "/uploads/sanbong/san-5-so-1.jpg" : datSan.SanBong.AnhSan,
                NgayDa = datSan.NgayDat,
                KhungGioDisplay = datSan.KhungGio != null
                    ? $"{datSan.KhungGio.GioBatDau:HH:mm} - {datSan.KhungGio.GioKetThuc:HH:mm}"
                    : "Ca thi đấu tiêu chuẩn",
                TongTien = datSan.TongTien,
                TenNguoiDung = User.Identity?.Name ?? "Sinh viên UTC",
                SoSao = 5
            };

            return View(model);
        }

        /// <summary>
        /// Xử lý gửi đánh giá chất lượng sân và lưu vào dbo.DanhGia.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VietDanhGia(CreateReviewViewModel model)
        {
            var userId = GetCurrentUserId();

            var datSan = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .FirstOrDefaultAsync(d => d.Id == model.DatSanId && d.NguoiDungId == userId);

            if (datSan == null)
            {
                TempData["ErrorMessage"] = "Đơn đặt sân không tồn tại hoặc bạn không có quyền thao tác.";
                return RedirectToAction("LichSu", "DatSan");
            }

            // Kiểm tra điều kiện Verified Booking
            if (!string.Equals(datSan.TrangThai, "Hoàn thành", StringComparison.OrdinalIgnoreCase))
            {
                TempData["ErrorMessage"] = "Chỉ đơn đặt sân ở trạng thái 'Hoàn thành' mới được gửi đánh giá.";
                return RedirectToAction("LichSu", "DatSan");
            }

            // Kiểm tra điều kiện 1 lần duy nhất
            var daDanhGia = await _context.DanhGia.AnyAsync(dg => dg.DatSanId == model.DatSanId);
            if (daDanhGia)
            {
                TempData["WarningMessage"] = "Đơn đặt sân này đã có đánh giá trong hệ thống.";
                return RedirectToAction("LichSu", "DatSan");
            }

            if (!ModelState.IsValid)
            {
                model.TenSan = datSan.SanBong?.TenSan ?? "Sân bóng UTC";
                model.LoaiSan = datSan.SanBong?.LoaiSan ?? "Sân bóng";
                model.AnhSan = string.IsNullOrWhiteSpace(datSan.SanBong?.AnhSan) ? "/uploads/sanbong/san-5-so-1.jpg" : datSan.SanBong.AnhSan;
                model.NgayDa = datSan.NgayDat;
                model.KhungGioDisplay = datSan.KhungGio != null
                    ? $"{datSan.KhungGio.GioBatDau:HH:mm} - {datSan.KhungGio.GioKetThuc:HH:mm}"
                    : "Ca thi đấu tiêu chuẩn";
                return View(model);
            }

            // Lưu đánh giá mới vào dbo.DanhGia
            var entity = new DanhGium
            {
                DatSanId = datSan.Id,
                NguoiDungId = userId,
                SanBongId = datSan.SanBongId,
                SoSao = model.SoSao,
                BinhLuan = model.BinhLuan?.Trim(),
                NgayTao = DateTime.Now
            };

            _context.DanhGia.Add(entity);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Cảm ơn bạn đã gửi đánh giá {model.SoSao} sao cho {datSan.SanBong?.TenSan}! Đánh giá thực tế của bạn đã được ghi nhận.";
            return RedirectToAction("SanBong", new { id = datSan.SanBongId });
        }

        /// <summary>
        /// Trang xem toàn bộ đánh giá thực tế (Verified Reviews) của một sân bóng cụ thể.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> SanBong(int id)
        {
            var userId = GetCurrentUserId();

            var sanBong = await _context.SanBongs.FirstOrDefaultAsync(s => s.Id == id);
            if (sanBong == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân bóng.";
                return RedirectToAction("Index", "Home");
            }

            var danhSachDanhGia = await _context.DanhGia
                .Where(dg => dg.SanBongId == id)
                .Include(dg => dg.NguoiDung)
                .OrderByDescending(dg => dg.NgayTao)
                .Select(dg => new PitchReviewDisplayModel
                {
                    Id = dg.Id,
                    DatSanId = dg.DatSanId,
                    SanBongId = dg.SanBongId,
                    TenNguoiDung = dg.NguoiDung.HoTen,
                    AnhDaiDien = dg.NguoiDung.AnhDaiDienUrl,
                    SoSao = dg.SoSao,
                    BinhLuan = dg.BinhLuan,
                    NgayTao = dg.NgayTao,
                    TenSan = sanBong.TenSan,
                    LoaiSan = sanBong.LoaiSan,
                    IsVerifiedBooking = true,
                    IsCurrentUser = dg.NguoiDungId == userId
                })
                .ToListAsync();

            var totalReviews = danhSachDanhGia.Count;
            var avgScore = totalReviews > 0 ? danhSachDanhGia.Average(dg => dg.SoSao) : 5.0;

            // Kiểm tra xem khách hiện tại có đơn "Hoàn thành" nào cho sân này mà chưa đánh giá không
            var donChuaDanhGia = await _context.DatSans
                .Where(d => d.NguoiDungId == userId && d.SanBongId == id && d.TrangThai == "Hoàn thành")
                .Where(d => !_context.DanhGia.Any(dg => dg.DatSanId == d.Id))
                .OrderByDescending(d => d.NgayDat)
                .FirstOrDefaultAsync();

            var giaThamKhao = await _context.ThietLapGia
                .Where(t => t.SanBongId == id)
                .Select(t => t.GiaThue)
                .FirstOrDefaultAsync();
            if (giaThamKhao <= 0)
            {
                giaThamKhao = sanBong.LoaiSan.Contains("7") ? 450000 : 250000;
            }

            var viewModel = new PitchReviewsViewModel
            {
                SanBongId = sanBong.Id,
                TenSan = sanBong.TenSan,
                LoaiSan = sanBong.LoaiSan,
                GiaThamKhao = giaThamKhao,
                AnhSan = string.IsNullOrWhiteSpace(sanBong.AnhSan) ? "/uploads/sanbong/san-5-so-1.jpg" : sanBong.AnhSan,
                DiemTrungBinh = Math.Round(avgScore, 1),
                TongSoDanhGia = totalReviews,
                Sao5Count = danhSachDanhGia.Count(d => d.SoSao == 5),
                Sao4Count = danhSachDanhGia.Count(d => d.SoSao == 4),
                Sao3Count = danhSachDanhGia.Count(d => d.SoSao == 3),
                Sao2Count = danhSachDanhGia.Count(d => d.SoSao == 2),
                Sao1Count = danhSachDanhGia.Count(d => d.SoSao == 1),
                DanhSachDanhGia = danhSachDanhGia,
                CoDonChoDanhGia = donChuaDanhGia != null,
                DatSanIdChoDanhGia = donChuaDanhGia?.Id
            };

            return View(viewModel);
        }
    }
}
