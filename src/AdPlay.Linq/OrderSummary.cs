namespace AdPlay.Linq;

public record CustomerOrderSummary(
    int CustomerId,
    int TotalOrders,
    decimal? HighestOrderAmount,
    decimal? AverageOrderAmount,
    DateTime? LatestOrderDate);

// Q12
public static class OrderSummary
{
    // One row per customer who has orders
    public static List<CustomerOrderSummary> FromOrders(List<Order> orders) =>
        orders
            .GroupBy(o => o.CustomerId)
            .Select(g => new CustomerOrderSummary(
                g.Key,
                g.Count(),
                g.Max(o => o.Amount),
                g.Average(o => o.Amount),
                g.Max(o => o.OrderDate)))
            .OrderBy(s => s.CustomerId)
            .ToList();

    // Customers and orders in separate lists: a group join keeps customers with no orders.
    // They get 0 orders and null amounts/date (nullable casts, Max/Average on an empty group would throw).
    public static List<CustomerOrderSummary> FromCustomersAndOrders(List<Customer> customers, List<Order> orders) =>
        (from c in customers
         join o in orders on c.Id equals o.CustomerId into g
         orderby c.Id
         select new CustomerOrderSummary(
             c.Id,
             g.Count(),
             g.Max(o => (decimal?)o.Amount),
             g.Average(o => (decimal?)o.Amount),
             g.Max(o => (DateTime?)o.OrderDate)))
        .ToList();
}
