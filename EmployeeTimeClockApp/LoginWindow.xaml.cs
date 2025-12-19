using System.Windows;
using EmployeeTimeClockApp.Data;
using EmployeeTimeClockApp.Models;

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

        // Login button → validate and authenticate, then open MainWindow
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorText.Text = string.Empty;

            string username = UsernameTextBox.Text.Trim();
            string password = PasswordBox.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                ErrorText.Text = "Please enter both username and password.";
                return;
            }

            UserAccount user = DatabaseHelper.AuthenticateUser(username, password);

            if (user == null)
            {
                ErrorText.Text = "Invalid username or password.";
                return;
            }

            MainWindow mainWindow = new MainWindow(user);
            mainWindow.Show();

            this.Close();
        }
    }
}
