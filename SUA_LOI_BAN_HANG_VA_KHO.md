# Sửa đơn hàng, bán hàng và trừ nguyên liệu

## Hành vi đã thay đổi

- Các thẻ trạng thái dùng cùng phép chuyển mã trạng thái với bộ lọc, nên đơn `COMPLETED` được đếm ở Hoàn tất, `PREPARING` ở Đang pha chế, `CANCELLED` ở Đã hủy.
- Trang Bán hàng chỉ còn một ô Bàn / mã đơn. Bàn và ghi chú cập nhật ngay khi nhập, không phải chờ rời ô.
- F4 mở xác nhận thanh toán; Ctrl+K hoặc Cmd+K đưa con trỏ đến tìm món; Escape đóng xác nhận/menu. F4 khi giỏ trống không làm gì, F4 khi hộp xác nhận đã mở không tạo thêm hộp hoặc tự xác nhận thanh toán.
- Tên trang Đơn hàng và chữ trong minh họa đăng nhập thống nhất Gỗ Coffee. Email tài khoản demo được giữ nguyên để các tài khoản cũ vẫn sử dụng được.
- Bỏ ô Khách hàng/Khách lẻ trong giỏ; thay nội dung phụ bằng số món đang chọn. Menu bên trái phân bố theo chiều cao, chữ nội dung lớn hơn và bảng cuộn ngang khi màn hình hẹp.
- Ảnh được lấy từ `products.image_url` và hiển thị tại Bán hàng, giỏ, Thực đơn và món bán chạy trên Dashboard. 12 món demo có file ảnh Internet lưu cục bộ, kèm nguồn trong `wwwroot/images/products/SOURCES.md`. Database đã tạo trước đây chạy thêm `database/demo_product_images.sql`; script không ghi đè ảnh người dùng đã thiết lập. Không đưa ảnh tạo bằng AI vào project.

## Công thức và tồn kho

Admin/Quản lý mở **Thực đơn > Công thức** ở từng món. Định lượng là lượng tiêu hao cho **một phần**, theo đúng đơn vị của kho; ví dụ 18 g ghi `0.018` nếu kho tính bằng kg. Mỗi nguyên liệu xuất hiện một lần, lượng dương, tối đa 3 chữ số thập phân. Công thức không được rỗng. Nhân viên bán hàng và thủ kho không có quyền sửa công thức.

Khi thanh toán, server lấy giá và công thức hiện tại từ database, khóa các món theo thứ tự mã ID, gộp lượng nguyên liệu của cả giỏ và khóa các hàng tồn kho theo thứ tự ID. Không đủ tồn, món thiếu công thức hoặc nguyên liệu ngừng dùng sẽ bị từ chối với thông báo cụ thể.

Đơn, chi tiết món, khoản thanh toán, giảm tồn và dòng `EXPORT` trong `inventory_transactions` nằm trong cùng transaction. Phiếu xuất ghi mã đơn và nhân viên thu ngân. Nếu có lỗi, toàn bộ transaction được hoàn tác. Đổi đơn sang hoàn tất/đang pha chế không trừ kho lần nữa. Các đơn lịch sử không bị trừ kho hồi tố. Quy tắc cấm hủy đơn đã thanh toán vẫn áp dụng vì chưa có hoàn tiền.

Số lượng phần bán phải là số nguyên; hỗ trợ tối đa 100 phần/món trong một đơn, kể cả khi món xuất hiện nhiều dòng.

## Công thức minh họa

Chạy `database/demo_recipes.sql` sau script khởi tạo schema trong cùng database demo. Script bổ sung 8 nguyên liệu có tên `(demo)` và 26 định lượng cho 12 món hiện có. Khi chạy lại, giữ nguyên tồn kho và toàn bộ công thức đã thiết lập; không tự bổ sung các dòng thiếu vào một công thức đã được quản lý chỉnh sửa.

Đây là định lượng minh họa do người dùng yêu cầu để kiểm thử; không phải công thức vận hành thật của quán. Cần thay định lượng theo thực tế trước khi sử dụng kinh doanh. Ví dụ Espresso tiêu hao 0.018 kg hạt; bánh tiêu hao 1 đơn vị bánh thành phẩm để tránh áp công thức pha chế cho bánh.

## Kiểm tra đã thực hiện trên Windows

- Build .NET 10: 0 lỗi, 0 cảnh báo.
- 39/39 kiểm tra HTTP: đăng nhập, đăng xuất, chống giả mạo POST đăng nhập và ma trận quyền 4 vai trò/7 route.
- 14/14 kiểm tra tích hợp PostgreSQL trong `tests/GoCoffee.IntegrationChecks`.
- Trình duyệt: sửa định lượng rồi mở lại xác nhận đã lưu; chỉ một ô Bàn; Ctrl+K đúng focus; F4 giỏ trống không mở; F4 mở một hộp; F4 lần hai không xác nhận; Escape đóng; thanh toán thành công trừ 0.018 kg hạt và ghi một phiếu xuất.
- Đối chiếu hai đơn demo: thẻ tổng 2, đang pha chế 1, hoàn tất 1.

Các kiểm tra tích hợp bao gồm nguyên liệu dùng chung nhiều món/dòng trùng, thiếu kho, thiếu công thức, nguyên liệu ngừng dùng, hoàn tác khi lỗi thanh toán, hai thu ngân tranh phần tồn cuối, thứ tự món ngược nhau, sửa/kiểm tra công thức, quyền nhân viên và đổi trạng thái không trừ lại.

Để chạy kiểm tra kho, tạo một database riêng có tên bắt đầu bằng `cafe_pos_test_`, import `database/cafe_pos_postgresql_18.sql` (có thể thêm `demo_recipes.sql`). Đặt biến môi trường `GOCOFFEE_TEST_CONNECTION` chứa connection string tới **database thử**, sau đó:

```powershell
dotnet run --project "tests/GoCoffee.IntegrationChecks/GoCoffee.IntegrationChecks.csproj"
```

Chương trình từ chối database không có tiền tố trên, tạo dữ liệu kiểm thử trong database riêng và trả exit code khác 0 khi có kiểm tra thất bại. Không in connection string ra log. Database thử cần quyền tạo trigger/function để mô phỏng thanh toán lỗi; không dùng database thật.

## File chính

- `Gỗ Coffee/Services/StoreDb.cs`: giao dịch kho và dịch vụ công thức.
- `Gỗ Coffee/Components/Pages/Products.razor`: trình chỉnh công thức và số nguyên liệu.
- `Gỗ Coffee/Components/Pages/Sales.razor`: ô bàn, chỉ báo công thức, hộp thanh toán và điểm gắn phím tắt.
- `Gỗ Coffee/Components/Pages/Orders.razor`: bộ đếm và tiêu đề.
- `Gỗ Coffee/wwwroot/ui.js`: phím tắt theo trang và đóng menu.
- `Gỗ Coffee/wwwroot/soft-sakura.css`: bố cục trình chỉnh công thức.
- `Gỗ Coffee/wwwroot/app.css`: tên thương hiệu trong minh họa đăng nhập.
- `database/demo_recipes.sql`: công thức minh họa tùy chọn.
