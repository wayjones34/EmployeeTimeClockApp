using System;
using System.Windows;
using System.Windows.Threading;
using EmployeeTimeClockApp.Data;
using EmployeeTimeClockApp.Models;

namespace EmployeeTimeClockApp
{
    public partial class MainWindow : Window
    {
        private DispatcherTimer _timer;

        public MainWindow()
        {
            InitializeComponent();
            LoadEmployees();
            StartClock();
        }

        // -------------------------------
        // Load Employee List
        // -------------------------------
        private void LoadEmployees()
        {
            try
            {
                var employees = DatabaseHelper.GetActiveEmployees();
                EmployeeComboBox.ItemsSource = employees;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error loading employees.";
                MessageBox.Show(ex.Message, "Database Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
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
            var employee = GetSelectedEmployee();
            if (employee == null)
                return;

            try
            {
                var entries = DatabaseHelper.GetTodayEntries(employee.EmployeeId);
                EntriesDataGrid.ItemsSource = entries;
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
            var employee = GetSelectedEmployee();
            if (employee == null)
            {
                MessageBox.Show("Please select an employee first.");
                return;
            }

            try
            {
                DatabaseHelper.ClockIn(employee.EmployeeId);
                StatusText.Text = $"{employee.FullName} clocked in.";
                RefreshEntries();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error clocking in.";
                MessageBox.Show(ex.Message, "Clock In Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // -------------------------------
        // Clock Out Button
        // -------------------------------
        private void ClockOutButton_Click(object sender, RoutedEventArgs e)
        {
            var employee = GetSelectedEmployee();
            if (employee == null)
            {
                MessageBox.Show("Please select an employee first.");
                return;
            }

            try
            {
                DatabaseHelper.ClockOut(employee.EmployeeId);
                StatusText.Text = $"{employee.FullName} clocked out.";
                RefreshEntries();
            }
            catch (Exception ex)
            {
                StatusText.Text = "Error clocking out.";
                MessageBox.Show(ex.Message, "Clock Out Error",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
