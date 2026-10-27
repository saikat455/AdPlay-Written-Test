namespace AdPlay.Linq;

public record DeptSalaryStats(int DepartmentId, decimal? Highest, decimal? Lowest, decimal? Average, decimal? Median);

// Q13
public static class SalaryStats
{
    // Departments that have employees
    public static List<DeptSalaryStats> Calculate(List<Employee> employees) =>
        employees
            .GroupBy(e => e.DepartmentId)
            .Select(g => Build(g.Key, g.Select(e => e.Salary).OrderBy(s => s).ToList()))  // sorted once per department
            .OrderBy(s => s.DepartmentId)
            .ToList();

    // With a department list too: empty departments come back with null stats
    public static List<DeptSalaryStats> Calculate(List<Department> departments, List<Employee> employees)
    {
        var salariesByDept = employees.ToLookup(e => e.DepartmentId, e => e.Salary);

        return departments
            .OrderBy(d => d.Id)
            .Select(d => Build(d.Id, salariesByDept[d.Id].OrderBy(s => s).ToList()))
            .ToList();
    }

    // salaries must already be sorted, so min/max/median are just index lookups
    private static DeptSalaryStats Build(int departmentId, List<decimal> salaries)
    {
        if (salaries.Count == 0)
            return new DeptSalaryStats(departmentId, null, null, null, null);

        return new DeptSalaryStats(
            departmentId,
            salaries[^1],
            salaries[0],
            salaries.Average(),
            Median(salaries));
    }

    private static decimal Median(List<decimal> sorted)
    {
        var mid = sorted.Count / 2;

        // odd count: middle value, even count: average of the two middle values
        return sorted.Count % 2 == 1
            ? sorted[mid]
            : (sorted[mid - 1] + sorted[mid]) / 2;
    }
}
