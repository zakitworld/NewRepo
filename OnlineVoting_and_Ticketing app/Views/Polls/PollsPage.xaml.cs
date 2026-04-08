using OnlineVoting_and_Ticketing_app.Models;
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

        // --- FAB ---
        private async void OnCreatePollTapped(object? sender, TappedEventArgs e)
            => await _viewModel.CreatePollCommand.ExecuteAsync(null);

        // --- List selection ---
        private async void OnPollSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Poll selected)
            {
                PollsCollectionView.SelectedItem = null;
                await _viewModel.SelectPollCommand.ExecuteAsync(selected);
            }
        }

        // --- Pull-to-refresh ---
        private async void OnRefreshing(object? sender, EventArgs e)
        {
            await _viewModel.RefreshCommand.ExecuteAsync(null);
            RefreshView.IsRefreshing = false;
        }
    }
}
