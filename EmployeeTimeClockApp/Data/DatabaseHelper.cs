using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using EmployeeTimeClockApp.Models;

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
                "SELECT EmployeeId, FirstName, LastName, BadgeNumber FROM Employees WHERE IsActive = 1 ORDER BY LastName, FirstName",
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

        // 3. Clock Out
        public static void ClockOut(int employeeId)
        {
            using (var conn = new SqlConnection(ConnectionString))
            using (var cmd = new SqlCommand(
                @"UPDATE TimeEntries
                  SET ClockOutTime = SYSDATETIME()
                  WHERE EmployeeId = @EmployeeId
                    AND ClockOutTime IS NULL;", conn))
            {
                cmd.Parameters.AddWithValue("@EmployeeId", employeeId);

                conn.Open();
                cmd.ExecuteNonQuery();
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
    }
}
