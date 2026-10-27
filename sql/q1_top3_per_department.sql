-- Q1: Top 3 highest-paid employees per department

-- Version A: exactly 3 rows per department
-- ROW_NUMBER never gives the same number twice, so ties are broken
-- by JoiningDate (earlier joiner first), then Id to keep it deterministic
SELECT Id, Name, DepartmentId, Salary, JoiningDate
FROM (
    SELECT e.*,
           ROW_NUMBER() OVER (
               PARTITION BY DepartmentId
               ORDER BY Salary DESC, JoiningDate ASC, Id ASC
           ) AS rn
    FROM Employee e
) t
WHERE rn <= 3;

-- Version B: include everyone tied at the 3rd place
-- RANK gives equal salaries the same rank, so a dept can return more than 3 rows
-- (use DENSE_RANK if you want the top 3 distinct salary values instead)
SELECT Id, Name, DepartmentId, Salary, JoiningDate
FROM (
    SELECT e.*,
           RANK() OVER (PARTITION BY DepartmentId ORDER BY Salary DESC) AS rnk
    FROM Employee e
) t
WHERE rnk <= 3;
