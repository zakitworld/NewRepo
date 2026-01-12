using Microsoft.Maui.Controls.Shapes;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Polls
{
    [QueryProperty(nameof(PollId), "pollId")]
    public partial class PollDetailsPage : ContentPage
    {
        private readonly IPollService _pollService;
        private Poll? _currentPoll;
        private string _pollId = string.Empty;
        private bool _hasVoted = false;
        private List<PollOptionControl> _optionControls = new();

        public string PollId
        {
            get => _pollId;
            set
            {
                _pollId = value;
                LoadPollDetailsAsync();
            }
        }

        public PollDetailsPage(IPollService pollService)
        {
            InitializeComponent();
            _pollService = pollService;
        }

        private async void LoadPollDetailsAsync()
        {
            if (string.IsNullOrEmpty(_pollId))
                return;

            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            try
            {
                _currentPoll = await _pollService.GetPollByIdAsync(_pollId);

                if (_currentPoll == null)
                {
                    await DisplayAlert("Error", "Poll not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                DisplayPollDetails();

                var userId = Preferences.Get(AppConstants.Preferences.UserId, string.Empty);
                if (!string.IsNullOrEmpty(userId))
                {
                    _hasVoted = await _pollService.HasUserVotedAsync(_pollId, userId);
                }

                if (_hasVoted || _currentPoll.Status == PollStatus.Closed || DateTime.UtcNow > _currentPoll.EndDate)
                {
                    ShowResults();
                }
                else if (DateTime.UtcNow < _currentPoll.StartDate)
                {
                    ShowNotStarted();
                }
                else
                {
                    ShowVotingInterface();
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error loading poll: {ex.Message}");
            }
            finally
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
            }
        }

        private void DisplayPollDetails()
        {
            if (_currentPoll == null) return;

            TitleLabel.Text = _currentPoll.Title;
            DescriptionLabel.Text = _currentPoll.Description;
            CreatorLabel.Text = _currentPoll.CreatorName;
            VoteCountLabel.Text = $"{_currentPoll.TotalVotes} {(_currentPoll.TotalVotes == 1 ? "vote" : "votes")}";

            // Duration
            DurationLabel.Text = $"{_currentPoll.StartDate:MMM dd, yyyy} - {_currentPoll.EndDate:MMM dd, yyyy}";

            // Time remaining
            var now = DateTime.UtcNow;
            if (now < _currentPoll.StartDate)
            {
                var timeUntilStart = _currentPoll.StartDate - now;
                TimeRemainingLabel.Text = $"Starts in {FormatTimeSpan(timeUntilStart)}";
            }
            else if (now > _currentPoll.EndDate)
            {
                TimeRemainingLabel.Text = "Poll has ended";
            }
            else
            {
                var timeRemaining = _currentPoll.EndDate - now;
                TimeRemainingLabel.Text = $"{FormatTimeSpan(timeRemaining)} remaining";
            }

            // Status
            UpdateStatusBadge(_currentPoll.Status);

            // Settings badges
            MultipleChoiceBadge.IsVisible = _currentPoll.AllowMultipleChoices;
            AnonymousBadge.IsVisible = _currentPoll.IsAnonymous;

            // Voting instructions
            VotingInstructionsLabel.Text = _currentPoll.AllowMultipleChoices
                ? "Select one or more options below"
                : "Select one option below";
        }

        private void UpdateStatusBadge(PollStatus status)
        {
            StatusLabel.Text = status.ToString();

            switch (status)
            {
                case PollStatus.Active:
                    StatusBorder.BackgroundColor = Color.FromArgb("#10B981");
                    break;
                case PollStatus.Closed:
                    StatusBorder.BackgroundColor = Color.FromArgb("#6B7280");
                    break;
                case PollStatus.Draft:
                    StatusBorder.BackgroundColor = Color.FromArgb("#F59E0B");
                    break;
                case PollStatus.Archived:
                    StatusBorder.BackgroundColor = Color.FromArgb("#9CA3AF");
                    break;
            }
        }

        private void ShowVotingInterface()
        {
            VotingSection.IsVisible = true;
            ResultsSection.IsVisible = false;
            AlreadyVotedSection.IsVisible = false;

            OptionsContainer.Children.Clear();
            _optionControls.Clear();

            if (_currentPoll?.Options == null) return;

            foreach (var option in _currentPoll.Options.OrderBy(o => o.Order))
            {
                var optionControl = new PollOptionControl(option, _currentPoll.AllowMultipleChoices);
                _optionControls.Add(optionControl);
                OptionsContainer.Children.Add(optionControl);
            }
        }

        private async void ShowResults()
        {
            VotingSection.IsVisible = false;
            ResultsSection.IsVisible = true;
            AlreadyVotedSection.IsVisible = _hasVoted && DateTime.UtcNow <= _currentPoll?.EndDate;

            ResultsContainer.Children.Clear();

            if (_currentPoll?.Options == null) return;

            ResultsVoteCountLabel.Text = $"{_currentPoll.TotalVotes} {(_currentPoll.TotalVotes == 1 ? "vote" : "votes")}";

            var totalVotes = _currentPoll.TotalVotes > 0 ? _currentPoll.TotalVotes : 1; // Prevent division by zero

            foreach (var option in _currentPoll.Options.OrderByDescending(o => o.VoteCount))
            {
                var percentage = (double)option.VoteCount / totalVotes * 100;
                var resultView = new PollResultView(option.Text, option.VoteCount, percentage);
                ResultsContainer.Children.Add(resultView);
            }

            if (_hasVoted)
            {
                ThankYouLabel.IsVisible = true;
            }
        }

        private void ShowNotStarted()
        {
            VotingSection.IsVisible = false;
            ResultsSection.IsVisible = false;
            AlreadyVotedSection.IsVisible = false;

            ShowError($"This poll hasn't started yet. It will begin on {_currentPoll?.StartDate:MMM dd, yyyy 'at' h:mm tt}");
        }

        private async void OnSubmitVoteClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            var selectedOptions = _optionControls.Where(c => c.IsSelected).Select(c => c.OptionId).ToList();

            if (selectedOptions.Count == 0)
            {
                ShowError("Please select at least one option");
                return;
            }

            if (!_currentPoll!.AllowMultipleChoices && selectedOptions.Count > 1)
            {
                ShowError("You can only select one option");
                return;
            }

            var userId = Preferences.Get(AppConstants.Preferences.UserId, string.Empty);
            if (string.IsNullOrEmpty(userId))
            {
                await DisplayAlert("Authentication Required", "Please login to vote", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            SubmitVoteButton.IsEnabled = false;
            SubmitVoteButton.Text = "Submitting...";

            try
            {
                var (success, error) = await _pollService.CastVoteAsync(_pollId, userId, selectedOptions);

                if (success)
                {
                    _hasVoted = true;
                    // Reload poll to get updated vote counts
                    _currentPoll = await _pollService.GetPollByIdAsync(_pollId);
                    ShowResults();
                }
                else
                {
                    ShowError(error ?? "Failed to submit vote");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                SubmitVoteButton.IsEnabled = true;
                SubmitVoteButton.Text = "Submit Vote";
            }
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }

        private string FormatTimeSpan(TimeSpan timeSpan)
        {
            if (timeSpan.TotalDays >= 1)
                return $"{(int)timeSpan.TotalDays} {((int)timeSpan.TotalDays == 1 ? "day" : "days")}";
            if (timeSpan.TotalHours >= 1)
                return $"{(int)timeSpan.TotalHours} {((int)timeSpan.TotalHours == 1 ? "hour" : "hours")}";
            if (timeSpan.TotalMinutes >= 1)
                return $"{(int)timeSpan.TotalMinutes} {((int)timeSpan.TotalMinutes == 1 ? "minute" : "minutes")}";
            return "less than a minute";
        }
    }

    // Custom control for poll option with checkbox/radio button
    public class PollOptionControl : Border
    {
        private CheckBox? _checkBox;
        private RadioButton? _radioButton;
        private readonly bool _isMultipleChoice;

        public string OptionId { get; }
        public bool IsSelected => _isMultipleChoice ? (_checkBox?.IsChecked ?? false) : (_radioButton?.IsChecked ?? false);

        public PollOptionControl(PollOption option, bool isMultipleChoice)
        {
            OptionId = option.Id;
            _isMultipleChoice = isMultipleChoice;

            StrokeShape = new RoundRectangle { CornerRadius = 12 };
            Stroke = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#374151")
                : Color.FromArgb("#E5E7EB");
            StrokeThickness = 1;
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#1F2937")
                : Colors.White;
            Padding = 15;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 12
            };

            if (isMultipleChoice)
            {
                _checkBox = new CheckBox
                {
                    Color = Color.FromArgb("#6366F1"),
                    VerticalOptions = LayoutOptions.Center
                };
                grid.Add(_checkBox, 0);
            }
            else
            {
                _radioButton = new RadioButton
                {
                    GroupName = "PollOptions",
                    VerticalOptions = LayoutOptions.Center
                };
                grid.Add(_radioButton, 0);
            }

            var label = new Label
            {
                Text = option.Text,
                FontSize = 15,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Colors.White
                    : Color.FromArgb("#111827"),
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            grid.Add(label, 1);

            Content = grid;

            // Make entire border tappable
            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                if (_isMultipleChoice && _checkBox != null)
                {
                    _checkBox.IsChecked = !_checkBox.IsChecked;
                }
                else if (_radioButton != null)
                {
                    _radioButton.IsChecked = true;
                }
            };
            GestureRecognizers.Add(tapGesture);
        }
    }

    // Custom control for displaying poll results with progress bar
    public class PollResultView : Border
    {
        public PollResultView(string optionText, int voteCount, double percentage)
        {
            StrokeShape = new RoundRectangle { CornerRadius = 10 };
            Stroke = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#374151")
                : Color.FromArgb("#E5E7EB");
            StrokeThickness = 1;
            BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                ? Color.FromArgb("#1F2937")
                : Colors.White;
            Padding = 15;

            var layout = new VerticalStackLayout { Spacing = 10 };

            // Header with option text and percentage
            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            var optionLabel = new Label
            {
                Text = optionText,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Colors.White
                    : Color.FromArgb("#111827"),
                LineBreakMode = LineBreakMode.WordWrap
            };

            var percentageLabel = new Label
            {
                Text = $"{percentage:F1}%",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#6366F1"),
                VerticalOptions = LayoutOptions.Center
            };

            header.Add(optionLabel, 0);
            header.Add(percentageLabel, 1);
            layout.Children.Add(header);

            // Progress bar background
            var progressBackground = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                StrokeThickness = 0,
                BackgroundColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#374151")
                    : Color.FromArgb("#E5E7EB"),
                HeightRequest = 8
            };

            // Progress bar fill
            var progressFill = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 4 },
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#6366F1"),
                HeightRequest = 8,
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = Math.Max(0, percentage) // Use percentage for width
            };

            // Create a grid to overlay the progress bar
            var progressGrid = new Grid();
            progressGrid.Children.Add(progressBackground);
            progressGrid.Children.Add(progressFill);

            layout.Children.Add(progressGrid);

            // Vote count
            var voteCountLabel = new Label
            {
                Text = $"{voteCount} {(voteCount == 1 ? "vote" : "votes")}",
                FontSize = 13,
                TextColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#9CA3AF")
                    : Color.FromArgb("#6B7280")
            };

            layout.Children.Add(voteCountLabel);

            Content = layout;

            // Animate the progress bar
            progressFill.WidthRequest = 0;
            progressFill.Animate("ProgressAnimation",
                new Animation(v => progressFill.WidthRequest = v, 0, percentage * 2), // Multiply by factor for visual width
                16, 500, Easing.CubicOut);
        }
    }
}
