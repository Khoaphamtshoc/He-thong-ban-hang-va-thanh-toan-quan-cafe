-- GỖ COFFEE: ảnh minh họa có sẵn trên Internet, nguồn trong wwwroot/images/products/sources.json.
-- Chỉ điền ảnh còn thiếu, không ghi đè ảnh đã được người dùng thiết lập.
UPDATE products p SET image_url=images.image_url
FROM (VALUES
    ('CF001','/images/products/bac-xiu.png'),
    ('CF002','/images/products/ca-phe-sua-da.png'),
    ('CF003','/images/products/espresso.jpg'),
    ('CF004','/images/products/latte-kem-muoi.webp'),
    ('CF005','/images/products/cold-brew-cam.jpg'),
    ('TE001','/images/products/tra-dao-cam-sa.png'),
    ('TE002','/images/products/matcha-latte.png'),
    ('TE003','/images/products/tra-o-long-vai.png'),
    ('DX001','/images/products/matcha-da-xay.png'),
    ('DX002','/images/products/chocolate-da-xay.png'),
    ('BK001','/images/products/croissant-bo.png'),
    ('BK002','/images/products/tiramisu.png')
) AS images(product_code,image_url)
WHERE p.product_code=images.product_code AND (p.image_url IS NULL OR BTRIM(p.image_url)='');
