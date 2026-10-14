// Q12: Assumes Order has CustomerId, Amount (decimal) and OrderDate (DateTime).
// Customer has Id. These are snippets for the supplied collections.

var summary = orders
    .GroupBy(o => o.CustomerId)
    .Select(g => new
    {
        CustomerId = g.Key,
        TotalOrders = g.Count(),
        HighestOrderAmount = g.Max(o => o.Amount),
        AverageOrderAmount = g.Average(o => o.Amount),
        LatestOrderDate = g.Max(o => o.OrderDate)
    })
    .ToList();

// Alternative when all customers, including those with no orders, are required.
var includingEmptyCustomers = customers
    .GroupJoin(orders, c => c.Id, o => o.CustomerId, (c, group) => new
    {
        CustomerId = c.Id,
        TotalOrders = group.Count(),
        HighestOrderAmount = group.Max(o => (decimal?)o.Amount),
        AverageOrderAmount = group.Average(o => (decimal?)o.Amount),
        LatestOrderDate = group.Max(o => (DateTime?)o.OrderDate)
    })
    .ToList();
