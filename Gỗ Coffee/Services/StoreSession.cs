using CuaHangTienLoi.Web.Models;

namespace CuaHangTienLoi.Web.Services;

public sealed class StoreSession
{
    public AuthUser? CurrentUser { get; private set; }

    public bool IsAuthenticated => CurrentUser is not null;

    public string DisplayName => CurrentUser?.TenNhanVien ?? "Khách";

    public string RoleName => CurrentUser?.VaiTro ?? "Chưa đăng nhập";

    public void SetUser(AuthUser user) => CurrentUser = user;

    public void Clear() => CurrentUser = null;
}
