using System.Security.Claims;
using CuaHangTienLoi.Web.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;
using Npgsql;

// Run only against a separate seeded database. Never use the running cafe_pos database.
var connectionString = Environment.GetEnvironmentVariable("GOCOFFEE_TEST_CONNECTION")
    ?? throw new InvalidOperationException("Set GOCOFFEE_TEST_CONNECTION for a separate seeded test database.");
if (new NpgsqlConnectionStringBuilder(connectionString).Database?.StartsWith("cafe_pos_test_",StringComparison.Ordinal) != true)
    throw new InvalidOperationException("The database name must start with cafe_pos_test_.");
var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>
    { ["ConnectionStrings:StoreDb"] = connectionString }).Build();
var adminId = await Scalar<long>("SELECT employee_id FROM user_accounts WHERE username='admin@moc.coffee'");
var staffId = await Scalar<long>("SELECT employee_id FROM user_accounts WHERE username='staff@moc.coffee'");
StoreDb Service(string role="ADMIN") => new(configuration,new TestAuthentication(role=="STAFF"?staffId:adminId,role));
var failures = new List<string>();
var passed = 0;

await Run("aggregate shared ingredients across products and preserve paid order", async () =>
{
    var f=await Fixture(1m,0.018m,0.020m);
    var code=await Service().CreateOrderAsync([new(f.Product1,2,null),new(f.Product2,3,null)],"Tiền mặt",8,0,"test","T1");
    Equal(0.904m,await Stock(f.Ingredient),"aggregated deduction");
    Equal(1L,await Scalar<long>("SELECT COUNT(*) FROM inventory_transactions WHERE ingredient_id=@id AND transaction_type='EXPORT'",("id",f.Ingredient)),"one aggregated export");
    Equal(0.096m,await Scalar<decimal>("SELECT quantity FROM inventory_transactions WHERE ingredient_id=@id",("id",f.Ingredient)),"export quantity");
    Equal(54000m,await Scalar<decimal>("SELECT total_amount FROM orders WHERE order_code=@code",("code",code)),"total");
    Equal(2L,await Scalar<long>("SELECT COUNT(*) FROM order_items WHERE order_id=(SELECT order_id FROM orders WHERE order_code=@code)",("code",code)),"order items");
    Equal(1L,await Scalar<long>("SELECT COUNT(*) FROM payments WHERE order_id=(SELECT order_id FROM orders WHERE order_code=@code) AND payment_status='PAID' AND amount=54000",("code",code)),"payment");
    Equal(1L,await Scalar<long>("SELECT COUNT(*) FROM inventory_transactions WHERE ingredient_id=@id AND employee_id=@employee AND reference_note LIKE '%'||@code",("id",f.Ingredient),("employee",adminId),("code",code)),"export links cashier and order");
});

await Run("duplicate product lines are aggregated", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    await Service().CreateOrderAsync([new(f.Product1,2,null),new(f.Product1,3,null)],"Tiền mặt",8,0,null,null);
    Equal(0.5m,await Stock(f.Ingredient),"duplicate lines deduction");
});

await Run("insufficient shared stock leaves no order, payment or stock log", async () =>
{
    var f=await Fixture(0.05m,0.03m,0.03m);
    var before=await Snapshot();
    await Reject(()=>Service().CreateOrderAsync([new(f.Product1,1,null),new(f.Product2,1,null)],"Tiền mặt",8,0,null,null),"Không đủ");
    Equal(before,await Snapshot(),"all state unchanged");
});

await Run("missing recipe blocks checkout", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    await Execute("DELETE FROM product_ingredients WHERE product_id=@id",("id",f.Product1));
    var before=await Snapshot();
    await Reject(()=>Service().CreateOrderAsync([new(f.Product1,1,null)],"Tiền mặt",8,0,null,null),"chưa có công thức");
    Equal(before,await Snapshot(),"missing recipe atomicity");
});

await Run("inactive ingredients block checkout", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    await Execute("UPDATE ingredients SET is_active=FALSE WHERE ingredient_id=@id",("id",f.Ingredient));
    var before=await Snapshot();
    await Reject(()=>Service().CreateOrderAsync([new(f.Product1,1,null)],"Tiền mặt",8,0,null,null),"ngừng sử dụng");
    Equal(before,await Snapshot(),"inactive ingredient atomicity");
});

await Run("a later insufficient ingredient keeps earlier ingredients unchanged", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    var missing=await Ingredient(0m);
    await Service().SaveRecipeAsync(f.Product1,[new(f.Ingredient,0.1m),new(missing,0.1m)]);
    var before=await Snapshot();
    await Reject(()=>Service().CreateOrderAsync([new(f.Product1,1,null)],"Tiền mặt",8,0,null,null),"Không đủ");
    Equal(before,await Snapshot(),"multi ingredient atomicity");
});

await Run("payment failure rolls back order, stock deduction and export log", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    await Execute("""
        CREATE OR REPLACE FUNCTION reject_test_payment() RETURNS trigger LANGUAGE plpgsql AS $$
        BEGIN RAISE EXCEPTION 'forced payment failure'; END; $$;
        CREATE TRIGGER reject_test_payment BEFORE INSERT ON payments FOR EACH ROW EXECUTE FUNCTION reject_test_payment();
        """);
    try
    {
        var before=await Snapshot();
        await Reject(()=>Service().CreateOrderAsync([new(f.Product1,1,null)],"Tiền mặt",8,0,null,null),"forced payment failure");
        Equal(before,await Snapshot(),"payment rollback");
    }
    finally { await Execute("DROP TRIGGER reject_test_payment ON payments; DROP FUNCTION reject_test_payment();"); }
});

await Run("two tills cannot oversell the last shared ingredient", async () =>
{
    var f=await Fixture(0.1m,0.1m,0.1m);
    async Task<bool> Buy(long product)
    {
        try { await Service().CreateOrderAsync([new(product,1,null)],"Tiền mặt",8,0,null,null);return true; }
        catch(InvalidOperationException ex) when(ex.Message.Contains("Không đủ")) { return false; }
    }
    var results=await Task.WhenAll(Buy(f.Product1),Buy(f.Product2));
    Equal(1,results.Count(x=>x),"exactly one successful till");
    Equal(0m,await Stock(f.Ingredient),"no negative stock");
    Equal(1L,await Scalar<long>("SELECT COUNT(*) FROM inventory_transactions WHERE ingredient_id=@id",("id",f.Ingredient)),"one export");
});

await Run("opposite product ordering completes without deadlock", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    await Task.WhenAll(
        Service().CreateOrderAsync([new(f.Product1,1,null),new(f.Product2,1,null)],"Tiền mặt",8,0,null,null),
        Service().CreateOrderAsync([new(f.Product2,1,null),new(f.Product1,1,null)],"Tiền mặt",8,0,null,null)).WaitAsync(TimeSpan.FromSeconds(15));
    Equal(0.6m,await Stock(f.Ingredient),"both carts consumed stock");
});

await Run("recipe edits persist and change the next checkout", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    await Service().SaveRecipeAsync(f.Product1,[new(f.Ingredient,0.025m)]);
    Equal(0.025m,(await Service().GetRecipeAsync(f.Product1)).Single().Quantity,"saved recipe");
    await Service().CreateOrderAsync([new(f.Product1,2,null)],"Tiền mặt",8,0,null,null);
    Equal(0.95m,await Stock(f.Ingredient),"new recipe used");
});

await Run("invalid recipe edits preserve the previous recipe", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    await Reject(()=>Service().SaveRecipeAsync(f.Product1,[]),"Công thức cần");
    await Reject(()=>Service().SaveRecipeAsync(f.Product1,[new(f.Ingredient,0.0001m)]),"3 số lẻ");
    await Reject(()=>Service().SaveRecipeAsync(f.Product1,[new(f.Ingredient,0.1m),new(f.Ingredient,0.2m)]),"một lần");
    Equal(0.1m,(await Service().GetRecipeAsync(f.Product1)).Single().Quantity,"old recipe preserved");
});

await Run("staff may sell but cannot edit recipes", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    await Reject(()=>Service("STAFF").SaveRecipeAsync(f.Product1,[new(f.Ingredient,0.5m)]),"không có quyền");
    await Service("STAFF").CreateOrderAsync([new(f.Product1,1,null)],"Tiền mặt",8,0,null,null);
    Equal(0.9m,await Stock(f.Ingredient),"staff sale");
});

await Run("fractional dishes and excessive duplicate lines are rejected", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    var before=await Snapshot();
    await Reject(()=>Service().CreateOrderAsync([new(f.Product1,0.5m,null)],"Tiền mặt",8,0,null,null),"số nguyên");
    await Reject(()=>Service().CreateOrderAsync([new(f.Product1,60,null),new(f.Product1,60,null)],"Tiền mặt",8,0,null,null),"tối đa 100");
    Equal(before,await Snapshot(),"invalid quantity atomicity");
});

await Run("changing paid order status does not deduct stock twice", async () =>
{
    var f=await Fixture(1m,0.1m,0.1m);
    var code=await Service().CreateOrderAsync([new(f.Product1,1,null)],"Tiền mặt",8,0,null,null);
    var id=await Scalar<long>("SELECT order_id FROM orders WHERE order_code=@code",("code",code));
    await Service().UpdateOrderStatusAsync(id,"Hoàn tất");
    await Service().UpdateOrderStatusAsync(id,"Đang pha chế");
    await Service().UpdateOrderStatusAsync(id,"Hoàn tất");
    await Reject(()=>Service().UpdateOrderStatusAsync(id,"Đã hủy"),"Không thể hủy");
    Equal(0.9m,await Stock(f.Ingredient),"one deduction only");
    Equal(1L,await Scalar<long>("SELECT COUNT(*) FROM inventory_transactions WHERE ingredient_id=@id",("id",f.Ingredient)),"one log only");
});

Console.WriteLine($"Integration checks: {passed} passed, {failures.Count} failed.");
return failures.Count==0?0:1;

async Task Run(string name,Func<Task> test)
{
    try { await test();passed++;Console.WriteLine($"PASS {name}"); }
    catch(Exception ex) { failures.Add(name);Console.WriteLine($"FAIL {name}: {ex.GetType().Name}: {ex.Message}"); }
}
static void Equal<T>(T expected,T actual,string label)
{
    if(!EqualityComparer<T>.Default.Equals(expected,actual))throw new Exception($"{label}: expected {expected}, got {actual}");
}
static async Task Reject(Func<Task> action,string expectedMessage)
{
    try { await action(); }
    catch(Exception ex) when(ex.Message.Contains(expectedMessage,StringComparison.Ordinal)) { return; }
    throw new Exception($"Expected rejection containing: {expectedMessage}");
}
async Task<T> Scalar<T>(string sql,params (string Name,object Value)[] values)
{
    await using var db=new NpgsqlConnection(connectionString);await db.OpenAsync();
    await using var cmd=new NpgsqlCommand(sql,db);
    foreach(var (name,value) in values)cmd.Parameters.AddWithValue(name,value);
    return (T)Convert.ChangeType((await cmd.ExecuteScalarAsync())!,typeof(T));
}
async Task Execute(string sql,params (string Name,object Value)[] values)
{
    await using var db=new NpgsqlConnection(connectionString);await db.OpenAsync();
    await using var cmd=new NpgsqlCommand(sql,db);
    foreach(var (name,value) in values)cmd.Parameters.AddWithValue(name,value);
    await cmd.ExecuteNonQueryAsync();
}
Task<decimal> Stock(long id)=>Scalar<decimal>("SELECT current_stock FROM ingredients WHERE ingredient_id=@id",("id",id));
Task<string> Snapshot()=>Scalar<string>("""
    SELECT json_build_array((SELECT COUNT(*) FROM orders),(SELECT COUNT(*) FROM order_items),
        (SELECT COUNT(*) FROM payments),(SELECT COUNT(*) FROM inventory_transactions),
        (SELECT json_agg(json_build_array(ingredient_id,current_stock) ORDER BY ingredient_id) FROM ingredients))::text
    """);
Task<long> Ingredient(decimal stock)=>Scalar<long>("""
    INSERT INTO ingredients(ingredient_code,ingredient_name,unit,current_stock,min_stock)
    VALUES(@key,@key,'kg',@stock,0) RETURNING ingredient_id
    """,("key","TEST-"+Guid.NewGuid().ToString("N")[..12]),("stock",stock));
async Task<FixtureRows> Fixture(decimal stock,decimal first,decimal second)
{
    var ingredient=await Ingredient(stock);
    var category=await Scalar<long>("SELECT MIN(category_id) FROM categories WHERE is_active");
    async Task<long> Product(decimal quantity)
    {
        var id=await Scalar<long>("""
            INSERT INTO products(product_code,product_name,category_id,sale_price)
            VALUES(@key,@key,@category,10000) RETURNING product_id
            """,("key","TEST-"+Guid.NewGuid().ToString("N")[..12]),("category",category));
        await Service().SaveRecipeAsync(id,[new(ingredient,quantity)]);
        return id;
    }
    return new(ingredient,await Product(first),await Product(second));
}

sealed record FixtureRows(long Ingredient,long Product1,long Product2);
sealed class TestAuthentication(long employeeId,string role):AuthenticationStateProvider
{
    public override Task<AuthenticationState> GetAuthenticationStateAsync()=>Task.FromResult(new AuthenticationState(
        new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,employeeId.ToString()),new Claim(ClaimTypes.Role,role)],"IntegrationChecks"))));
}
