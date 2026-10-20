# Gỗ Coffee — giao diện hoa anh đào (bản đơn giản)

## Giao diện được thay đổi

- **Trang đăng nhập:** nền tím mận + hoa anh đào SVG; biểu mẫu giữ nguyên phương thức POST có xác thực.
- **Tổng quan:** vùng chào mừng mới, hai nút điều hướng tạo/xem đơn, thẻ số liệu và biểu đồ/bảng dễ đọc.
- **Các trang khác:** dùng cùng kiểu thẻ, bảng, nút, thanh tìm kiếm và thông báo.
- **Thiết bị nhỏ:** nút ☰ mở/đóng menu; nhấn vùng tối bên ngoài hoặc `Esc` để đóng.
- **Chuyển động:** CSS `transition` nhẹ cho nút và thẻ, vài cánh hoa chuyển động chậm. Hỗ trợ `prefers-reduced-motion`.

Không dùng Three.js, WebGL, video nền, API AI, thư viện giao diện mới hoặc mô hình 3D. Quyền theo vai trò và xử lý dữ liệu không bị chỉnh sửa bởi gói giao diện này.

## Các file chính

- `Gỗ Coffee/wwwroot/soft-sakura.css`: **toàn bộ CSS giao diện mới**, tách riêng để dễ sửa.
- `Gỗ Coffee/wwwroot/sakura-branch.svg`: minh họa hoa anh đào bằng SVG (ảnh vector).
- `Gỗ Coffee/wwwroot/ui.js`: chỉ quản lý thao tác đóng menu điện thoại.
- `Gỗ Coffee/Components/Pages/Dashboard.razor`: banner mới, vẫn lấy thống kê từ PostgreSQL.
- `Gỗ Coffee/Components/Pages/Login.razor`: nội dung và màu sắc đăng nhập.
- `Gỗ Coffee/Components/Layout/MainLayout.razor`: thanh menu và nút mở menu điện thoại.
- `Gỗ Coffee/Components/App.razor`: nạp CSS và JS mới.

## Chạy trong VS Code (Windows)

1. Giải nén thư mục, mở **cả thư mục dự án** trong VS Code (không chỉ một file).
2. Cài .NET 10 SDK, C# Dev Kit và PostgreSQL. Khởi động dịch vụ PostgreSQL và bảo đảm database `cafe_pos` đã được tạo; xem `README.md` cho phần khởi tạo DB.
3. Nếu chưa cấu hình chuỗi kết nối, mở PowerShell trong thư mục gốc và chạy (thay mật khẩu):

   ```powershell
   dotnet user-secrets set "ConnectionStrings:StoreDb" "Host=localhost;Port=5432;Database=cafe_pos;Username=postgres;Password=MAT_KHAU" --project "Gỗ Coffee/CuaHangTienLoi.Web.csproj"
   ```
4. Khởi chạy:

   ```powershell
   dotnet restore "Gỗ Coffee/CuaHangTienLoi.Web.csproj"
   dotnet build "Gỗ Coffee/CuaHangTienLoi.Web.csproj"
   dotnet run --project "Gỗ Coffee/CuaHangTienLoi.Web.csproj" --launch-profile http
   ```

5. Mở địa chỉ mà terminal hiển thị (thường là `http://localhost:5237/login`). Nhấn `Ctrl+F5` ở trình duyệt để tải stylesheet mới nếu trình duyệt vẫn dùng CSS cũ.

## Nếu thầy hỏi “giao diện chuyển động làm thế nào?”

> Em sử dụng CSS `transition` để nút và các ô thống kê chuyển nhẹ khi di chuột. Hoa anh đào là hình SVG nên không phải mô hình 3D. Chỉ có một vài cánh hoa dùng CSS `@keyframes`. Em giữ nguyên Blazor phía giao diện, tầng dịch vụ C# kết nối PostgreSQL và phân quyền theo Admin/Manager/Staff/Warehouse.

## Checklist kiểm tra trước khi nộp

- Đăng nhập bằng tài khoản hợp lệ, sai mật khẩu phải bị từ chối.
- Tài khoản nhân viên không mở được trang Admin/nhân viên; menu chỉ hiện trang có quyền.
- Dashboard hiển thị dữ liệu thật, làm mới không báo lỗi.
- Tạo đơn ở Bán hàng, kiểm tra đơn mới xuất hiện trong Đơn hàng và doanh thu.
- Thử cửa sổ rộng và hẹp (mô phỏng màn hình điện thoại), mở/đóng menu.
- Thử tải lại trang xem dữ liệu còn được lưu trong PostgreSQL.

**Tình trạng xác minh:** Kiểm tra được cú pháp CSS/JavaScript/SVG; môi trường tạo gói không có .NET 10 SDK và trình duyệt thử nghiệm bị chặn tải trang, nên **chưa thể khẳng định đã build và kiểm thử giao diện thực chạy**. Bạn cần làm checklist trên máy Windows của mình.
