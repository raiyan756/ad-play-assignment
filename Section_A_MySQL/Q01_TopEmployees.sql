-- Exactly three employees per department, or all if fewer than three exist.
WITH ranked AS (
    SELECT e.*,
           ROW_NUMBER() OVER (
               PARTITION BY DepartmentId ORDER BY Salary DESC, Id ASC
           ) AS rn
    FROM Employee e
)
SELECT Id, Name, DepartmentId, Salary, JoiningDate
FROM ranked WHERE rn <= 3
ORDER BY DepartmentId, Salary DESC, Id;

-- Include everyone tied with the third employee's salary.
WITH ranked AS (
    SELECT e.*,
           RANK() OVER (
               PARTITION BY DepartmentId ORDER BY Salary DESC
           ) AS salary_rank
    FROM Employee e
)
SELECT Id, Name, DepartmentId, Salary, JoiningDate
FROM ranked WHERE salary_rank <= 3
ORDER BY DepartmentId, Salary DESC, Id;
-- DENSE_RANK <= 3 instead means the top three DISTINCT salary levels.
