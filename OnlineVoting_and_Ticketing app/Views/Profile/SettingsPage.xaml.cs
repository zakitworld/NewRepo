using OnlineVoting_and_Ticketing_app.ViewModels.Profile;

namespace OnlineVoting_and_Ticketing_app.Views.Profile
{
    public partial class SettingsPage : ContentPage
    {
        private readonly SettingsViewModel _viewModel;

        public SettingsPage(SettingsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadSettingsCommand.ExecuteAsync(null);
        }
    }
}
