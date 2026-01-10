using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Polls
{
    public partial class PollsPage : ContentPage
    {
        private readonly IPollService _pollService;
        private List<Poll> _polls = new();

        public PollsPage(IPollService pollService)
        {
            InitializeComponent();
            _pollService = pollService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadPollsAsync();
        }

        private async Task LoadPollsAsync()
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            _polls = await _pollService.GetActivePollsAsync();
            PollsCollectionView.ItemsSource = _polls;

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            RefreshView.IsRefreshing = false;
        }

        private async void OnRefreshing(object? sender, EventArgs e)
        {
            await LoadPollsAsync();
        }

        private async void OnPollSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Poll selectedPoll)
            {
                await Shell.Current.GoToAsync($"polldetails?pollId={selectedPoll.Id}");
                ((CollectionView)sender!).SelectedItem = null;
            }
        }

        private async void OnCreatePollTapped(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("createpoll");
        }
    }
}
