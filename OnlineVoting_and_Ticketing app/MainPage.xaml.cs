using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app
{
    public partial class MainPage : ContentPage
    {
        private readonly IEventService _eventService;
        private readonly IPollService _pollService;
        private readonly ITicketService _ticketService;
        private readonly INavigationService _navigation;

        public MainPage(IEventService eventService, IPollService pollService,
                        ITicketService ticketService, INavigationService navigation)
        {
            InitializeComponent();
            _eventService  = eventService;
            _pollService   = pollService;
            _ticketService = ticketService;
            _navigation    = navigation;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            try
            {
                var userName = await SecureStorage.GetAsync(AppConstants.Preferences.UserName) ?? "Explorer";
                WelcomeLabel.Text = $"Welcome, {userName}!";
            }
            catch
            {
                WelcomeLabel.Text = "Welcome!";
            }

            await Task.WhenAll(LoadStatsAsync(), LoadFeaturedEventsAsync());
        }

        // ── Stats ──────────────────────────────────────────────────────────
        private async Task LoadStatsAsync()
        {
            try
            {
                var eventsTask  = _eventService.GetTotalEventsCountAsync();
                var pollsTask   = _pollService.GetTotalPollsCountAsync(activeOnly: true);
                var userId      = await SecureStorage.GetAsync(AppConstants.Preferences.UserId) ?? string.Empty;
                var ticketsTask = string.IsNullOrEmpty(userId)
                    ? Task.FromResult(0)
                    : _ticketService.GetUserTicketCountAsync(userId);

                await Task.WhenAll(eventsTask, pollsTask, ticketsTask);

                EventsCountLabel.Text  = (await eventsTask).ToString();
                PollsCountLabel.Text   = (await pollsTask).ToString();
                TicketsCountLabel.Text = (await ticketsTask).ToString();
            }
            catch
            {
                EventsCountLabel.Text  = "—";
                PollsCountLabel.Text   = "—";
                TicketsCountLabel.Text = "—";
            }
        }

        // ── Featured carousel ──────────────────────────────────────────────
        private async Task LoadFeaturedEventsAsync()
        {
            try
            {
                var events = await _eventService.GetUpcomingEventsAsync(page: 1, pageSize: 8);

                FeaturedCarousel.ItemsSource = events;

                // Hide skeleton, show carousel + dots
                CarouselSkeleton.IsVisible = false;
                FeaturedCarousel.IsVisible = events.Count > 0;
                CarouselDots.IsVisible     = events.Count > 1;
            }
            catch
            {
                CarouselSkeleton.IsVisible = false;
                FeaturedCarousel.IsVisible = false;
                CarouselDots.IsVisible     = false;
            }
        }

        // ── Carousel tap handler ───────────────────────────────────────────
        private async void OnFeaturedEventTapped(object? sender, TappedEventArgs e)
        {
            if (sender is not BindableObject bo) return;
            if (bo.BindingContext is not Event ev) return;
            await _navigation.GoToAsync("eventdetails",
                new Dictionary<string, object> { ["eventId"] = ev.Id });
        }

        // ── "See All" tap ──────────────────────────────────────────────────
        private async void OnSeeAllEventsTapped(object? sender, TappedEventArgs e)
            => await Shell.Current.GoToAsync("//events");

        // ── Category card taps ─────────────────────────────────────────────
        private async void OnEventsCardTapped(object? sender, EventArgs e)
            => await Shell.Current.GoToAsync("//events");

        private async void OnPollsCardTapped(object? sender, EventArgs e)
            => await Shell.Current.GoToAsync("//polls");

        private async void OnTicketsCardTapped(object? sender, EventArgs e)
            => await Shell.Current.GoToAsync("//tickets");

        private async void OnProfileCardTapped(object? sender, EventArgs e)
            => await Shell.Current.GoToAsync("//profile");
    }
}
