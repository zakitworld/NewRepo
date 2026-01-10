using System.Text.RegularExpressions;

namespace OnlineVoting_and_Ticketing_app.Helpers
{
    public static class ValidationHelper
    {
        public static bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var regex = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$");
                return regex.IsMatch(email);
            }
            catch
            {
                return false;
            }
        }

        public static bool IsValidPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return false;

            return password.Length >= 8;
        }

        public static bool IsValidPhoneNumber(string phoneNumber)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber))
                return false;

            var regex = new Regex(@"^\+?[\d\s-]{10,}$");
            return regex.IsMatch(phoneNumber);
        }

        public static (bool IsValid, string? ErrorMessage) ValidateRegistration(string email, string password, string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName))
                return (false, "Full name is required");

            if (fullName.Length < 2)
                return (false, "Full name must be at least 2 characters");

            if (!IsValidEmail(email))
                return (false, "Please enter a valid email address");

            if (!IsValidPassword(password))
                return (false, "Password must be at least 8 characters");

            return (true, null);
        }

        public static (bool IsValid, string? ErrorMessage) ValidateLogin(string email, string password)
        {
            if (!IsValidEmail(email))
                return (false, "Please enter a valid email address");

            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required");

            return (true, null);
        }
    }
}
