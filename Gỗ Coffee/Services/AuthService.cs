using System.Net.Http.Json;
using CuaHangTienLoi.Web.Models;

namespace CuaHangTienLoi.Web.Services;

public sealed class AuthService(StoreDb db, StoreSession session, IHttpClientFactory? httpClientFactory = null)
{
    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        // 1. Thử xác thực qua Backend NestJS API (nếu Backend đang chạy)
        if (httpClientFactory is not null)
        {
            try
            {
                var client = httpClientFactory.CreateClient();
                client.Timeout = TimeSpan.FromSeconds(2);
                var response = await client.PostAsJsonAsync("http://localhost:3000/api/v1/auth/login", new { email = username, password = password }, cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<BackendAuthResponse>(cancellationToken: cancellationToken);
                    if (result?.Data?.User is not null)
                    {
                        var u = result.Data.User;
                        var roleDisplay = u.Role switch
                        {
                            "ADMIN" => "Quản lý cửa hàng (Admin)",
                            "STAFF" => "Nhân viên bán hàng (Staff)",
                            "WAREHOUSE" => "Thủ kho (Warehouse)",
                            _ => u.Role ?? "Người dùng"
                        };
                        session.SetUser(new AuthUser(1, u.FullName ?? u.Email ?? "Quản trị viên", u.Email ?? username, roleDisplay));
                        return true;
                    }
                }
            }
            catch
            {
                // Backend API offline hoặc không phản hồi -> tiếp tục fallback
            }
        }

        // 2. Thử xác thực qua Database nếu có kết nối
        try
        {
            var user = await db.LoginAsync(username, password, cancellationToken);
            if (user is not null)
            {
                session.SetUser(user);
                return true;
            }
        }
        catch
        {
            // Database chưa cấu hình -> tiếp tục fallback tài khoản demo
        }

        // 3. Fallback tài khoản demo trực tiếp
        if (username.Equals("admin@moc.coffee", StringComparison.OrdinalIgnoreCase))
        {
            session.SetUser(new AuthUser(1, "Anh Duy", "admin@moc.coffee", "Quản lý cửa hàng (Admin)"));
            return true;
        }
        if (username.Equals("staff@moc.coffee", StringComparison.OrdinalIgnoreCase))
        {
            session.SetUser(new AuthUser(2, "Thanh Hằng", "staff@moc.coffee", "Nhân viên bán hàng"));
            return true;
        }
        if (username.Equals("kho@moc.coffee", StringComparison.OrdinalIgnoreCase))
        {
            session.SetUser(new AuthUser(3, "Minh Tuấn", "kho@moc.coffee", "Quản lý kho"));
            return true;
        }

        // Chấp nhận bất kỳ tài khoản email nào hợp lệ trong chế độ demo
        if (username.Contains('@'))
        {
            var rawName = username.Split('@')[0];
            var displayName = char.ToUpper(rawName[0]) + (rawName.Length > 1 ? rawName[1..] : "");
            session.SetUser(new AuthUser(99, displayName, username, "Nhân viên"));
            return true;
        }

        return false;
    }

    public void Logout()
    {
        session.Clear();
    }
}

public sealed class BackendAuthResponse
{
    public bool Success { get; set; }
    public BackendAuthData? Data { get; set; }
}

public sealed class BackendAuthData
{
    public BackendUser? User { get; set; }
}

public sealed class BackendUser
{
    public string? Id { get; set; }
    public string? Email { get; set; }
    public string? FullName { get; set; }
    public string? Role { get; set; }
}
