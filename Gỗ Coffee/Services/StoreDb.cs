using System.Globalization;
using System.Security.Claims;
using CuaHangTienLoi.Web.Models;
using Microsoft.AspNetCore.Components.Authorization;
using Npgsql;

namespace CuaHangTienLoi.Web.Services;

public sealed record MenuProduct(long Id, string Code, string Name, string? Description, string Category, decimal Price, bool Active, bool HasRecipe, string? ImageUrl = null);
public sealed record ProductRow(long Id, string Code, string Name, string? Description, long CategoryId, string Category, decimal Price, bool Active, int RecipeCount, string? ImageUrl = null);
public sealed record CategoryRow(long Id, string Name);
public sealed record CheckoutLine(long ProductId, decimal Quantity, string? Note);
public sealed record RecipeLine(long IngredientId, decimal Quantity);
public sealed record RecipeIngredient(long IngredientId, string Name, string Unit, decimal Quantity, bool Active);
public sealed record StoreOrder(long Id, string Code, string Customer, string Items, DateTimeOffset OrderedAt, decimal Total, string Payment, string Status);
public sealed record IngredientRow(long Id, string Code, string Name, string Category, string Supplier, string Unit, decimal Stock, decimal Minimum);
public sealed record EmployeeRow(long Id, string Name, string Email, string Role, string Status, DateTime Joined);
public sealed record DailyRevenue(DateTime Date, int Count, decimal Revenue);
public sealed record TopDrink(string Name, string Category, int Quantity, decimal Revenue, string? ImageUrl = null);
public sealed record DashboardSnapshot(decimal TodayRevenue, int TodayOrders, decimal AverageOrder, int ActiveProducts, int LowStock, List<DailyRevenue> Days, List<TopDrink> BestSellers, List<StoreOrder> RecentOrders);

/// <summary>PostgreSQL data access/service layer. UI never sends raw SQL and server-side role checks protect writes.</summary>
public sealed class StoreDb(IConfiguration configuration, AuthenticationStateProvider authentication)
{
    private NpgsqlConnection CreateConnection() => new(configuration.GetConnectionString("StoreDb")
        ?? throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:StoreDb bằng dotnet user-secrets."));

    private async Task<long> RequireAsync(params string[] allowedRoles)
    {
        var user = (await authentication.GetAuthenticationStateAsync()).User;
        if (user.Identity?.IsAuthenticated != true || !allowedRoles.Any(user.IsInRole) ||
            !long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id))
            throw new UnauthorizedAccessException("Tài khoản không có quyền thực hiện thao tác này.");
        // Database is the source of truth: revoking an account or changing its role
        // invalidates permissions even on an already-open Blazor circuit.
        await using var connection = CreateConnection();
        await connection.OpenAsync();
        await using var verify = new NpgsqlCommand("""
            SELECT r.role_name FROM user_accounts u
            JOIN employees e ON e.employee_id=u.employee_id
            JOIN roles r ON r.role_id=e.role_id
            WHERE e.employee_id=@id AND u.is_active AND e.status='ACTIVE' AND r.is_active
            LIMIT 1
            """, connection);
        verify.Parameters.AddWithValue("id", id);
        var currentRole = await verify.ExecuteScalarAsync() as string;
        if (currentRole is null || !allowedRoles.Contains(currentRole, StringComparer.Ordinal) || !user.IsInRole(currentRole))
            throw new UnauthorizedAccessException("Quyền truy cập đã thay đổi. Vui lòng đăng nhập lại.");
        return id;
    }

    public async Task<List<MenuProduct>> GetProductsAsync(CancellationToken ct = default)
    {
        await RequireAsync("ADMIN", "MANAGER", "STAFF");
        await using var db = CreateConnection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""
            SELECT p.product_id,p.product_code,p.product_name,p.description,c.category_name,p.sale_price,p.is_active,
                EXISTS(SELECT 1 FROM product_ingredients pi WHERE pi.product_id=p.product_id),p.image_url
            FROM products p JOIN categories c ON c.category_id=p.category_id
            WHERE p.is_active AND c.is_active ORDER BY c.category_name,p.product_name
            """, db);
        var result = new List<MenuProduct>(); await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct)) result.Add(new(reader.GetInt64(0),reader.GetString(1),reader.GetString(2),reader.IsDBNull(3)?null:reader.GetString(3),reader.GetString(4),reader.GetDecimal(5),reader.GetBoolean(6),reader.GetBoolean(7),reader.IsDBNull(8)?null:reader.GetString(8)));
        return result;
    }

    public async Task<List<ProductRow>> GetAllProductsAsync(CancellationToken ct = default)
    {
        await RequireAsync("ADMIN", "MANAGER");
        await using var db = CreateConnection(); await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""
            SELECT p.product_id,p.product_code,p.product_name,p.description,p.category_id,c.category_name,p.sale_price,p.is_active,
                (SELECT COUNT(*)::integer FROM product_ingredients pi WHERE pi.product_id=p.product_id),p.image_url
            FROM products p JOIN categories c ON c.category_id=p.category_id ORDER BY p.product_id DESC
            """, db);
        var items = new List<ProductRow>(); await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) items.Add(new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.IsDBNull(3)?null:r.GetString(3),r.GetInt64(4),r.GetString(5),r.GetDecimal(6),r.GetBoolean(7),r.GetInt32(8),r.IsDBNull(9)?null:r.GetString(9)));
        return items;
    }

    public async Task<List<CategoryRow>> GetCategoriesAsync(CancellationToken ct = default)
    {
        await RequireAsync("ADMIN", "MANAGER");
        await using var db=CreateConnection(); await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("SELECT category_id,category_name FROM categories WHERE is_active ORDER BY category_name",db);
        var result=new List<CategoryRow>(); await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) result.Add(new(r.GetInt64(0),r.GetString(1)));
        return result;
    }

    public async Task SaveProductAsync(long id,string code,string name,string? description,long categoryId,decimal price,bool active,CancellationToken ct=default)
    {
        await RequireAsync("ADMIN", "MANAGER");
        code=code.Trim().ToUpperInvariant();name=name.Trim();
        if(code.Length is < 2 or > 30 || name.Length is < 2 or > 150 || price < 0 || categoryId < 1)
            throw new ArgumentException("Kiểm tra mã, tên, danh mục và giá sản phẩm.");
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand(id==0 ? """
            INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
            VALUES(@code,@name,@desc,@category,@price,@active)
            """ : """
            UPDATE products SET product_code=@code,product_name=@name,description=@desc,
                category_id=@category,sale_price=@price,is_active=@active,updated_at=NOW()
            WHERE product_id=@id
            """,db);
        cmd.Parameters.AddWithValue("id",id);cmd.Parameters.AddWithValue("code",code);cmd.Parameters.AddWithValue("name",name);
        cmd.Parameters.AddWithValue("desc",(object?)description?.Trim()??DBNull.Value);cmd.Parameters.AddWithValue("category",categoryId);
        cmd.Parameters.AddWithValue("price",price);cmd.Parameters.AddWithValue("active",active);
        if(await cmd.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Không tìm thấy sản phẩm.");
    }

    public async Task DeactivateProductAsync(long id,CancellationToken ct=default)
    {
        await RequireAsync("ADMIN", "MANAGER");
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("UPDATE products SET is_active=FALSE,updated_at=NOW() WHERE product_id=@id",db);
        cmd.Parameters.AddWithValue("id",id);
        if(await cmd.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Sản phẩm không tồn tại.");
    }

    public async Task<List<RecipeIngredient>> GetRecipeAsync(long productId, CancellationToken ct = default)
    {
        await RequireAsync("ADMIN", "MANAGER");
        await using var db = CreateConnection();
        await db.OpenAsync(ct);
        await using var cmd = new NpgsqlCommand("""
            SELECT i.ingredient_id,i.ingredient_name,i.unit,pi.quantity,i.is_active
            FROM product_ingredients pi JOIN ingredients i ON i.ingredient_id=pi.ingredient_id
            WHERE pi.product_id=@id ORDER BY i.ingredient_name
            """, db);
        cmd.Parameters.AddWithValue("id", productId);
        var result = new List<RecipeIngredient>();
        await using var reader = await cmd.ExecuteReaderAsync(ct);
        while (await reader.ReadAsync(ct))
            result.Add(new(reader.GetInt64(0),reader.GetString(1),reader.GetString(2),reader.GetDecimal(3),reader.GetBoolean(4)));
        return result;
    }

    public async Task SaveRecipeAsync(long productId, IReadOnlyCollection<RecipeLine> lines, CancellationToken ct = default)
    {
        await RequireAsync("ADMIN", "MANAGER");
        if (lines.Count is < 1 or > 100 || lines.Any(l => l.IngredientId < 1 || l.Quantity <= 0 || l.Quantity > 100000 || decimal.Round(l.Quantity,3) != l.Quantity))
            throw new ArgumentException("Công thức cần nguyên liệu với định lượng lớn hơn 0, tối đa 3 số lẻ và không quá 100.000 đơn vị.");
        if (lines.Select(l => l.IngredientId).Distinct().Count() != lines.Count)
            throw new ArgumentException("Mỗi nguyên liệu chỉ được xuất hiện một lần trong công thức.");
        await using var db = CreateConnection();
        await db.OpenAsync(ct);
        await using var tx = await db.BeginTransactionAsync(ct);
        // The same product lock is used by checkout, so an order sees one complete recipe.
        await using (var product = new NpgsqlCommand("SELECT product_id FROM products WHERE product_id=@id FOR UPDATE", db, tx))
        {
            product.Parameters.AddWithValue("id", productId);
            if (await product.ExecuteScalarAsync(ct) is null)
                throw new InvalidOperationException("Món không tồn tại.");
        }
        foreach (var line in lines.OrderBy(l => l.IngredientId))
        {
            await using var ingredient = new NpgsqlCommand("SELECT is_active FROM ingredients WHERE ingredient_id=@id FOR SHARE", db, tx);
            ingredient.Parameters.AddWithValue("id", line.IngredientId);
            if (await ingredient.ExecuteScalarAsync(ct) is not true)
                throw new InvalidOperationException("Công thức có nguyên liệu không tồn tại hoặc đã ngừng sử dụng.");
        }
        await using (var clear = new NpgsqlCommand("DELETE FROM product_ingredients WHERE product_id=@id", db, tx))
        {
            clear.Parameters.AddWithValue("id", productId);
            await clear.ExecuteNonQueryAsync(ct);
        }
        foreach (var line in lines)
        {
            await using var insert = new NpgsqlCommand("INSERT INTO product_ingredients(product_id,ingredient_id,quantity) VALUES(@product,@ingredient,@qty)", db, tx);
            insert.Parameters.AddWithValue("product", productId);
            insert.Parameters.AddWithValue("ingredient", line.IngredientId);
            insert.Parameters.AddWithValue("qty", line.Quantity);
            await insert.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task<string> CreateOrderAsync(IReadOnlyCollection<CheckoutLine> lines,string paymentMethod,decimal vatRate,decimal discount,string? note,string? tableNo,CancellationToken ct=default)
    {
        var employeeId=await RequireAsync("ADMIN","MANAGER","STAFF");
        if(lines.Count==0||lines.Count>100||lines.Any(l=>l.ProductId<1||l.Quantity<=0||l.Quantity>100||decimal.Truncate(l.Quantity)!=l.Quantity))throw new ArgumentException("Số lượng món phải là số nguyên từ 1 đến 100.");
        if(lines.GroupBy(l=>l.ProductId).Any(g=>g.Sum(l=>l.Quantity)>100))throw new ArgumentException("Mỗi món chỉ được tối đa 100 phần trong một đơn.");
        if(lines.Any(l=>l.Note is { Length: > 255 }))throw new ArgumentException("Ghi chú từng món không được quá 255 ký tự.");
        if(discount<0||vatRate<0||vatRate>20)throw new ArgumentException("Thuế hoặc giảm giá không hợp lệ.");
        if(tableNo is { Length: > 30 } || note is { Length: > 5000 }) throw new ArgumentException("Mã bàn hoặc ghi chú quá dài.");
        var paymentCode=paymentMethod switch{"Tiền mặt"=>"CASH","Chuyển khoản"=>"BANK_TRANSFER","Thẻ"=>"CARD",_=>throw new ArgumentException("Phương thức thanh toán không hợp lệ.")};
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var tx=await db.BeginTransactionAsync(ct);
        var prices=new Dictionary<long,decimal>();
        var requiredIngredients = new Dictionary<long,decimal>();
        // Lock products and ingredients in a stable order to avoid deadlocks between tills.
        foreach(var group in lines.GroupBy(l=>l.ProductId).OrderBy(g=>g.Key))
        {
            await using var product=new NpgsqlCommand("""
                SELECT p.sale_price,p.product_name FROM products p
                JOIN categories c ON c.category_id=p.category_id
                WHERE p.product_id=@id AND p.is_active AND c.is_active FOR UPDATE OF p
                """,db,tx);
            product.Parameters.AddWithValue("id",group.Key);
            string productName;
            await using (var reader = await product.ExecuteReaderAsync(ct))
            {
                if (!await reader.ReadAsync(ct)) throw new InvalidOperationException("Có món hoặc danh mục đã ngừng kinh doanh.");
                prices[group.Key]=reader.GetDecimal(0);
                productName=reader.GetString(1);
            }
            await using var recipe = new NpgsqlCommand("SELECT ingredient_id,quantity FROM product_ingredients WHERE product_id=@id ORDER BY ingredient_id",db,tx);
            recipe.Parameters.AddWithValue("id",group.Key);
            var hasRecipe=false;
            await using (var reader = await recipe.ExecuteReaderAsync(ct))
            {
                while (await reader.ReadAsync(ct))
                {
                    hasRecipe=true;
                    var ingredientId=reader.GetInt64(0);
                    var needed=reader.GetDecimal(1)*group.Sum(l=>l.Quantity);
                    requiredIngredients[ingredientId]=requiredIngredients.GetValueOrDefault(ingredientId)+needed;
                }
            }
            if (!hasRecipe) throw new InvalidOperationException($"Món {productName} chưa có công thức. Nhờ quản lý cấu hình trong Thực đơn.");
        }
        foreach (var (ingredientId, needed) in requiredIngredients.OrderBy(x=>x.Key))
        {
            await using var stock = new NpgsqlCommand("SELECT ingredient_name,unit,current_stock,is_active FROM ingredients WHERE ingredient_id=@id FOR UPDATE",db,tx);
            stock.Parameters.AddWithValue("id",ingredientId);
            await using var reader = await stock.ExecuteReaderAsync(ct);
            if (!await reader.ReadAsync(ct) || !reader.GetBoolean(3))
                throw new InvalidOperationException("Công thức có nguyên liệu đã ngừng sử dụng. Nhờ quản lý kiểm tra lại.");
            if (reader.GetDecimal(2)<needed)
                throw new InvalidOperationException($"Không đủ {reader.GetString(0)}: cần {needed:0.###} {reader.GetString(1)}, còn {reader.GetDecimal(2):0.###} {reader.GetString(1)}.");
        }
        var subtotal=lines.Sum(l=>prices[l.ProductId]*l.Quantity);
        var vat=decimal.Round(subtotal*vatRate/100m,2,MidpointRounding.AwayFromZero);
        var appliedDiscount=Math.Min(discount,subtotal+vat);
        var total=subtotal+vat-appliedDiscount;
        var code=$"DH-{DateTimeOffset.UtcNow:yyMMddHHmmss}-{Guid.NewGuid():N}"[..28];
        await using var orderCmd=new NpgsqlCommand("""
            INSERT INTO orders(order_code,employee_id,table_no,note,order_status,subtotal,vat_rate,vat_amount,discount_amount,total_amount)
            VALUES(@code,@employee,@table,@note,'PREPARING',@subtotal,@rate,@vat,@discount,@total)
            RETURNING order_id
            """,db,tx);
        orderCmd.Parameters.AddWithValue("code",code);orderCmd.Parameters.AddWithValue("employee",employeeId);
        orderCmd.Parameters.AddWithValue("table",(object?)tableNo??DBNull.Value);orderCmd.Parameters.AddWithValue("note",(object?)note??DBNull.Value);
        orderCmd.Parameters.AddWithValue("subtotal",subtotal);orderCmd.Parameters.AddWithValue("rate",vatRate);
        orderCmd.Parameters.AddWithValue("vat",vat);orderCmd.Parameters.AddWithValue("discount",appliedDiscount);orderCmd.Parameters.AddWithValue("total",total);
        var orderId=(long)(await orderCmd.ExecuteScalarAsync(ct)??throw new InvalidOperationException("Không lưu được đơn."));
        foreach(var l in lines)
        {
            await using var item=new NpgsqlCommand("""
                INSERT INTO order_items(order_id,product_id,quantity,unit_price,note,line_total)
                VALUES(@order,@product,@qty,@price,@note,@total)
                """,db,tx);
            item.Parameters.AddWithValue("order",orderId);item.Parameters.AddWithValue("product",l.ProductId);
            item.Parameters.AddWithValue("qty",l.Quantity);item.Parameters.AddWithValue("price",prices[l.ProductId]);
            item.Parameters.AddWithValue("note",(object?)l.Note??DBNull.Value);item.Parameters.AddWithValue("total",prices[l.ProductId]*l.Quantity);
            await item.ExecuteNonQueryAsync(ct);
        }
        foreach (var (ingredientId,needed) in requiredIngredients.OrderBy(x=>x.Key))
        {
            await using var deduct = new NpgsqlCommand("""
                UPDATE ingredients SET current_stock=current_stock-@qty,updated_at=NOW()
                WHERE ingredient_id=@id AND is_active AND current_stock>=@qty
                """,db,tx);
            deduct.Parameters.AddWithValue("qty",needed);deduct.Parameters.AddWithValue("id",ingredientId);
            if (await deduct.ExecuteNonQueryAsync(ct)!=1)throw new InvalidOperationException("Tồn kho vừa thay đổi. Vui lòng thử lại.");
            await using var log = new NpgsqlCommand("""
                INSERT INTO inventory_transactions(ingredient_id,employee_id,transaction_type,quantity,reference_note)
                VALUES(@ingredient,@employee,'EXPORT',@qty,@reference)
                """,db,tx);
            log.Parameters.AddWithValue("ingredient",ingredientId);log.Parameters.AddWithValue("employee",employeeId);
            log.Parameters.AddWithValue("qty",needed);log.Parameters.AddWithValue("reference",$"Xuất bán đơn {code}");
            await log.ExecuteNonQueryAsync(ct);
        }
        await using var payment=new NpgsqlCommand("INSERT INTO payments(order_id,payment_method,amount,payment_status) VALUES(@order,@method,@total,'PAID')",db,tx);
        payment.Parameters.AddWithValue("order",orderId);payment.Parameters.AddWithValue("method",paymentCode);payment.Parameters.AddWithValue("total",total);
        await payment.ExecuteNonQueryAsync(ct);await tx.CommitAsync(ct);
        return code;
    }

    public async Task<List<StoreOrder>> GetOrdersAsync(CancellationToken ct=default)
    {
        await RequireAsync("ADMIN","MANAGER","STAFF");
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("""
            SELECT o.order_id,o.order_code,COALESCE(c.full_name,'Đơn tại quầy'),
                COALESCE(string_agg(p.product_name || CASE WHEN i.quantity>1 THEN ' × '||i.quantity::text ELSE '' END,', ' ORDER BY i.order_item_id),''),
                o.ordered_at,o.total_amount,COALESCE(pay.payment_method,'PENDING'),o.order_status
            FROM orders o LEFT JOIN customers c ON c.customer_id=o.customer_id
            LEFT JOIN order_items i ON i.order_id=o.order_id LEFT JOIN products p ON p.product_id=i.product_id
            LEFT JOIN LATERAL (SELECT payment_method FROM payments WHERE order_id=o.order_id ORDER BY paid_at DESC LIMIT 1) pay ON TRUE
            GROUP BY o.order_id,c.full_name,pay.payment_method ORDER BY o.ordered_at DESC LIMIT 500
            """,db);
        var result=new List<StoreOrder>();await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))result.Add(new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetFieldValue<DateTimeOffset>(4),r.GetDecimal(5),r.GetString(6),r.GetString(7)));
        return result;
    }

    public async Task UpdateOrderStatusAsync(long orderId,string status,CancellationToken ct=default)
    {
        await RequireAsync("ADMIN","MANAGER","STAFF");
        var code=status switch{"Hoàn tất"=>"COMPLETED","Đang pha chế"=>"PREPARING","Đã hủy"=>"CANCELLED",_=>throw new ArgumentException("Trạng thái không hợp lệ.")};
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("""
            UPDATE orders SET order_status=@status,completed_at=CASE WHEN @status='COMPLETED' THEN NOW() ELSE NULL END
            WHERE order_id=@id AND (@status<>'CANCELLED' OR NOT EXISTS
                (SELECT 1 FROM payments WHERE order_id=@id AND payment_status='PAID'))
            """,db);
        cmd.Parameters.AddWithValue("id",orderId);cmd.Parameters.AddWithValue("status",code);
        if(await cmd.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Không thể hủy đơn đã thanh toán. Cần thực hiện quy trình hoàn tiền riêng.");
    }

    public async Task<List<IngredientRow>> GetIngredientsAsync(CancellationToken ct=default)
    {
        await RequireAsync("ADMIN","MANAGER","WAREHOUSE");
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("""
            SELECT ingredient_id,ingredient_code,ingredient_name,COALESCE(category,''),COALESCE(supplier,''),unit,current_stock,min_stock
            FROM ingredients WHERE is_active ORDER BY ingredient_name
            """,db);
        var result=new List<IngredientRow>();await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) result.Add(new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetDecimal(6),r.GetDecimal(7)));
        return result;
    }

    public async Task ImportIngredientAsync(long ingredientId,decimal quantity,CancellationToken ct=default)
    {
        var employeeId=await RequireAsync("ADMIN","MANAGER","WAREHOUSE");
        if(quantity<=0||quantity>100000)throw new ArgumentException("Số lượng nhập phải lớn hơn 0.");
        await using var db=CreateConnection();await db.OpenAsync(ct);await using var tx=await db.BeginTransactionAsync(ct);
        await using(var update=new NpgsqlCommand("UPDATE ingredients SET current_stock=current_stock+@qty,updated_at=NOW() WHERE ingredient_id=@id AND is_active",db,tx))
        {
            update.Parameters.AddWithValue("qty",quantity);update.Parameters.AddWithValue("id",ingredientId);
            if(await update.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Không tìm thấy nguyên liệu.");
        }
        await using(var log=new NpgsqlCommand("INSERT INTO inventory_transactions(ingredient_id,employee_id,transaction_type,quantity,reference_note) VALUES(@id,@employee,'IMPORT',@qty,'Nhập qua giao diện')",db,tx))
        {
            log.Parameters.AddWithValue("id",ingredientId);log.Parameters.AddWithValue("employee",employeeId);log.Parameters.AddWithValue("qty",quantity);
            await log.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task<List<EmployeeRow>> GetEmployeesAsync(CancellationToken ct=default)
    {
        await RequireAsync("ADMIN");
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("""
            SELECT e.employee_id,e.full_name,e.email,COALESCE(r.role_name,'STAFF'),e.status,e.hire_date
            FROM employees e LEFT JOIN roles r ON r.role_id=e.role_id ORDER BY e.employee_id
            """,db);
        var result=new List<EmployeeRow>();await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))result.Add(new(r.GetInt64(0),r.GetString(1),r.GetString(2),r.GetString(3),r.GetString(4),r.GetDateTime(5)));
        return result;
    }

    public async Task SaveEmployeeAsync(long id,string name,string email,string role,string? temporaryPassword,CancellationToken ct=default)
    {
        var callerId = await RequireAsync("ADMIN");
        name=name.Trim();email=email.Trim().ToLowerInvariant();
        if(name.Length is < 2 or > 150 || email.Length>150 || !new System.ComponentModel.DataAnnotations.EmailAddressAttribute().IsValid(email))
            throw new ArgumentException("Họ tên hoặc email không hợp lệ.");
        if(role is not ("ADMIN" or "MANAGER" or "STAFF" or "WAREHOUSE"))throw new ArgumentException("Vai trò không hợp lệ.");
        if(id==0 && (string.IsNullOrWhiteSpace(temporaryPassword)||temporaryPassword.Length<10))
            throw new ArgumentException("Nhân viên mới cần mật khẩu tạm ít nhất 10 ký tự.");
        if(id!=0 && id==callerId && role!="ADMIN")
            throw new InvalidOperationException("Không thể tự hạ quyền tài khoản đang đăng nhập.");
        await using var db=CreateConnection();await db.OpenAsync(ct);await using var tx=await db.BeginTransactionAsync(ct);
        if(id!=0 && role!="ADMIN")
        {
            await using var lastAdmin=new NpgsqlCommand("""
                SELECT CASE WHEN EXISTS(SELECT 1 FROM employees e JOIN roles r ON r.role_id=e.role_id WHERE e.employee_id=@id AND r.role_name='ADMIN')
                THEN (SELECT COUNT(*) FROM employees e JOIN roles r ON r.role_id=e.role_id WHERE r.role_name='ADMIN' AND e.status='ACTIVE') ELSE 2 END
                """,db,tx);
            lastAdmin.Parameters.AddWithValue("id",id);
            if(Convert.ToInt64(await lastAdmin.ExecuteScalarAsync(ct),CultureInfo.InvariantCulture)<=1)
                throw new InvalidOperationException("Không thể hạ quyền quản trị viên hoạt động cuối cùng.");
        }
        if(id==0)
        {
            await using var add=new NpgsqlCommand("""
                INSERT INTO employees(full_name,email,role_id,status)
                SELECT @name,@email,role_id,'ACTIVE' FROM roles WHERE role_name=@role AND is_active
                RETURNING employee_id
                """,db,tx);
            add.Parameters.AddWithValue("name",name);add.Parameters.AddWithValue("email",email);add.Parameters.AddWithValue("role",role);
            var result=await add.ExecuteScalarAsync(ct);
            if(result is not long newId)throw new InvalidOperationException("Không tìm thấy vai trò.");
            await using var account=new NpgsqlCommand("INSERT INTO user_accounts(employee_id,username,password_hash,is_active) VALUES(@id,@email,@hash,TRUE)",db,tx);
            account.Parameters.AddWithValue("id",newId);account.Parameters.AddWithValue("email",email);account.Parameters.AddWithValue("hash",PasswordHasher.Hash(temporaryPassword!));
            await account.ExecuteNonQueryAsync(ct);
        }
        else
        {
            await using var update=new NpgsqlCommand("""
                UPDATE employees SET full_name=@name,email=@email,role_id=(SELECT role_id FROM roles WHERE role_name=@role AND is_active),updated_at=NOW()
                WHERE employee_id=@id
                """,db,tx);
            update.Parameters.AddWithValue("name",name);update.Parameters.AddWithValue("email",email);update.Parameters.AddWithValue("role",role);update.Parameters.AddWithValue("id",id);
            if(await update.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Không tìm thấy nhân viên.");
            await using var syncAccount=new NpgsqlCommand("UPDATE user_accounts SET username=@email WHERE employee_id=@id",db,tx);
            syncAccount.Parameters.AddWithValue("email",email);syncAccount.Parameters.AddWithValue("id",id);
            await syncAccount.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task SetEmployeeActiveAsync(long id,bool active,CancellationToken ct=default)
    {
        var caller=await RequireAsync("ADMIN");
        if(id==caller&&!active)throw new InvalidOperationException("Không thể vô hiệu hóa chính tài khoản đang sử dụng.");
        await using var db=CreateConnection();await db.OpenAsync(ct);await using var tx=await db.BeginTransactionAsync(ct);
        if(!active)
        {
            await using var check=new NpgsqlCommand("""
                SELECT CASE WHEN EXISTS(SELECT 1 FROM employees e JOIN roles r ON r.role_id=e.role_id WHERE e.employee_id=@id AND r.role_name='ADMIN')
                  THEN (SELECT COUNT(*) FROM employees e JOIN roles r ON r.role_id=e.role_id WHERE r.role_name='ADMIN' AND e.status='ACTIVE') ELSE 2 END
                """,db,tx);
            check.Parameters.AddWithValue("id",id);
            if(Convert.ToInt64(await check.ExecuteScalarAsync(ct),CultureInfo.InvariantCulture)<=1)
                throw new InvalidOperationException("Phải giữ ít nhất một quản trị viên đang hoạt động.");
        }
        await using(var update=new NpgsqlCommand("UPDATE employees SET status=@status,updated_at=NOW() WHERE employee_id=@id",db,tx))
        {
            update.Parameters.AddWithValue("status",active?"ACTIVE":"INACTIVE");update.Parameters.AddWithValue("id",id);
            if(await update.ExecuteNonQueryAsync(ct)==0)throw new InvalidOperationException("Nhân viên không tồn tại.");
        }
        await using(var account=new NpgsqlCommand("UPDATE user_accounts SET is_active=@active WHERE employee_id=@id",db,tx))
        {
            account.Parameters.AddWithValue("active",active);account.Parameters.AddWithValue("id",id);await account.ExecuteNonQueryAsync(ct);
        }
        await tx.CommitAsync(ct);
    }

    public async Task<List<DailyRevenue>> GetDailyRevenueAsync(int days=30,CancellationToken ct=default)
    {
        await RequireAsync("ADMIN","MANAGER");
        if(days<1||days>365)throw new ArgumentOutOfRangeException(nameof(days));
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("""
            SELECT (o.ordered_at AT TIME ZONE 'Asia/Ho_Chi_Minh')::date,
                COUNT(*)::integer,COALESCE(SUM(o.total_amount),0)
            FROM orders o WHERE o.order_status<>'CANCELLED'
              AND o.ordered_at >= @since
            GROUP BY 1 ORDER BY 1
            """,db);
        var vietnamToday = DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)).Date;
        var sinceUtc = new DateTimeOffset(vietnamToday.AddDays(1-days), TimeSpan.FromHours(7)).UtcDateTime;
        cmd.Parameters.AddWithValue("since",sinceUtc);
        var rows=new List<DailyRevenue>();await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct)) rows.Add(new(r.GetDateTime(0),r.GetInt32(1),r.GetDecimal(2)));
        return rows;
    }

    public async Task<List<TopDrink>> GetBestSellersAsync(int limit=5,CancellationToken ct=default)
    {
        await RequireAsync("ADMIN","MANAGER");
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("""
            SELECT p.product_name,c.category_name,COALESCE(SUM(i.quantity),0)::integer,COALESCE(SUM(i.line_total),0),p.image_url
            FROM order_items i JOIN orders o ON o.order_id=i.order_id AND o.order_status<>'CANCELLED'
                JOIN products p ON p.product_id=i.product_id JOIN categories c ON c.category_id=p.category_id
            WHERE (o.ordered_at AT TIME ZONE 'Asia/Ho_Chi_Minh')::date=(NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date
            GROUP BY p.product_id,c.category_name ORDER BY SUM(i.quantity) DESC LIMIT @limit
            """,db);
        cmd.Parameters.AddWithValue("limit",Math.Clamp(limit,1,20));
        var result=new List<TopDrink>();await using var r=await cmd.ExecuteReaderAsync(ct);
        while(await r.ReadAsync(ct))result.Add(new(r.GetString(0),r.GetString(1),r.GetInt32(2),r.GetDecimal(3),r.IsDBNull(4)?null:r.GetString(4)));
        return result;
    }

    public async Task<DashboardSnapshot> GetDashboardAsync(CancellationToken ct=default)
    {
        await RequireAsync("ADMIN","MANAGER");
        await using var db=CreateConnection();await db.OpenAsync(ct);
        await using var cmd=new NpgsqlCommand("""
            SELECT COALESCE(SUM(o.total_amount),0),COUNT(*)::integer
            FROM orders o WHERE o.order_status<>'CANCELLED'
            AND (o.ordered_at AT TIME ZONE 'Asia/Ho_Chi_Minh')::date=(NOW() AT TIME ZONE 'Asia/Ho_Chi_Minh')::date
            """,db);
        decimal revenue;int count;
        await using(var r=await cmd.ExecuteReaderAsync(ct)){await r.ReadAsync(ct);revenue=r.GetDecimal(0);count=r.GetInt32(1);}
        await using var meta=new NpgsqlCommand("SELECT (SELECT COUNT(*)::integer FROM products WHERE is_active),(SELECT COUNT(*)::integer FROM ingredients WHERE is_active AND current_stock<=min_stock)",db);
        int products,low;
        await using(var r=await meta.ExecuteReaderAsync(ct)){await r.ReadAsync(ct);products=r.GetInt32(0);low=r.GetInt32(1);}
        // Sequential DB operations avoid concurrent use of the same connection.
        var days=await GetDailyRevenueAsync(7,ct);
        var top=await GetBestSellersAsync(5,ct);
        var orders=(await GetOrdersAsync(ct)).Take(5).ToList();
        return new(revenue,count,count==0?0:revenue/count,products,low,days,top,orders);
    }
}
