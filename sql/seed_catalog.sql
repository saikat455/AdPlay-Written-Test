-- Test data for GET /api/products. Run AFTER the EF migration (needs the tables).

INSERT INTO Categories (Name) VALUES ('Phone'), ('Laptop'), ('Accessories'), ('Audio');
INSERT INTO Brands (Name) VALUES ('Samsung'), ('Apple'), ('Dell');

SET SESSION cte_max_recursion_depth = 100000;

-- 5000 fake products
INSERT INTO CatalogProducts (Name, Price, CategoryId, BrandId, CreatedAt)
WITH RECURSIVE n AS (
    SELECT 1 AS i
    UNION ALL
    SELECT i + 1 FROM n WHERE i < 5000
)
SELECT CONCAT('Product ', i),
       ROUND(10 + (i * 37 % 990), 2),
       1 + (i % 4),
       1 + (i % 3),
       NOW() - INTERVAL (i % 365) DAY
FROM n;
