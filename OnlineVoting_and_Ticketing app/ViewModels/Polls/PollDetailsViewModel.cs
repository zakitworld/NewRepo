using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;
using System.Collections.ObjectModel;

namespace OnlineVoting_and_Ticketing_app.ViewModels.Polls
{
    public partial class PollDetailsViewModel : BaseViewModel, IDisposable
    {
        private readonly IPollService _pollService;
        private readonly INavigationService _navigation;
        private readonly ILogger<PollDetailsViewModel> _logger;

        // Live refresh: reload results every 15 seconds while poll is active
        private CancellationTokenSource? _refreshCts;
        private const int LiveRefreshIntervalMs = 15_000;

        [ObservableProperty]
        private Poll? _poll;

        [ObservableProperty]
        private ObservableCollection<PollOption> _options = [];

        [ObservableProperty]
        private ObservableCollection<string> _selectedOptionIds = [];

        [ObservableProperty]
        private bool _hasVoted;

        [ObservableProperty]
        private bool _showVoting;

        [ObservableProperty]
        private bool _showResults;

        [ObservableProperty]
        private string _timeRemainingText = string.Empty;

        [ObservableProperty]
        private string _statusText = string.Empty;

        public PollDetailsViewModel(
            IPollService pollService,
            INavigationService navigation,
            ILogger<PollDetailsViewModel> logger)
        {
            _pollService = pollService;
            _navigation = navigation;
            _logger = logger;
        }

        [RelayCommand]
        private async Task LoadPollAsync(string pollId)
        {
            if (IsBusy) return;
            IsBusy = true;
            ClearError();

            try
            {
                Poll = await _pollService.GetPollByIdAsync(pollId);
                if (Poll is null)
                {
                    await _navigation.DisplayAlertAsync("Error", "Poll not found.", "OK");
                    await _navigation.GoBackAsync();
                    return;
                }

                Options = new ObservableCollection<PollOption>(Poll.Options.OrderBy(o => o.Order));

                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
                if (!string.IsNullOrEmpty(userId))
                    HasVoted = await _pollService.HasUserVotedAsync(pollId, userId);

                UpdateDisplay();
                StartLiveRefresh(pollId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "LoadPollAsync failed for {PollId}", pollId);
                SetError($"Error loading poll: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SubmitVoteAsync()
        {
            if (Poll is null || SelectedOptionIds.Count == 0)
            {
                SetError("Please select at least one option.");
                return;
            }

            if (!Poll.AllowMultipleChoices && SelectedOptionIds.Count > 1)
            {
                SetError("Only one option can be selected.");
                return;
            }

            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            if (string.IsNullOrEmpty(userId))
            {
                await _navigation.GoToRootAsync("login");
                return;
            }

            IsBusy = true;
            ClearError();

            try
            {
                var (success, error) = await _pollService.CastVoteAsync(
                    Poll.Id, userId, [.. SelectedOptionIds]);

                if (success)
                {
                    HasVoted = true;
                    Poll = await _pollService.GetPollByIdAsync(Poll.Id);
                    if (Poll is not null)
                        Options = new ObservableCollection<PollOption>(Poll.Options.OrderBy(o => o.Order));
                    UpdateDisplay();
                }
                else
                {
                    SetError(error ?? "Failed to submit vote.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SubmitVoteAsync failed");
                SetError($"Error: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ToggleOption(string optionId)
        {
            if (Poll is null) return;

            if (Poll.AllowMultipleChoices)
            {
                if (SelectedOptionIds.Contains(optionId))
                    SelectedOptionIds.Remove(optionId);
                else
                    SelectedOptionIds.Add(optionId);
            }
            else
            {
                SelectedOptionIds.Clear();
                SelectedOptionIds.Add(optionId);
            }
        }

        /// <summary>
        /// Starts a periodic background refresh of vote counts while the poll is active.
        /// Cancels automatically when the ViewModel is disposed.
        /// </summary>
        private void StartLiveRefresh(string pollId)
        {
            StopLiveRefresh();

            if (Poll?.Status != PollStatus.Active) return;

            _refreshCts = new CancellationTokenSource();
            var token = _refreshCts.Token;

            _ = Task.Run(async () =>
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(LiveRefreshIntervalMs, token);
                    if (token.IsCancellationRequested) break;

                    try
                    {
                        var fresh = await _pollService.GetPollByIdAsync(pollId);
                        if (fresh is not null)
                        {
                            await MainThread.InvokeOnMainThreadAsync(() =>
                            {
                                Poll = fresh;
                                Options = new ObservableCollection<PollOption>(
                                    fresh.Options.OrderBy(o => o.Order));
                                UpdateDisplay();
                            });
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Live poll refresh failed for {PollId}", pollId);
                    }
                }
            }, token);
        }

        private void StopLiveRefresh()
        {
            _refreshCts?.Cancel();
            _refreshCts?.Dispose();
            _refreshCts = null;
        }

        private void UpdateDisplay()
        {
            if (Poll is null) return;

            var now = DateTime.UtcNow;
            StatusText = Poll.Status.ToString().ToUpperInvariant();

            if (now < Poll.StartDate)
            {
                var diff = Poll.StartDate - now;
                TimeRemainingText = $"Starts in {FormatTimeSpan(diff)}";
                ShowVoting = false;
                ShowResults = false;
            }
            else if (now > Poll.EndDate || Poll.Status == PollStatus.Closed)
            {
                TimeRemainingText = "Poll has ended";
                ShowVoting = false;
                ShowResults = true;
            }
            else if (HasVoted)
            {
                var diff = Poll.EndDate - now;
                TimeRemainingText = $"{FormatTimeSpan(diff)} remaining";
                ShowVoting = false;
                ShowResults = true;
            }
            else
            {
                var diff = Poll.EndDate - now;
                TimeRemainingText = $"{FormatTimeSpan(diff)} remaining";
                ShowVoting = true;
                ShowResults = false;
            }
        }

        private static string FormatTimeSpan(TimeSpan ts)
        {
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d";
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m";
            return "< 1m";
        }

        public void Dispose() => StopLiveRefresh();
    }
}
