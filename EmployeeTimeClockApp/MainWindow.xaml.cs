using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Threading;
using EmployeeTimeClockApp.Data;
using EmployeeTimeClockApp.Models;
using Microsoft.Win32;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;



namespace EmployeeTimeClockApp

{
    public partial class MainWindow : Window


    {
        private void ExportWeeklyPdf_Click(object sender, RoutedEventArgs e)
{
    var employee = GetEffectiveEmployee();
    if (employee == null)
    {
        MessageBox.Show(IsAdmin ? "Please select an employee." : "Your login is not linked to an employee.");
        return;
    }

    DateTime weekStart = GetWeekStart(DateTime.Today);
    var rows = DatabaseHelper.GetWeeklyDailyHours(employee.EmployeeId, weekStart);

    var dlg = new Microsoft.Win32.SaveFileDialog
    {
        Filter = "PDF file (*.pdf)|*.pdf",
        FileName = $"WeeklyReport_{employee.EmployeeId}_{weekStart:yyyyMMdd}.pdf"
    };

    if (dlg.ShowDialog() != true) return;

    using (var fs = new System.IO.FileStream(dlg.FileName, System.IO.FileMode.Create, System.IO.FileAccess.Write))
    {
        var pdfDoc = new iTextSharp.text.Document(iTextSharp.text.PageSize.LETTER, 36, 36, 36, 36);
        iTextSharp.text.pdf.PdfWriter.GetInstance(pdfDoc, fs);

        pdfDoc.Open();

        pdfDoc.Add(new iTextSharp.text.Paragraph("Weekly Hours Report"));
        pdfDoc.Add(new iTextSharp.text.Paragraph($"Employee: {employee.FullName} (ID: {employee.EmployeeId})"));
        pdfDoc.Add(new iTextSharp.text.Paragraph($"Week: {weekStart:MMM dd, yyyy} - {weekStart.AddDays(6):MMM dd, yyyy}"));
        pdfDoc.Add(new iTextSharp.text.Paragraph(" "));

        var table = new iTextSharp.text.pdf.PdfPTable(2)
        {
            WidthPercentage = 100
        };
        table.SetWidths(new float[] { 70f, 30f });

        table.AddCell("Day");
        table.AddCell("Hours");

        foreach (var r in rows)
        {
            table.AddCell(r.Day);
            table.AddCell(r.Hours.ToString("0.00"));
        }

        double total = rows.Sum(x => x.Hours);
        table.AddCell("Total");
        table.AddCell(total.ToString("0.00"));

        pdfDoc.Add(table);
        pdfDoc.Close();
    }


    MessageBox.Show("Weekly PDF exported successfully.", "Export");
}

        private bool IsAdmin =>
    _currentUserAccount != null &&
    string.Equals(_currentUserAccount.Role, "Admin", StringComparison.OrdinalIgnoreCase);

        private Employee GetEffectiveEmployee()
        {
            if (IsAdmin)
                return EmployeeComboBox.SelectedItem as Employee;

            // Employee login: must be tied to EmployeeId
            if (_currentUserAccount == null || _currentUserAccount.EmployeeId == null)
                return null;

            return DatabaseHelper.GetEmployeeById(_currentUserAccount.EmployeeId.Value);

        }
        private void UpdateCreateAccountButtonState()
        {
            if (!IsAdmin)
            {
                CreateAccountButton.IsEnabled = false;
                ResetPasswordButton.IsEnabled = false;
                return;
            }

            var emp = EmployeeComboBox.SelectedItem as EmployeeTimeClockApp.Models.Employee;
            if (emp == null)
            {
                CreateAccountButton.IsEnabled = false;
                ResetPasswordButton.IsEnabled = false;
                CreateAccountButton.Content = "Create Account";
                return;
            }

            bool hasAccount = DatabaseHelper.EmployeeHasUserAccount(emp.EmployeeId);

            // If they already have an account → enable Reset
            ResetPasswordButton.IsEnabled = hasAccount;

            // If no account → enable Create Account
            CreateAccountButton.IsEnabled = !hasAccount;

            CreateAccountButton.Content = hasAccount ? "Account Exists" : "Create Account";
        }

        private void CreateAccount_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("Only Admin can create accounts.");
                return;
            }

            var emp = EmployeeComboBox.SelectedItem as EmployeeTimeClockApp.Models.Employee;
            if (emp == null)
            {
                MessageBox.Show("Select an employee first.");
                return;
            }

            var win = new CreateAccountWindow(emp.EmployeeId);
            win.Owner = this;
            win.ShowDialog();
        }


        private DispatcherTimer _timer;   // ✅ REQUIRED for StartClock()
        private DispatcherTimer _refreshTimer;

        private readonly UserAccount _currentUserAccount;
        private bool _isPunchInProgress = false;


        public MainWindow(UserAccount user)
        {
            InitializeComponent();

            _currentUserAccount = user;   // ✅ set first

            AddEmployeeButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            CreateAccountButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            ResetPasswordButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;
            BadgeTextBox.IsEnabled = IsAdmin;

            EmployeeComboBox.SelectionChanged += EmployeeComboBox_SelectionChanged;
            UpdateCreateAccountButtonState();


            CreateAccountButton.Visibility = IsAdmin ? Visibility.Visible : Visibility.Collapsed;


            string displayName = _currentUserAccount.Username; // fallback

            // If employee login is linked to an EmployeeId, show their real name
            if (_currentUserAccount.EmployeeId.HasValue)
            {
                var emp = DatabaseHelper.GetEmployeeById(_currentUserAccount.EmployeeId.Value);
                if (emp != null)
                    displayName = emp.FullName;
            }

            LoggedInUserText.Text = $"Logged in as: {displayName} ({_currentUserAccount.Role})";



            LoadEmployees();
            StartClock();
            StartAutoRefresh();

        }
        private void AddEmployee_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("Only Admin can add employees.");
                return;

            }
            if (!IsAdmin)
            {
                MessageBox.Show("Access denied.");
                return;
            }


            var win = new CreateEmployeeWindow();
            win.Owner = this;

            if (win.ShowDialog() == true)
            {

                LoadEmployees(); // refresh employee dropdown
            }

        }



        public MainWindow(string username) : this(new UserAccount
        {
            Username = username,
            Role = "Unknown",
            EmployeeId = null
        })

        {
        }


        public MainWindow() : this("Unknown")
        {

        }



        private void ExportWeeklyCsv_Click(object sender, RoutedEventArgs e)
        {
            var employee = GetEffectiveEmployee();
            if (employee == null)
            {
                MessageBox.Show(IsAdmin ? "Select an employee first." : "Your login is not linked to an employee.");
                return;
            }

            DateTime weekStart = GetWeekStart(DateTime.Today);
            var rows = DatabaseHelper.GetWeeklyDailyHours(employee.EmployeeId, weekStart);

            var dlg = new SaveFileDialog
            {
                Filter = "CSV file (*.csv)|*.csv",
                FileName = $"WeeklyReport_{employee.EmployeeId}_{weekStart:yyyyMMdd}.csv"
            };

            if (dlg.ShowDialog() != true) return;

            var sb = new StringBuilder();
            sb.AppendLine("WorkDate,TotalHours");

            foreach (var r in rows)
            {
                sb.AppendLine($"{r.WorkDate:yyyy-MM-dd},{r.Hours.ToString("0.00", CultureInfo.InvariantCulture)}");
            }

            double total = rows.Sum(x => x.Hours);
            sb.AppendLine();
            sb.AppendLine($"Total,{total.ToString("0.00", CultureInfo.InvariantCulture)}");

            File.WriteAllText(dlg.FileName, sb.ToString(), Encoding.UTF8);
            MessageBox.Show("CSV exported successfully.", "Export");
        }

        private DateTime GetWeekStart(DateTime date)
        {
            int diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
            return date.Date.AddDays(-diff);
        }

        private void StartAutoRefresh()
        {
            _refreshTimer = new DispatcherTimer();
            _refreshTimer.Interval = TimeSpan.FromSeconds(60);
            _refreshTimer.Tick += (s, e) =>
            {
                var employee = GetEffectiveEmployee();
                if (employee == null) return;

                // Only refresh live if currently clocked in (open punch)
                if (DatabaseHelper.HasOpenTimeEntry(employee.EmployeeId))
                {
                    RefreshEntries();
                }
            };
            _refreshTimer.Start();
        }
        private void EmployeeComboBox_SelectionChanged(
    object sender,
    System.Windows.Controls.SelectionChangedEventArgs e)
        {
            RefreshEntries();
            UpdateCreateAccountButtonState();
        }
        private void ResetPassword_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdmin)
            {
                MessageBox.Show("Only Admin can reset passwords.");
                return;
            }

            var emp = EmployeeComboBox.SelectedItem as EmployeeTimeClockApp.Models.Employee;
            if (emp == null)
            {
                MessageBox.Show("Select an employee first.");
                return;
            }

            var win = new ResetPasswordWindow(emp.EmployeeId, emp.FullName);
            win.Owner = this;
            win.ShowDialog();
        }




        private void LoadWeeklyReport()
        {
            var employee = GetEffectiveEmployee();
            if (employee == null) return;

            DateTime weekStart = GetWeekStart(DateTime.Today);

            if (WeekLabel != null)
                WeekLabel.Text = $"Week: {weekStart:MMM dd, yyyy} - {weekStart.AddDays(6):MMM dd, yyyy}";

            // ✅ Use your existing static DatabaseHelper style
            var data = DatabaseHelper.GetWeeklyDailyHours(employee.EmployeeId, weekStart);

            if (WeeklyReportGrid != null)
                WeeklyReportGrid.ItemsSource = data;
        }






        // -------------------------------
        // Load Employee List
        // -------------------------------

        private void LoadEmployees()
        {
            try
            {
                if (IsAdmin)
                {
                    var employees = DatabaseHelper.GetActiveEmployees();
                    EmployeeComboBox.ItemsSource = employees;
                    EmployeeComboBox.IsEnabled = true;
                    BadgeTextBox.IsEnabled = true;

                    if (employees.Count > 0 && EmployeeComboBox.SelectedIndex < 0)
                        EmployeeComboBox.SelectedIndex = 0;
                }
                else
                {
                    // Employee must be linked to an EmployeeId
                    if (_currentUserAccount.EmployeeId == null)
                    {
                        StatusText.Text = "User account not linked to an EmployeeId.";
                        EmployeeComboBox.IsEnabled = false;
                        BadgeTextBox.IsEnabled = false;
                        return;
                    }

                    var emp = DatabaseHelper.GetEmployeeById(_currentUserAccount.EmployeeId.Value);

                    if (emp == null)
                    {
                        StatusText.Text = "Employee record not found.";
                        EmployeeComboBox.IsEnabled = false;
                        BadgeTextBox.IsEnabled = false;
                        return;
                    }

                    // Show only themselves
                    EmployeeComboBox.ItemsSource = new List<EmployeeTimeClockApp.Models.Employee> { emp };
                    EmployeeComboBox.SelectedIndex = 0;

                    // Lock UI for employee
                    EmployeeComboBox.IsEnabled = false;
                    BadgeTextBox.IsEnabled = false;
                }

                RefreshEntries();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error loading employees.";
                MessageBox.Show(ex.Message, "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }




        // -------------------------------
        // Start live clock on the header
        // -------------------------------
        private void StartClock()
        {
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) =>
            {
                CurrentDateText.Text = DateTime.Now.ToString("dddd, MMMM dd, yyyy");
                CurrentTimeText.Text = DateTime.Now.ToString("h:mm:ss tt");
            };
            _timer.Start();
        }

        // -------------------------------
        // Get selected employee
        // -------------------------------
        private Employee GetSelectedEmployee()
        {
            return EmployeeComboBox.SelectedItem as Employee;
        }

        // -------------------------------
        // Refresh Time Entry Grid
        // -------------------------------
        private void RefreshEntries()
        {
            var employee = GetEffectiveEmployee();
            if (employee == null)
                return;

            try
            {
                var entries = DatabaseHelper.GetTodayEntries(employee.EmployeeId);
                EntriesDataGrid.ItemsSource = entries;
                double hours = DatabaseHelper.GetTotalHoursToday(employee.EmployeeId);
                TotalHoursText.Text = $"Total Hours Today: {hours:0.00}";

                StatusText.Text = "Entries updated.";
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error loading entries.";
                MessageBox.Show(ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // -------------------------------
        // Clock In Button
        // -------------------------------
        private void ClockInButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPunchInProgress) return;
            _isPunchInProgress = true;
            ClockInButton.IsEnabled = false;
            ClockOutButton.IsEnabled = false;

            try
            {
                var employee = GetEffectiveEmployee();
                if (employee == null)
                {
                    MessageBox.Show(IsAdmin ? "Please select an employee first."
                                            : "Your login is not linked to an employee record.");
                    return;
                }

                // ✅ Block if already clocked in
                if (DatabaseHelper.HasOpenTimeEntry(employee.EmployeeId))
                {
                    StatusText.Text = "Already clocked in.";
                    MessageBox.Show($"{employee.FullName} is already clocked in.", "Punch Validation",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshEntries();
                    return;
                }

                DatabaseHelper.ClockIn(employee.EmployeeId);
                StatusText.Text = $"{employee.FullName} clocked in.";
                RefreshEntries();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error clocking in.";
                MessageBox.Show(ex.Message, "Clock In Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isPunchInProgress = false;
                ClockInButton.IsEnabled = true;
                ClockOutButton.IsEnabled = true;
            }
        }

        private void ClockOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (_isPunchInProgress) return;
            _isPunchInProgress = true;
            ClockInButton.IsEnabled = false;
            ClockOutButton.IsEnabled = false;

            try
            {
                var employee = GetEffectiveEmployee();
                if (employee == null)
                {
                    MessageBox.Show(IsAdmin ? "Please select an employee first."
                                            : "Your login is not linked to an employee record.");
                    return;
                }

                // ✅ Block if there is no active open punch
                if (!DatabaseHelper.HasOpenTimeEntry(employee.EmployeeId))
                {
                    StatusText.Text = "No active clock-in to clock out.";
                    MessageBox.Show($"{employee.FullName} has no active clock-in to clock out.", "Punch Validation",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    RefreshEntries();
                    return;
                }

                DatabaseHelper.ClockOut(employee.EmployeeId);
                StatusText.Text = $"{employee.FullName} clocked out.";
                RefreshEntries();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error clocking out.";
                MessageBox.Show(ex.Message, "Clock Out Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                _isPunchInProgress = false;
                ClockInButton.IsEnabled = true;
                ClockOutButton.IsEnabled = true;
            }
        }


    }
}