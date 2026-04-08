using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.ViewModels.Events;

namespace OnlineVoting_and_Ticketing_app.Views.Events
{
    public partial class EventsPage : ContentPage
    {
        private readonly EventsViewModel _viewModel;

        public EventsPage(EventsViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadEventsCommand.ExecuteAsync(null);
        }

        // ── FAB ────────────────────────────────────────────────────────────
        private async void OnCreateEventTapped(object? sender, TappedEventArgs e)
            => await _viewModel.CreateEventCommand.ExecuteAsync(null);

        // ── Search ─────────────────────────────────────────────────────────
        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
            => _viewModel.SearchText = e.NewTextValue ?? string.Empty;

        // ── Category chips ─────────────────────────────────────────────────
        private async void OnCategoryTapped(object? sender, TappedEventArgs e)
        {
            var category = e.Parameter as string ?? "All";
            await _viewModel.SelectCategoryCommand.ExecuteAsync(category);

            AllCategoryButton.BackgroundColor = category == "All"
                ? (Color)Application.Current!.Resources["Primary"]
                : Colors.Transparent;
        }

        // ── Trending carousel tap ──────────────────────────────────────────
        private async void OnTrendingEventSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Event selected)
            {
                TrendingCarousel.SelectedItem = null;
                await _viewModel.SelectEventCommand.ExecuteAsync(selected);
            }
        }

        // ── Main list selection ────────────────────────────────────────────
        private async void OnEventSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Event selected)
            {
                EventsCollectionView.SelectedItem = null;
                await _viewModel.SelectEventCommand.ExecuteAsync(selected);
            }
        }

        // ── Infinite scroll (load more when near end) ──────────────────────
        private async void OnLoadMore(object? sender, EventArgs e)
            => await _viewModel.LoadMoreEventsCommand.ExecuteAsync(null);

        // ── Pull-to-refresh ────────────────────────────────────────────────
        private async void OnRefreshing(object? sender, EventArgs e)
        {
            await _viewModel.RefreshCommand.ExecuteAsync(null);
            RefreshView.IsRefreshing = false;
        }
    }
}
