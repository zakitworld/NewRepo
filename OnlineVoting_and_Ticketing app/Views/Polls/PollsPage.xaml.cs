using OnlineVoting_and_Ticketing_app.ViewModels.Polls;

namespace OnlineVoting_and_Ticketing_app.Views.Polls
{
    public partial class PollsPage : ContentPage
    {
        private readonly PollsViewModel _viewModel;

        public PollsPage(PollsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadPollsCommand.ExecuteAsync(null);
        }
    }
}
