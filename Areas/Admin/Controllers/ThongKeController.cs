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
    public class ThongKeController : Controller
    {
        private readonly QLSanBongDbContext _context;

        public ThongKeController(QLSanBongDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Báo cáo thống kê tổng quan: Doanh thu, số lượng đơn theo trạng thái, tỷ lệ hủy,
        /// cơ cấu loại sân, khung giờ cao điểm và dữ liệu trực quan cho Chart.js.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Index(DateTime? tuNgay, DateTime? denNgay)
        {
            // Thiết lập mặc định 30 ngày gần nhất nếu chưa truyền vào
            var end = denNgay?.Date ?? DateTime.Today;
            var start = tuNgay?.Date ?? end.AddDays(-29);

            if (start > end)
            {
                var temp = start;
                start = end;
                end = temp;
            }

            var tuNgayDateOnly = DateOnly.FromDateTime(start);
            var denNgayDateOnly = DateOnly.FromDateTime(end);

            // 1. Lấy toàn bộ đơn đặt trong khoảng thời gian lọc
            var bookings = await _context.DatSans
                .Include(d => d.SanBong)
                .Include(d => d.KhungGio)
                .Include(d => d.ThanhToans)
                .Where(d => d.NgayDat >= tuNgayDateOnly && d.NgayDat <= denNgayDateOnly)
                .ToListAsync();

            // 2. Lấy toàn bộ giao dịch thanh toán thành công trong khoảng thời gian lọc
            var endOfDay = end.AddDays(1);
            var payments = await _context.ThanhToans
                .Where(t => t.ThoiGianThanhToan.HasValue &&
                            t.ThoiGianThanhToan.Value >= start &&
                            t.ThoiGianThanhToan.Value < endOfDay &&
                            (t.TrangThai == "Đã thanh toán" || t.TrangThai == "Thành công"))
                .ToListAsync();

            // 3. Tính toán các chỉ số KPI
            var tongDoanhThu = payments.Sum(t => t.SoTien);
            var tongTienMat = payments
                .Where(t => t.PhuongThucThanhToan == "Tiền mặt")
                .Sum(t => t.SoTien);
            var tongChuyenKhoan = payments
                .Where(t => t.PhuongThucThanhToan != "Tiền mặt")
                .Sum(t => t.SoTien);

            var tongDonDat = bookings.Count;
            var donHoanThanh = bookings.Count(d => d.TrangThai == "Hoàn thành" || d.TrangThai == "Đã hoàn thành");
            var donChoDuyet = bookings.Count(d => d.TrangThai == "Chờ duyệt" || d.TrangThai == "Chờ thanh toán");
            var donDaHuy = bookings.Count(d => d.TrangThai == "Đã hủy" || d.TrangThai == "Từ chối");
            var tyLeHuy = tongDonDat > 0 ? Math.Round((double)donDaHuy / tongDonDat * 100, 1) : 0;

            // 4. Tổng hợp theo từng ngày (Daily Trend) cho biểu đồ Doanh thu & Đơn đặt
            var dailyStats = new List<AdminThongKeNgayItemViewModel>();
            var chartLabels = new List<string>();
            var chartRevenue = new List<decimal>();
            var chartOrders = new List<int>();

            for (var curDate = tuNgayDateOnly; curDate <= denNgayDateOnly; curDate = curDate.AddDays(1))
            {
                var dayBookings = bookings.Where(b => b.NgayDat == curDate).ToList();
                var dayRevenue = payments
                    .Where(p => p.ThoiGianThanhToan.HasValue && DateOnly.FromDateTime(p.ThoiGianThanhToan.Value) == curDate)
                    .Sum(p => p.SoTien);

                dailyStats.Add(new AdminThongKeNgayItemViewModel
                {
                    Ngay = curDate,
                    SoDon = dayBookings.Count,
                    SoDonHuy = dayBookings.Count(b => b.TrangThai == "Đã hủy" || b.TrangThai == "Từ chối"),
                    DoanhThu = dayRevenue
                });

                chartLabels.Add(curDate.ToString("dd/MM"));
                chartRevenue.Add(dayRevenue);
                chartOrders.Add(dayBookings.Count);
            }

            // 5. Tỷ lệ đặt theo loại sân (Sân 5, 7, 11)
            var loaiSanGroup = bookings
                .Where(b => b.SanBong != null)
                .GroupBy(b => b.SanBong.LoaiSan ?? "Khác")
                .Select(g => new { Loai = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .ToList();

            // 6. Khung giờ thi đấu cao điểm
            var khungGioGroup = bookings
                .Where(b => b.KhungGio != null)
                .GroupBy(b => $"{b.KhungGio.GioBatDau:HH:mm}-{b.KhungGio.GioKetThuc:HH:mm}")
                .Select(g => new { Gio = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(7)
                .ToList();

            // 7. Cơ cấu trạng thái đơn
            var trangThaiGroup = bookings
                .GroupBy(b => b.TrangThai ?? "Khác")
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToList();

            var viewModel = new AdminThongKeViewModel
            {
                TuNgay = start,
                DenNgay = end,
                TongDoanhThu = tongDoanhThu,
                TongDonDat = tongDonDat,
                DonHoanThanh = donHoanThanh,
                DonChoDuyet = donChoDuyet,
                DonDaHuy = donDaHuy,
                TyLeHuy = tyLeHuy,
                TongTienMat = tongTienMat,
                TongChuyenKhoan = tongChuyenKhoan,

                DoanhThuTheoNgayLabels = chartLabels,
                DoanhThuTheoNgayValues = chartRevenue,
                SoDonTheoNgayValues = chartOrders,

                LoaiSanLabels = loaiSanGroup.Select(l => l.Loai).ToList(),
                LoaiSanValues = loaiSanGroup.Select(l => l.Count).ToList(),

                KhungGioLabels = khungGioGroup.Select(k => k.Gio).ToList(),
                KhungGioValues = khungGioGroup.Select(k => k.Count).ToList(),

                TrangThaiDonLabels = trangThaiGroup.Select(t => t.Status).ToList(),
                TrangThaiDonValues = trangThaiGroup.Select(t => t.Count).ToList(),

                BangTongHopNgay = dailyStats.OrderByDescending(d => d.Ngay).ToList()
            };

            return View(viewModel);
        }
    }
}

