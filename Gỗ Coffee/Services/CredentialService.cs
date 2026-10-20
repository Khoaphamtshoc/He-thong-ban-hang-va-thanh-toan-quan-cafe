using System.Security.Cryptography;
using CuaHangTienLoi.Web.Models;
using Npgsql;

namespace CuaHangTienLoi.Web.Services;

/// <summary>Verifies a PostgreSQL user; upgrades old local-demo cleartext passwords on successful login.</summary>
public sealed class CredentialService(IConfiguration configuration)
{
    public async Task<AuthUser?> ValidateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var connectionString = configuration.GetConnectionString("StoreDb")
            ?? throw new InvalidOperationException("Thiếu ConnectionStrings:StoreDb.");
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT e.employee_id, e.full_name, u.username, r.role_name, u.password_hash, u.user_id
            FROM user_accounts u
            JOIN employees e ON e.employee_id = u.employee_id
            JOIN roles r ON r.role_id = e.role_id AND r.is_active
            WHERE LOWER(u.username) = LOWER(@username) AND u.is_active AND e.status = 'ACTIVE'
            LIMIT 1
            """, connection);
        command.Parameters.AddWithValue("username", username);
        long employeeId, accountId;
        string fullName, email, role, stored;
        await using (var reader = await command.ExecuteReaderAsync(cancellationToken))
        {
            if (!await reader.ReadAsync(cancellationToken)) return null;
            employeeId = reader.GetInt64(0);
            fullName = reader.GetString(1);
            email = reader.GetString(2);
            role = reader.GetString(3);
            stored = reader.GetString(4);
            accountId = reader.GetInt64(5);
        }
        if (!PasswordHasher.Verify(password, stored)) return null;
        if (!stored.StartsWith("PBKDF2-SHA256$", StringComparison.Ordinal))
        {
            await using var upgrade = new NpgsqlCommand("UPDATE user_accounts SET password_hash=@hash WHERE user_id=@id AND password_hash=@old", connection);
            upgrade.Parameters.AddWithValue("hash", PasswordHasher.Hash(password));
            upgrade.Parameters.AddWithValue("id", accountId);
            upgrade.Parameters.AddWithValue("old", stored);
            await upgrade.ExecuteNonQueryAsync(cancellationToken);
        }
        await using var seen = new NpgsqlCommand("UPDATE user_accounts SET last_login_at=NOW() WHERE user_id=@id", connection);
        seen.Parameters.AddWithValue("id", accountId);
        await seen.ExecuteNonQueryAsync(cancellationToken);
        return new AuthUser(checked((int)employeeId), fullName, email, role);
    }
}

public static class PasswordHasher
{
    private const int Iterations = 210_000;
    public static string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        var digest = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, 32);
        return $"PBKDF2-SHA256${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(digest)}";
    }

    public static bool Verify(string password, string stored)
    {
        if (!stored.StartsWith("PBKDF2-SHA256$", StringComparison.Ordinal))
        {
            // Support ONE migration of the original local-demo schema only.
            var supplied = System.Text.Encoding.UTF8.GetBytes(password);
            var expected = System.Text.Encoding.UTF8.GetBytes(stored);
            return supplied.Length == expected.Length && CryptographicOperations.FixedTimeEquals(supplied, expected);
        }
        var parts = stored.Split('$');
        if (parts.Length != 4 || !int.TryParse(parts[1], out var iterations) || iterations < 100_000 || iterations > 2_000_000)
            return false;
        try
        {
            var salt = Convert.FromBase64String(parts[2]);
            var digest = Convert.FromBase64String(parts[3]);
            if (salt.Length < 16 || digest.Length != 32) return false;
            var test = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, digest.Length);
            return CryptographicOperations.FixedTimeEquals(test, digest);
        }
        catch (FormatException) { return false; }
    }
}
