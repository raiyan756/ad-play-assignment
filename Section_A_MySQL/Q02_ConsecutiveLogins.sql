-- Calendar days are interpreted in the application's documented business timezone.
-- LoginDate stores those local dates/times in this exercise.
-- Return ALL overlapping seven-day windows, not maximal streaks.
WITH days AS (
    SELECT DISTINCT UserId, DATE(LoginDate) AS LoginDay FROM UserLogin
), windows AS (
    SELECT UserId, LoginDay AS StartDate,
           LEAD(LoginDay, 6) OVER (
               PARTITION BY UserId ORDER BY LoginDay
           ) AS EndDate
    FROM days
)
SELECT UserId, StartDate, EndDate
FROM windows
WHERE EndDate = DATE_ADD(StartDate, INTERVAL 6 DAY)
ORDER BY UserId, StartDate;
