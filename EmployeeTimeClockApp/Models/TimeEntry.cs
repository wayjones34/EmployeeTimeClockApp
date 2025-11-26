using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeTimeClockApp.Models
{
    public class TimeEntry
    {
        public int TimeEntryId { get; set; }
        public int EmployeeId { get; set; }
        public DateTime ClockInTime { get; set; }
        public DateTime? ClockOutTime { get; set; }

        // For display in DataGrid
        public string EmployeeName { get; set; }
    }
}