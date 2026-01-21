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
                    await DisplayAlertAsync("Error", "Poll not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }

                DisplayPollDetails();

                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
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
            StatusLabel.Text = status.ToString().ToUpper();

            switch (status)
            {
                case PollStatus.Active:
                    StatusBorder.BackgroundColor = Color.FromArgb("#2010B981");
                    StatusLabel.TextColor = Color.FromArgb("#10B981");
                    break;
                case PollStatus.Closed:
                    StatusBorder.BackgroundColor = Color.FromArgb("#206B7280");
                    StatusLabel.TextColor = Color.FromArgb("#9CA3AF");
                    break;
                case PollStatus.Draft:
                    StatusBorder.BackgroundColor = Color.FromArgb("#20F59E0B");
                    StatusLabel.TextColor = Color.FromArgb("#FBBF24");
                    break;
                case PollStatus.Archived:
                    StatusBorder.BackgroundColor = Color.FromArgb("#209CA3AF");
                    StatusLabel.TextColor = Color.FromArgb("#D1D5DB");
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

            ResultsVoteCountLabel.Text = $"{_currentPoll.TotalVotes} {(_currentPoll.TotalVotes == 1 ? "voter" : "voters")}";

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

            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            if (string.IsNullOrEmpty(userId))
            {
                await DisplayAlertAsync("Authentication Required", "Please login to vote", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            SubmitVoteButton.IsEnabled = false;
            SubmitVoteButton.Text = "Recording your voice...";

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
            Stroke = Color.FromArgb("#30FFFFFF"); // Glass border
            StrokeThickness = 1;
            BackgroundColor = Color.FromArgb("#10FFFFFF"); // Glass background
            Padding = 18;

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) }
                },
                ColumnSpacing = 15
            };

            if (isMultipleChoice)
            {
                _checkBox = new CheckBox
                {
                    Color = Color.FromArgb("#8B5CF6"), // Primary color
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
                TextColor = Colors.White,
                VerticalOptions = LayoutOptions.Center,
                LineBreakMode = LineBreakMode.WordWrap
            };

            grid.Add(label, 1);
            Content = grid;

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) =>
            {
                if (_isMultipleChoice && _checkBox != null) _checkBox.IsChecked = !_checkBox.IsChecked;
                else if (_radioButton != null) _radioButton.IsChecked = true;
            };
            GestureRecognizers.Add(tapGesture);
        }
    }

    // Custom control for displaying poll results with progress bar
    public class PollResultView : Border
    {
        public PollResultView(string optionText, int voteCount, double percentage)
        {
            StrokeShape = new RoundRectangle { CornerRadius = 12 };
            Stroke = Color.FromArgb("#20FFFFFF");
            StrokeThickness = 1;
            BackgroundColor = Color.FromArgb("#08FFFFFF");
            Padding = 20;

            var layout = new VerticalStackLayout { Spacing = 12 };

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                }
            };

            header.Add(new Label
            {
                Text = optionText,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.WordWrap
            }, 0);

            header.Add(new Label
            {
                Text = $"{percentage:F1}%",
                FontSize = 16,
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#3B82F6"), // Secondary/Blue
                VerticalOptions = LayoutOptions.Center
            }, 1);

            layout.Children.Add(header);

            var progressGrid = new Grid { HeightRequest = 6 };
            progressGrid.Children.Add(new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 3 },
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#1AFFFFFF"),
                HorizontalOptions = LayoutOptions.Fill
            });

            var progressFill = new Border
            {
                StrokeShape = new RoundRectangle { CornerRadius = 3 },
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#8B5CF6"), // Primary
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = 0
            };
            progressGrid.Children.Add(progressFill);

            layout.Children.Add(progressGrid);

            layout.Children.Add(new Label
            {
                Text = $"{voteCount} {(voteCount == 1 ? "participation" : "participations")}",
                FontSize = 12,
                TextColor = Color.FromArgb("#94A3B8") // TextSecondary
            });

            Content = layout;

            // Animate progress
            progressFill.Animate("ProgressAnim", new Animation(v => progressFill.WidthRequest = v, 0, percentage * 2.5), 16, 800, Easing.CubicOut);
        }
    }
}
