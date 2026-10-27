# CẤU TRÚC KIẾN TRÚC DỰ ÁN QLSANBONG

```text

QLSanBong (Solution / Web Project)
│
├── Areas/
│   ├── Admin/                      # Phân hệ Quản trị viên (Quản lý hệ thống, thống kê, duyệt đơn)
│   │   ├── Controllers/            # DashboardController, SanBongController, DatSanController,...
│   │   ├── Models/                 # ViewModels dành riêng cho Admin (Thống kê, Báo cáo)
│   │   └── Views/                  # Giao diện quản trị, LayoutAdmin, Components
│   │       ├── _ViewImports.cshtml
│   │       ├── _ViewStart.cshtml
│   │       └── Shared/
│   │           └── _LayoutAdmin.cshtml
│   │
│   └── KhachHang/                  # Phân hệ Khách hàng (Tìm sân, đặt ca, thanh toán, quản lý đơn)
│       ├── Controllers/            # HomeController, DatSanController, LichSuController,...
│       ├── Models/                 # ViewModels đặt sân, giỏ hàng, thông tin cá nhân
│       └── Views/                  # Giao diện đặt sân, chi tiết sân, hóa đơn
│           ├── _ViewImports.cshtml
│           ├── _ViewStart.cshtml
│           └── Shared/
│               └── _LayoutCustomer.cshtml
│
├── Controllers/                    # Phân hệ dùng chung / Trang công khai ngoài Area
│   ├── HomeController.cs           # Giới thiệu chung, bảng giá tham khảo, liên hệ
│   └── AccountController.cs        # Đăng ký, Đăng nhập, Đăng xuất, Phân quyền
│
├── Models/
│   ├── Entities/                   # 14 Entity Models & DbContext sinh tự động từ Database
│   │   ├── QLSanBongDbContext.cs   # Lớp Context trung tâm quản lý kết nối và các DbSet
│   │   ├── QuyenHan.cs             # Quyền hạn / Vai trò hệ thống
│   │   ├── NguoiDung.cs            # Tài khoản người dùng (Admin, Khách)
│   │   ├── SanBong.cs              # Thông tin sân bóng (Sân 5, Sân 7, Sân 11)
│   │   ├── KhungGio.cs             # Danh mục ca đá trong ngày
│   │   ├── ThietLapGia.cs          # Đơn giá theo từng sân, khung giờ và ngày trong tuần
│   │   ├── DatSan.cs               # Đơn đặt sân bóng
│   │   ├── ChiTietDichVuDat.cs     # Chi tiết số lượng dịch vụ phát sinh kèm theo đơn
│   │   ├── DichVu.cs               # Danh mục phụ trợ: Nước giải khát, thuê bóng, áo pitch
│   │   ├── HoaDon.cs               # Hóa đơn thanh toán tổng hợp tiền sân & dịch vụ
│   │   ├── ThanhToan.cs            # Giao dịch thanh toán (Tiền mặt, QR Banking)
│   │   ├── MaGiamGia.cs            # Coupon khuyến mãi & giảm giá
│   │   ├── DanhGia.cs              # Đánh giá & phản hồi chất lượng sân từ khách
│   │   ├── LichBaoTri.cs           # Lịch bảo dưỡng mặt sân/hệ thống chiếu sáng (khóa ca)
│   │   ├── ThongBao.cs             # Thông báo gửi đến tài khoản người dùng
│   │   └── YeuCauDoiLich.cs        # Yêu cầu dời ca/đổi lịch từ khách hàng
│   │
│   └── ViewModels/                 # Models đóng gói dữ liệu phục vụ hiển thị & form nhập liệu
│       ├── LoginViewModel.cs       # Dữ liệu form đăng nhập
│       ├── RegisterViewModel.cs    # Dữ liệu form đăng ký tài khoản mới
│       └── BookingViewModel.cs     # Dữ liệu xử lý form đặt sân
│
├── Views/                          # Giao diện dùng chung ngoài Areas
│   ├── Account/                    # Views đăng nhập, đăng ký
│   │   ├── Login.cshtml
│   │   └── Register.cshtml
│   ├── Home/                       # View trang chủ công khai
│   ├── Shared/                     # Giao diện dùng chung toàn web
│   │   ├── _Layout.cshtml          # Layout mặc định
│   │   └── _NotificationPartial.cshtml
│   ├── _ViewImports.cshtml         # Khai báo TagHelper và namespace chung cho Views
│   └── _ViewStart.cshtml           # Chỉ định layout mặc định
│
├── wwwroot/                        # Tài nguyên tĩnh của ứng dụng
│   ├── css/                        # Bảng định kiểu CSS chia theo phân hệ và từng trang
│   │   ├── common/                 # Định dạng dùng chung toàn website
│   │   │   └── site.css            # CSS reset, biến màu chủ đạo, font chữ
│   │   ├── client/                 # CSS giao diện Khách hàng & Trang chủ
│   │   │   ├── trangchu.css        # Banner, giới thiệu, danh sách sân nổi bật
│   │   │   ├── datsan.css          # Sơ đồ chọn sân trực quan, chọn ca giờ
│   │   │   ├── lichsu.css          # Danh sách đơn đặt, chi tiết vé đặt sân
│   │   │   └── auth.css            # Giao diện form đăng ký, đăng nhập
│   │   └── admin/                  # CSS giao diện Quản trị viên
│   │       ├── admin.css           # Bố cục Sidebar, Topbar, Navbar quản trị
│   │       ├── dashboard.css       # Biểu đồ doanh thu, thống kê lượt đặt sân
│   │       ├── sanbong.css         # Danh sách quản lý sân, form thêm/sửa sân
│   │       └── datsan-admin.css    # Bảng theo dõi lịch đặt sân trực tiếp theo ngày
│   │
│   ├── js/                         # JavaScript chia theo từng tính năng
│   │   ├── common/                 # JS tiện ích dùng chung
│   │   │   └── site.js             # Xử lý thông báo toast, validate chung
│   │   ├── client/                 # JS tương tác cho Khách hàng
│   │   │   ├── datsan.js           # AJAX lọc sân trống, tính tổng tiền theo ca
│   │   │   └── thanhtoan.js        # Hiển thị QR chuyển khoản, xác nhận thanh toán
│   │   └── admin/                  # JS tương tác cho Quản trị viên
│   │       ├── dashboard.js        # Vẽ biểu đồ Chart.js (doanh thu, tỷ lệ lấp đầy)
│   │       └── sanbong.js          # Modal thêm mới/sửa sân, xác nhận xóa
│   │
│   ├── lib/                        # Thư viện bên ngoài (Bootstrap 5, jQuery, FontAwesome, Chart.js)
│   └── uploads/                    # Thư mục lưu file do người dùng / admin tải lên
│       ├── sanbong/                # Ảnh chụp thực tế của các sân bóng
│       ├── avatars/                # Ảnh đại diện tài khoản người dùng
│       └── qrcode/                 # Ảnh mã QR thanh toán ngân hàng
│
├── Docs/                           # Tài liệu thiết kế & phân tích hệ thống
│   ├── 01_Project_Structure.md     # Tài liệu cấu trúc dự án & thư mục
│   ├── 02_Database_Dictionary.md   # Cơ sở dữ liệu
│   └── 03_ERD_QLSanBong.png        # Sơ đồ quan hệ ERD
|   └── 04_Sitemap.png
|   └── 05_usecase					# Chứa hình ảnh usecase
│
├── appsettings.json                # Chuỗi kết nối Database SQL Server & thông số ứng dụng
└── Program.cs                      # File khởi chạy: Đăng ký DI DbContext, Authentication, Area Routing

```
