-- Q3: Order analytics per customer

-- Same OrderDate? Id is the tie-breaker so LAG and running total are stable.
-- ROWS frame is used so equal dates are not treated as one group.
SELECT Id,
       CustomerId,
       OrderDate,
       Amount AS CurrentAmount,
       LAG(Amount) OVER w AS PreviousAmount,
       Amount - LAG(Amount) OVER w AS DiffFromPrevious,
       SUM(Amount) OVER (w ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW) AS RunningTotal
FROM Orders
WINDOW w AS (PARTITION BY CustomerId ORDER BY OrderDate, Id)
ORDER BY CustomerId, OrderDate, Id;
