using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EmployeeTimeClockApp.Data;

namespace EmployeeTimeClockApp
{
    public partial class ResetPasswordWindow : Window
    {
        private readonly int _employeeId;
        private readonly string _employeeName;

        public ResetPasswordWindow(int employeeId, string employeeName)
        {
            InitializeComponent();
            _employeeId = employeeId;
            _employeeName = employeeName;
            TitleText.Text = $"Reset password for {_employeeName}";

            UpdateRulesUI();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => Close();

        private void PasswordBoxes_Changed(object sender, RoutedEventArgs e)
        {
            UpdateRulesUI();
        }

        private void UpdateRulesUI()
        {
            string pw = NewPasswordBox.Password ?? "";
            string confirm = ConfirmPasswordBox.Password ?? "";

            bool len = pw.Length >= 8;
            bool upper = pw.Any(char.IsUpper);
            bool lower = pw.Any(char.IsLower);
            bool number = pw.Any(char.IsDigit);
            bool special = pw.Any(ch => !char.IsLetterOrDigit(ch)); // includes @#$% etc.
            bool match = pw.Length > 0 && pw == confirm;

            SetRule(RuleLength, len);
            SetRule(RuleUpper, upper);
            SetRule(RuleLower, lower);
            SetRule(RuleNumber, number);
            SetRule(RuleSpecial, special);
            SetRule(RuleMatch, match);

            ResetButton.IsEnabled = len && upper && lower && number && special && match;
        }

        private void SetRule(TextBlock ruleText, bool pass)
        {
            ruleText.Foreground = pass ? Brushes.Green : Brushes.DarkRed;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            string pw = NewPasswordBox.Password ?? "";
            string confirm = ConfirmPasswordBox.Password ?? "";

            if (!ResetButton.IsEnabled)
            {
                MessageBox.Show("Password does not meet the rules yet.", "Validation");
                return;
            }

            if (pw != confirm)
            {
                MessageBox.Show("Passwords do not match.", "Validation");
                return;
            }

            try
            {
                DatabaseHelper.ResetUserPasswordByEmployeeId(_employeeId, pw);
                MessageBox.Show("Password reset successfully.");
                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Reset Password Error");
            }
        }
    }
}
