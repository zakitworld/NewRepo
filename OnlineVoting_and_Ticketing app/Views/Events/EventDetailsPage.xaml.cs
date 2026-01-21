using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Helpers;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Events
{
    [QueryProperty(nameof(EventId), "eventId")]
    public partial class EventDetailsPage : ContentPage
    {
        private readonly IEventService _eventService;
        private readonly ITicketService _ticketService;
        private readonly IPaymentService _paymentService;
        private Event? _currentEvent;
        private string _eventId = string.Empty;

        public string EventId
        {
            get => _eventId;
            set
            {
                _eventId = value;
                LoadEventDetailsAsync();
            }
        }

        public EventDetailsPage(IEventService eventService, ITicketService ticketService, IPaymentService paymentService)
        {
            InitializeComponent();
            _eventService = eventService;
            _ticketService = ticketService;
            _paymentService = paymentService;
        }

        private async void LoadEventDetailsAsync()
        {
            if (string.IsNullOrEmpty(_eventId))
                return;

            _currentEvent = await _eventService.GetEventByIdAsync(_eventId);

            if (_currentEvent == null)
            {
                await DisplayAlertAsync("Error", "Event not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            EventImage.Source = _currentEvent.ImageUrl;
            TitleLabel.Text = _currentEvent.Title;
            CategoryLabel.Text = _currentEvent.Category.ToString();
            OrganizerLabel.Text = _currentEvent.OrganizerName;
            DateLabel.Text = DateTimeHelper.FormatEventDate(_currentEvent.StartDate, _currentEvent.EndDate);
            LocationLabel.Text = _currentEvent.Location;
            TicketsLabel.Text = $"{_currentEvent.AvailableTickets} tickets available";
            DescriptionLabel.Text = _currentEvent.Description;
            TicketTypesCollectionView.ItemsSource = _currentEvent.TicketTypes;

            if (_currentEvent.AvailableTickets <= 0)
            {
                BuyTicketButton.Text = "Sold Out";
                BuyTicketButton.IsEnabled = false;
            }
        }

        private async void OnBuyTicketClicked(object? sender, EventArgs e)
        {
            if (_currentEvent == null || !_currentEvent.TicketTypes.Any())
                return;

            var ticketType = _currentEvent.TicketTypes.FirstOrDefault();
            if (ticketType == null)
                return;

            var userId = await SecureStorage.GetAsync(AppConstants.Preferences.UserId);
            if (string.IsNullOrEmpty(userId))
            {
                await DisplayAlertAsync("Authentication Required", "Please login to purchase tickets", "OK");
                await Shell.Current.GoToAsync("//login");
                return;
            }

            var confirm = await DisplayAlertAsync(
                "Confirm Purchase",
                $"Purchase {ticketType.Name} for GH₵ {ticketType.Price:F2}?",
                "Confirm",
                "Cancel");

            if (!confirm)
                return;

            BuyTicketButton.IsEnabled = false;
            BuyTicketButton.Text = "Processing...";

            try
            {
                var reference = await _paymentService.GeneratePaymentReferenceAsync();
                var email = await SecureStorage.GetAsync(AppConstants.Preferences.UserEmail);

                var (paymentSuccess, paymentError, transactionId) = await _paymentService.InitiatePaymentAsync(
                    ticketType.Price,
                    email ?? string.Empty,
                    reference);

                if (!paymentSuccess || string.IsNullOrEmpty(transactionId))
                {
                    await DisplayAlertAsync("Payment Failed", paymentError ?? "Unable to process payment", "OK");
                    return;
                }

                await Task.Delay(2000);

                var (verifySuccess, verifyError) = await _paymentService.VerifyPaymentAsync(transactionId);

                if (!verifySuccess)
                {
                    await DisplayAlertAsync("Payment Verification Failed", verifyError ?? "Unable to verify payment", "OK");
                    return;
                }

                var (ticketSuccess, ticketError, ticket) = await _ticketService.PurchaseTicketAsync(
                    _currentEvent.Id,
                    ticketType.Id,
                    userId);

                if (ticketSuccess && ticket != null)
                {
                    await DisplayAlertAsync("Success", AppConstants.Messages.TicketPurchasedSuccess, "OK");
                    await Shell.Current.GoToAsync($"//tickets");
                }
                else
                {
                    await DisplayAlertAsync("Error", ticketError ?? "Failed to purchase ticket", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlertAsync("Error", $"An error occurred: {ex.Message}", "OK");
            }
            finally
            {
                BuyTicketButton.IsEnabled = true;
                BuyTicketButton.Text = "Buy Ticket";
            }
        }
    }
}
