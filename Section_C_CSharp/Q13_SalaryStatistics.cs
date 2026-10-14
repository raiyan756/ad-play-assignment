// Q13: Employee has DepartmentId and Salary (decimal).
// The main answer uses only the supplied List<Employee>.

var departmentIds = employees.Select(e => e.DepartmentId).Distinct();

// To include empty departments when a separate departments collection is available,
// replace the line above with: var departmentIds = departments.Select(d => d.Id).Distinct();

var salariesByDepartment = employees.ToLookup(e => e.DepartmentId, e => e.Salary);

var statistics = departmentIds.Select(departmentId =>
{
    var salaries = salariesByDepartment[departmentId].OrderBy(s => s).ToArray();
    int count = salaries.Length;

    decimal? median = count == 0 ? null
        : count % 2 == 1 ? salaries[count / 2]
        : salaries[count / 2 - 1] / 2m + salaries[count / 2] / 2m;

    return new
    {
        DepartmentId = departmentId,
        HighestSalary = count == 0 ? (decimal?)null : salaries[^1],
        LowestSalary = count == 0 ? (decimal?)null : salaries[0],
        AverageSalary = count == 0 ? (decimal?)null : salaries.Average(),
        MedianSalary = median
    };
}).ToList();
