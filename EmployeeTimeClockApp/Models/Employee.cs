using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeTimeClockApp.Models
{
    public class Employee
    {
        public int EmployeeId { get; set; }
        public string BadgeNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }

        // Easy to display name in ComboBox
        public string FullName => $"{FirstName} {LastName}";
    }
}

