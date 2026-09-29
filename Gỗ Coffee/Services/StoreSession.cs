using CuaHangTienLoi.Web.Models;

namespace CuaHangTienLoi.Web.Services;

public sealed class StoreSession
{
    public AuthUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public string DisplayName => CurrentUser?.TenNhanVien ?? "Khách";

    public string RoleName => CurrentUser?.VaiTro ?? "Chưa đăng nhập";

    public string Initials
    {
        get
        {
            if (string.IsNullOrWhiteSpace(DisplayName) || DisplayName == "Khách") return "AD";
            var parts = DisplayName.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2) return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[^1][0])}";
            return DisplayName[..Math.Min(2, DisplayName.Length)].ToUpper();
        }
    }

    public void SetUser(AuthUser user) => CurrentUser = user;

    public void Clear() => CurrentUser = null;
}
