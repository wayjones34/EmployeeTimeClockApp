using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeTimeClockApp.Models
{
    public class UserAccount
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Role { get; set; }          // "Admin" or "Employee"
        public int? EmployeeId { get; set; }      // null for admin accounts
    }
}
