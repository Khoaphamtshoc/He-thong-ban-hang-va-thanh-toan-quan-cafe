using System.Data;
using Microsoft.Data.SqlClient;
using CuaHangTienLoi.Web.Models;

namespace CuaHangTienLoi.Web.Services;

public sealed class StoreDb(IConfiguration configuration)
{
    private string ConnectionString => configuration.GetConnectionString("StoreDb")
        ?? throw new InvalidOperationException("Missing StoreDb connection string.");

    public async Task<AuthUser?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (1) nv.MaNhanVien, nv.TenNhanVien, nv.TenDangNhap, vt.TenVaiTro
FROM NHAN_VIEN nv
LEFT JOIN VAI_TRO vt ON nv.MaVaiTro = vt.MaVaiTro
WHERE nv.TenDangNhap = @username AND nv.MatKhau = @password;";
        command.Parameters.AddWithValue("@username", username);
        command.Parameters.AddWithValue("@password", password);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new AuthUser(
            reader.GetInt32(0),
            reader.GetString(1),
            reader.GetString(2),
            reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    public async Task<int> CountAsync(string tableName, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM {tableName};";
        var result = await command.ExecuteScalarAsync(cancellationToken);
        return Convert.ToInt32(result, System.Globalization.CultureInfo.InvariantCulture);
    }

    public async Task<List<(string Label, int Value)>> TopProductsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandText = @"
SELECT TOP (5) sp.TenSanPham, ISNULL(SUM(ct.SoLuong), 0) AS Sold
FROM SAN_PHAM sp
LEFT JOIN CHI_TIET_HOA_DON ct ON ct.MaSanPham = sp.MaSanPham
GROUP BY sp.TenSanPham
ORDER BY Sold DESC, sp.TenSanPham;";

        var items = new List<(string, int)>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            items.Add((reader.GetString(0), reader.GetInt32(1)));
        }

        return items;
    }
}
