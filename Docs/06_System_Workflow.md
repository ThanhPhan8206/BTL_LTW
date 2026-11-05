# HỆ THỐNG QUẢN LÝ SÂN BÓNG ĐÁ "UTC STADIUM"
## ĐẶC TẢ SƠ ĐỒ KIẾN TRÚC & LUỒNG HOẠT ĐỘNG TOÀN HỆ THỐNG
*(SYSTEM WORKFLOW & USER JOURNEY SPECIFICATION)*

---

> [!NOTE]
> **Thông tin dự án:**
> - **Tên hệ thống:** UTC STADIUM - Cổng Thông Tin & Quản Lý Đặt Sân Bóng Đá Sinh Viên
> - **Nền tảng công nghệ:** ASP.NET Core MVC (.NET 8), Entity Framework Core, SQL Server, Chart.js, Bootstrap 5
> - **Mô hình kiến trúc:** Area-based MVC (`Areas/KhachHang`, `Areas/Admin`, Core Controllers & Services)

---

## MỤC LỤC TỔNG QUAN

1. [PHẦN 1: TỔNG QUAN LUỒNG XÁC THỰC & ĐIỀU HƯỚNG PHÂN QUYỀN (AUTH & RBAC FLOW)](#phần-1-tổng-quan-luồng-xác-thực--điều-hướng-phân-quyền-auth--rbac-flow)
2. [PHẦN 2: LUỒNG HOẠT ĐỘNG PHÂN HỆ KHÁCH HÀNG (AREAS/KHACHHANG)](#phần-2-luồng-hoạt-động-phân-hệ-khách-hàng-areaskhachhang)
3. [PHẦN 3: LUỒNG HOẠT ĐỘNG PHÂN HỆ QUẢN TRỊ ADMIN (AREAS/ADMIN)](#phần-3-luồng-hoạt-động-phân-hệ-quản-trị-admin-areasadmin)
4. [PHẦN 4: SƠ ĐỒ TUẦN TỰ E2E TƯƠNG TÁC TOÀN HỆ THỐNG (MERMAID SEQUENCE DIAGRAM)](#phần-4-sơ-đồ-tuần-tự-e2e-tương-tác-toàn-hệ-thống-mermaid-sequence-diagram)
5. [PHẦN 5: BẢNG MA TRẬN GIẢI PHÁP KỸ THUẬT NỔI BẬT](#phần-5-bảng-ma-trận-giải-pháp-kỹ-thuật-nổi-bật)

---

## PHẦN 1: TỔNG QUAN LUỒNG XÁC THỰC & ĐIỀU HƯỚNG PHÂN QUYỀN (AUTH & RBAC FLOW)

Hệ thống xác thực của UTC STADIUM được xây dựng trên nền tảng **Cookie Authentication & ASP.NET Core Claims Identity**, kết hợp mã hóa một chiều bằng thuật toán `PasswordHasher<NguoiDung>` (PBKDF2 HMAC-SHA256 có Salt ngẫu nhiên).

```mermaid
flowchart TD
    Start(["Khách hàng / Quản trị viên"]) --> Choice{"Hành động"}

    %% Nhánh Đăng Ký
    Choice -->|"Đăng ký mới"| Reg["Truy cập /Account/Register"]
    Reg --> RegSubmit["Nhập Họ tên, SĐT, Email, Mật khẩu"]
    RegSubmit --> RegHash["Băm mật khẩu PBKDF2<br/>Gán Quyền: Khách hàng (RoleId=2)<br/>Trạng thái: Hoạt động"]
    RegHash --> DBUser[("Lưu vào dbo.NguoiDung")]
    DBUser --> RegRedirect["Chuyển hướng đến /Account/Login"]

    %% Nhánh Quên Mật Khẩu
    Choice -->|"Quên mật khẩu"| Forgot["Truy cập /Account/ForgotPassword"]
    Forgot --> ForgotInput["Nhập Email đã đăng ký"]
    ForgotInput --> OTPGen["Hệ thống sinh OTP ngẫu nhiên 6 số<br/>Lưu vào MemoryCache (TTL = 5 phút)"]
    OTPGen --> SMTPSend["Dịch vụ EmailService bắn Gmail SMTP<br/>chứa mã OTP xác thực"]
    SMTPSend --> ResetView["Nhập mã OTP và Mật khẩu mới<br/>tại /Account/ResetPassword"]
    ResetView --> OTPVerify{"Kiểm tra OTP"}
    OTPVerify -->|"Sai / Hết hạn"| OTPFail["Báo lỗi, yêu cầu lấy lại mã"]
    OTPVerify -->|"Chính xác"| OTPPass["Cập nhật MatKhauMaHoa mới<br/>Xóa mã OTP khỏi Cache"] --> RegRedirect

    %% Nhánh Đăng Nhập
    Choice -->|"Đăng nhập"| Login["Truy cập /Account/Login"]
    RegRedirect --> Login
    Login --> LoginAuth["Xác thực Email & Mật khẩu"]
    LoginAuth --> CheckStatus{"Kiểm tra TrangThai"}
    CheckStatus -->|"Bị khóa"| LockAlert["Chặn truy cập!<br/>Báo liên hệ Hotline BQL Sân"]
    CheckStatus -->|"Hoạt động"| GenClaims["Cấp Claims Cookie Xác Thực<br/>Role: Admin hoặc KhachHang"]
    
    %% Điều hướng sau đăng nhập
    GenClaims --> RoleRoute{"Kiểm tra Vai trò (Role)"}
    RoleRoute -->|"Admin / Quản trị"| AdminHub["Điều hướng vào<br/>Areas/Admin/Dashboard/Index"]
    RoleRoute -->|"Khách hàng / Sinh viên"| ClientHub["Điều hướng vào<br/>Areas/KhachHang/Home/Index"]
```

### Chi Tiết Kỹ Thuật:
1. **Đăng ký tài khoản (`/Account/Register`)**: Khách hàng nhập thông tin. Mật khẩu được băm bảo mật bằng `PasswordHasher<NguoiDung>`, gán vai trò mặc định `"Khách hàng"`, trạng thái `"Hoạt động"`.
2. **Khôi phục mật khẩu qua Gmail OTP**: Hệ thống sinh mã OTP 6 số ngẫu nhiên qua `RandomNumberGenerator`, lưu đệm trong `MemoryCache` trong vòng **5 phút** (không cần tạo thêm cột trong DB) và bắn qua Gmail SMTP. Khách xác thực thành công sẽ được cấp quyền cập nhật mật khẩu mới.
3. **Đăng nhập & Điều hướng thông minh**:
   - Tài khoản có cờ `TrangThai == "Bị khóa"` sẽ bị chặn ngay lập tức kèm thông báo hotline hỗ trợ.
   - Nếu đăng nhập thành công, hệ thống kiểm tra Claim `Role`:
     - **Admin**: Điều hướng thẳng vào `Areas/Admin/Dashboard/Index`.
     - **Khách hàng**: Trả về `Areas/KhachHang/Home/Index` hoặc trang đặt sân trước đó (`ReturnUrl`).

---

## PHẦN 2: LUỒNG HOẠT ĐỘNG PHÂN HỆ KHÁCH HÀNG (`AREAS/KHACHHANG`)

Quy trình trải nghiệm người dùng (User Journey) từ lúc tìm kiếm sân đến khi hoàn tất trận đấu và để lại đánh giá:

```mermaid
flowchart TD
    %% Giai đoạn 1: Tra cứu & Lọc sân
    subgraph G1 ["1. Tra Cứu & Lọc Sân Thông Minh"]
        C_Home["Trang chủ Khách hàng (/KhachHang/Home)"] --> C_Filter["Bộ lọc: Ngày đá, Khung giờ, Loại sân (5/7/11), Giá"]
        C_Filter --> C_Query["Hệ thống quét CSDL:<br/>- Loại trừ sân trong dbo.LichBaoTri<br/>- Loại trừ ca đã kín lịch trong dbo.DatSan"]
        C_Query --> C_List["Hiển thị danh sách sân bóng sẵn sàng phục vụ"]
    end

    %% Giai đoạn 2: Tạo đơn đặt & Dịch vụ
    subgraph G2 ["2. Tạo Đơn Đặt Sân, Dịch Vụ & Thanh Toán"]
        C_List --> C_Pick["Chọn sân ưng ý -> Nhấn 'Đặt sân ngay'"]
        C_Pick --> C_Form["Mở trang đặt sân (/KhachHang/DatSan/TaoDon)<br/>- Auto-fill thông tin cá nhân khách<br/>- Chọn ca thi đấu từ dbo.KhungGio"]
        C_Form --> C_Service["Tùy chọn dịch vụ tiện ích:<br/>Nước Lavie, Revive, Bóng thi đấu, Áo bib, Trọng tài"]
        C_Service --> C_Voucher["Nhập mã giảm giá (Voucher):<br/>Kiểm tra hiệu lực từ dbo.MaGiamGia"]
        C_Voucher --> C_Calc["Tính tổng tiền = Tiền sân + Tiền dịch vụ - Giảm giá"]
        C_Calc --> C_Submit["Bấm 'Tiếp tục & Thanh toán'"]
        C_Submit --> C_SaveOrder["Lưu đơn vào dbo.DatSan (Trạng thái: Chờ duyệt)<br/>Lưu dịch vụ vào dbo.ChiTietDichVuDat"]
        C_SaveOrder --> C_VietQR["Hiển thị cổng thanh toán VietQR Napas247:<br/>- Tùy chọn: Đặt cọc 30% hoặc Thanh toán 100%<br/>- Sinh mã QR động đúng cú pháp: COC SAN [ID] [SĐT]"]
    end

    %% Giai đoạn 3: Theo dõi & Đổi lịch
    subgraph G3 ["3. Quản Lý Đơn & Đổi Lịch (Time-lock Rule)"]
        C_VietQR --> C_History["Khách theo dõi tại /KhachHang/DatSan/LichSu"]
        C_History --> C_StateCheck{"Theo dõi trạng thái đơn"}
        C_StateCheck -->|"Chờ duyệt"| C_Wait["Đang chờ Admin duyệt cọc"]
        C_StateCheck -->|"Đã xác nhận"| C_Confirmed["Sân đã khóa giữ chỗ thành công"]
        
        C_Confirmed --> C_NeedChange{"Khách có việc đột xuất?"}
        C_NeedChange -->|"Xin đổi lịch"| C_Reschedule["Vào /KhachHang/YeuCauDoiLich/TaoYeuCau"]
        C_Reschedule --> C_TimeLock{"Kiểm tra Time-lock Rule:<br/>Cách giờ đá >= 2 tiếng?"}
        C_TimeLock -->|"Dưới 2 tiếng"| C_LockErr["CHẶN ĐỔI: Khóa theo quy định Time-lock"]
        C_TimeLock -->|"Đủ điều kiện"| C_SendReq["Gửi yêu cầu đổi sân/ca mới lên BQL"]
        
        C_NeedChange -->|"Không đổi"| C_Play["Đến sân tham gia trận đấu"]
    end

    %% Giai đoạn 4: Đánh giá Feedback
    subgraph G4 ["4. Hoàn Thành & Đánh Giá Chất Lượng"]
        C_Play --> C_Done["Trận đấu kết thúc (Hoàn thành)"]
        C_Done --> C_Invoice["Xem & tải Hóa đơn điện tử chi tiết"]
        C_Done --> C_Review["Vào /KhachHang/DanhGia/VietDanhGia:<br/>Chấm điểm từ 1 đến 5 sao (★)<br/>Gửi bình luận cảm nghĩ về mặt cỏ, ánh sáng"]
        C_Review --> C_SavedReview[("Lưu vào dbo.DanhGia")]
    end
```

---

## PHẦN 3: LUỒNG HOẠT ĐỘNG PHÂN HỆ QUẢN TRỊ ADMIN (`AREAS/ADMIN`)

Phân hệ Admin phụ trách toàn diện công tác **Vận hành sân cỏ - Giám sát dòng tiền - Điều phối lịch thi đấu - Chăm sóc khách hàng**:

```mermaid
flowchart TD
    AdminStart(["Quản trị viên đăng nhập"]) --> AdminDash["Dashboard Trung Tâm (/Admin/Dashboard)"]

    %% Nhánh 1: Dashboard
    AdminDash --> M1["1. Giám sát KPI & Doanh thu:<br/>- 4 thẻ KPI động: Đơn chờ duyệt, Đổi lịch, Đơn hôm nay, Doanh thu<br/>- Biểu đồ xu hướng Chart.js 7 ngày gần nhất"]

    %% Nhánh 2: Vận hành Lịch Đặt
    AdminDash --> M2["2. Vận hành Lịch Đặt Sân (/Admin/DatSan)"]
    M2 --> Op_Duyet{"Xử lý đơn Chờ duyệt"}
    Op_Duyet -->|"Xác nhận"| Op_Confirm["Chuyển trạng thái: Đã xác nhận<br/>Tự động gửi Gmail SMTP + Thông báo Web cho khách"]
    Op_Duyet -->|"Từ chối"| Op_Reject["Bắt buộc nhập Lý do từ chối<br/>Gửi email giải trình cho khách"]
    
    Op_Confirm --> Op_Deliver{"Bàn Giao Sân Thi Đấu"}
    Op_Deliver -->|"Đội bóng đến sân"| Op_Nhan["Bấm 'Nhận sân' -> Chuyển: Đang đá"]
    Op_Deliver -->|"Hết giờ thi đấu"| Op_Tra["Bấm 'Trả sân' -> Chuyển: Hoàn thành"]
    Op_Tra --> Op_AutoInvoice["TỰ ĐỘNG SINH HÓA ĐƠN ĐIỆN TỬ<br/>Tạo bản ghi trong dbo.HoaDon (mã HD#####yyyyMMdd)<br/>Hỗ trợ in hóa đơn khổ A4 sạch sẽ"]

    %% Nhánh 3: Xử lý Đổi lịch
    AdminDash --> M3["3. Xử lý Đổi Lịch (/Admin/DatSan/YeuCauDoiLich)"]
    M3 --> Resched_Check{"Duyệt Yêu Cầu Đổi Lịch"}
    Resched_Check --> Resched_Validate["Kiểm tra tự động:<br/>1. Ca mới đề xuất có bị trùng đơn khác không?<br/>2. Sân mới có bị dính lịch bảo trì đột xuất không?"]
    Resched_Validate -->|"Bị trùng / Bảo trì"| Resched_Block["Chặn duyệt, báo lỗi xung đột lịch"]
    Resched_Validate -->|"Sân ca mới trống"| Resched_Approve["Duyệt đổi: Cập nhật ngày/giờ/sân mới sang đơn gốc<br/>Bắn email thông báo giờ đá mới cho khách"]

    %% Nhánh 4: Đặt hộ tại quầy
    AdminDash --> M4["4. Đặt Hộ Khách Tại Quầy (CreateTaiQuay)"]
    M4 --> Desk_Action["Nhập SĐT khách (tự tạo tài khoản vãng lai nếu chưa có)<br/>Chọn sân, ca, dịch vụ -> Thu tiền mặt 100% -> Chuyển thẳng 'Đã xác nhận'"]

    %% Nhánh 5: Hạ tầng & Giá
    AdminDash --> M5["5. Hạ Tầng & Biểu Giá (/Admin/SanBong, KhungGio, ThietLapGia)"]
    M5 --> Pitch_Action["- Thêm/sửa sân, upload ảnh vào wwwroot/uploads/sanbong<br/>- Xóa mềm: Sân đã có đơn chuyển 'Ngừng hoạt động'<br/>- Thiết lập bảo trì: Chặn trùng đơn đã duyệt<br/>- Cập nhật ma trận giá ngày thường / cuối tuần, sao chép biểu giá"]

    %% Nhánh 6: Tài chính & Tiện ích
    AdminDash --> M6["6. Tài Chính & Tiện Ích (/Admin/ThanhToan, DichVu, MaGiamGia)"]
    M6 --> Fin_Action["- Thu tiền mặt tại quầy / Hoàn cọc khi hủy đơn<br/>- Quản lý dịch vụ: Ngừng bán (xóa mềm [Ngừng bán])<br/>- Quản lý Voucher: Tắt mã tức thì / Gia hạn / Gửi mail tặng voucher"]

    %% Nhánh 7: Khách hàng & Đánh giá
    AdminDash --> M7["7. Khách Hàng, Đánh Giá & Sự Kiện (/Admin/KhachHang, DanhGia, ThongBao)"]
    M7 --> Cust_Action["- Khóa / Mở khóa tài khoản khách (chặn đăng nhập, bảo mật mật khẩu)<br/>- Phản hồi bình luận khách (nối || [Admin]: ...) / Ẩn đánh giá vi phạm<br/>- Soạn & phát hành thông báo sự kiện / giải đấu đến toàn hệ thống qua Gmail"]
```

---

## PHẦN 4: SƠ ĐỒ TUẦN TỰ E2E TƯƠNG TÁC TOÀN HỆ THỐNG (MERMAID SEQUENCE DIAGRAM)

Sơ đồ thể hiện trực quan tương tác giữa 4 thực thể: **Khách hàng** $\leftrightarrow$ **Web Controller / Services** $\leftrightarrow$ **SQL Server Database** $\leftrightarrow$ **Quản trị viên (Admin)**:

```mermaid
sequenceDiagram
    autonumber
    actor Khach as Khách Hàng
    participant Web as Web Application (MVC & Services)
    participant DB as SQL Server Database
    actor Admin as Ban Quản Trị (Admin)

    %% 1. Đặt sân
    Note over Khach, DB: GIAI ĐOẠN 1: TẠO ĐƠN & THANH TOÁN CỌC
    Khach->>Web: 1. Chọn Sân 1, Ca 19h00, chọn Dịch vụ & Áp mã Voucher
    Web->>DB: 2. Quét kiểm tra trùng ca & kiểm tra lịch bảo trì
    DB-->>Web: 3. Kết quả: Sân trống, không có lịch bảo trì
    Web->>DB: 4. Lưu DatSan (Trạng thái: "Chờ duyệt") + ChiTietDichVuDat
    DB-->>Web: 5. Khởi tạo thành công (Mã đơn: #102)
    Web-->>Khach: 6. Hiển thị mã VietQR Napas247 cọc 30% (Cú pháp: COC SAN 102 0333...)
    Khach->>Web: 7. Xác nhận đã chuyển khoản cọc VietQR

    %% 2. Duyệt đơn
    Note over Admin, Khach: GIAI ĐOẠN 2: TIẾP NHẬN & PHÊ DUYỆT ĐƠN HÀNG
    Admin->>Web: 8. Mở Dashboard Admin (Thấy đơn #102 Chờ duyệt)
    Admin->>Web: 9. Kiểm tra tài khoản ngân hàng & bấm "Xác nhận đơn"
    Web->>DB: 10. Cập nhật DatSan #102 -> TrangThai = "Đã xác nhận"
    Web->>DB: 11. Lưu thông báo vào dbo.ThongBao
    Web-->>Khach: 12. Bắn Gmail SMTP thông báo: "Đơn #102 đã được xác nhận giữ chỗ"

    %% 3. Bàn giao sân
    Note over Khach, Admin: GIAI ĐOẠN 3: BÀN GIAO SÂN & SINH HÓA ĐƠN ĐIỆN TỬ
    Khach->>Admin: 13. Có mặt tại sân thi đấu đúng giờ
    Admin->>Web: 14. Bấm "Nhận sân" -> Cập nhật TrangThai = "Đang đá"
    Web->>DB: 15. Ghi nhận thời gian nhận sân thực tế
    Note over Khach, Admin: Trận đấu diễn ra trong 90 phút...
    Khach->>Admin: 16. Hết giờ, bàn giao lại bóng & áo tập
    Admin->>Web: 17. Bấm "Trả sân" -> Cập nhật TrangThai = "Hoàn thành"
    Web->>DB: 18. TỰ ĐỘNG INSERT BẢN GHI VÀO dbo.HoaDon (Mã HD00102yyyyMMdd)
    Web-->>Admin: 19. Xuất Hóa đơn điện tử A4 có chữ ký, hỗ trợ in ra giấy
    Web-->>Khach: 20. Cập nhật trạng thái "Hoàn thành" trong lịch sử cá nhân

    %% 4. Đánh giá
    Note over Khach, Admin: GIAI ĐOẠN 4: ĐÁNH GIÁ VÀ PHẢN HỒI Ý KIẾN
    Khach->>Web: 21. Gửi đánh giá 5 Sao (★) + Lời bình: "Mặt sân cỏ rất êm, đèn sáng"
    Web->>DB: 22. Lưu vào dbo.DanhGia
    Admin->>Web: 23. Mở module Đánh giá, đọc nhận xét của khách
    Admin->>Web: 24. Nhập phản hồi: "Cảm ơn bạn đã ủng hộ UTC Stadium!"
    Web->>DB: 25. Cập nhật BinhLuan = "... || [Admin]: Cảm ơn bạn..."
    Web-->>Khach: 26. Gửi thông báo Web: "Admin đã phản hồi nhận xét của bạn"
```

---

## PHẦN 5: BẢNG MA TRẬN GIẢI PHÁP KỸ THUẬT NỔI BẬT

| Hạng mục kỹ thuật | Giải pháp triển khai trong dự án UTC STADIUM | Giá trị thực tiễn khi bảo vệ đồ án |
| :--- | :--- | :--- |
| **Bảo toàn CSDL (Schema Integrity)** | Tuyệt đối không chỉnh sửa cấu trúc `Models/Entities/` gốc. Mọi logic mới giải quyết bằng ViewModel và tiền tố nghiệp vụ. | Chứng minh năng lực làm việc trên hệ thống kế thừa (Legacy Code) mà không gây rủi ro đứt gãy hệ thống. |
| **Xóa mềm chống lỗi Khóa ngoại** | Áp dụng tiền tố `[Ngừng bán]` cho Dịch vụ và cờ `Ngừng hoạt động` cho Sân bóng thay vì xóa cứng khỏi SQL. | Triệt tiêu 100% lỗi `SqlException: Foreign Key Constraint Violation`, bảo toàn dữ liệu kế toán và hóa đơn cũ. |
| **Quy tắc Time-lock Rule** | Chặn hủy và đổi lịch trước giờ thi đấu dưới 2 tiếng trên cả Client (JavaScript) lẫn Server (Controller validation). | Đảm bảo tính công bằng thương mại, bảo vệ doanh thu cụm sân, ngăn chặn hành vi đặt ảo / bùng sân phút chót. |
| **Bàn giao sân & Sinh Hóa đơn** | Luồng 1 chiều `Chờ duyệt` $\rightarrow$ `Đã xác nhận` $\rightarrow$ `Đang đá` $\rightarrow$ `Hoàn thành` tự sinh `dbo.HoaDon`. | Khép kín chu trình kế toán quản trị chuyên nghiệp, tự động hóa toàn bộ khâu xuất hóa đơn thanh toán. |
| **Truyền thông đa kênh (Multi-channel)** | Tích hợp song song thông báo hòm thư Web (`dbo.ThongBao`) và bắn email HTML chuẩn Gmail qua giao thức SMTP. | Nâng cao trải nghiệm người dùng, đảm bảo khách hàng nhận thông tin tức thì ngay cả khi không mở website. |

---
*Tài liệu được khởi tạo và đóng gói tự động bởi Antigravity System Architect.*

