# BẢN GIAO DIỆN MỚI · GỖ COFFEE

Phiên bản này có giao diện hoa anh đào đơn giản (Dashboard + Đăng nhập + đồng bộ UI).
Đọc **`GIAO_DIEN_MOI_VSCODE.md`** để biết các file đã đổi, cách chạy VS Code và cách giải thích với giảng viên.
Không cần cài thư viện frontend mới.

---

# Hệ thống bán hàng và thanh toán cho quán cà phê — Gỗ Coffee

## Mở và chạy bằng Visual Studio Code (Windows)

**Phần mềm chạy web chính:** ASP.NET Core **Blazor Server .NET 10** + PostgreSQL 18 (pgAdmin 4 để quản lý database). Đây là Blazor theo mô hình tách giao diện Razor, dịch vụ nghiệp vụ và lớp truy cập dữ liệu; **không phải ASP.NET MVC (Controller + Views)**. Folder `backend/` là dự án NestJS độc lập từ bản gốc, **không cần chạy để sử dụng web**. Không chạy đồng thời hai cơ chế đăng nhập cho web chính.

### Bước 1. Cài công cụ

- .NET 10 SDK: mở Terminal VS Code chạy `dotnet --version` (phải là `10.x`).
- PostgreSQL 18 đang hoạt động trên `localhost:5432`; pgAdmin 4 để quản lý.
- VS Code và extension **C# Dev Kit** của Microsoft.

### Bước 2. Khởi tạo database (chỉ lần đầu hoặc khi dữ liệu demo còn thiếu)

Trong pgAdmin 4, tạo database `cafe_pos` (Encoding `UTF8`). Mở **Query Tool** của database **cafe_pos**, nhấn Open file, chọn `database/cafe_pos_postgresql_18.sql`, rồi Execute/F5 **toàn bộ file**. Không tự chèn `CREATE DATABASE` vào đầu script. File SQL không xóa đơn/sản phẩm đã có, tài khoản seed được thêm nếu chưa tồn tại.

Nếu PostgreSQL đang dùng database có sẵn từ bản cũ, **sao lưu trước khi chạy lại SQL**. SQL sửa các hàng dữ liệu demo bị thiếu; mật khẩu plaintext cũ sẽ tự nâng sang PBKDF2 sau một lần đăng nhập thành công.

### Bước 3. Lưu mật khẩu database an toàn

Mở VS Code vào **thư mục gốc đã giải nén** và mở Terminal PowerShell, chạy:

```powershell
dotnet user-secrets set "ConnectionStrings:StoreDb" "Host=localhost;Port=5432;Database=cafe_pos;Username=postgres;Password=MAT_KHAU_POSTGRES" --project "Gỗ Coffee/CuaHangTienLoi.Web.csproj"
```

Thay `MAT_KHAU_POSTGRES` bằng mật khẩu PostgreSQL thực của máy. **Không commit mật khẩu vào GitHub hoặc `.env`.**

### Bước 4. Build và chạy web

```powershell
dotnet restore "Gỗ Coffee/CuaHangTienLoi.Web.csproj"
dotnet build "Gỗ Coffee/CuaHangTienLoi.Web.csproj"
dotnet run --project "Gỗ Coffee/CuaHangTienLoi.Web.csproj" --launch-profile http
```

Mở http://localhost:5237/login trong trình duyệt. Có thể chạy VS Code Task **Terminal > Run Task > Go Coffee: run** và Build Task **Ctrl+Shift+B**. Dừng bằng `Ctrl+C`.

### Tài khoản mẫu cho database mới

| Vai trò | Email | Mật khẩu |
| --- | --- | --- |
| Admin | `admin@moc.coffee` | `Admin@123456` |
| Quản lý | `manager@moc.coffee` | `Manager@123456` |
| Nhân viên | `staff@moc.coffee` | `Staff@123456` |
| Thủ kho | `kho@moc.coffee` | `Warehouse@123456` |

**Chỉ dùng tài khoản này khi demo trên máy cá nhân** và đổi mật khẩu/tạo tài khoản mới trước khi đưa ứng dụng lên Internet. SQL mới lưu hash PBKDF2-SHA256, không lưu plaintext. Nếu database trước đây có tài khoản cũ, SQL không đặt lại mật khẩu của tài khoản cũ. Tài khoản không hoạt động bị từ chối đăng nhập.

### Phân quyền hoạt động

| Trang / chức năng | Admin | Quản lý | Nhân viên | Thủ kho |
| --- | :---: | :---: | :---: | :---: |
| Dashboard / Báo cáo | ✓ | ✓ | | |
| Quản lý thực đơn | ✓ | ✓ | | |
| Đơn hàng / Bán hàng | ✓ | ✓ | ✓ | |
| Xem kho / Nhập kho | ✓ | ✓ | | ✓ |
| Nhân viên / Vai trò | ✓ | | | |

Các trang có `[Authorize(Roles=...)]`. Server dùng ASP.NET Core Cookie Authentication, kiểm tra lại trạng thái và quyền hiện tại từ PostgreSQL khi gọi dịch vụ dữ liệu. Không có mật khẩu dự phòng, không có tài khoản ảo được tự động đăng nhập.

### Đã kết nối dữ liệu thật

- Dashboard: đơn hàng, doanh thu trong ngày, món bán chạy, tồn kho thấp.
- Báo cáo: thống kê từ PostgreSQL theo 7/30/90 ngày, xuất CSV.
- Sản phẩm: xem (bao gồm món đã ngừng), thêm, sửa, ngừng bán (không xóa lịch sử đơn).
- Kho: xem nguyên liệu, nhập tồn kho và ghi `inventory_transactions` trong transaction.
- Nhân viên: tạo tài khoản có mật khẩu tạm, sửa thông tin/vai trò, khóa/mở tài khoản (không xóa lịch sử).
- Bán hàng: món/giá đọc từ DB, tạo đơn + thanh toán trong transaction (thu ngân ghi nhận thanh toán thủ công, **không tích hợp cổng thanh toán**).
- Đơn hàng: đọc DB, cập nhật trạng thái. **Đơn đã thanh toán không được tự hủy** vì chưa xây quy trình hoàn tiền.

### Một số tình huống lỗi thường gặp

- **`dotnet` is not recognized**: cài .NET 10 SDK; mở lại VS Code và Terminal.
- **Failed to connect 127.0.0.1:5432**: kiểm tra dịch vụ PostgreSQL đang chạy, `Test-NetConnection localhost -Port 5432`, cấu hình `listen_addresses` và mật khẩu.
- **Invalid password / Cannot connect**: kiểm tra `dotnet user-secrets list --project "Gỗ Coffee/CuaHangTienLoi.Web.csproj"` (không chia sẻ output vì có mật khẩu), database `cafe_pos` có bảng và tài khoản demo.
- **Trang trống hoặc không đủ quyền**: đăng nhập lại, kiểm tra role trong PostgreSQL, xem Terminal lỗi. Role đã thay đổi khi đang đăng nhập có thể cần tải lại trang.
- **Dữ liệu demo cũ vẫn xuất hiện**: chạy phiên bản dự án đã sửa, không mở bản ZIP cũ hoặc nhầm cổng ứng dụng khác.
- **SQL encoding**: lưu/run script UTF-8 bằng Query Tool pgAdmin 4. Đừng import bằng console có mã hóa Windows 1252.

### Ranh giới kiểm thử

Các chỉnh sửa đã được kiểm tra cấu trúc/tĩnh trong môi trường tạo bản ZIP. Môi trường này **không có dotnet SDK và dịch vụ PostgreSQL đang chạy**, nên không thể xác nhận `dotnet build`, đăng nhập cookie hay chạy end-to-end. Cần chạy 3 lệnh Bước 4 và test các vai trò trên máy Windows thực tế. Không nên xem đây là xác nhận hệ thống đã hoàn hảo trong mọi môi trường.
