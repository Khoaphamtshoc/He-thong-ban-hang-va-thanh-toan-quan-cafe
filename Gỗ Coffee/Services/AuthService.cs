using CuaHangTienLoi.Web.Models;

namespace CuaHangTienLoi.Web.Services;

public sealed class AuthService(StoreDb db, StoreSession session)
{
    public async Task<bool> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await db.LoginAsync(username, password, cancellationToken);
        if (user is null)
        {
            return false;
        }

        session.SetUser(user);
        return true;
    }
}
