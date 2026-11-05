using System.Net;
using Microsoft.EntityFrameworkCore;
using QLSanBong.Models.Entities;

namespace QLSanBong.Services
{
    public class NotificationService
    {
        private readonly QLSanBongDbContext _context;
        private readonly EmailService _emailService;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(
            QLSanBongDbContext context,
            EmailService emailService,
            ILogger<NotificationService> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        /// <summary>
        /// Gửi thông báo đa kênh: Lưu vào database dbo.ThongBao và tự động bắn Gmail SMTP.
        /// </summary>
        public async Task<ThongBao?> GuiThongBaoAsync(int nguoiDungId, string tieuDe, string noiDung, string loaiThongBao)
        {
            try
            {
                // 1. Lưu thông báo vào bảng dbo.ThongBao
                var thongBao = new ThongBao
                {
                    NguoiDungId = nguoiDungId,
                    TieuDe = tieuDe,
                    NoiDung = noiDung,
                    LoaiThongBao = loaiThongBao,
                    DaDoc = false,
                    NgayTao = DateTime.Now
                };

                _context.ThongBaos.Add(thongBao);
                await _context.SaveChangesAsync();

                // 2. Lấy thông tin tài khoản để gửi Gmail SMTP bất đồng bộ (Non-blocking)
                var user = await _context.NguoiDungs.FindAsync(nguoiDungId);
                if (user != null && !string.IsNullOrWhiteSpace(user.Email))
                {
                    string targetEmail = user.Email.Trim();
                    string targetName = user.HoTen;

                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            var badgeColor = loaiThongBao switch
                            {
                                "DuyetDon" => "#10B981",       // Xanh lá
                                "TuChoi" => "#EF4444",         // Đỏ
                                "HoanThanh" => "#3B82F6",      // Xanh dương
                                "DoiLich" => "#F59E0B",        // Vàng cam
                                _ => "#002B5B"
                            };

                            var htmlEmail = $@"
                            <div style='font-family: Arial, sans-serif; max-width: 580px; margin: 0 auto; border: 1px solid #e2e8f0; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.06);'>
                                <div style='background-color: #002B5B; color: #ffffff; padding: 22px 24px; text-align: center;'>
                                    <h2 style='margin: 0; font-size: 22px;'>UTC STADIUM</h2>
                                    <p style='margin: 4px 0 0 0; color: #FFC107; font-size: 12px; font-weight: bold; text-transform: uppercase;'>Hệ Thống Đặt Sân Bóng Đá Sinh Viên</p>
                                </div>
                                <div style='padding: 24px; background-color: #ffffff; color: #1e293b;'>
                                    <div style='display: inline-block; padding: 4px 12px; border-radius: 9999px; background-color: {badgeColor}; color: #ffffff; font-size: 11px; font-weight: bold; text-transform: uppercase; margin-bottom: 12px;'>
                                        {WebUtility.HtmlEncode(loaiThongBao)}
                                    </div>
                                    <h3 style='margin: 0 0 14px 0; color: #002B5B; font-size: 18px;'>{WebUtility.HtmlEncode(tieuDe)}</h3>
                                    <p style='margin: 0 0 16px 0; font-size: 14px; line-height: 1.6; color: #334155;'>
                                        Xin chào <strong>{WebUtility.HtmlEncode(targetName)}</strong>,
                                    </p>
                                    <div style='background-color: #f8fafc; border-left: 4px solid {badgeColor}; padding: 14px 16px; border-radius: 8px; margin-bottom: 20px; font-size: 13.5px; line-height: 1.6; color: #1e293b;'>
                                        {WebUtility.HtmlEncode(noiDung).Replace("\n", "<br/>")}
                                    </div>
                                    <p style='font-size: 12.5px; color: #64748b; line-height: 1.5; margin-bottom: 0;'>
                                        Bạn có thể đăng nhập vào cổng thông tin UTC STADIUM để theo dõi chi tiết lịch đặt sân và quản lý tài khoản.
                                    </p>
                                </div>
                                <div style='background-color: #f1f5f9; padding: 12px 24px; text-align: center; font-size: 11.5px; color: #64748b; border-top: 1px solid #e2e8f0;'>
                                    &copy; 2026 UTC STADIUM &bull; Trường Đại học Giao thông Vận tải Hà Nội
                                </div>
                            </div>";

                            await _emailService.SendEmailAsync(targetEmail, $"[UTC STADIUM] {tieuDe}", htmlEmail);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Lỗi khi bắn Gmail thông báo đến {Email}", targetEmail);
                        }
                    });
                }

                return thongBao;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu thông báo cho người dùng {UserId}", nguoiDungId);
                return null;
            }
        }

        /// <summary>
        /// Bắn thông báo khi Admin duyệt, từ chối hoặc chuyển trạng thái đơn đặt sân.
        /// </summary>
        public async Task ThongBaoTrangThaiDatSanAsync(int datSanId, string trangThaiMoi, string? ghiChu = null)
        {
            try
            {
                var datSan = await _context.DatSans
                    .Include(d => d.SanBong)
                    .Include(d => d.KhungGio)
                    .FirstOrDefaultAsync(d => d.Id == datSanId);

                if (datSan == null) return;

                string tieuDe;
                string noiDung;
                string loaiThongBao;

                var tenSan = datSan.SanBong?.TenSan ?? "Sân bóng";
                var ngayDa = datSan.NgayDat.ToString("dd/MM/yyyy");
                var gioDa = datSan.KhungGio != null
                    ? $"{datSan.KhungGio.GioBatDau:HH:mm} - {datSan.KhungGio.GioKetThuc:HH:mm}"
                    : "Ca đã chọn";

                switch (trangThaiMoi)
                {
                    case "Đã duyệt":
                        tieuDe = $"Đơn đặt sân #{datSan.Id} đã được Ban Quản Lý duyệt";
                        noiDung = $"Ban Quản Lý sân đã xác nhận thành công đơn #{datSan.Id} của bạn tại {tenSan} cho ca {gioDa} ngày {ngayDa}. Vui lòng đến trước 15 phút để làm thủ tục nhận sân.";
                        loaiThongBao = "DuyetDon";
                        break;

                    case "Từ chối":
                        tieuDe = $"Đơn đặt sân #{datSan.Id} đã bị từ chối";
                        noiDung = $"Rất tiếc, đơn #{datSan.Id} tại {tenSan} (ngày {ngayDa}, ca {gioDa}) đã bị từ chối phê duyệt. Lý do: {(string.IsNullOrWhiteSpace(ghiChu) ? "Trùng lịch thi đấu sự kiện trường hoặc quá hạn giữ chỗ" : ghiChu)}. Vui lòng liên hệ hotline nếu cần hỗ trợ hoàn cọc.";
                        loaiThongBao = "TuChoi";
                        break;

                    case "Hoàn thành":
                        tieuDe = $"Trận đấu tại {tenSan} đã hoàn thành - Mời bạn đánh giá!";
                        noiDung = $"Đơn đặt sân #{datSan.Id} tại {tenSan} (ngày {ngayDa}) đã chuyển sang trạng thái Hoàn thành. Hãy dành 1 phút viết đánh giá và chấm điểm sao để giúp nâng cao chất lượng dịch vụ của sân nhé!";
                        loaiThongBao = "HoanThanh";
                        break;

                    case "Đã hủy":
                        tieuDe = $"Đơn đặt sân #{datSan.Id} đã được hủy bỏ";
                        noiDung = $"Đơn đặt sân #{datSan.Id} tại {tenSan} ngày {ngayDa} đã được hủy theo yêu cầu. {(string.IsNullOrWhiteSpace(ghiChu) ? "" : "Ghi chú: " + ghiChu)}";
                        loaiThongBao = "TuChoi";
                        break;

                    default:
                        tieuDe = $"Cập nhật trạng thái đơn đặt sân #{datSan.Id}";
                        noiDung = $"Đơn đặt sân #{datSan.Id} của bạn đã chuyển sang trạng thái '{trangThaiMoi}'. {(string.IsNullOrWhiteSpace(ghiChu) ? "" : "Ghi chú: " + ghiChu)}";
                        loaiThongBao = "HeThong";
                        break;
                }

                await GuiThongBaoAsync(datSan.NguoiDungId, tieuDe, noiDung, loaiThongBao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi bắn thông báo trạng thái đơn đặt sân #{DatSanId}", datSanId);
            }
        }

        /// <summary>
        /// Bắn thông báo khi Admin duyệt hoặc từ chối yêu cầu đổi lịch.
        /// </summary>
        public async Task ThongBaoTrangThaiDoiLichAsync(int yeuCauDoiLichId, string trangThaiMoi, string? ghiChu = null)
        {
            try
            {
                var yeuCau = await _context.YeuCauDoiLiches
                    .Include(y => y.DatSan)
                        .ThenInclude(d => d.SanBong)
                    .Include(y => y.DatSan)
                        .ThenInclude(d => d.NguoiDung)
                    .Include(y => y.KhungGioMoi)
                    .FirstOrDefaultAsync(y => y.Id == yeuCauDoiLichId);

                if (yeuCau == null || yeuCau.DatSan == null) return;

                int nguoiDungId = yeuCau.DatSan.NguoiDungId;
                string tieuDe;
                string noiDung;
                string loaiThongBao = "DoiLich";

                var tenSan = yeuCau.DatSan?.SanBong?.TenSan ?? "Sân bóng";
                var ngayMoi = yeuCau.NgayDatMoi.ToString("dd/MM/yyyy");
                var gioMoi = yeuCau.KhungGioMoi != null
                    ? $"{yeuCau.KhungGioMoi.GioBatDau:HH:mm} - {yeuCau.KhungGioMoi.GioKetThuc:HH:mm}"
                    : "Ca mới";

                if (trangThaiMoi == "Đã duyệt")
                {
                    tieuDe = $"Yêu cầu đổi lịch đơn #{yeuCau.DatSanId} đã được chấp thuận";
                    noiDung = $"Ban Quản Lý đã phê duyệt yêu cầu đổi lịch của bạn. Giờ thi đấu mới tại {tenSan} là ca {gioMoi} ngày {ngayMoi}. Chúc bạn và đội bóng có trận đấu vui vẻ!";
                }
                else
                {
                    tieuDe = $"Yêu cầu đổi lịch đơn #{yeuCau.DatSanId} không được chấp thuận";
                    noiDung = $"Yêu cầu đổi lịch cho đơn #{yeuCau.DatSanId} đã bị từ chối. Lý do: {(string.IsNullOrWhiteSpace(ghiChu) ? "Khung giờ mới đã có đội bóng khác giữ chỗ hoặc trùng lịch giải đấu" : ghiChu)}. Lịch đá ban đầu vẫn được giữ nguyên.";
                }

                await GuiThongBaoAsync(nguoiDungId, tieuDe, noiDung, loaiThongBao);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi bắn thông báo yêu cầu đổi lịch #{YeuCauId}", yeuCauDoiLichId);
            }
        }
    }
}
