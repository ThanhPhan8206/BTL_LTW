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
    public class ThongBaoController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public ThongBaoController(QLSanBongDbContext context)
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
        /// Trang danh sách thông báo đa kênh của khách hàng (Cập nhật lịch, duyệt/hủy ca, đánh giá).
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? filter = "all")
        {
            var userId = GetCurrentUserId();

            var query = _context.ThongBaos
                .Where(t => t.NguoiDungId == userId);

            if (filter == "unread")
            {
                query = query.Where(t => t.DaDoc != true);
            }
            else if (filter == "datsan")
            {
                query = query.Where(t => t.LoaiThongBao == "DuyetDon" || t.LoaiThongBao == "TuChoi" || t.LoaiThongBao == "HoanThanh");
            }
            else if (filter == "doilich")
            {
                query = query.Where(t => t.LoaiThongBao == "DoiLich");
            }

            var list = await query
                .OrderByDescending(t => t.NgayTao)
                .Select(t => new NotificationItemViewModel
                {
                    Id = t.Id,
                    TieuDe = t.TieuDe,
                    NoiDung = t.NoiDung,
                    LoaiThongBao = t.LoaiThongBao,
                    DaDoc = t.DaDoc == true,
                    NgayTao = t.NgayTao ?? DateTime.Now
                })
                .ToListAsync();

            ViewBag.CurrentFilter = filter;
            ViewBag.TotalUnread = await _context.ThongBaos.CountAsync(t => t.NguoiDungId == userId && t.DaDoc != true);

            return View(list);
        }

        /// <summary>
        /// Đánh dấu một thông báo là đã đọc.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DanhDauDaDoc(int id)
        {
            var userId = GetCurrentUserId();
            var thongBao = await _context.ThongBaos.FirstOrDefaultAsync(t => t.Id == id && t.NguoiDungId == userId);

            if (thongBao != null)
            {
                thongBao.DaDoc = true;
                await _context.SaveChangesAsync();
            }

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Đánh dấu tất cả thông báo của người dùng là đã đọc.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DanhDauTatCaDaDoc()
        {
            var userId = GetCurrentUserId();
            var unreadList = await _context.ThongBaos
                .Where(t => t.NguoiDungId == userId && t.DaDoc != true)
                .ToListAsync();

            foreach (var item in unreadList)
            {
                item.DaDoc = true;
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Đã đánh dấu tất cả thông báo là đã đọc.";

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true });
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa một thông báo.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Xoa(int id)
        {
            var userId = GetCurrentUserId();
            var thongBao = await _context.ThongBaos.FirstOrDefaultAsync(t => t.Id == id && t.NguoiDungId == userId);

            if (thongBao != null)
            {
                _context.ThongBaos.Remove(thongBao);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đã xóa thông báo.";
            }

            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// API endpoint lấy danh sách thông báo mới nhất cho chuông thông báo trên Header Navbar.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetThongBaoMoiNhat()
        {
            var userId = GetCurrentUserId();
            var items = await _context.ThongBaos
                .Where(t => t.NguoiDungId == userId)
                .OrderByDescending(t => t.NgayTao)
                .Take(5)
                .Select(t => new
                {
                    t.Id,
                    t.TieuDe,
                    t.NoiDung,
                    t.LoaiThongBao,
                    DaDoc = t.DaDoc == true,
                    NgayTao = (t.NgayTao ?? DateTime.Now).ToString("dd/MM/yyyy HH:mm")
                })
                .ToListAsync();

            var unreadCount = await _context.ThongBaos.CountAsync(t => t.NguoiDungId == userId && t.DaDoc != true);

            return Json(new { unreadCount, items });
        }
    }
}

