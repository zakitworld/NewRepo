using Microsoft.Maui.Controls.Shapes;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Polls
{
    public partial class CreatePollPage : ContentPage
    {
        private readonly IPollService _pollService;
        private List<PollOptionView> _optionViews = new();
        private bool _isPaidVoting = false;

        public CreatePollPage(IPollService pollService)
        {
            InitializeComponent();
            _pollService = pollService ?? throw new ArgumentNullException(nameof(pollService));

            try
            {
                StartDatePicker.MinimumDate = DateTime.Now;
                EndDatePicker.MinimumDate = DateTime.Now;
                StartDatePicker.Date = DateTime.Now;
                EndDatePicker.Date = DateTime.Now.AddDays(7);
                StartTimePicker.Time = DateTime.Now.TimeOfDay;
                EndTimePicker.Time = new TimeSpan(23, 59, 0);
                PollTypePicker.SelectedIndex = 0;

                AddOptionView();
                AddOptionView();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreatePollPage init error: {ex.Message}");
            }
        }

        private void OnPaidVotingToggled(object? sender, ToggledEventArgs e)
        {
            _isPaidVoting = e.Value;
            PaidVotingOptions.IsVisible = _isPaidVoting;
            OptionsLabel.Text = _isPaidVoting ? "CONTESTANTS" : "RESPONSE OPTIONS";
            AddOptionButton.Text = _isPaidVoting ? "+ Add Contestant" : "+ New Option";

            // Refresh all option views to show/hide contestant fields
            foreach (var view in _optionViews)
                view.SetContestantMode(_isPaidVoting);
        }

        private void OnPollTypeChanged(object? sender, EventArgs e)
        {
            MultipleChoiceSwitch.IsToggled = PollTypePicker.SelectedIndex == 1;
        }

        private void OnAddOptionClicked(object? sender, EventArgs e) => AddOptionView();

        private void AddOptionView()
        {
            var view = new PollOptionView(_optionViews.Count + 1, _isPaidVoting);
            view.RemoveRequested += (s, e) => RemoveOptionView(s as PollOptionView);
            _optionViews.Add(view);
            OptionsContainer.Children.Add(view);
        }

        private async void RemoveOptionView(PollOptionView? view)
        {
            if (view != null && _optionViews.Count > 2)
            {
                _optionViews.Remove(view);
                OptionsContainer.Children.Remove(view);
                for (int i = 0; i < _optionViews.Count; i++)
                    _optionViews[i].UpdateNumber(i + 1);
            }
            else
            {
                await DisplayAlertAsync("Error", "At least 2 options are required", "OK");
            }
        }

        private async void OnCreatePollClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                ShowError("Poll title is required");
                return;
            }

            if (PollTypePicker.SelectedIndex < 0)
            {
                ShowError("Please select a poll type");
                return;
            }

            var startDateTime = StartDatePicker.Date + StartTimePicker.Time;
            var endDateTime = EndDatePicker.Date + EndTimePicker.Time;

            if (endDateTime <= startDateTime)
            {
                ShowError("End date must be after start date");
                return;
            }

            // Validate paid voting fields
            decimal votePrice = 1.0m;
            int maxVotes = 0;
            if (_isPaidVoting)
            {
                if (!decimal.TryParse(VotePriceEntry.Text, out votePrice) || votePrice <= 0)
                {
                    ShowError("Please enter a valid price per vote");
                    return;
                }
                if (!int.TryParse(MaxVotesEntry.Text, out maxVotes) || maxVotes < 0)
                {
                    ShowError("Max votes must be 0 (unlimited) or a positive number");
                    return;
                }
            }

            // Build options list
            var options = new List<PollOption>();
            for (int i = 0; i < _optionViews.Count; i++)
            {
                var text = _optionViews[i].GetOptionText();
                if (string.IsNullOrWhiteSpace(text))
                {
                    ShowError(_isPaidVoting ? $"Contestant {i + 1} name is required" : $"Option {i + 1} cannot be empty");
                    return;
                }
                options.Add(new PollOption
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = text.Trim(),
                    Order = i,
                    VoteCount = 0,
                    ContestantPhotoUrl = _optionViews[i].GetPhotoUrl(),
                    ContestantBio = _optionViews[i].GetBio()
                });
            }

            if (options.Count < 2)
            {
                ShowError("At least 2 options are required");
                return;
            }

            CreateButton.IsEnabled = false;
            CreateButton.Text = "Creating...";

            try
            {
                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
                var userName = await SecureStorage.GetAsync(AppConstants.Preferences.UserName) ?? "User";
                var pollType = PollTypePicker.SelectedIndex == 0 ? PollType.SingleChoice : PollType.MultipleChoice;

                var newPoll = new Poll
                {
                    Title = TitleEntry.Text.Trim(),
                    Description = DescriptionEditor.Text?.Trim() ?? string.Empty,
                    CreatorId = userId ?? string.Empty,
                    CreatorName = userName,
                    StartDate = startDateTime ?? DateTime.UtcNow,
                    EndDate = endDateTime ?? DateTime.UtcNow.AddDays(7),
                    Status = PollStatus.Active,
                    Type = pollType,
                    AllowMultipleChoices = MultipleChoiceSwitch.IsToggled,
                    IsAnonymous = AnonymousSwitch.IsToggled,
                    RequireAuthentication = RequireAuthSwitch.IsToggled,
                    IsPaidVoting = _isPaidVoting,
                    VotePriceGhs = votePrice,
                    MaxVotesPerUser = maxVotes,
                    Options = options,
                    TotalVotes = 0
                };

                var (success, error, pollId) = await _pollService.CreatePollAsync(newPoll);

                if (success)
                {
                    await DisplayAlertAsync("Success",
                        _isPaidVoting
                            ? $"Voting poll created! Each vote costs GHS {votePrice:F2}."
                            : "Poll created successfully!",
                        "OK");
                    await Shell.Current.GoToAsync("//polls");
                }
                else
                {
                    ShowError(error ?? "Failed to create poll");
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
            }
            finally
            {
                CreateButton.IsEnabled = true;
                CreateButton.Text = "LAUNCH POLL";
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            if (await DisplayAlertAsync("Cancel", "Discard all changes?", "Yes", "No"))
                await Shell.Current.GoToAsync("..");
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }
    }

    public class PollOptionView : Border
    {
        private readonly Label _numberLabel;
        private readonly Entry _nameEntry;
        private readonly Entry _photoUrlEntry;
        private readonly Entry _bioEntry;
        private readonly VerticalStackLayout _contestantFields;

        public event EventHandler? RemoveRequested;

        public PollOptionView(int number, bool contestantMode)
        {
            StrokeShape = new RoundRectangle { CornerRadius = 15 };
            Stroke = (Color)(Application.Current?.Resources["GlassBorderBrush"] ?? Colors.Transparent);
            StrokeThickness = 1;
            BackgroundColor = (Color)(Application.Current?.Resources["GlassBrush"] ?? Colors.Transparent);
            Padding = new Thickness(15, 12);

            _numberLabel = new Label
            {
                Text = $"{number}.",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)(Application.Current?.Resources["Primary"] ?? Colors.Purple),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            _nameEntry = new Entry
            {
                Placeholder = contestantMode ? "Contestant name" : "Option text",
                TextColor = Colors.White,
                PlaceholderColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray),
                FontSize = 14
            };

            var removeLabel = new Label
            {
                Text = "Remove",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)(Application.Current?.Resources["Error"] ?? Colors.Red),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            removeLabel.GestureRecognizers.Add(tap);

            var header = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 12
            };
            header.Add(_numberLabel, 0);
            header.Add(_nameEntry, 1);
            header.Add(removeLabel, 2);

            _photoUrlEntry = new Entry
            {
                Placeholder = "Photo URL (optional)",
                TextColor = Colors.White,
                PlaceholderColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray),
                FontSize = 13,
                Keyboard = Keyboard.Url
            };

            _bioEntry = new Entry
            {
                Placeholder = "Short bio (optional)",
                TextColor = Colors.White,
                PlaceholderColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray),
                FontSize = 13
            };

            _contestantFields = new VerticalStackLayout
            {
                Spacing = 8,
                IsVisible = contestantMode,
                Children = { _photoUrlEntry, _bioEntry }
            };

            Content = new VerticalStackLayout
            {
                Spacing = 8,
                Children = { header, _contestantFields }
            };
        }

        public void SetContestantMode(bool enabled)
        {
            _nameEntry.Placeholder = enabled ? "Contestant name" : "Option text";
            _contestantFields.IsVisible = enabled;
        }

        public void UpdateNumber(int number) => _numberLabel.Text = $"{number}.";
        public string GetOptionText() => _nameEntry.Text ?? string.Empty;
        public string GetPhotoUrl() => _photoUrlEntry.Text?.Trim() ?? string.Empty;
        public string GetBio() => _bioEntry.Text?.Trim() ?? string.Empty;
    }
}
