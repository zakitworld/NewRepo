using CommunityToolkit.Mvvm.ComponentModel;

namespace OnlineVoting_and_Ticketing_app.ViewModels
{
    public abstract partial class BaseViewModel : ObservableValidator
    {
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = string.Empty;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        [ObservableProperty]
        private bool _isOffline;

        public bool IsNotBusy => !IsBusy;

        protected void SetError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }

        protected void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
    }
}
