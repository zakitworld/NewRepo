using CommunityToolkit.Mvvm.ComponentModel;

namespace OnlineVoting_and_Ticketing_app.ViewModels
{
    public abstract partial class BaseViewModel : ObservableObject
    {
        [ObservableProperty]
        private bool _isBusy;

        [ObservableProperty]
        private string _title = string.Empty;

        public bool IsNotBusy => !IsBusy;
    }
}
