namespace OnlineVoting_and_Ticketing_app.Services
{
    public class NavigationService : INavigationService
    {
        public Task GoToAsync(string route) =>
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route));

        public Task GoToAsync(string route, IDictionary<string, object> parameters) =>
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(route, parameters));

        public Task GoBackAsync() =>
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync(".."));

        public Task GoToRootAsync(string route) =>
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.GoToAsync($"//{route}"));

        public Task DisplayAlertAsync(string title, string message, string cancel) =>
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync(title, message, cancel));

        public Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel) =>
            MainThread.InvokeOnMainThreadAsync(() => Shell.Current.DisplayAlertAsync(title, message, accept, cancel));

        public Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string? placeholder = null) =>
            MainThread.InvokeOnMainThreadAsync(() =>
                Shell.Current.DisplayPromptAsync(title, message, accept, cancel, placeholder));
    }
}
