using OnlineVoting_and_Ticketing_app.ViewModels.Profile;

namespace OnlineVoting_and_Ticketing_app.Views.Profile
{
    public partial class ProfilePage : ContentPage
    {
        private readonly ProfileViewModel _viewModel;

        public ProfilePage(ProfileViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadProfileCommand.ExecuteAsync(null);
        }
    }
}
