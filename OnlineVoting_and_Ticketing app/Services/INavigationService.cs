namespace OnlineVoting_and_Ticketing_app.Services
{
    /// <summary>
    /// Abstracts Shell navigation so ViewModels don't depend directly on Shell.Current,
    /// making them unit-testable and decoupled from the MAUI shell.
    /// </summary>
    public interface INavigationService
    {
        Task GoToAsync(string route);
        Task GoToAsync(string route, IDictionary<string, object> parameters);
        Task GoBackAsync();
        Task GoToRootAsync(string route);
        Task DisplayAlertAsync(string title, string message, string cancel);
        Task<bool> DisplayConfirmAsync(string title, string message, string accept, string cancel);
        Task<string?> DisplayPromptAsync(string title, string message, string accept = "OK", string cancel = "Cancel", string? placeholder = null);
    }
}
