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
    public class KhungGioController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public KhungGioController(QLSanBongDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Danh sách toàn bộ khung giờ ca thi đấu
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rawSlots = await _context.KhungGios
                .Include(k => k.DatSans)
                .OrderBy(k => k.GioBatDau)
                .ToListAsync();

            var items = rawSlots.Select(k => new AdminKhungGioItemViewModel
            {
                Id = k.Id,
                GioBatDau = k.GioBatDau,
                GioKetThuc = k.GioKetThuc,
                TongDonDat = k.DatSans.Count,
                CoTheXoa = !k.DatSans.Any()
            }).ToList();

            var viewModel = new AdminKhungGioViewModel
            {
                Items = items
            };

            return View(viewModel);
        }

        /// <summary>
        /// Thêm mới khung giờ (POST) kèm kiểm tra chồng chéo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TimeOnly gioBatDau, TimeOnly gioKetThuc)
        {
            if (gioKetThuc <= gioBatDau)
            {
                TempData["ErrorMessage"] = "Giờ kết thúc ca phải sau giờ bắt đầu ca.";
                return RedirectToAction(nameof(Index));
            }

            // KIỂM TRA RÀNG BUỘC CHỒNG CHÉO: Không cho phép hai khung giờ bị đè hoặc giao nhau
            var existingSlots = await _context.KhungGios.ToListAsync();
            var overlappingSlot = existingSlots.FirstOrDefault(s => s.GioBatDau < gioKetThuc && s.GioKetThuc > gioBatDau);

            if (overlappingSlot != null)
            {
                TempData["ErrorMessage"] = $"LỖI CHỒNG CHÉO THỜI GIAN! Khung giờ đề xuất [{gioBatDau:HH:mm} - {gioKetThuc:HH:mm}] đang bị giao nhau với ca hiện có [{overlappingSlot.GioBatDau:HH:mm} - {overlappingSlot.GioKetThuc:HH:mm}]. Vui lòng chọn khoảng giờ tách biệt.";
                return RedirectToAction(nameof(Index));
            }

            var newSlot = new KhungGio
            {
                GioBatDau = gioBatDau,
                GioKetThuc = gioKetThuc
            };

            _context.KhungGios.Add(newSlot);
            await _context.SaveChangesAsync();

            // Tự động khởi tạo giá thuê cho các sân hiện có
            var activePitches = await _context.SanBongs.ToListAsync();
            var dayTypes = new[] { "Ngày thường", "Cuối tuần", "Ngày lễ" };

            foreach (var pitch in activePitches)
            {
                decimal basePrice = pitch.LoaiSan.Contains("7") ? 400000m : (pitch.LoaiSan.Contains("11") ? 750000m : 250000m);
                foreach (var dayType in dayTypes)
                {
                    decimal price = basePrice;
                    if (dayType == "Cuối tuần") price += 50000m;
                    else if (dayType == "Ngày lễ") price += 100000m;

                    _context.ThietLapGia.Add(new ThietLapGium
                    {
                        SanBongId = pitch.Id,
                        KhungGioId = newSlot.Id,
                        NgayTrongTuan = dayType,
                        GiaThue = price
                    });
                }
            }
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã thêm thành công khung giờ [{newSlot.GioBatDau:HH:mm} - {newSlot.GioKetThuc:HH:mm}] và tự động cấu hình bảng giá!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Cập nhật khung giờ (POST) kèm kiểm tra chồng chéo
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TimeOnly gioBatDau, TimeOnly gioKetThuc)
        {
            if (gioKetThuc <= gioBatDau)
            {
                TempData["ErrorMessage"] = "Giờ kết thúc ca phải sau giờ bắt đầu ca.";
                return RedirectToAction(nameof(Index));
            }

            var slot = await _context.KhungGios.FindAsync(id);
            if (slot == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khung giờ cần sửa.";
                return RedirectToAction(nameof(Index));
            }

            // Kiểm tra chồng chéo với các ca khác (ngoại trừ chính nó)
            var otherSlots = await _context.KhungGios.Where(s => s.Id != id).ToListAsync();
            var overlappingSlot = otherSlots.FirstOrDefault(s => s.GioBatDau < gioKetThuc && s.GioKetThuc > gioBatDau);

            if (overlappingSlot != null)
            {
                TempData["ErrorMessage"] = $"LỖI CHỒNG CHÉO THỜI GIAN! Khung giờ [{gioBatDau:HH:mm} - {gioKetThuc:HH:mm}] bị trùng với ca [{overlappingSlot.GioBatDau:HH:mm} - {overlappingSlot.GioKetThuc:HH:mm}].";
                return RedirectToAction(nameof(Index));
            }

            slot.GioBatDau = gioBatDau;
            slot.GioKetThuc = gioKetThuc;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã cập nhật khung giờ thành [{slot.GioBatDau:HH:mm} - {slot.GioKetThuc:HH:mm}] thành công!";
            return RedirectToAction(nameof(Index));
        }

        /// <summary>
        /// Xóa khung giờ: Khung giờ nào đã phát sinh đơn đặt thì tuyệt đối không xóa cứng.
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var slot = await _context.KhungGios
                .Include(k => k.DatSans)
                .Include(k => k.ThietLapGia)
                .FirstOrDefaultAsync(k => k.Id == id);

            if (slot == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy khung giờ.";
                return RedirectToAction(nameof(Index));
            }

            // RÀNG BUỘC BẢO TOÀN LỊCH SỬ
            if (slot.DatSans.Any())
            {
                TempData["ErrorMessage"] = $"CHẶN XÓA! Khung giờ [{slot.GioBatDau:HH:mm} - {slot.GioKetThuc:HH:mm}] đã phát sinh {slot.DatSans.Count} đơn đặt sân từ trước. Không thể xóa cứng để bảo toàn toàn vẹn dữ liệu hệ thống!";
                return RedirectToAction(nameof(Index));
            }

            _context.ThietLapGia.RemoveRange(slot.ThietLapGia);
            _context.KhungGios.Remove(slot);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Đã xóa khung giờ [{slot.GioBatDau:HH:mm} - {slot.GioKetThuc:HH:mm}] thành công.";
            return RedirectToAction(nameof(Index));
        }
    }
}

