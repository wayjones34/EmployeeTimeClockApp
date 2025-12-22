using System;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Controls;
using EmployeeTimeClockApp.Data;

namespace EmployeeTimeClockApp
{
    public partial class CreateAccountWindow : Window
    {
        private readonly int? _preselectEmployeeId;

        public CreateAccountWindow(int? preselectEmployeeId = null)
        {
            InitializeComponent();
            _preselectEmployeeId = preselectEmployeeId;

            LoadEmployees();
            PreselectEmployee();
        }

        private void LoadEmployees()
        {
            EmployeeComboBox.ItemsSource = DatabaseHelper.GetActiveEmployees();
        }

        private void PreselectEmployee()
        {
            if (_preselectEmployeeId.HasValue)
                EmployeeComboBox.SelectedValue = _preselectEmployeeId.Value;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void Create_Click(object sender, RoutedEventArgs e)
        {
            string username = UsernameTextBox.Text?.Trim();
            string password = PasswordBox.Password;
            string role = ((ComboBoxItem)RoleComboBox.SelectedItem).Content.ToString();

            if (string.IsNullOrWhiteSpace(username))
            {
                MessageBox.Show("Username is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Password is required.");
                return;
            }

            int? employeeId = null;

            // Employee role must be linked to an employee
            if (string.Equals(role, "Employee", StringComparison.OrdinalIgnoreCase))
            {
                if (EmployeeComboBox.SelectedValue == null)
                {
                    MessageBox.Show("Select an employee.");
                    return;
                }
                employeeId = (int)EmployeeComboBox.SelectedValue;
            }

            try
            {
                DatabaseHelper.CreateUserAccount(username, password, role, employeeId);
                MessageBox.Show("Account created successfully.");
                DialogResult = true;
                Close();
            }
            catch (SqlException ex) when (ex.Number == 2627 || ex.Number == 2601)
            {
                MessageBox.Show("That username already exists. Choose a different one.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create Account Error");
            }
        }
    }
}
