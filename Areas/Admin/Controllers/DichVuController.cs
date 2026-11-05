using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Areas.Admin.Models;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DichVuController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public DichVuController(QLSanBongDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Danh sách các dịch vụ đi kèm: Nước suối, Bóng đá, Áo bib, Trọng tài...
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? search = null)
        {
            var query = _context.DichVus
                .Include(d => d.ChiTietDichVuDats)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim().ToLower();
                query = query.Where(d => d.TenDichVu.ToLower().Contains(s) || d.DonViTinh.ToLower().Contains(s));
            }

            var allServices = await query
                .OrderBy(d => d.Id)
                .ToListAsync();

            var items = allServices.Select(d =>
            {
                var isDiscontinued = d.TenDichVu.StartsWith("[Ngừng bán]", StringComparison.OrdinalIgnoreCase) ||
                                     d.DonViTinh.Equals("Ngừng bán", StringComparison.OrdinalIgnoreCase);

                return new AdminDichVuItemViewModel
                {
                    Id = d.Id,
                    TenDichVu = d.TenDichVu,
                    DonViTinh = d.DonViTinh,
                    GiaDichVu = d.GiaDichVu,
                    DaNgungBan = isDiscontinued,
                    SoLuotSuDung = d.ChiTietDichVuDats.Sum(c => c.SoLuong)
                };
            }).ToList();

            var tongDichVu = items.Count;
            var ngungBan = items.Count(i => i.DaNgungBan);
            var dangKinhDoanh = tongDichVu - ngungBan;

            var viewModel = new AdminDichVuListViewModel
            {
                Items = items,
                Search = search,
                TongDichVu = tongDichVu,
                DangKinhDoanh = dangKinhDoanh,
                NgungBan = ngungBan
            };

            return View(viewModel);
        }

        /// <summary>
        /// Thêm mới dịch vụ
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string tenDichVu, string donViTinh, decimal giaDichVu)
        {
            if (string.IsNullOrWhiteSpace(tenDichVu) || string.IsNullOrWhiteSpace(donViTinh) || giaDichVu < 0)
            {
                TempData["ErrorMessage"] = "Vui lòng nhập đầy đủ tên, đơn vị tính và giá dịch vụ >= 0.";
                return RedirectToAction(nameof(Index));
            }

            var exists = await _context.DichVus.AnyAsync(d => d.TenDichVu.ToLower() == tenDichVu.Trim().ToLower());
            if (exists)
            {
                TempData["ErrorMessage"] = "Tên dịch vụ này đã tồn tại trên hệ thống.";
                return RedirectToAction(nameof(Index));
            }

            var dichVu = new DichVu
            {
                TenDichVu = tenDichVu.Trim(),
                DonViTinh = donViTinh.Trim(),
                GiaDichVu = giaDichVu
            };

            _context.DichVus.Add(dichVu);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm dịch vụ '{dichVu.TenDichVu}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Cập nhật thông tin dịch vụ
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string tenDichVu, string donViTinh, decimal giaDichVu)
        {
            if (string.IsNullOrWhiteSpace(tenDichVu) || string.IsNullOrWhiteSpace(donViTinh) || giaDichVu < 0)
            {
                TempData["ErrorMessage"] = "Thông tin dịch vụ không hợp lệ.";
                return RedirectToAction(nameof(Index));
            }

            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction(nameof(Index));
            }

            dichVu.TenDichVu = tenDichVu.Trim();
            dichVu.DonViTinh = donViTinh.Trim();
            dichVu.GiaDichVu = giaDichVu;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật dịch vụ '{dichVu.TenDichVu}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Quy ước ngừng bán: Cập nhật tiền tố [Ngừng bán] thay vì xóa cứng để bảo toàn bảng ChiTietDichVuDat
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> NgungBan(int id)
        {
            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction(nameof(Index));
            }

            if (!dichVu.TenDichVu.StartsWith("[Ngừng bán]", StringComparison.OrdinalIgnoreCase))
            {
                dichVu.TenDichVu = $"[Ngừng bán] {dichVu.TenDichVu}";
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã chuyển dịch vụ '{dichVu.TenDichVu}' sang trạng thái Ngừng bán.";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Kích hoạt lại dịch vụ đã ngừng bán
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> KichHoat(int id, string? donViTinhMoi)
        {
            var dichVu = await _context.DichVus.FindAsync(id);
            if (dichVu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction(nameof(Index));
            }

            dichVu.TenDichVu = dichVu.TenDichVu.Replace("[Ngừng bán] ", "").Trim();
            if (!string.IsNullOrWhiteSpace(donViTinhMoi) && dichVu.DonViTinh == "Ngừng bán")
            {
                dichVu.DonViTinh = donViTinhMoi.Trim();
            }

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã mở bán lại dịch vụ '{dichVu.TenDichVu}' thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa dịch vụ: Nếu đã phát sinh đơn thì tự động chuyển sang ngừng bán để bảo vệ dữ liệu lịch sử
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var dichVu = await _context.DichVus
                .Include(d => d.ChiTietDichVuDats)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (dichVu == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy dịch vụ.";
                return RedirectToAction(nameof(Index));
            }

            if (dichVu.ChiTietDichVuDats.Any())
            {
                // Xóa mềm: Chuyển sang Ngừng bán
                if (!dichVu.TenDichVu.StartsWith("[Ngừng bán]", StringComparison.OrdinalIgnoreCase))
                {
                    dichVu.TenDichVu = $"[Ngừng bán] {dichVu.TenDichVu}";
                }
                await _context.SaveChangesAsync();

                TempData["ErrorMessage"] = $"Dịch vụ '{dichVu.TenDichVu}' đã phát sinh trong {dichVu.ChiTietDichVuDats.Count} đơn đặt sân trước đó. Hệ thống đã tự động chuyển sang chế độ '[Ngừng bán]' (xóa mềm) để bảo toàn dữ liệu lịch sử!";
                return RedirectToAction(nameof(Index));
            }

            // Chưa phát sinh đơn: Xóa cứng
            _context.DichVus.Remove(dichVu);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa hoàn toàn dịch vụ '{dichVu.TenDichVu}' khỏi hệ thống.";
            return RedirectToAction(nameof(Index));
        }
    }
}

