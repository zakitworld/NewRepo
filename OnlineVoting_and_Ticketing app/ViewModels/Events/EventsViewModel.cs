using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;
using System.Collections.ObjectModel;

namespace OnlineVoting_and_Ticketing_app.ViewModels.Events
{
    public partial class EventsViewModel : BaseViewModel
    {
        private readonly IEventService _eventService;
        private readonly INavigationService _navigation;
        private readonly IConnectivityService _connectivity;
        private readonly ILogger<EventsViewModel> _logger;

        private int _currentPage = 1;
        private const int PageSize = 20;
        private bool _hasMorePages = true;

        [ObservableProperty]
        private ObservableCollection<Event> _events = [];

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SearchCommand))]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedCategory = "All";

        [ObservableProperty]
        private bool _isLoadingMore;

        public EventsViewModel(
            IEventService eventService,
            INavigationService navigation,
            IConnectivityService connectivity,
            ILogger<EventsViewModel> logger)
        {
            _eventService = eventService;
            _navigation = navigation;
            _connectivity = connectivity;
            _logger = logger;

            Title = "Events";
            IsOffline = !_connectivity.IsConnected;
            _connectivity.ConnectivityChanged += (_, isOnline) => IsOffline = !isOnline;
        }

        [RelayCommand]
        private async Task LoadEventsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ClearError();
            _currentPage = 1;
            _hasMorePages = true;

            try
            {
                List<Event> loaded;
                if (!string.IsNullOrWhiteSpace(SearchText))
                    loaded = await _eventService.SearchEventsAsync(SearchText, _currentPage, PageSize);
                else if (SelectedCategory != "All" && Enum.TryParse<EventCategory>(SelectedCategory, out var cat))
                    loaded = await _eventService.GetEventsByCategoryAsync(cat, _currentPage, PageSize);
                else
                    loaded = await _eventService.GetUpcomingEventsAsync(_currentPage, PageSize);

                Events = new ObservableCollection<Event>(loaded);
                _hasMorePages = loaded.Count == PageSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadEventsAsync failed");
                SetError("Failed to load events. Pull down to retry.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadMoreEventsAsync()
        {
            if (IsBusy || IsLoadingMore || !_hasMorePages) return;
            IsLoadingMore = true;

            try
            {
                _currentPage++;
                List<Event> more;

                if (!string.IsNullOrWhiteSpace(SearchText))
                    more = await _eventService.SearchEventsAsync(SearchText, _currentPage, PageSize);
                else if (SelectedCategory != "All" && Enum.TryParse<EventCategory>(SelectedCategory, out var cat))
                    more = await _eventService.GetEventsByCategoryAsync(cat, _currentPage, PageSize);
                else
                    more = await _eventService.GetUpcomingEventsAsync(_currentPage, PageSize);

                foreach (var ev in more)
                    Events.Add(ev);

                _hasMorePages = more.Count == PageSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadMoreEventsAsync failed");
                _currentPage--;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        private async Task SearchAsync()
        {
            await LoadEventsAsync();
        }

        [RelayCommand]
        private async Task SelectCategoryAsync(string category)
        {
            if (SelectedCategory == category) return;
            SelectedCategory = category;
            await LoadEventsAsync();
        }

        [RelayCommand]
        private async Task SelectEventAsync(Event selectedEvent)
        {
            if (selectedEvent is null) return;
            await _navigation.GoToAsync("eventdetails",
                new Dictionary<string, object> { ["eventId"] = selectedEvent.Id });
        }

        [RelayCommand]
        private async Task CreateEventAsync() =>
            await _navigation.GoToAsync("createevent");

        [RelayCommand]
        private async Task RefreshAsync() =>
            await LoadEventsAsync();
    }
}
