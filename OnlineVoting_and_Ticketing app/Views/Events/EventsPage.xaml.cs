using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Events
{
    public partial class EventsPage : ContentPage
    {
        private readonly IEventService _eventService;
        private List<Event> _allEvents = new();
        private List<Event> _filteredEvents = new();

        public EventsPage(IEventService eventService)
        {
            InitializeComponent();
            _eventService = eventService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadEventsAsync();
        }

        private async Task LoadEventsAsync()
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            _allEvents = await _eventService.GetUpcomingEventsAsync();
            _filteredEvents = _allEvents;
            EventsCollectionView.ItemsSource = _filteredEvents;

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            RefreshView.IsRefreshing = false;
        }

        private async void OnRefreshing(object? sender, EventArgs e)
        {
            await LoadEventsAsync();
        }

        private void OnSearchTextChanged(object? sender, TextChangedEventArgs e)
        {
            var searchText = e.NewTextValue?.ToLower() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(searchText))
            {
                _filteredEvents = _allEvents;
            }
            else
            {
                _filteredEvents = _allEvents
                    .Where(ev => ev.Title.ToLower().Contains(searchText) ||
                                ev.Description.ToLower().Contains(searchText) ||
                                ev.Location.ToLower().Contains(searchText))
                    .ToList();
            }

            EventsCollectionView.ItemsSource = _filteredEvents;
        }

        private async void OnCategoryTapped(object? sender, EventArgs e)
        {
            if (sender is not TapGestureRecognizer gesture || gesture.CommandParameter is not string category)
                return;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            if (category == "All")
            {
                _filteredEvents = _allEvents;
            }
            else
            {
                if (Enum.TryParse<EventCategory>(category, out var eventCategory))
                {
                    _filteredEvents = _allEvents.Where(e => e.Category == eventCategory).ToList();
                }
            }

            EventsCollectionView.ItemsSource = _filteredEvents;

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
        }

        private async void OnEventSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Event selectedEvent)
            {
                await Shell.Current.GoToAsync($"eventdetails?eventId={selectedEvent.Id}");
                ((CollectionView)sender!).SelectedItem = null;
            }
        }

        private async void OnCreateEventTapped(object? sender, EventArgs e)
        {
            try
            {
                await Shell.Current.GoToAsync("createevent");
            }
            catch (Exception ex)
            {
                await DisplayAlert("Error", $"Unable to open create event page: {ex.Message}", "OK");
                System.Diagnostics.Debug.WriteLine($"Navigation error: {ex}");
            }
        }
    }
}
