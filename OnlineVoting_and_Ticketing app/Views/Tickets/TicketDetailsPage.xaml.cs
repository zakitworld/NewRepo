using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Tickets
{
    [QueryProperty(nameof(TicketId), "ticketId")]
    public partial class TicketDetailsPage : ContentPage
    {
        private readonly ITicketService _ticketService;
        private Ticket? _currentTicket;
        private string _ticketId = string.Empty;

        public string TicketId
        {
            get => _ticketId;
            set
            {
                _ticketId = value;
                LoadTicketDetailsAsync();
            }
        }

        public TicketDetailsPage(ITicketService ticketService)
        {
            InitializeComponent();
            _ticketService = ticketService;
        }

        private async void LoadTicketDetailsAsync()
        {
            if (string.IsNullOrEmpty(_ticketId))
                return;

            _currentTicket = await _ticketService.GetTicketByIdAsync(_ticketId);

            if (_currentTicket == null)
            {
                await DisplayAlert("Error", "Ticket not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            EventTitleLabel.Text = _currentTicket.EventTitle;
            TicketTypeLabel.Text = _currentTicket.TicketTypeName;
            TicketHolderLabel.Text = _currentTicket.UserName;
            PriceLabel.Text = $"GH₵ {_currentTicket.Price:F2}";
            PurchaseDateLabel.Text = _currentTicket.PurchasedAt.ToString("MMM dd, yyyy");
            StatusLabel.Text = _currentTicket.Status.ToString();
            TicketIdLabel.Text = _currentTicket.Id;

            switch (_currentTicket.Status)
            {
                case TicketStatus.Active:
                    StatusBorder.BackgroundColor = Color.FromArgb("#10B981");
                    break;
                case TicketStatus.Used:
                    StatusBorder.BackgroundColor = Color.FromArgb("#6B7280");
                    break;
                case TicketStatus.Cancelled:
                    StatusBorder.BackgroundColor = Color.FromArgb("#EF4444");
                    break;
                case TicketStatus.Expired:
                    StatusBorder.BackgroundColor = Color.FromArgb("#F59E0B");
                    break;
            }

            if (_currentTicket.CheckedInAt.HasValue)
            {
                CheckInBorder.IsVisible = true;
                CheckInDateLabel.Text = $"Checked in on {_currentTicket.CheckedInAt.Value:MMM dd, yyyy • h:mm tt}";
            }

            if (!string.IsNullOrEmpty(_currentTicket.QRCode))
            {
                QRCodeImage.Source = _currentTicket.QRCode;
            }
        }

        private async void OnDownloadTicketClicked(object? sender, EventArgs e)
        {
            if (_currentTicket == null)
                return;

            await DisplayAlert("Download", "Ticket download feature coming soon!", "OK");
        }

        private async void OnShareTicketClicked(object? sender, EventArgs e)
        {
            if (_currentTicket == null)
                return;

            try
            {
                await Share.RequestAsync(new ShareTextRequest
                {
                    Title = "Share Ticket",
                    Text = $"Check out my ticket for {_currentTicket.EventTitle}!\nTicket ID: {_currentTicket.Id}"
                });
            }
            catch
            {
                await DisplayAlert("Error", "Unable to share ticket", "OK");
            }
        }
    }
}
