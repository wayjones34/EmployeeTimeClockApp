using System;
using System.Windows;
using EmployeeTimeClockApp.Data;

namespace EmployeeTimeClockApp
{
    public partial class CreateEmployeeWindow : Window
    {
        public CreateEmployeeWindow()
        {
            InitializeComponent();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string firstName = FirstNameTextBox.Text?.Trim();
            string lastName = LastNameTextBox.Text?.Trim();
            string badge = BadgeTextBox.Text?.Trim();

            if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
            {
                MessageBox.Show("First Name and Last Name are required.");
                return;
            }

            try
            {
                DatabaseHelper.InsertEmployee(firstName, lastName, badge);
                MessageBox.Show("Employee created successfully.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Create Employee Error");
            }
        }
    }
}
