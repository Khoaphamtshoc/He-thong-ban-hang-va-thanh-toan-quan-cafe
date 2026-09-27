# Gỗ Coffee — Hệ thống bán hàng và thanh toán

Ứng dụng ASP.NET Core Blazor Server (.NET 10), kết nối PostgreSQL 18 bằng Npgsql. Luồng đăng nhập, lấy thực đơn, tạo đơn/thanh toán và quản lý trạng thái đơn sử dụng PostgreSQL.

## Cần cài

- PostgreSQL 18 (pgAdmin 4 đi kèm tùy lựa chọn khi cài).
- .NET 10 SDK.

## 1. Tạo cơ sở dữ liệu

Trong pgAdmin, kết nối PostgreSQL, tạo database tên `cafe_pos`, mở Query Tool của database đó, mở file `database/cafe_pos_postgresql_18.sql`, bỏ dòng `CREATE DATABASE cafe_pos;` ở đầu file nếu đã tạo database bằng pgAdmin, rồi chạy phần SQL còn lại bằng nút Execute/F5.

Kiểm tra trong `Schemas > public > Tables` có các bảng `categories`, `products`, `user_accounts`, `orders`, `order_items`, `payments` và các bảng còn lại.

## 2. Đặt mật khẩu PostgreSQL

Mở PowerShell ở thư mục dự án đã giải nén. Thay `MAT_KHAU_POSTGRES` bằng mật khẩu đã đặt cho user PostgreSQL `postgres`:

```powershell
dotnet user-secrets set "ConnectionStrings:StoreDb" "Host=localhost;Port=5432;Database=cafe_pos;Username=postgres;Password=MAT_KHAU_POSTGRES" --project "Gỗ Coffee/CuaHangTienLoi.Web.csproj"
```

Mật khẩu được lưu bằng .NET User Secrets, không cần ghi vào file source.

## 3. Chạy thử

```powershell
dotnet run --project "Gỗ Coffee/CuaHangTienLoi.Web.csproj"
```

Mở `http://localhost:5237` (hoặc địa chỉ hiện trong terminal). Tài khoản đăng nhập mẫu theo SQL:

- Email/tên đăng nhập: `admin@moc.coffee`
- Mật khẩu: `Admin@123456`

SQL mẫu hiện lưu mật khẩu plaintext để tương thích tài khoản demo. Chỉ dùng dữ liệu demo trên máy phát triển; đổi cách lưu mật khẩu trước khi triển khai thật.

## Chức năng đang nối PostgreSQL

- Đăng nhập: `user_accounts`, `employees`, `roles`.
- Bán hàng: danh sách đang bán từ `products`, `categories`.
- Thanh toán: tạo `orders`, `order_items`, `payments` trong một transaction.
- Đơn hàng: đọc danh sách và cập nhật trạng thái trong `orders`.

Dashboard, báo cáo, kho, nhân viên và trang quản lý thực đơn vẫn có phần dữ liệu mẫu/chưa được nối hoàn chỉnh. File SQL được cung cấp riêng tại `database/cafe_pos_postgresql_18.sql`.

## Đẩy lên GitHub

Giải nén ZIP, mở thư mục trong VS Code, kiểm tra thay đổi rồi tạo repository trên GitHub. Có thể dùng GitHub Desktop: **Add an existing repository** nếu thư mục đã có Git; hoặc tạo repository mới và commit/push theo hướng dẫn GitHub. Không tải lên mật khẩu database, thư mục `bin/`, `obj/`, `node_modules/` hoặc file `backend/.env`.
