using System;
using System.Collections.Generic;
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
    public class DashboardController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public DashboardController(QLSanBongDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);
            var todayDateOnly = DateOnly.FromDateTime(today);

            // 1. Thống kê 4 thẻ KPI cốt lõi
            var choDuyetCount = await _context.DatSans
                .CountAsync(d => d.TrangThai == "Chờ duyệt");

            var yeuCauDoiLichCount = await _context.YeuCauDoiLiches
                .CountAsync(y => y.TrangThai == "Chờ duyệt");

            var donHomNayCount = await _context.DatSans
                .CountAsync(d => d.NgayDat == todayDateOnly);

            var doanhThuHomNay = await _context.ThanhToans
                .Where(t => (t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công")
                         && t.ThoiGianThanhToan.HasValue
                         && t.ThoiGianThanhToan.Value >= today
                         && t.ThoiGianThanhToan.Value < tomorrow)
                .SumAsync(t => (decimal?)t.SoTien) ?? 0m;

            // Thống kê phụ trợ
            var tongSanHoatDong = await _context.SanBongs
                .CountAsync(s => s.TrangThai == "Hoạt động");

            var tongKhachHang = await _context.NguoiDungs
                .CountAsync(n => n.QuyenHan == null ||
                                 (!n.QuyenHan.TenQuyen.Contains("Admin") &&
                                  !n.QuyenHan.TenQuyen.Contains("Quản trị") &&
                                  !n.QuyenHan.TenQuyen.Contains("Quan tri")));

            // 2. Thống kê Doanh thu 7 ngày gần nhất cho biểu đồ Chart.js
            var startDate = today.AddDays(-6);
            var payments7Days = await _context.ThanhToans
                .Where(t => (t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công")
                         && t.ThoiGianThanhToan.HasValue
                         && t.ThoiGianThanhToan.Value >= startDate
                         && t.ThoiGianThanhToan.Value < tomorrow)
                .Select(t => new { t.ThoiGianThanhToan, t.SoTien })
                .ToListAsync();

            var chartData = new List<AdminRevenueChartItemViewModel>();
            for (int i = -6; i <= 0; i++)
            {
                var day = today.AddDays(i);
                var nextDay = day.AddDays(1);
                var dayPayments = payments7Days
                    .Where(p => p.ThoiGianThanhToan!.Value >= day && p.ThoiGianThanhToan!.Value < nextDay)
                    .ToList();

                chartData.Add(new AdminRevenueChartItemViewModel
                {
                    Date = day,
                    Label = day.ToString("dd/MM"),
                    Revenue = dayPayments.Sum(p => p.SoTien),
                    BookingCount = dayPayments.Count
                });
            }

            // 3. Top 5 đơn đặt mới nhất đang chờ duyệt
            var rawPendingBookings = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Include(d => d.NguoiDung)
                .Include(d => d.ThanhToans)
                .Include(d => d.ChiTietDichVuDats)
                    .ThenInclude(c => c.DichVu)
                .Where(d => d.TrangThai == "Chờ duyệt")
                .OrderByDescending(d => d.Id)
                .Take(5)
                .ToListAsync();

            var recentPending = rawPendingBookings.Select(b =>
            {
                var paid = b.ThanhToans
                    .Where(t => t.TrangThai == "Thành công" || t.TrangThai == "Đã thanh toán")
                    .Sum(t => t.SoTien);

                return new AdminBookingItemViewModel
                {
                    Id = b.Id,
                    SanBongId = b.SanBongId,
                    TenSan = b.SanBong?.TenSan ?? "Sân bóng",
                    LoaiSan = b.SanBong?.LoaiSan ?? "Sân 5 người",
                    NguoiDungId = b.NguoiDungId,
                    TenNguoiDung = b.NguoiDung?.HoTen ?? "Khách hàng",
                    SoDienThoai = b.NguoiDung?.SoDienThoai ?? "",
                    Email = b.NguoiDung?.Email ?? "",
                    NgayDat = b.NgayDat,
                    GioBatDau = b.KhungGio?.GioBatDau ?? new TimeOnly(17, 30),
                    GioKetThuc = b.KhungGio?.GioKetThuc ?? new TimeOnly(19, 0),
                    TongTien = b.TongTien,
                    DaThanhToan = paid,
                    TrangThai = b.TrangThai,
                    NgayTao = b.NgayTao,
                    DanhSachDichVu = b.ChiTietDichVuDats.Select(c => $"{c.DichVu.TenDichVu} (x{c.SoLuong})").ToList()
                };
            }).ToList();

            var viewModel = new AdminDashboardViewModel
            {
                ChoDuyetCount = choDuyetCount,
                YeuCauDoiLichCount = yeuCauDoiLichCount,
                DonHomNayCount = donHomNayCount,
                DoanhThuHomNay = doanhThuHomNay,
                TongSanHoatDong = tongSanHoatDong,
                TongKhachHang = tongKhachHang,
                RevenueChartData = chartData,
                RecentPendingBookings = recentPending
            };

            return View(viewModel);
        }
    }
}
