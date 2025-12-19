using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using EmployeeTimeClockApp.Models;
using EmployeeTimeClockApp.Data;

namespace EmployeeTimeClockApp.Data
{
    public static class DatabaseHelper

    {
        private const string ConnectionString =
            @"Data Source=WAYJONES\SQLEXPRESS;Initial Catalog=EmployeeTimeClockDB;Integrated Security=True;";

        // 1. Get all active employees
        public static List<Employee> GetActiveEmployees()
        {
            var employees = new List<Employee>();

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                "SELECT EmployeeId, FirstName, LastName, BadgeNumber " +
                "FROM Employees WHERE IsActive = 1 ORDER BY LastName, FirstName",
                conn))
            {
                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        employees.Add(new Employee
                        {
                            EmployeeId = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            BadgeNumber = reader.GetString(3)
                        });
                    }
                }
            }

            return employees;
        }

        // 1b. Get a single employee by ID (used for employee-locked logins)
        public static Employee GetEmployeeById(int employeeId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                @"SELECT EmployeeId, FirstName, LastName, BadgeNumber
          FROM Employees
          WHERE EmployeeId = @EmployeeId;", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Employee
                        {
                            EmployeeId = reader.GetInt32(0),
                            FirstName = reader.GetString(1),
                            LastName = reader.GetString(2),
                            BadgeNumber = reader.GetString(3)
                        };
                    }
                }
            }

            return null;
        }
        public static List<WeeklyDayHours> GetWeeklyDailyHours(int employeeId, DateTime weekStart)
        {
            var result = new List<WeeklyDayHours>();
            DateTime weekEnd = weekStart.AddDays(7);

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(@"
        ;WITH x AS (
            SELECT
                CAST(ClockInTime AS date) AS WorkDate,
                DATEDIFF(MINUTE, ClockInTime, ISNULL(ClockOutTime, GETDATE())) / 60.0 AS HoursWorked
            FROM TimeEntries
            WHERE EmployeeId = @EmployeeId
              AND ClockInTime >= @WeekStart
              AND ClockInTime < @WeekEnd
        )
        SELECT WorkDate, SUM(HoursWorked) AS TotalHours
        FROM x
        GROUP BY WorkDate
        ORDER BY WorkDate;", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                cmd.Parameters.AddWithValue("@WeekStart", weekStart);
                cmd.Parameters.AddWithValue("@WeekEnd", weekEnd);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        result.Add(new WeeklyDayHours
                        {
                            WorkDate = (DateTime)reader["WorkDate"],
                            Hours = Convert.ToDouble(reader["TotalHours"])
                        });
                    }
                }
            }

            return result;
        }



        // 2. Clock In
        public static void ClockIn(int employeeId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                @"INSERT INTO TimeEntries (EmployeeId, ClockInTime)
                  VALUES (@EmployeeId, SYSDATETIME());", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }


        // 3. Clock Out (close ONLY the most recent open entry)
        public static void ClockOut(int employeeId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                @"UPDATE TimeEntries
          SET ClockOutTime = SYSDATETIME()
          WHERE TimeEntryId = (
              SELECT TOP (1) TimeEntryId
              FROM TimeEntries
              WHERE EmployeeId = @EmployeeId
                AND ClockOutTime IS NULL
              ORDER BY ClockInTime DESC
          );", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);
                conn.Open();
                cmd.ExecuteNonQuery();
            }
        }
        // Total hours worked today (multiple shifts; open shift counts up to now)
        public static double GetTotalHoursToday(int employeeId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                @"SELECT SUM(DATEDIFF(SECOND, ClockInTime, ISNULL(ClockOutTime, SYSDATETIME())))
          FROM TimeEntries
          WHERE EmployeeId = @EmployeeId
            AND CAST(ClockInTime AS DATE) = CAST(SYSDATETIME() AS DATE);", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                conn.Open();
                object result = cmd.ExecuteScalar();

                if (result == null || result == DBNull.Value)
                    return 0.0;

                int totalSeconds = Convert.ToInt32(result);
                return totalSeconds / 3600.0;
            }
        }







        // 4. Get today's entries for an employee
        public static List<TimeEntry> GetTodayEntries(int employeeId)
        {
            var entries = new List<TimeEntry>();

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                @"SELECT t.TimeEntryId,
                         t.ClockInTime,
                         t.ClockOutTime,
                         e.FirstName,
                         e.LastName
                  FROM TimeEntries t
                  JOIN Employees e ON t.EmployeeId = e.EmployeeId
                  WHERE t.EmployeeId = @EmployeeId
                    AND CAST(t.ClockInTime AS DATE) = CAST(SYSDATETIME() AS DATE)
                  ORDER BY t.ClockInTime;", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        entries.Add(new TimeEntry
                        {
                            TimeEntryId = reader.GetInt32(0),
                            ClockInTime = reader.GetDateTime(1),
                            ClockOutTime = reader.IsDBNull(2) ? (DateTime?)null : reader.GetDateTime(2),
                            EmployeeName = $"{reader.GetString(3)} {reader.GetString(4)}"
                        });
                    }
                }
            }



            return entries;
        }
        // Check if employee currently has an open clock-in (no ClockOutTime yet)
        public static bool HasOpenTimeEntry(int employeeId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                @"SELECT COUNT(1)
          FROM TimeEntries
          WHERE EmployeeId = @EmployeeId
            AND ClockOutTime IS NULL;", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                conn.Open();
                int count = (int)cmd.ExecuteScalar();
                return count > 0;
            }
        }


        // 5. Authenticate user (Admin or Employee) from Users table
        public static UserAccount AuthenticateUser(string username, string password)
        {
            UserAccount user = null;

            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                @"SELECT UserId, Username, Role, EmployeeId
                  FROM Users
                  WHERE Username = @Username AND Password = @Password;", conn))
            {
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@Password", password);

                conn.Open();
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        user = new UserAccount
                        {
                            UserId = reader.GetInt32(0),
                            Username = reader.GetString(1),
                            Role = reader.GetString(2),
                            EmployeeId = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3)
                        };
                    }
                }
            }



            // null means login failed
            return user;
        }
    }
}

