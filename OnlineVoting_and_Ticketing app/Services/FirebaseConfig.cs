namespace OnlineVoting_and_Ticketing_app.Services
{
    public static class FirebaseConfig
    {
        public const string ApiKey = "YOUR_FIREBASE_API_KEY";
        public const string AuthDomain = "YOUR_PROJECT_ID.firebaseapp.com";
        public const string DatabaseUrl = "https://YOUR_PROJECT_ID.firebaseio.com";
        public const string ProjectId = "YOUR_PROJECT_ID";
        public const string StorageBucket = "YOUR_PROJECT_ID.appspot.com";
        public const string MessagingSenderId = "YOUR_MESSAGING_SENDER_ID";
        public const string AppId = "YOUR_APP_ID";

        public static string GetAuthToken()
        {
            return Preferences.Get("firebase_auth_token", string.Empty);
        }

        public static void SetAuthToken(string token)
        {
            Preferences.Set("firebase_auth_token", token);
        }

        public static void ClearAuthToken()
        {
            Preferences.Remove("firebase_auth_token");
        }
    }
}
