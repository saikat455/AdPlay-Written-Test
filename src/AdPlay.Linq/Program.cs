using AdPlay.Linq;

var customers = new List<Customer>
{
    new(1, "Rahim"), new(2, "Sara"), new(3, "John")   // John has no orders
};

var orders = new List<Order>
{
    new(1, 1, 100m, new DateTime(2026, 9, 1)),
    new(2, 1, 250m, new DateTime(2026, 9, 5)),
    new(3, 1, 50m,  new DateTime(2026, 9, 20)),
    new(4, 2, 500m, new DateTime(2026, 9, 2)),
    new(5, 2, 400m, new DateTime(2026, 9, 10)),
};

Console.WriteLine("Q12 - customers with orders");
foreach (var s in OrderSummary.FromOrders(orders))
    PrintOrder(s);

Console.WriteLine("\nQ12 - all customers (including no orders)");
foreach (var s in OrderSummary.FromCustomersAndOrders(customers, orders))
    PrintOrder(s);

var departments = new List<Department>
{
    new(10, "IT"), new(20, "HR"), new(30, "Finance")   // Finance is empty
};

var employees = new List<Employee>
{
    new(1, "A", 10, 50000m), new(2, "B", 10, 70000m), new(3, "C", 10, 60000m),                    // 3 -> odd
    new(4, "D", 20, 40000m), new(5, "E", 20, 45000m), new(6, "F", 20, 55000m), new(7, "G", 20, 60000m), // 4 -> even
};

Console.WriteLine("\nQ13 - salary stats");
foreach (var s in SalaryStats.Calculate(departments, employees))
    Console.WriteLine($"Dept {s.DepartmentId}: max={Show(s.Highest)} min={Show(s.Lowest)} avg={Show(s.Average)} median={Show(s.Median)}");

static string Show(decimal? value) => value?.ToString("F2") ?? "-";

static void PrintOrder(CustomerOrderSummary s) =>
    Console.WriteLine($"Customer {s.CustomerId}: orders={s.TotalOrders} highest={Show(s.HighestOrderAmount)} " +
                      $"avg={Show(s.AverageOrderAmount)} latest={s.LatestOrderDate:yyyy-MM-dd}");