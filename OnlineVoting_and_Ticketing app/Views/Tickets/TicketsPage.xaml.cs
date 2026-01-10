using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Tickets
{
    public partial class TicketsPage : ContentPage
    {
        private readonly ITicketService _ticketService;
        private List<Ticket> _tickets = new();

        public TicketsPage(ITicketService ticketService)
        {
            InitializeComponent();
            _ticketService = ticketService;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadTicketsAsync();
        }

        private async Task LoadTicketsAsync()
        {
            LoadingIndicator.IsVisible = true;
            LoadingIndicator.IsRunning = true;

            var userId = Preferences.Get(AppConstants.Preferences.UserId, string.Empty);
            if (string.IsNullOrEmpty(userId))
            {
                LoadingIndicator.IsVisible = false;
                LoadingIndicator.IsRunning = false;
                await DisplayAlert("Authentication Required", "Please login to view your tickets", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            _tickets = await _ticketService.GetUserTicketsAsync(userId);
            TicketsCollectionView.ItemsSource = _tickets;
            TicketCountLabel.Text = $"{_tickets.Count} {(_tickets.Count == 1 ? "ticket" : "tickets")}";

            LoadingIndicator.IsVisible = false;
            LoadingIndicator.IsRunning = false;
            RefreshView.IsRefreshing = false;
        }

        private async void OnRefreshing(object? sender, EventArgs e)
        {
            await LoadTicketsAsync();
        }

        private async void OnTicketSelected(object? sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Ticket selectedTicket)
            {
                await Shell.Current.GoToAsync($"ticketdetails?ticketId={selectedTicket.Id}");
                ((CollectionView)sender!).SelectedItem = null;
            }
        }

        private async void OnBrowseEventsClicked(object? sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//events");
        }
    }
}
