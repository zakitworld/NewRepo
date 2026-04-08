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
        private readonly IPaymentService _paymentService;

        private Poll? _poll;
        private string _pollId = string.Empty;
        private string? _selectedOptionId;
        private int _voteQty = 1;
        private string? _pendingPaymentRef; // set after Paystack browser opens
        private List<ContestantCard> _cards = new();

        public string PollId
        {
            get => _pollId;
            set { _pollId = value; LoadPollAsync(); }
        }

        public PollDetailsPage(IPollService pollService, IPaymentService paymentService)
        {
            InitializeComponent();
            _pollService = pollService;
            _paymentService = paymentService;
        }

        // ─── Loading ──────────────────────────────────────────────────────────

        private async void LoadPollAsync()
        {
            if (string.IsNullOrEmpty(_pollId)) return;

            SetLoading(true);
            try
            {
                _poll = await _pollService.GetPollByIdAsync(_pollId);
                if (_poll == null)
                {
                    await DisplayAlertAsync("Error", "Poll not found", "OK");
                    await Shell.Current.GoToAsync("..");
                    return;
                }
                await RenderPageAsync();
            }
            catch (Exception ex)
            {
                ShowError($"Error loading poll: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task RenderPageAsync()
        {
            if (_poll == null) return;

            // Header
            TitleLabel.Text = _poll.Title;
            DescriptionLabel.Text = _poll.Description;
            CreatorLabel.Text = _poll.CreatorName;
            VoteCountLabel.Text = $"{_poll.TotalVotes} {(_poll.TotalVotes == 1 ? "vote" : "votes")}";
            DurationLabel.Text = $"{_poll.StartDate:MMM dd} – {_poll.EndDate:MMM dd, yyyy}";
            UpdateStatusBadge(_poll.Status);

            var now = DateTime.UtcNow;
            TimeRemainingLabel.Text = now > _poll.EndDate
                ? "Ended"
                : now < _poll.StartDate
                    ? $"Starts {FormatTs(_poll.StartDate - now)}"
                    : $"{FormatTs(_poll.EndDate - now)} left";

            // Paid voting header decorations
            if (_poll.IsPaidVoting)
            {
                PaidBadge.IsVisible = true;
                PriceBadgeLabel.Text = $"GHS {_poll.VotePriceGhs:F2} / vote";
                ContestantsHeader.Text = "LEADERBOARD";

                if (_poll.MaxVotesPerUser > 0)
                {
                    var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId) ?? string.Empty;
                    var cast = userId.Length > 0 ? await _pollService.GetUserVoteCountAsync(_pollId, userId) : 0;
                    MaxVotesLabel.Text = $"You've cast {cast} / {_poll.MaxVotesPerUser} allowed votes";
                    MaxVotesLabel.IsVisible = true;
                }
            }
            else
            {
                ContestantsHeader.Text = "OPTIONS";
            }

            // Build contestant/option cards
            BuildCards();

            // Determine what voting panel to show
            var userId2 = await SecureStorage.GetAsync(AppConstants.Preferences.UserId) ?? string.Empty;
            var pollActive = _poll.Status == PollStatus.Active && now >= _poll.StartDate && now <= _poll.EndDate;

            if (_poll.IsPaidVoting)
            {
                // Paid polls: always show results (leaderboard); show voting panel if active
                if (pollActive)
                {
                    int alreadyCast = userId2.Length > 0
                        ? await _pollService.GetUserVoteCountAsync(_pollId, userId2)
                        : 0;
                    bool maxReached = _poll.MaxVotesPerUser > 0 && alreadyCast >= _poll.MaxVotesPerUser;

                    if (maxReached)
                    {
                        ShowVotedBanner($"You've reached the maximum of {_poll.MaxVotesPerUser} votes.", "Maximum votes used");
                    }
                    else
                    {
                        ShowVotingPanel(isPaid: true);
                    }
                }
                else
                {
                    ShowVotedBanner("Voting has closed. Final results below.", "Voting Closed");
                }
            }
            else
            {
                // Free poll
                bool hasVoted = userId2.Length > 0 && await _pollService.HasUserVotedAsync(_pollId, userId2);
                if (!pollActive || hasVoted)
                {
                    if (hasVoted) ShowVotedBanner("Thank you for participating!", "Vote Recorded");
                    else ShowVotedBanner("Voting has closed.", "Voting Closed");
                }
                else
                {
                    ShowVotingPanel(isPaid: false);
                }
            }
        }

        // ─── Card building ────────────────────────────────────────────────────

        private void BuildCards()
        {
            ContestantsContainer.Children.Clear();
            _cards.Clear();
            if (_poll?.Options == null) return;

            var sorted = _poll.IsPaidVoting
                ? _poll.Options.OrderByDescending(o => o.VoteCount).ToList()
                : _poll.Options.OrderBy(o => o.Order).ToList();

            int total = _poll.TotalVotes > 0 ? _poll.TotalVotes : 1;
            bool canVote = _poll.IsPaidVoting; // cards are selectable only for paid polls

            for (int i = 0; i < sorted.Count; i++)
            {
                var card = new ContestantCard(
                    option: sorted[i],
                    rank: _poll.IsPaidVoting ? i + 1 : 0,
                    totalVotes: total,
                    selectable: canVote,
                    onSelected: OnContestantSelected);
                _cards.Add(card);
                ContestantsContainer.Children.Add(card);
            }
        }

        private void OnContestantSelected(string optionId)
        {
            _selectedOptionId = optionId;
            foreach (var c in _cards) c.SetSelected(c.OptionId == optionId);

            var option = _poll?.Options.FirstOrDefault(o => o.Id == optionId);
            SelectedLabel.Text = option != null ? $"Voting for: {option.Text}" : "Contestant selected";

            VoteButton.IsEnabled = true;
            UpdateCostLabel();
        }

        // ─── Paid vote quantity ───────────────────────────────────────────────

        private void OnDecreaseVotes(object? sender, EventArgs e)
        {
            if (_voteQty > 1) { _voteQty--; UpdateCostLabel(); }
        }

        private void OnIncreaseVotes(object? sender, EventArgs e)
        {
            if (_poll == null) return;
            int max = _poll.MaxVotesPerUser > 0 ? _poll.MaxVotesPerUser : 999;
            if (_voteQty < max) { _voteQty++; UpdateCostLabel(); }
        }

        private void UpdateCostLabel()
        {
            if (_poll == null) return;
            VoteQtyLabel.Text = _voteQty.ToString();
            var total = _voteQty * _poll.VotePriceGhs;
            TotalCostLabel.Text = $"Total: GHS {total:F2}  ({_voteQty} {(_voteQty == 1 ? "vote" : "votes")})";
            VoteButton.Text = _selectedOptionId != null
                ? $"Pay GHS {total:F2} & Vote"
                : "Select a contestant first";
        }

        // ─── Vote submission ──────────────────────────────────────────────────

        private async void OnVoteClicked(object? sender, EventArgs e)
        {
            ErrorLabel.IsVisible = false;

            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            if (string.IsNullOrEmpty(userId))
            {
                await DisplayAlertAsync("Login Required", "Please login to vote", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            if (_poll == null) return;

            if (_poll.IsPaidVoting)
            {
                if (_selectedOptionId == null)
                {
                    ShowError("Please select a contestant first");
                    return;
                }
                await InitiatePaidVoteAsync(userId);
            }
            else
            {
                await SubmitFreeVoteAsync(userId);
            }
        }

        private async Task InitiatePaidVoteAsync(string userId)
        {
            if (_poll == null || _selectedOptionId == null) return;

            SetLoading(true);
            VoteButton.IsEnabled = false;

            try
            {
                var email = await SecureStorage.GetAsync(AppConstants.Preferences.UserEmail) ?? string.Empty;
                if (string.IsNullOrEmpty(email))
                {
                    ShowError("Your email is needed for payment. Please update your profile.");
                    return;
                }

                var amount = _voteQty * _poll.VotePriceGhs;
                _pendingPaymentRef = await _paymentService.GeneratePaymentReferenceAsync();

                var (success, error, _) = await _paymentService.InitiatePaymentAsync(
                    amount, email, _pendingPaymentRef);

                if (!success)
                {
                    ShowError(error ?? "Payment initialisation failed");
                    _pendingPaymentRef = null;
                    return;
                }

                // Browser has opened. Show the verify button.
                VerifyButton.IsVisible = true;
                VoteButton.IsVisible = false;
            }
            catch (Exception ex)
            {
                ShowError($"Payment error: {ex.Message}");
                _pendingPaymentRef = null;
            }
            finally
            {
                SetLoading(false);
                VoteButton.IsEnabled = _selectedOptionId != null;
            }
        }

        private async void OnVerifyPaymentClicked(object? sender, EventArgs e)
        {
            if (_pendingPaymentRef == null || _poll == null || _selectedOptionId == null) return;

            SetLoading(true);
            VerifyButton.IsEnabled = false;

            try
            {
                var (verified, verifyError) = await _paymentService.VerifyPaymentAsync(_pendingPaymentRef);
                if (!verified)
                {
                    ShowError(verifyError ?? "Payment not confirmed yet. Please try again.");
                    VerifyButton.IsEnabled = true;
                    return;
                }

                var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId) ?? string.Empty;
                var (voteSuccess, voteError) = await _pollService.CastPaidVoteAsync(
                    _pollId, userId, _selectedOptionId, _voteQty);

                if (voteSuccess)
                {
                    _pendingPaymentRef = null;
                    _poll = await _pollService.GetPollByIdAsync(_pollId);
                    VotingPanel.IsVisible = false;
                    VerifyButton.IsVisible = false;

                    var option = _poll?.Options.FirstOrDefault(o => o.Id == _selectedOptionId);
                    ShowVotedBanner(
                        $"You cast {_voteQty} {(_voteQty == 1 ? "vote" : "votes")} for {option?.Text ?? "the contestant"}!",
                        "Payment Confirmed ✓");

                    // Rebuild leaderboard with fresh data
                    BuildCards();
                    VoteCountLabel.Text = $"{_poll?.TotalVotes} votes";
                }
                else
                {
                    ShowError(voteError ?? "Failed to record votes after payment. Contact support with ref: " + _pendingPaymentRef);
                    VerifyButton.IsEnabled = true;
                }
            }
            catch (Exception ex)
            {
                ShowError($"Verification error: {ex.Message}");
                VerifyButton.IsEnabled = true;
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async Task SubmitFreeVoteAsync(string userId)
        {
            if (_poll == null) return;

            var selected = _cards.Where(c => c.IsSelected).Select(c => c.OptionId).ToList();
            if (selected.Count == 0)
            {
                ShowError("Please select an option");
                return;
            }

            SetLoading(true);
            VoteButton.IsEnabled = false;
            VoteButton.Text = "Submitting...";

            try
            {
                var (success, error) = await _pollService.CastVoteAsync(_pollId, userId, selected);
                if (success)
                {
                    _poll = await _pollService.GetPollByIdAsync(_pollId);
                    VotingPanel.IsVisible = false;
                    ShowVotedBanner("Thank you for voting!", "Vote Recorded");
                    BuildCards();
                    VoteCountLabel.Text = $"{_poll?.TotalVotes} votes";
                }
                else
                {
                    ShowError(error ?? "Failed to submit vote");
                    VoteButton.IsEnabled = true;
                    VoteButton.Text = "Submit Vote";
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error: {ex.Message}");
                VoteButton.IsEnabled = true;
                VoteButton.Text = "Submit Vote";
            }
            finally
            {
                SetLoading(false);
            }
        }

        // ─── UI helpers ───────────────────────────────────────────────────────

        private void ShowVotingPanel(bool isPaid)
        {
            VotingPanel.IsVisible = true;
            VotedBanner.IsVisible = false;
            VoteQuantityRow.IsVisible = isPaid;
            TotalCostLabel.IsVisible = isPaid;
            VoteQtyLabel.Text = "1";
            _voteQty = 1;

            if (isPaid)
            {
                VoteButton.Text = "Select a contestant first";
                VoteButton.IsEnabled = false;
                UpdateCostLabel();
            }
            else
            {
                VoteButton.Text = "Submit Vote";
                VoteButton.IsEnabled = true;
                SelectedLabel.Text = "Select an option below";
            }
        }

        private void ShowVotedBanner(string subtitle, string title)
        {
            VotingPanel.IsVisible = false;
            VotedBanner.IsVisible = true;
            VotedBannerTitle.Text = title;
            VotedBannerSub.Text = subtitle;
        }

        private void UpdateStatusBadge(PollStatus status)
        {
            StatusLabel.Text = status.ToString().ToUpper();
            (StatusBorder.BackgroundColor, StatusLabel.TextColor) = status switch
            {
                PollStatus.Active  => (Color.FromArgb("#2010B981"), Color.FromArgb("#10B981")),
                PollStatus.Closed  => (Color.FromArgb("#206B7280"), Color.FromArgb("#9CA3AF")),
                PollStatus.Draft   => (Color.FromArgb("#20F59E0B"), Color.FromArgb("#FBBF24")),
                _                  => (Color.FromArgb("#209CA3AF"), Color.FromArgb("#D1D5DB"))
            };
        }

        private void SetLoading(bool loading)
        {
            LoadingIndicator.IsVisible = loading;
            LoadingIndicator.IsRunning = loading;
        }

        private void ShowError(string msg)
        {
            ErrorLabel.Text = msg;
            ErrorLabel.IsVisible = true;
        }

        private static string FormatTs(TimeSpan ts)
        {
            if (ts.TotalDays >= 1) return $"{(int)ts.TotalDays}d {ts.Hours}h";
            if (ts.TotalHours >= 1) return $"{(int)ts.TotalHours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1) return $"{(int)ts.TotalMinutes}m";
            return "< 1m";
        }
    }

    // ─── Contestant / Option card control ─────────────────────────────────────

    public class ContestantCard : Border
    {
        private static readonly Color PrimaryColor = Color.FromArgb("#8B5CF6");
        private static readonly Color SurfaceColor = Color.FromArgb("#1E1B4B");
        private static readonly Color GlassBorder  = Color.FromArgb("#30FFFFFF");

        public string OptionId { get; }
        public bool IsSelected { get; private set; }

        public ContestantCard(PollOption option, int rank, int totalVotes,
                              bool selectable, Action<string> onSelected)
        {
            OptionId = option.Id;
            StrokeShape = new RoundRectangle { CornerRadius = 16 };
            StrokeThickness = 1;
            Stroke = GlassBorder;
            BackgroundColor = Color.FromArgb("#0AFFFFFF");
            Padding = 16;

            var root = new VerticalStackLayout { Spacing = 12 };

            // Top row: rank medal | photo | name + bio
            var topRow = new Grid
            {
                ColumnDefinitions = new ColumnDefinitionCollection
                {
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = GridLength.Auto },
                    new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) },
                    new ColumnDefinition { Width = GridLength.Auto }
                },
                ColumnSpacing = 12
            };

            // Rank medal (for paid/leaderboard polls)
            if (rank > 0)
            {
                var medal = new Label
                {
                    Text = rank switch { 1 => "🥇", 2 => "🥈", 3 => "🥉", _ => $"#{rank}" },
                    FontSize = rank <= 3 ? 22 : 14,
                    VerticalOptions = LayoutOptions.Center,
                    TextColor = Colors.White
                };
                topRow.Add(medal, 0);
            }

            // Photo or initials avatar
            var avatarBorder = new Border
            {
                WidthRequest = 52,
                HeightRequest = 52,
                StrokeShape = new RoundRectangle { CornerRadius = 26 },
                StrokeThickness = 2,
                Stroke = GlassBorder,
                BackgroundColor = SurfaceColor
            };

            if (!string.IsNullOrWhiteSpace(option.ContestantPhotoUrl))
            {
                avatarBorder.Content = new Image
                {
                    Source = ImageSource.FromUri(new Uri(option.ContestantPhotoUrl)),
                    Aspect = Aspect.AspectFill
                };
            }
            else
            {
                var initial = option.Text.Length > 0 ? option.Text[0].ToString().ToUpper() : "?";
                avatarBorder.Content = new Label
                {
                    Text = initial,
                    FontSize = 22,
                    FontAttributes = FontAttributes.Bold,
                    TextColor = PrimaryColor,
                    HorizontalOptions = LayoutOptions.Center,
                    VerticalOptions = LayoutOptions.Center
                };
            }
            topRow.Add(avatarBorder, 1);

            // Name + bio
            var nameStack = new VerticalStackLayout { Spacing = 3, VerticalOptions = LayoutOptions.Center };
            nameStack.Children.Add(new Label
            {
                Text = option.Text,
                FontSize = 15,
                FontAttributes = FontAttributes.Bold,
                TextColor = Colors.White,
                LineBreakMode = LineBreakMode.WordWrap
            });
            if (!string.IsNullOrWhiteSpace(option.ContestantBio))
            {
                nameStack.Children.Add(new Label
                {
                    Text = option.ContestantBio,
                    FontSize = 12,
                    TextColor = Color.FromArgb("#94A3B8"),
                    MaxLines = 2,
                    LineBreakMode = LineBreakMode.TailTruncation
                });
            }
            topRow.Add(nameStack, 2);

            // Vote count badge
            var voteBadge = new VerticalStackLayout
            {
                VerticalOptions = LayoutOptions.Center,
                HorizontalOptions = LayoutOptions.End,
                Spacing = 2
            };
            voteBadge.Children.Add(new Label
            {
                Text = option.VoteCount.ToString(),
                FontSize = 18,
                FontAttributes = FontAttributes.Bold,
                TextColor = PrimaryColor,
                HorizontalOptions = LayoutOptions.Center
            });
            voteBadge.Children.Add(new Label
            {
                Text = option.VoteCount == 1 ? "vote" : "votes",
                FontSize = 10,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.Center
            });
            topRow.Add(voteBadge, 3);

            root.Children.Add(topRow);

            // Progress bar
            var pct = totalVotes > 0 ? (double)option.VoteCount / totalVotes : 0;
            var track = new Border
            {
                HeightRequest = 6,
                StrokeShape = new RoundRectangle { CornerRadius = 3 },
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#1AFFFFFF")
            };
            root.Children.Add(track);

            var fill = new Border
            {
                HeightRequest = 6,
                StrokeShape = new RoundRectangle { CornerRadius = 3 },
                StrokeThickness = 0,
                BackgroundColor = rank == 1 ? Color.FromArgb("#F59E0B") : PrimaryColor,
                HorizontalOptions = LayoutOptions.Start,
                WidthRequest = 0
            };
            // Overlay fill on track
            var progressGrid = new Grid { HeightRequest = 6 };
            progressGrid.Children.Add(track);
            progressGrid.Children.Add(fill);
            root.Children.Add(progressGrid);

            // Percentage label
            root.Children.Add(new Label
            {
                Text = $"{pct * 100:F1}%",
                FontSize = 11,
                TextColor = Color.FromArgb("#94A3B8"),
                HorizontalOptions = LayoutOptions.End
            });

            Content = root;

            // Animate progress bar
            fill.Animate("fill", new Animation(v => fill.WidthRequest = v, 0, pct * 300),
                         16, 900, Easing.CubicOut);

            // Tap to select
            if (selectable)
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => onSelected(OptionId);
                GestureRecognizers.Add(tap);
            }
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            Stroke = selected ? PrimaryColor : GlassBorder;
            StrokeThickness = selected ? 2 : 1;
            BackgroundColor = selected ? Color.FromArgb("#158B5CF6") : Color.FromArgb("#0AFFFFFF");
        }
    }
}
