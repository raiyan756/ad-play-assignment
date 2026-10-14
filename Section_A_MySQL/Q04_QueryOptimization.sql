-- Q4: June 2026 orders from customers in Bangladesh.
SELECT o.Id, o.CustomerId, o.ProductId, o.OrderDate, o.Amount,
       c.Name AS CustomerName, p.Name AS ProductName
FROM Orders o
JOIN Customers c ON c.Id = o.CustomerId
JOIN Products p ON p.Id = o.ProductId
WHERE o.OrderDate >= '2026-06-01'
  AND o.OrderDate <  '2026-07-01'
  AND c.Country = 'BD'
ORDER BY o.OrderDate DESC, o.Id DESC;

-- Candidate indexes. Check existing indexes before adding these.
-- Country-first plan: find customers, then their orders in the date range.
CREATE INDEX ix_customers_country_id ON Customers (Country, Id);
CREATE INDEX ix_orders_customer_date_id ON Orders (CustomerId, OrderDate, Id);

-- Alternative for a selective date range and a date-first plan.
CREATE INDEX ix_orders_date_id ON Orders (OrderDate, Id);

-- Customers.Id and Products.Id are assumed to be primary keys.
-- Compare plans and retain the indexes useful for the actual workload.
-- Prefix the rewritten SELECT with EXPLAIN ANALYZE (MySQL 8.0.18+).
