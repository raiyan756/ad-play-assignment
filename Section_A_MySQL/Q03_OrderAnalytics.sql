WITH analytics AS (
    SELECT Id, CustomerId, OrderDate, Amount,
           LAG(Amount) OVER (
               PARTITION BY CustomerId ORDER BY OrderDate, Id
           ) AS PreviousAmount,
           SUM(Amount) OVER (
               PARTITION BY CustomerId ORDER BY OrderDate, Id
               ROWS BETWEEN UNBOUNDED PRECEDING AND CURRENT ROW
           ) AS RunningTotal
    FROM Orders
)
SELECT Id, CustomerId, OrderDate, Amount, PreviousAmount,
       Amount - PreviousAmount AS Difference, RunningTotal
FROM analytics
ORDER BY CustomerId, OrderDate, Id;
