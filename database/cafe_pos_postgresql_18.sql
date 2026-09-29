
-- ============================================================
-- GỖ COFFEE - PostgreSQL 18
-- Database schema based on the current Blazor UI/source
-- ============================================================

CREATE DATABASE cafe_pos;
-- Sau khi tạo database, mở Query Tool của cafe_pos rồi chạy
-- phần bên dưới (không chạy lại CREATE DATABASE).

-- ===== 1. VAI TRÒ =====
CREATE TABLE IF NOT EXISTS roles (
    role_id       BIGSERIAL PRIMARY KEY,
    role_name     VARCHAR(50) NOT NULL UNIQUE,
    description   VARCHAR(255),
    is_active     BOOLEAN NOT NULL DEFAULT TRUE
);

-- ===== 2. NHÂN VIÊN =====
CREATE TABLE IF NOT EXISTS employees (
    employee_id   BIGSERIAL PRIMARY KEY,
    full_name     VARCHAR(150) NOT NULL,
    email         VARCHAR(150) NOT NULL UNIQUE,
    phone         VARCHAR(30),
    role_id       BIGINT REFERENCES roles(role_id),
    hire_date     DATE NOT NULL DEFAULT CURRENT_DATE,
    status        VARCHAR(30) NOT NULL DEFAULT 'ACTIVE'
                  CHECK (status IN ('ACTIVE','LEAVE','INACTIVE')),
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ===== 3. TÀI KHOẢN ĐĂNG NHẬP =====
CREATE TABLE IF NOT EXISTS user_accounts (
    user_id       BIGSERIAL PRIMARY KEY,
    employee_id   BIGINT NOT NULL UNIQUE REFERENCES employees(employee_id) ON DELETE CASCADE,
    username      VARCHAR(150) NOT NULL UNIQUE,
    password_hash VARCHAR(255) NOT NULL,
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    last_login_at TIMESTAMPTZ,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ===== 4. CA LÀM =====
CREATE TABLE IF NOT EXISTS shifts (
    shift_id      BIGSERIAL PRIMARY KEY,
    shift_name    VARCHAR(100) NOT NULL,
    start_time    TIME NOT NULL,
    end_time      TIME NOT NULL,
    description   VARCHAR(255),
    is_active     BOOLEAN NOT NULL DEFAULT TRUE
);

CREATE TABLE IF NOT EXISTS employee_shifts (
    employee_shift_id BIGSERIAL PRIMARY KEY,
    employee_id       BIGINT NOT NULL REFERENCES employees(employee_id),
    shift_id          BIGINT NOT NULL REFERENCES shifts(shift_id),
    work_date         DATE NOT NULL,
    check_in          TIMESTAMPTZ,
    check_out         TIMESTAMPTZ,
    status            VARCHAR(30) NOT NULL DEFAULT 'SCHEDULED'
                      CHECK (status IN ('SCHEDULED','WORKING','DONE','ABSENT','LEAVE')),
    UNIQUE(employee_id, shift_id, work_date)
);

-- ===== 5. DANH MỤC =====
CREATE TABLE IF NOT EXISTS categories (
    category_id   BIGSERIAL PRIMARY KEY,
    category_code VARCHAR(30) UNIQUE,
    category_name VARCHAR(100) NOT NULL UNIQUE,
    description   VARCHAR(255),
    is_active     BOOLEAN NOT NULL DEFAULT TRUE
);

-- ===== 6. SẢN PHẨM =====
CREATE TABLE IF NOT EXISTS products (
    product_id    BIGSERIAL PRIMARY KEY,
    product_code  VARCHAR(30) NOT NULL UNIQUE,
    product_name  VARCHAR(150) NOT NULL,
    description   TEXT,
    category_id   BIGINT NOT NULL REFERENCES categories(category_id),
    sale_price    NUMERIC(14,2) NOT NULL CHECK (sale_price >= 0),
    image_url     TEXT,
    is_active     BOOLEAN NOT NULL DEFAULT TRUE,
    created_at    TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at    TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ===== 7. NGUYÊN LIỆU =====
CREATE TABLE IF NOT EXISTS ingredients (
    ingredient_id   BIGSERIAL PRIMARY KEY,
    ingredient_code VARCHAR(30) NOT NULL UNIQUE,
    ingredient_name VARCHAR(150) NOT NULL UNIQUE,
    category        VARCHAR(100),
    supplier        VARCHAR(150),
    unit            VARCHAR(30) NOT NULL,
    current_stock   NUMERIC(14,3) NOT NULL DEFAULT 0 CHECK (current_stock >= 0),
    min_stock       NUMERIC(14,3) NOT NULL DEFAULT 0 CHECK (min_stock >= 0),
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    updated_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ===== 8. CÔNG THỨC: SẢN PHẨM - NGUYÊN LIỆU =====
CREATE TABLE IF NOT EXISTS product_ingredients (
    product_id      BIGINT NOT NULL REFERENCES products(product_id) ON DELETE CASCADE,
    ingredient_id   BIGINT NOT NULL REFERENCES ingredients(ingredient_id),
    quantity        NUMERIC(14,3) NOT NULL CHECK (quantity > 0),
    PRIMARY KEY (product_id, ingredient_id)
);

-- ===== 9. PHIẾU NHẬP/XUẤT KHO =====
CREATE TABLE IF NOT EXISTS inventory_transactions (
    transaction_id  BIGSERIAL PRIMARY KEY,
    ingredient_id   BIGINT NOT NULL REFERENCES ingredients(ingredient_id),
    employee_id     BIGINT REFERENCES employees(employee_id),
    transaction_type VARCHAR(20) NOT NULL
                     CHECK (transaction_type IN ('IMPORT','EXPORT','ADJUST')),
    quantity        NUMERIC(14,3) NOT NULL CHECK (quantity > 0),
    unit_cost       NUMERIC(14,2) DEFAULT 0 CHECK (unit_cost >= 0),
    reference_note  VARCHAR(255),
    created_at      TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ===== 10. KHÁCH HÀNG =====
CREATE TABLE IF NOT EXISTS customers (
    customer_id    BIGSERIAL PRIMARY KEY,
    full_name      VARCHAR(150),
    phone          VARCHAR(30),
    email          VARCHAR(150),
    customer_type  VARCHAR(30) NOT NULL DEFAULT 'WALK_IN'
                   CHECK (customer_type IN ('WALK_IN','MEMBER')),
    created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ===== 11. ĐƠN HÀNG =====
CREATE TABLE IF NOT EXISTS orders (
    order_id        BIGSERIAL PRIMARY KEY,
    order_code      VARCHAR(30) NOT NULL UNIQUE,
    employee_id     BIGINT REFERENCES employees(employee_id),
    customer_id     BIGINT REFERENCES customers(customer_id),
    table_no        VARCHAR(30),
    note            TEXT,
    order_status    VARCHAR(30) NOT NULL DEFAULT 'PREPARING'
                    CHECK (order_status IN ('PENDING','PREPARING','COMPLETED','CANCELLED')),
    subtotal        NUMERIC(14,2) NOT NULL DEFAULT 0,
    vat_rate        NUMERIC(5,2) NOT NULL DEFAULT 8.00,
    vat_amount      NUMERIC(14,2) NOT NULL DEFAULT 0,
    discount_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    total_amount    NUMERIC(14,2) NOT NULL DEFAULT 0,
    ordered_at      TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    completed_at    TIMESTAMPTZ
);

-- ===== 12. CHI TIẾT ĐƠN =====
CREATE TABLE IF NOT EXISTS order_items (
    order_item_id BIGSERIAL PRIMARY KEY,
    order_id      BIGINT NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    product_id    BIGINT NOT NULL REFERENCES products(product_id),
    quantity      NUMERIC(10,2) NOT NULL CHECK (quantity > 0),
    unit_price    NUMERIC(14,2) NOT NULL CHECK (unit_price >= 0),
    note          VARCHAR(255),
    line_total    NUMERIC(14,2) NOT NULL CHECK (line_total >= 0)
);

-- ===== 13. THANH TOÁN =====
CREATE TABLE IF NOT EXISTS payments (
    payment_id      BIGSERIAL PRIMARY KEY,
    order_id        BIGINT NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    payment_method  VARCHAR(30) NOT NULL
                    CHECK (payment_method IN ('CASH','BANK_TRANSFER','CARD')),
    amount          NUMERIC(14,2) NOT NULL CHECK (amount >= 0),
    payment_status  VARCHAR(30) NOT NULL DEFAULT 'PAID'
                    CHECK (payment_status IN ('PENDING','PAID','FAILED','REFUNDED')),
    transaction_ref VARCHAR(100),
    paid_at         TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- ===== 14. KHUYẾN MÃI =====
CREATE TABLE IF NOT EXISTS promotions (
    promotion_id    BIGSERIAL PRIMARY KEY,
    promotion_code  VARCHAR(50) NOT NULL UNIQUE,
    promotion_name  VARCHAR(150) NOT NULL,
    discount_type   VARCHAR(20) NOT NULL
                    CHECK (discount_type IN ('PERCENT','FIXED')),
    discount_value  NUMERIC(14,2) NOT NULL CHECK (discount_value >= 0),
    start_at        TIMESTAMPTZ NOT NULL,
    end_at          TIMESTAMPTZ NOT NULL,
    is_active       BOOLEAN NOT NULL DEFAULT TRUE,
    CHECK (end_at > start_at)
);

CREATE TABLE IF NOT EXISTS order_promotions (
    order_id      BIGINT NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
    promotion_id  BIGINT NOT NULL REFERENCES promotions(promotion_id),
    discount_amount NUMERIC(14,2) NOT NULL DEFAULT 0,
    PRIMARY KEY (order_id, promotion_id)
);

-- ===== INDEX =====
CREATE INDEX IF NOT EXISTS idx_products_category ON products(category_id);
CREATE INDEX IF NOT EXISTS idx_orders_date ON orders(ordered_at);
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(order_status);
CREATE INDEX IF NOT EXISTS idx_order_items_order ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_inventory_ingredient ON inventory_transactions(ingredient_id);
CREATE INDEX IF NOT EXISTS idx_employee_shifts_date ON employee_shifts(work_date);
CREATE INDEX IF NOT EXISTS idx_payments_method ON payments(payment_method);

-- ===== DỮ LIỆU BAN ĐẦU =====
INSERT INTO roles(role_name, description) VALUES
('ADMIN','Quản trị hệ thống'),
('MANAGER','Quản lý cửa hàng'),
('STAFF','Nhân viên bán hàng'),
('WAREHOUSE','Nhân viên kho')
ON CONFLICT (role_name) DO NOTHING;

INSERT INTO shifts(shift_name,start_time,end_time,description) VALUES
('Ca sáng','07:00','15:00','Ca làm buổi sáng'),
('Ca chiều','15:00','23:00','Ca làm buổi chiều'),
('Ca linh hoạt','08:00','22:00','Ca linh hoạt theo phân công')
ON CONFLICT DO NOTHING;

INSERT INTO categories(category_code,category_name) VALUES
('CF','Cà phê'),
('TE','Trà'),
('DX','Đá xay'),
('BK','Bánh')
ON CONFLICT (category_name) DO NOTHING;

INSERT INTO customers(full_name, customer_type)
SELECT 'Khách lẻ','WALK_IN'
WHERE NOT EXISTS (
    SELECT 1 FROM customers WHERE customer_type='WALK_IN' AND full_name='Khách lẻ'
);

INSERT INTO employees(full_name,email,role_id,hire_date,status)
SELECT 'Anh Duy','anhduy@moc.coffee',r.role_id,'2024-03-12','ACTIVE'
FROM roles r WHERE r.role_name='ADMIN'
AND NOT EXISTS (SELECT 1 FROM employees WHERE email='anhduy@moc.coffee');

INSERT INTO employees(full_name,email,role_id,hire_date,status)
SELECT 'Hoàng Nam','hoangnam@moc.coffee',r.role_id,'2024-06-08','ACTIVE'
FROM roles r WHERE r.role_name='MANAGER'
AND NOT EXISTS (SELECT 1 FROM employees WHERE email='hoangnam@moc.coffee');

INSERT INTO employees(full_name,email,role_id,hire_date,status)
SELECT 'Thảo Vy','thaovy@moc.coffee',r.role_id,'2024-07-22','ACTIVE'
FROM roles r WHERE r.role_name='MANAGER'
AND NOT EXISTS (SELECT 1 FROM employees WHERE email='thaovy@moc.coffee');

INSERT INTO employees(full_name,email,role_id,hire_date,status)
SELECT 'Quốc Bảo','quocbao@moc.coffee',r.role_id,'2025-01-14','ACTIVE'
FROM roles r WHERE r.role_name='STAFF'
AND NOT EXISTS (SELECT 1 FROM employees WHERE email='quocbao@moc.coffee');

INSERT INTO employees(full_name,email,role_id,hire_date,status)
SELECT 'Hà Linh','halinh@moc.coffee',r.role_id,'2025-04-02','ACTIVE'
FROM roles r WHERE r.role_name='STAFF'
AND NOT EXISTS (SELECT 1 FROM employees WHERE email='halinh@moc.coffee');

INSERT INTO employees(full_name,email,role_id,hire_date,status)
SELECT 'Khánh An','khanhan@moc.coffee',r.role_id,'2025-11-17','LEAVE'
FROM roles r WHERE r.role_name='STAFF'
AND NOT EXISTS (SELECT 1 FROM employees WHERE email='khanhan@moc.coffee');

-- Tài khoản demo hiện tại.
-- Mật khẩu demo đang để dạng plaintext để tương thích với code Blazor hiện tại.
-- Sau khi kết nối ổn định, nên đổi sang bcrypt/ASP.NET Identity.
INSERT INTO user_accounts(employee_id,username,password_hash)
SELECT e.employee_id,'admin@moc.coffee','Admin@123456'
FROM employees e
WHERE e.email='anhduy@moc.coffee'
AND NOT EXISTS (SELECT 1 FROM user_accounts WHERE username='admin@moc.coffee');

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'CF001','Bạc xỉu','Cà phê sữa đá truyền thống',category_id,49000,TRUE
FROM categories WHERE category_code='CF'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'CF002','Cà phê sữa đá','Cà phê sữa đá',category_id,39000,TRUE
FROM categories WHERE category_code='CF'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'CF003','Espresso','Espresso nguyên chất',category_id,35000,TRUE
FROM categories WHERE category_code='CF'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'CF004','Latte kem muối','Cà phê espresso, kem mặn',category_id,70000,TRUE
FROM categories WHERE category_code='CF'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'TE001','Trà đào cam sả','Trà đào tươi, sả, cam',category_id,55000,TRUE
FROM categories WHERE category_code='TE'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'CF005','Cold brew cam','Cà phê ủ lạnh vị cam',category_id,70000,TRUE
FROM categories WHERE category_code='CF'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'TE002','Matcha latte','Matcha Nhật, sữa tươi',category_id,65000,TRUE
FROM categories WHERE category_code='TE'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'TE003','Trà ô long vải','Trà ô long vải',category_id,59000,TRUE
FROM categories WHERE category_code='TE'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'DX001','Matcha đá xay','Matcha đá xay',category_id,68000,TRUE
FROM categories WHERE category_code='DX'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'DX002','Chocolate đá xay','Chocolate đá xay',category_id,69000,TRUE
FROM categories WHERE category_code='DX'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'BK001','Croissant bơ','Bánh sừng bò bơ Pháp',category_id,38000,TRUE
FROM categories WHERE category_code='BK'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO products(product_code,product_name,description,category_id,sale_price,is_active)
SELECT 'BK002','Tiramisu','Tiramisu',category_id,52000,TRUE
FROM categories WHERE category_code='BK'
ON CONFLICT (product_code) DO NOTHING;

INSERT INTO ingredients(ingredient_code,ingredient_name,category,supplier,unit,current_stock,min_stock)
VALUES
('NL001','Sữa tươi thanh trùng','Sữa và kem','Vinamilk','L',2,5),
('NL002','Hạt cà phê Arabica','Cà phê','Cầu Đất','kg',1,3),
('NL003','Syrup caramel Monin','Syrup','Monin','chai',0,2),
('NL004','Trà ô long đặc biệt','Trà và topping','Bảo Lộc','kg',4,2),
('NL005','Kem béo thực vật','Sữa và kem','Rich''s','hộp',12,4),
('NL006','Đường cát trắng','Khác','Biên Hòa','kg',8,3),
('NL007','Trân châu đen','Trà và topping','Đài Loan','kg',3,2),
('NL008','Syrup vani Monin','Syrup','Monin','chai',2,2)
ON CONFLICT (ingredient_code) DO NOTHING;

-- ===== KIỂM TRA NHANH =====
SELECT 'roles' AS table_name, COUNT(*) AS rows FROM roles
UNION ALL SELECT 'employees', COUNT(*) FROM employees
UNION ALL SELECT 'products', COUNT(*) FROM products
UNION ALL SELECT 'ingredients', COUNT(*) FROM ingredients
UNION ALL SELECT 'customers', COUNT(*) FROM customers;
