namespace AdPlay.Linq;

public record Customer(int Id, string Name);

public record Order(int Id, int CustomerId, decimal Amount, DateTime OrderDate);

public record Department(int Id, string Name);

public record Employee(int Id, string Name, int DepartmentId, decimal Salary);
