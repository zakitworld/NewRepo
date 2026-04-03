using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;
using System.Collections.ObjectModel;

namespace OnlineVoting_and_Ticketing_app.ViewModels.Tickets
{
    public partial class TicketsViewModel : BaseViewModel
    {
        private readonly ITicketService _ticketService;
        private readonly INavigationService _navigation;
        private readonly IConnectivityService _connectivity;
        private readonly ILogger<TicketsViewModel> _logger;

        private int _currentPage = 1;
        private const int PageSize = 20;
        private bool _hasMorePages = true;

        [ObservableProperty]
        private ObservableCollection<Ticket> _tickets = [];

        [ObservableProperty]
        private string _ticketCountText = "0 tickets";

        [ObservableProperty]
        private bool _hasTickets;

        [ObservableProperty]
        private bool _isLoadingMore;

        public TicketsViewModel(
            ITicketService ticketService,
            INavigationService navigation,
            IConnectivityService connectivity,
            ILogger<TicketsViewModel> logger)
        {
            _ticketService = ticketService;
            _navigation = navigation;
            _connectivity = connectivity;
            _logger = logger;

            Title = "My Tickets";
            IsOffline = !_connectivity.IsConnected;
            _connectivity.ConnectivityChanged += (_, isOnline) => IsOffline = !isOnline;
        }

        [RelayCommand]
        private async Task LoadTicketsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ClearError();
            _currentPage = 1;
            _hasMorePages = true;

            try
            {
                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
                if (string.IsNullOrEmpty(userId))
                {
                    await _navigation.GoToRootAsync("login");
                    return;
                }

                var loaded = await _ticketService.GetUserTicketsAsync(userId, _currentPage, PageSize);
                Tickets = new ObservableCollection<Ticket>(loaded);
                HasTickets = Tickets.Count > 0;
                var total = await _ticketService.GetUserTicketCountAsync(userId);
                TicketCountText = $"{total} {(total == 1 ? "ticket" : "tickets")}";
                _hasMorePages = loaded.Count == PageSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadTicketsAsync failed");
                SetError("Failed to load tickets. Pull down to retry.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LoadMoreAsync()
        {
            if (IsBusy || IsLoadingMore || !_hasMorePages) return;
            IsLoadingMore = true;

            try
            {
                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
                if (string.IsNullOrEmpty(userId)) return;

                _currentPage++;
                var more = await _ticketService.GetUserTicketsAsync(userId, _currentPage, PageSize);
                foreach (var t in more) Tickets.Add(t);
                _hasMorePages = more.Count == PageSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadMoreAsync failed");
                _currentPage--;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        private async Task SelectTicketAsync(Ticket ticket)
        {
            if (ticket is null) return;
            await _navigation.GoToAsync("ticketdetails",
                new Dictionary<string, object> { ["ticketId"] = ticket.Id });
        }

        [RelayCommand]
        private async Task BrowseEventsAsync() =>
            await _navigation.GoToRootAsync("main/events");

        [RelayCommand]
        private async Task RefreshAsync() =>
            await LoadTicketsAsync();
    }
}
