namespace Project.Constants
{
    public static class AuthenticationConstants
    {
        public const string ValidUsername = "Eliza";
        public const string ValidPassword = "Eliza123456";
        public static class ErrorMessages
        {
            public const string EmptyFields = "Please fill in all fields!";
            public const string InvalidCredentials = "Invalid username or password!";
        }
        public static class WindowTitles
        {
            public const string Login = "Sign In";
            public const string Dashboard = "Dashboard";
        }
        public static class Labels
        {
            public const string Username = "Username";
            public const string Password = "Password";
            public const string SignIn = "Sign In";
            public const string SignInTitle = "Welcome Back";
            public const string Logout = "Logout";
            public const string WelcomeMessage = "Welcome, Eliza!";
            public const string SubtitleMessage = "You have successfully signed in";
        }
    }
} 