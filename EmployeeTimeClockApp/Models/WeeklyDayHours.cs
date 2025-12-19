using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeTimeClockApp.Models
{
    public class WeeklyDayHours
    {
        public DateTime WorkDate { get; set; }
        public string Day => WorkDate.ToString("ddd MMM dd");
        public double Hours { get; set; }
    }
}