using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Areas.Admin.Models;
using QLSanBong.Models.Entities;

namespace QLSanBong.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class ThietLapGiaController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public ThietLapGiaController(QLSanBongDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Giao diện ma trận cấu hình giá thuê (Grid Table) theo: Sân x Khung giờ x Loại ngày
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(string? ngayTrongTuan = "Ngày thường", int? sanBongId = null)
        {
            if (string.IsNullOrWhiteSpace(ngayTrongTuan))
            {
                ngayTrongTuan = "Ngày thường";
            }

            var pitches = await _context.SanBongs
                .Where(s => s.TrangThai != "Ngừng hoạt động")
                .OrderBy(s => s.Id)
                .ToListAsync();

            var slots = await _context.KhungGios
                .OrderBy(k => k.GioBatDau)
                .ToListAsync();

            var allPrices = await _context.ThietLapGia.ToListAsync();

            var priceMatrix = new Dictionary<string, decimal>();
            foreach (var p in allPrices)
            {
                var key = $"{p.SanBongId}_{p.KhungGioId}_{p.NgayTrongTuan}";
                priceMatrix[key] = p.GiaThue;
            }

            var viewModel = new AdminThietLapGiaMatrixViewModel
            {
                SelectedNgayTrongTuan = ngayTrongTuan,
                SelectedSanBongId = sanBongId,
                DanhSachSan = pitches,
                DanhSachKhungGio = slots,
                PriceMatrix = priceMatrix
            };

            return View(viewModel);
        }

        /// <summary>
        /// Cập nhật trực tiếp giá trên một ô (AJAX hoặc Form submit)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatGia(int sanBongId, int khungGioId, string ngayTrongTuan, decimal giaThue)
        {
            if (giaThue < 0)
            {
                return Json(new { success = false, message = "Giá thuê không được nhỏ hơn 0." });
            }

            var setting = await _context.ThietLapGia
                .FirstOrDefaultAsync(t => t.SanBongId == sanBongId &&
                                          t.KhungGioId == khungGioId &&
                                          t.NgayTrongTuan == ngayTrongTuan);

            if (setting != null)
            {
                setting.GiaThue = giaThue;
            }
            else
            {
                _context.ThietLapGia.Add(new ThietLapGium
                {
                    SanBongId = sanBongId,
                    KhungGioId = khungGioId,
                    NgayTrongTuan = ngayTrongTuan,
                    GiaThue = giaThue
                });
            }

            await _context.SaveChangesAsync();
            return Json(new { success = true, message = "Đã cập nhật giá thuê thành công!" });
        }

        /// <summary>
        /// Lưu hàng loạt toàn bộ ma trận giá từ form
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CapNhatMaTran(IFormCollection form, string ngayTrongTuan)
        {
            var pitches = await _context.SanBongs.Where(s => s.TrangThai != "Ngừng hoạt động").ToListAsync();
            var slots = await _context.KhungGios.ToListAsync();

            var existingPrices = await _context.ThietLapGia
                .Where(t => t.NgayTrongTuan == ngayTrongTuan)
                .ToListAsync();

            var existingDict = existingPrices.ToDictionary(t => $"{t.SanBongId}_{t.KhungGioId}");

            int countUpdated = 0;
            foreach (var pitch in pitches)
            {
                foreach (var slot in slots)
                {
                    var formKey = $"price_{pitch.Id}_{slot.Id}";
                    if (form.ContainsKey(formKey))
                    {
                        var valStr = form[formKey].ToString().Replace(",", "").Replace(".", "").Trim();
                        if (decimal.TryParse(valStr, out decimal price) && price >= 0)
                        {
                            var dictKey = $"{pitch.Id}_{slot.Id}";
                            if (existingDict.TryGetValue(dictKey, out var existing))
                            {
                                if (existing.GiaThue != price)
                                {
                                    existing.GiaThue = price;
                                    countUpdated++;
                                }
                            }
                            else
                            {
                                _context.ThietLapGia.Add(new ThietLapGium
                                {
                                    SanBongId = pitch.Id,
                                    KhungGioId = slot.Id,
                                    NgayTrongTuan = ngayTrongTuan,
                                    GiaThue = price
                                });
                                countUpdated++;
                            }
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = $"Đã lưu và cập nhật ma trận giá cho '{ngayTrongTuan}' thành công ({countUpdated} mức giá được thay đổi)!";
            return RedirectToAction(nameof(Index), new { ngayTrongTuan });
        }

        /// <summary>
        /// Tính năng [Nên có]: Sao chép bảng giá từ Sân nguồn sang Sân đích hoặc toàn bộ sân cùng loại
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaoChepGia(int sanNguonId, int? sanDichId, bool apDungTatCaCungLoai = false)
        {
            var sanNguon = await _context.SanBongs
                .Include(s => s.ThietLapGia)
                .FirstOrDefaultAsync(s => s.Id == sanNguonId);

            if (sanNguon == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy sân nguồn để sao chép.";
                return RedirectToAction(nameof(Index));
            }

            var sourcePrices = sanNguon.ThietLapGia.ToList();
            if (!sourcePrices.Any())
            {
                TempData["ErrorMessage"] = $"Sân nguồn '{sanNguon.TenSan}' chưa có bảng giá nào để sao chép.";
                return RedirectToAction(nameof(Index));
            }

            var targetPitches = new List<SanBong>();
            if (apDungTatCaCungLoai)
            {
                targetPitches = await _context.SanBongs
                    .Where(s => s.Id != sanNguonId && s.LoaiSan == sanNguon.LoaiSan && s.TrangThai != "Ngừng hoạt động")
                    .Include(s => s.ThietLapGia)
                    .ToListAsync();

                if (!targetPitches.Any())
                {
                    TempData["ErrorMessage"] = $"Không tìm thấy sân nào khác cùng loại '{sanNguon.LoaiSan}' để áp dụng sao chép.";
                    return RedirectToAction(nameof(Index));
                }
            }
            else
            {
                if (!sanDichId.HasValue || sanDichId.Value <= 0 || sanDichId.Value == sanNguonId)
                {
                    TempData["ErrorMessage"] = "Vui lòng chọn sân đích hợp lệ khác với sân nguồn.";
                    return RedirectToAction(nameof(Index));
                }

                var target = await _context.SanBongs
                    .Include(s => s.ThietLapGia)
                    .FirstOrDefaultAsync(s => s.Id == sanDichId.Value);

                if (target == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy sân đích.";
                    return RedirectToAction(nameof(Index));
                }
                targetPitches.Add(target);
            }

            // Tiến hành sao chép bảng giá
            foreach (var target in targetPitches)
            {
                var targetPriceDict = target.ThietLapGia
                    .ToDictionary(t => $"{t.KhungGioId}_{t.NgayTrongTuan}");

                foreach (var sp in sourcePrices)
                {
                    var key = $"{sp.KhungGioId}_{sp.NgayTrongTuan}";
                    if (targetPriceDict.TryGetValue(key, out var existing))
                    {
                        existing.GiaThue = sp.GiaThue;
                    }
                    else
                    {
                        _context.ThietLapGia.Add(new ThietLapGium
                        {
                            SanBongId = target.Id,
                            KhungGioId = sp.KhungGioId,
                            NgayTrongTuan = sp.NgayTrongTuan,
                            GiaThue = sp.GiaThue
                        });
                    }
                }
            }

            await _context.SaveChangesAsync();

            var targetNames = string.Join(", ", targetPitches.Select(p => p.TenSan));
            TempData["SuccessMessage"] = $"Đã sao chép thành công bảng giá từ sân '{sanNguon.TenSan}' sang các sân: {targetNames}!";
            return RedirectToAction(nameof(Index));
        }
    }
}

