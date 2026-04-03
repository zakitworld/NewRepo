using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;
using System.Collections.ObjectModel;

namespace OnlineVoting_and_Ticketing_app.ViewModels.Polls
{
    public partial class PollsViewModel : BaseViewModel
    {
        private readonly IPollService _pollService;
        private readonly INavigationService _navigation;
        private readonly IConnectivityService _connectivity;
        private readonly ILogger<PollsViewModel> _logger;

        private int _currentPage = 1;
        private const int PageSize = 20;
        private bool _hasMorePages = true;

        [ObservableProperty]
        private ObservableCollection<Poll> _polls = [];

        [ObservableProperty]
        private bool _isLoadingMore;

        public PollsViewModel(
            IPollService pollService,
            INavigationService navigation,
            IConnectivityService connectivity,
            ILogger<PollsViewModel> logger)
        {
            _pollService = pollService;
            _navigation = navigation;
            _connectivity = connectivity;
            _logger = logger;

            Title = "Polls";
            IsOffline = !_connectivity.IsConnected;
            _connectivity.ConnectivityChanged += (_, isOnline) => IsOffline = !isOnline;
        }

        [RelayCommand]
        private async Task LoadPollsAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ClearError();
            _currentPage = 1;
            _hasMorePages = true;

            try
            {
                var loaded = await _pollService.GetActivePollsAsync(_currentPage, PageSize);
                Polls = new ObservableCollection<Poll>(loaded);
                _hasMorePages = loaded.Count == PageSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadPollsAsync failed");
                SetError("Failed to load polls. Pull down to retry.");
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
                _currentPage++;
                var more = await _pollService.GetActivePollsAsync(_currentPage, PageSize);
                foreach (var p in more) Polls.Add(p);
                _hasMorePages = more.Count == PageSize;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadMoreAsync polls failed");
                _currentPage--;
            }
            finally
            {
                IsLoadingMore = false;
            }
        }

        [RelayCommand]
        private async Task SelectPollAsync(Poll poll)
        {
            if (poll is null) return;
            await _navigation.GoToAsync("polldetails",
                new Dictionary<string, object> { ["pollId"] = poll.Id });
        }

        [RelayCommand]
        private async Task CreatePollAsync() =>
            await _navigation.GoToAsync("createpoll");

        [RelayCommand]
        private async Task RefreshAsync() =>
            await LoadPollsAsync();
    }
}
