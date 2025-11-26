using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace EmployeeTimeClockApp
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        // Cancel button → close the login window
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // Login button → basic validation, then open MainWindow
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            // Clear any previous error
            ErrorText.Text = string.Empty;

            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            // Simple validation for now
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Please enter both username and password.";
                return;
            }

            // TODO: Replace this with real database login later

            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();

            // Close the login window after successful login
            this.Close();
        }
    }
}