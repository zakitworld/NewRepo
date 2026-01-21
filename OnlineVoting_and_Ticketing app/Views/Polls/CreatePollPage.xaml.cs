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

        public CreatePollPage(IPollService pollService)
        {
            InitializeComponent();
            _pollService = pollService ?? throw new ArgumentNullException(nameof(pollService));

            try
            {
                // Set minimum and default dates
                StartDatePicker.MinimumDate = DateTime.Now;
                EndDatePicker.MinimumDate = DateTime.Now;
                StartDatePicker.Date = DateTime.Now;
                EndDatePicker.Date = DateTime.Now.AddDays(7);
                StartTimePicker.Time = DateTime.Now.TimeOfDay;
                EndTimePicker.Time = new TimeSpan(23, 59, 0);

                // Set default poll type
                PollTypePicker.SelectedIndex = 0;

                // Add default options
                AddOptionView();
                AddOptionView();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"CreatePollPage initialization error: {ex.Message}");
            }
        }

        private void OnPollTypeChanged(object? sender, EventArgs e)
        {
            if (PollTypePicker.SelectedIndex == 1) // Multiple Choice
            {
                MultipleChoiceSwitch.IsToggled = true;
            }
            else // Single Choice
            {
                MultipleChoiceSwitch.IsToggled = false;
            }
        }

        private void OnAddOptionClicked(object? sender, EventArgs e)
        {
            AddOptionView();
        }

        private void AddOptionView()
        {
            var optionView = new PollOptionView(_optionViews.Count + 1);
            optionView.RemoveRequested += (s, e) => RemoveOptionView(s as PollOptionView);
            _optionViews.Add(optionView);
            OptionsContainer.Children.Add(optionView);
        }

        private async void RemoveOptionView(PollOptionView? view)
        {
            if (view != null && _optionViews.Count > 2)
            {
                _optionViews.Remove(view);
                OptionsContainer.Children.Remove(view);

                // Renumber remaining options
                for (int i = 0; i < _optionViews.Count; i++)
                {
                    _optionViews[i].UpdateNumber(i + 1);
                }
            }
            else if (_optionViews.Count == 2)
            {
                await DisplayAlertAsync("Error", "At least 2 options are required", "OK");
            }
        }

        private async void OnCreatePollClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            // Validate inputs
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

            // Combine date and time
            var startDateTime = StartDatePicker.Date + StartTimePicker.Time;
            var endDateTime = EndDatePicker.Date + EndTimePicker.Time;

            if (endDateTime <= startDateTime)
            {
                ShowError("End date must be after start date");
                return;
            }

            // Validate options
            var options = new List<PollOption>();
            for (int i = 0; i < _optionViews.Count; i++)
            {
                var optionText = _optionViews[i].GetOptionText();
                if (string.IsNullOrWhiteSpace(optionText))
                {
                    ShowError($"Option {i + 1} cannot be empty");
                    return;
                }
                options.Add(new PollOption
                {
                    Id = Guid.NewGuid().ToString(),
                    Text = optionText.Trim(),
                    Order = i,
                    VoteCount = 0
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
                    EndDate = endDateTime ?? DateTime.UtcNow.AddDays(1),
                    Status = PollStatus.Active,
                    Type = pollType,
                    AllowMultipleChoices = MultipleChoiceSwitch.IsToggled,
                    IsAnonymous = AnonymousSwitch.IsToggled,
                    RequireAuthentication = RequireAuthSwitch.IsToggled,
                    Options = options,
                    TotalVotes = 0,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                var (success, error, pollId) = await _pollService.CreatePollAsync(newPoll);

                if (success && !string.IsNullOrEmpty(pollId))
                {
                    await DisplayAlertAsync("Success", "Poll created successfully!", "OK");
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
                CreateButton.Text = "Create Poll";
            }
        }

        private async void OnCancelClicked(object? sender, EventArgs e)
        {
            var confirm = await DisplayAlertAsync("Cancel", "Are you sure you want to cancel? All changes will be lost.", "Yes", "No");
            if (confirm)
            {
                await Shell.Current.GoToAsync("..");
            }
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorLabel.IsVisible = true;
        }
    }

    // Custom view for poll option input
    public class PollOptionView : Border
    {
        private Entry _optionEntry;
        private Label _numberLabel;

        public event EventHandler? RemoveRequested;

        public PollOptionView(int number)
        {
            StrokeShape = new RoundRectangle { CornerRadius = 15 };
            Stroke = (Color)(Application.Current?.Resources["GlassBorderBrush"] ?? Colors.Transparent);
            StrokeThickness = 1;
            BackgroundColor = (Color)(Application.Current?.Resources["GlassBrush"] ?? Colors.Transparent);
            Padding = new Thickness(15, 8);

            var grid = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 15
            };

            // Option number
            _numberLabel = new Label
            {
                Text = $"{number}.",
                FontSize = 14,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)(Application.Current?.Resources["Primary"] ?? Colors.Purple),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };

            // Option text entry
            _optionEntry = new Entry
            {
                Placeholder = "Option Text",
                TextColor = Colors.White,
                PlaceholderColor = (Color)(Application.Current?.Resources["TextSecondary"] ?? Colors.Gray),
                FontSize = 14,
                VerticalOptions = LayoutOptions.Center
            };

            // Remove button
            var removeButton = new Label
            {
                Text = "Remove",
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                TextColor = (Color)(Application.Current?.Resources["Error"] ?? Colors.Red),
                VerticalOptions = LayoutOptions.Center,
                Margin = new Thickness(0, 0, 5, 0)
            };

            var tapGesture = new TapGestureRecognizer();
            tapGesture.Tapped += (s, e) => RemoveRequested?.Invoke(this, EventArgs.Empty);
            removeButton.GestureRecognizers.Add(tapGesture);

            grid.Add(_numberLabel, 0);
            grid.Add(_optionEntry, 1);
            grid.Add(removeButton, 2);

            Content = grid;
        }

        public string GetOptionText()
        {
            return _optionEntry.Text ?? string.Empty;
        }

        public void UpdateNumber(int number)
        {
            _numberLabel.Text = $"{number}.";
        }
    }
}
