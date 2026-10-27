# PHÂN CÔNG NHIỆM VỤ - QUẢN LÝ SÂN BÓNG ĐÁ (ASP.NET MVC)

Repo: https://github.com/ThanhPhan8206/BTL_LTW

## 1. Thành viên và nhánh làm việc

| Người | GitHub | Nhánh | Mảng phụ trách |
|---|---|---|---|
| 1 | [@ThanhPhan8206](https://github.com/ThanhPhan8206) | `feature/auth-core` | Trang chủ, tài khoản, phân quyền, khách hàng, thông báo |
| 2 | [@minu3406](https://github.com/minu3406) | `feature/san-bong` | Sân, khung giờ, giá, dịch vụ, xem/lọc sân, đánh giá |
| 3 | [@Quan-Duong-Minh](https://github.com/Quan-Duong-Minh) | `feature/dat-san` | Đặt sân, lịch đặt, đổi lịch |
| 4 | [@Xangz](https://github.com/Xangz) | `feature/thanh-toan` | Thanh toán, hóa đơn, mã giảm giá, thống kê |

Mỗi người làm trọn cả phần Admin và Khách của mảng mình (controller + API + view).

## 2. Chi tiết công việc

### Người 1 - @ThanhPhan8206 (`feature/auth-core`)
- [ ] Trang chủ công khai
- [ ] Đăng nhập + session/cookie
- [ ] Đăng ký
- [ ] Quên mật khẩu + dịch vụ gửi mail dùng chung
- [ ] Phân quyền Admin/Khách + 3 layout (công khai, Admin, Khách)
- [ ] Hồ sơ cá nhân: sửa sđt, email, upload ảnh đại diện
- [ ] Admin quản lý khách hàng: xem thông tin, tài khoản, tìm/lọc
- [ ] Thông báo (kèm hàm dùng chung `TaoThongBao`)

### Người 2 - @minu3406 (`feature/san-bong`)
- [ ] Admin CRUD sân + upload ảnh
- [ ] Bảo trì sân
- [ ] Quản lý khung giờ
- [ ] Thiết lập giá theo ngày/giờ
- [ ] Quản lý dịch vụ (nước, áo,...)
- [ ] Khách xem danh sách sân, tìm/lọc (5, 7, 11, giá, thời gian) + phân trang
- [ ] Chi tiết sân + hiển thị đánh giá
- [ ] Admin xem feedback/đánh giá
- [ ] Viết `Database/seed.sql` (dữ liệu mẫu) sớm cho cả nhóm test

### Người 3 - @Quan-Duong-Minh (`feature/dat-san`)
- [ ] Khách đặt sân + chọn dịch vụ + tính tiền
- [ ] Chống trùng lịch + kiểm tra sân đang bảo trì
- [ ] Xác nhận / hủy / cập nhật đơn
- [ ] Khách yêu cầu đổi lịch
- [ ] Lịch sử đặt sân
- [ ] Admin danh sách lịch đặt + tìm/lọc
- [ ] Admin duyệt đổi lịch + cập nhật trạng thái

### Người 4 - @Xangz (`feature/thanh-toan`)
- [ ] Thanh toán: đặt cọc, QR, tiền mặt
- [ ] Mã giảm giá: Admin quản lý + áp mã khi thanh toán
- [ ] Tạo + in hóa đơn
- [ ] Admin theo dõi thanh toán, lịch sử, chi tiết giao dịch
- [ ] Doanh thu theo ngày + thống kê báo cáo
- [ ] Khách gửi đánh giá (chỉ đơn "Hoàn thành")

## 3. Hướng dẫn từng bước: lấy code, tạo nhánh, code, đẩy lên

> Làm lần lượt từng bước. Mỗi lệnh gõ xong nhấn Enter. Cần cài sẵn **Git** và **Visual Studio**.

### Bước 1: Nhận lời mời vào repo (chỉ làm 1 lần)
Vào email hoặc thông báo GitHub, bấm **Accept invitation** để được quyền push vào repo.

### Bước 2: Tải code về máy (chỉ làm 1 lần)
1. Mở thư mục muốn để dự án (ví dụ `D:\BTL`).
2. Bấm vào thanh địa chỉ của File Explorer, gõ `cmd`, nhấn Enter.
3. Gõ:
```
git clone https://github.com/ThanhPhan8206/BTL_LTW.git
```
4. Vào thư mục vừa tải:
```
cd BTL_LTW
```
5. Mở file `QLSanBong.slnx` bằng Visual Studio.

Nếu lần đầu dùng git mà bị báo chưa biết bạn là ai, chạy 2 lệnh (thay bằng tên và email GitHub của bạn):
```
git config --global user.name "Ten cua ban"
git config --global user.email "email@cua.ban"
```

### Bước 3: Tạo nhánh riêng của mình (chỉ làm 1 lần)
Dùng đúng tên nhánh ở bảng mục 1. Ví dụ Người 2:
```
git checkout -b feature/san-bong
```
Kiểm tra đang ở đúng nhánh:
```
git branch
```
Nhánh nào có dấu `*` là nhánh đang đứng. **Luôn code trên nhánh của mình, không code trên `main`.**

### Bước 4: Code phần việc của mình
Mở Visual Studio và code các chức năng ở mục 2. Chỉ thêm Controller, View, Model của phần mình, tránh sửa file của người khác.

### Bước 5: Lưu lại và đẩy lên GitHub (làm mỗi khi xong một phần)
1. Xem đã sửa những gì:
```
git status
```
2. Thêm tất cả thay đổi:
```
git add .
```
3. Lưu lại kèm lời nhắn ngắn:
```
git commit -m "Them CRUD san bong"
```
4. Đẩy nhánh lên GitHub. **Lần đầu** của nhánh đó phải gõ:
```
git push -u origin feature/san-bong
```
Các lần sau chỉ cần:
```
git push
```
(Nhớ thay `feature/san-bong` bằng tên nhánh của mình.)

### Bước 6: Tạo Pull Request để đưa code vào `main`
1. Vào trang repo trên GitHub, sẽ thấy dòng vàng **Compare & pull request**, bấm vào.
2. Viết tiêu đề ngắn (ví dụ "Xong CRUD sân bóng"), bấm **Create pull request**.
3. Nhờ một bạn khác trong nhóm xem qua, rồi bấm **Merge pull request**.

### Bước 7: Cập nhật code mới của cả nhóm (làm đầu mỗi buổi code)
Để lấy phần mà các bạn khác đã merge vào `main`:
```
git checkout main
git pull
git checkout feature/san-bong
git merge main
```
(Thay `feature/san-bong` bằng nhánh của mình.) Rồi mở lại Visual Studio và code tiếp.

### Nếu gặp lỗi thường gặp
- **Báo `CONFLICT` khi merge:** hai người sửa cùng một chỗ. Mở file bị báo lỗi, tìm các dòng `<<<<<<<`, `=======`, `>>>>>>>`, giữ lại đoạn đúng và xóa các dòng ký hiệu đó, rồi chạy `git add .` và `git commit -m "Giai quyet xung dot"`. Không chắc thì nhắn vào nhóm hỏi.
- **Push bị từ chối (permission denied):** chưa accept lời mời ở Bước 1, hoặc đăng nhập nhầm tài khoản GitHub.
- **Lỡ code trên `main`:** đừng push. Chạy `git checkout -b feature/ten-nhanh-cua-minh` ngay lúc đó, các thay đổi sẽ đi theo sang nhánh mới.

## 4. Quy tắc làm việc với Git
- **Không code trực tiếp trên `main`.** Mỗi người làm trên nhánh riêng ở bảng mục 1.
- Làm xong một chức năng thì tạo **Pull Request**, nhờ một bạn khác xem rồi mới merge.
- Commit message ngắn, rõ, ví dụ: `Them CRUD san bong`, `Sua loi chong trung lich`.
- **File chỉ Người 1 được sửa:** `Program.cs`, `AppDbContext`, các file `_Layout`, `appsettings.json`. Ai cần thêm gì ở các file này thì nhắn Người 1.
- Không commit thay đổi chuỗi kết nối trong `appsettings.json` của máy mình.

## 5. Quy ước chung
- Trạng thái đơn đặt (`DatSan.TrangThai`): `Chờ duyệt`, `Đã xác nhận`, `Hoàn thành`, `Đã hủy`.
- Mỗi đơn chỉ có tối đa 1 đánh giá, và chỉ đánh giá được khi đơn ở trạng thái `Hoàn thành`.
- Khi Admin duyệt/từ chối đơn hoặc đổi lịch, gọi hàm `TaoThongBao(...)` để báo cho khách.
- Yêu cầu chung của đề: phân trang, lọc, AJAX, validation, API RESTful, upload ảnh, responsive.

## 6. Cài đặt máy local
1. Chạy file SQL trong thư mục `Database/` (hoặc `Docs/`) trên SQL Server để tạo DB.
2. Sửa chuỗi kết nối trong `appsettings.json` cho khớp máy mình (**đừng commit** thay đổi này).
3. Mở `QLSanBong.slnx` bằng Visual Studio và chạy.
