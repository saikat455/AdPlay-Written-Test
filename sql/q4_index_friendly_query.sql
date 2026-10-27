-- Q4: Index-friendly rewrite

-- Problem: YEAR(o.OrderDate) / MONTH(o.OrderDate) wrap the column in a function,
-- so MySQL can't use an index on OrderDate and scans the whole table.
-- SELECT * also pulls every column of 3 tables for nothing.

-- Rewrite: plain range on the raw column + only the columns we need
SELECT o.Id, o.OrderDate, o.Amount, o.Status,
       c.Name AS CustomerName, c.Country,
       p.Name AS ProductName
FROM Orders o
JOIN Customers c ON c.Id = o.CustomerId
JOIN Products  p ON p.Id = o.ProductId
WHERE o.OrderDate >= '2026-06-01'
  AND o.OrderDate <  '2026-07-01'   -- half-open range, safe for DATETIME
  AND c.Country = 'BD'
ORDER BY o.OrderDate DESC;

-- Indexes
-- 1) Orders: range column first, then the join column.
--    Range scan on OrderDate also gives us the sort order for free.
CREATE INDEX idx_orders_date_customer ON Orders (OrderDate, CustomerId);

-- 2) Orders: for the plan that starts from customers (few BD customers)
--    equality column (CustomerId) first, then the range column (OrderDate)
CREATE INDEX idx_orders_customer_date ON Orders (CustomerId, OrderDate);

-- 3) Customers: filter column
CREATE INDEX idx_customers_country ON Customers (Country);

