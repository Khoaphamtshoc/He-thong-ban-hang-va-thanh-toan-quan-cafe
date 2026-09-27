using CuaHangTienLoi.Web.Models;
using Npgsql;

namespace CuaHangTienLoi.Web.Services;

public sealed record MenuProduct(long Id, string Code, string Name, string? Description, string Category, decimal Price, bool Active);
public sealed record CheckoutLine(long ProductId, decimal Quantity, string? Note);
public sealed record StoreOrder(long Id, string Code, string Customer, string Items, DateTimeOffset OrderedAt, decimal Total, string Payment, string Status);

public sealed class StoreDb(IConfiguration configuration)
{
    private string ConnectionString => configuration.GetConnectionString("StoreDb")
        ?? throw new InvalidOperationException("Missing StoreDb connection string.");

    private NpgsqlConnection CreateConnection() => new(ConnectionString);

    public async Task<AuthUser?> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT e.employee_id, e.full_name, u.username, r.role_name
            FROM user_accounts u
            JOIN employees e ON e.employee_id = u.employee_id
            LEFT JOIN roles r ON r.role_id = e.role_id
            WHERE u.username = @username AND u.password_hash = @password
              AND u.is_active AND e.status = 'ACTIVE'
            LIMIT 1;
            """, connection);
        command.Parameters.AddWithValue("username", username.Trim());
        command.Parameters.AddWithValue("password", password);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken)) return null;
        return new AuthUser(checked((int)reader.GetInt64(0)), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3));
    }

    public async Task<List<MenuProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT p.product_id, p.product_code, p.product_name, p.description,
                   c.category_name, p.sale_price, p.is_active
            FROM products p JOIN categories c ON c.category_id = p.category_id
            WHERE p.is_active
            ORDER BY c.category_name, p.product_name;
            """, connection);
        var products = new List<MenuProduct>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            products.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.IsDBNull(3) ? null : reader.GetString(3), reader.GetString(4), reader.GetDecimal(5), reader.GetBoolean(6)));
        return products;
    }

    public async Task<string> CreateOrderAsync(IReadOnlyCollection<CheckoutLine> lines, string paymentMethod,
        decimal vatRate, decimal discount, string? note, string? tableNo, CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0) throw new InvalidOperationException("Đơn hàng chưa có món.");
        var paymentCode = paymentMethod switch { "Tiền mặt" => "CASH", "Chuyển khoản" => "BANK_TRANSFER", "Thẻ" => "CARD", _ => throw new InvalidOperationException("Phương thức thanh toán không hợp lệ.") };
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        var productPrices = new Dictionary<long, decimal>();
        foreach (var line in lines)
        {
            await using var productCommand = new NpgsqlCommand("SELECT product_name, sale_price FROM products WHERE product_id=@id AND is_active FOR UPDATE", connection, transaction);
            productCommand.Parameters.AddWithValue("id", line.ProductId);
            await using var reader = await productCommand.ExecuteReaderAsync(cancellationToken);
            if (!await reader.ReadAsync(cancellationToken)) throw new InvalidOperationException("Một món trong đơn không còn được kinh doanh.");
            productPrices[line.ProductId] = reader.GetDecimal(1);
        }

        var subtotal = lines.Sum(line => productPrices[line.ProductId] * line.Quantity);
        var vat = decimal.Round(subtotal * vatRate / 100m, 2, MidpointRounding.AwayFromZero);
        var safeDiscount = Math.Clamp(discount, 0, subtotal + vat);
        var total = subtotal + vat - safeDiscount;
        var code = $"DH-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Guid.NewGuid():N}"[..24];

        await using (var orderCommand = new NpgsqlCommand("""
            INSERT INTO orders(order_code, table_no, note, order_status, subtotal, vat_rate, vat_amount, discount_amount, total_amount)
            VALUES(@code,@table,@note,'PREPARING',@subtotal,@vatRate,@vat,@discount,@total)
            RETURNING order_id;
            """, connection, transaction))
        {
            orderCommand.Parameters.AddWithValue("code", code);
            orderCommand.Parameters.AddWithValue("table", (object?)tableNo ?? DBNull.Value);
            orderCommand.Parameters.AddWithValue("note", (object?)note ?? DBNull.Value);
            orderCommand.Parameters.AddWithValue("subtotal", subtotal);
            orderCommand.Parameters.AddWithValue("vatRate", vatRate);
            orderCommand.Parameters.AddWithValue("vat", vat);
            orderCommand.Parameters.AddWithValue("discount", safeDiscount);
            orderCommand.Parameters.AddWithValue("total", total);
            var orderId = (long)(await orderCommand.ExecuteScalarAsync(cancellationToken) ?? throw new InvalidOperationException("Không tạo được đơn hàng."));
            foreach (var line in lines)
            {
                var price = productPrices[line.ProductId];
                await using var itemCommand = new NpgsqlCommand("""
                    INSERT INTO order_items(order_id, product_id, quantity, unit_price, note, line_total)
                    VALUES(@orderId,@productId,@quantity,@price,@note,@lineTotal)
                    """, connection, transaction);
                itemCommand.Parameters.AddWithValue("orderId", orderId);
                itemCommand.Parameters.AddWithValue("productId", line.ProductId);
                itemCommand.Parameters.AddWithValue("quantity", line.Quantity);
                itemCommand.Parameters.AddWithValue("price", price);
                itemCommand.Parameters.AddWithValue("note", (object?)line.Note ?? DBNull.Value);
                itemCommand.Parameters.AddWithValue("lineTotal", price * line.Quantity);
                await itemCommand.ExecuteNonQueryAsync(cancellationToken);
            }
            await using var paymentCommand = new NpgsqlCommand("""
                INSERT INTO payments(order_id,payment_method,amount,payment_status)
                VALUES(@orderId,@method,@amount,'PAID')
                """, connection, transaction);
            paymentCommand.Parameters.AddWithValue("orderId", orderId);
            paymentCommand.Parameters.AddWithValue("method", paymentCode);
            paymentCommand.Parameters.AddWithValue("amount", total);
            await paymentCommand.ExecuteNonQueryAsync(cancellationToken);
        }
        await transaction.CommitAsync(cancellationToken);
        return code;
    }

    public async Task<List<StoreOrder>> GetOrdersAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("""
            SELECT o.order_id, o.order_code, COALESCE(c.full_name,'Đơn tại quầy'),
                   COALESCE(string_agg(p.product_name || CASE WHEN i.quantity > 1 THEN ' × ' || i.quantity::text ELSE '' END, ', ' ORDER BY i.order_item_id),''),
                   o.ordered_at, o.total_amount, COALESCE(pay.payment_method,'PENDING'), o.order_status
            FROM orders o LEFT JOIN customers c ON c.customer_id=o.customer_id
            LEFT JOIN order_items i ON i.order_id=o.order_id LEFT JOIN products p ON p.product_id=i.product_id
            LEFT JOIN LATERAL (SELECT payment_method FROM payments WHERE order_id=o.order_id ORDER BY paid_at DESC LIMIT 1) pay ON TRUE
            GROUP BY o.order_id,c.full_name,pay.payment_method
            ORDER BY o.ordered_at DESC LIMIT 500;
            """, connection);
        var orders = new List<StoreOrder>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
            orders.Add(new(reader.GetInt64(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetFieldValue<DateTimeOffset>(4), reader.GetDecimal(5), reader.GetString(6), reader.GetString(7)));
        return orders;
    }

    public async Task UpdateOrderStatusAsync(long orderId, string status, CancellationToken cancellationToken = default)
    {
        var dbStatus = status switch { "Hoàn tất" => "COMPLETED", "Đang pha chế" => "PREPARING", "Đã hủy" => "CANCELLED", _ => throw new InvalidOperationException("Trạng thái không hợp lệ.") };
        await using var connection = CreateConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = new NpgsqlCommand("UPDATE orders SET order_status=@status, completed_at=CASE WHEN @status='COMPLETED' THEN NOW() ELSE NULL END WHERE order_id=@id", connection);
        command.Parameters.AddWithValue("status", dbStatus);
        command.Parameters.AddWithValue("id", orderId);
        if (await command.ExecuteNonQueryAsync(cancellationToken) == 0) throw new InvalidOperationException("Không tìm thấy đơn hàng.");
    }
}
