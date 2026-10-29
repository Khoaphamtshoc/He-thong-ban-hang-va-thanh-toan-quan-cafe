# Checklist thử trên máy Windows — VS Code + PostgreSQL 18

Đây là checklist **cần chạy**, không phải kết quả đã thực thi trong sandbox.

1. `dotnet --version` trả về `10.x`; `dotnet restore`, `dotnet build` không lỗi.
2. PostgreSQL (`postgresql-x64-18`) ở trạng thái Running, `Test-NetConnection localhost -Port 5432` = `True`.
3. Import `database/cafe_pos_postgresql_18.sql` vào **database cafe_pos** bằng pgAdmin 4, UTF-8. Tồn tại các bảng `roles`, `employees`, `user_accounts`, `products`, `orders`, `order_items`, `payments`, `ingredients`, `inventory_transactions`.
4. Mở `/dashboard` khi chưa đăng nhập: không thấy dữ liệu quản lý. Mở `/login`; nhập mật khẩu sai: từ chối đăng nhập. Nhập email lạ: từ chối đăng nhập.
5. Đăng nhập `admin@moc.coffee` bằng mật khẩu demo trong README: xem được mọi menu.
6. Đăng nhập `manager@moc.coffee`: có Dashboard, báo cáo, thực đơn, bán hàng, kho; không truy cập được `/employees`, kể cả nhập trực tiếp URL.
7. Đăng nhập `staff@moc.coffee`: thấy Bán hàng và Đơn hàng; thử `/reports`, `/products`, `/inventory`: không đủ quyền.
8. Đăng nhập `kho@moc.coffee`: chỉ được xem/nhập kho; không được truy cập dashboard và các trang khác.
9. Admin thêm một sản phẩm, sửa giá, tải lại trình duyệt: dữ liệu không mất. Ngừng bán: món biến mất khỏi menu bán hàng nhưng vẫn ở màn quản lý.
10. Thủ kho nhập nguyên liệu với số lượng hợp lệ; kiểm tra `ingredients.current_stock` tăng và `inventory_transactions` có dòng `IMPORT`.
11. Thu ngân thêm món, thanh toán tiền mặt; kiểm tra PostgreSQL đồng thời có `orders`, `order_items`, `payments`. Tải lại trang xem đơn hàng vẫn còn.
12. Đổi trạng thái đơn, tải lại trang xác nhận trạng thái không mất. Không cho hủy đơn đã trả tiền nếu chưa hoàn tiền.
13. Dashboard và Reports cập nhật số liệu theo đơn thật; xuất CSV bằng nút Xuất CSV.
14. Admin khóa tài khoản nhân viên, tài khoản bị từ chối sau đăng nhập lại; không thể tự khóa tài khoản đang dùng.
15. Đóng tab/tải lại trang vẫn còn cookie đăng nhập, bấm Đăng xuất thì trang quản trị không còn truy cập được.
16. Admin/Quản lý vào Thực đơn > Công thức, lưu định lượng rồi mở lại để xác nhận; nhân viên không được sửa công thức. Với dữ liệu demo, chạy thêm `database/demo_recipes.sql` sau schema.
17. Bán một món có công thức: kiểm tra tồn giảm đúng và có phiếu `EXPORT` ghi mã đơn. Thử thiếu nguyên liệu: không phát sinh đơn/thanh toán/phiếu xuất dở dang. Đổi trạng thái đơn không trừ kho lần nữa.
18. Trang Bán hàng có một ô Bàn; Ctrl+K chuyển tới tìm món, F4 mở xác nhận khi giỏ có món, Escape đóng, F4 lần hai không tự thanh toán.
19. Trang Đơn hàng: bộ đếm Đang pha chế/Hoàn tất/Đã hủy khớp trạng thái các hàng trong danh sách. Kiểm tra tranh tồn đồng thời và lỗi thanh toán bằng chương trình trong `tests/GoCoffee.IntegrationChecks` trên database thử riêng; xem `SUA_LOI_BAN_HANG_VA_KHO.md`.

Nếu build lỗi, hãy sao chép **lỗi đầu tiên đầy đủ** từ Terminal; không chỉ gửi ảnh trang web trắng. Cần SDK .NET 10 để xác minh kết quả thực tế.
