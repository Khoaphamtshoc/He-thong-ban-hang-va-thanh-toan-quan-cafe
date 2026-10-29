-- GỖ COFFEE: CÔNG THỨC MINH HỌA CHỈ DÙNG DEMO.
-- Chạy sau cafe_pos_postgresql_18.sql. Không dùng làm định lượng kinh doanh thật.
-- Không ghi đè tồn kho/nguyên liệu đã có hoặc công thức đã được thiết lập.
-- Các đơn bán trước khi thêm công thức không bị trừ kho hồi tố.
BEGIN;

INSERT INTO ingredients(ingredient_code,ingredient_name,category,supplier,unit,current_stock,min_stock)
VALUES
('NL009','Sữa đặc (demo)','Sữa và kem','Demo','L',5,1),
('NL010','Bột matcha (demo)','Trà và topping','Demo','kg',1,0.1),
('NL011','Bột chocolate (demo)','Khác','Demo','kg',2,0.2),
('NL012','Cam tươi (demo)','Trái cây','Demo','kg',5,1),
('NL013','Syrup đào (demo)','Syrup','Demo','chai',3,1),
('NL014','Syrup vải (demo)','Syrup','Demo','chai',3,1),
('NL015','Croissant thành phẩm (demo)','Bánh','Demo','cái',20,5),
('NL016','Tiramisu thành phẩm (demo)','Bánh','Demo','miếng',20,5)
ON CONFLICT (ingredient_code) DO NOTHING;

WITH sample(product_code,ingredient_code,quantity) AS (VALUES
    ('CF001','NL002',0.018),('CF001','NL001',0.100),('CF001','NL009',0.020),
    ('CF002','NL002',0.020),('CF002','NL009',0.030),
    ('CF003','NL002',0.018),
    ('CF004','NL002',0.018),('CF004','NL001',0.150),('CF004','NL005',0.050),
    ('CF005','NL002',0.025),('CF005','NL012',0.080),
    ('TE001','NL004',0.008),('TE001','NL012',0.050),('TE001','NL013',0.030),
    ('TE002','NL010',0.003),('TE002','NL001',0.160),
    ('TE003','NL004',0.008),('TE003','NL014',0.030),
    ('DX001','NL010',0.005),('DX001','NL001',0.100),('DX001','NL005',0.050),
    ('DX002','NL011',0.020),('DX002','NL001',0.120),('DX002','NL005',0.040),
    ('BK001','NL015',1.000),('BK002','NL016',1.000)
)
INSERT INTO product_ingredients(product_id,ingredient_id,quantity)
SELECT p.product_id,i.ingredient_id,s.quantity
FROM sample s
JOIN products p ON p.product_code=s.product_code
JOIN ingredients i ON i.ingredient_code=s.ingredient_code
WHERE NOT EXISTS(SELECT 1 FROM product_ingredients existing WHERE existing.product_id=p.product_id)
ON CONFLICT DO NOTHING;

COMMIT;
