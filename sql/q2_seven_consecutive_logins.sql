-- Q2: Users who logged in 7 consecutive days

-- Idea: number the distinct dates per user. Date minus row number stays
-- the same for consecutive days, so it works as a group key (gaps & islands).
-- Duplicate logins on the same day are removed with DISTINCT.
-- Each streak is returned once (non-overlapping), start to end of the streak.
WITH daily AS (
    SELECT DISTINCT UserId, DATE(LoginDate) AS LoginDay
    FROM UserLogin
),
grouped AS (
    SELECT UserId, LoginDay,
           DATE_SUB(LoginDay, INTERVAL ROW_NUMBER() OVER (
               PARTITION BY UserId ORDER BY LoginDay
           ) DAY) AS grp
    FROM daily
)
SELECT UserId,
       MIN(LoginDay) AS StartDate,
       MAX(LoginDay) AS EndDate
FROM grouped
GROUP BY UserId, grp
HAVING COUNT(*) >= 7;


