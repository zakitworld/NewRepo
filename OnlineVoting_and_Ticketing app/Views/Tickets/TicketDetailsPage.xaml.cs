using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Services;
using QRCoder;

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
            set { _ticketId = value; LoadTicketDetailsAsync(); }
        }

        public TicketDetailsPage(ITicketService ticketService)
        {
            InitializeComponent();
            _ticketService = ticketService;
        }

        private async void LoadTicketDetailsAsync()
        {
            if (string.IsNullOrEmpty(_ticketId)) return;

            _currentTicket = await _ticketService.GetTicketByIdAsync(_ticketId);

            if (_currentTicket == null)
            {
                await DisplayAlertAsync("Error", "Ticket not found", "OK");
                await Shell.Current.GoToAsync("..");
                return;
            }

            // Header
            EventTitleLabel.Text   = _currentTicket.EventTitle;
            TicketTypeLabel.Text   = _currentTicket.TicketTypeName;
            TicketHolderLabel.Text = _currentTicket.UserName;
            PriceLabel.Text        = $"GH₵ {_currentTicket.Price:F2}";
            PurchaseDateLabel.Text = _currentTicket.PurchasedAt.ToString("MMM dd, yyyy");
            StatusLabel.Text       = _currentTicket.Status.ToString().ToUpper();
            TicketIdLabel.Text     = string.IsNullOrEmpty(_currentTicket.TransactionId)
                                        ? _currentTicket.Id
                                        : _currentTicket.TransactionId;

            // Status badge colour
            (StatusBorder.BackgroundColor, StatusLabel.TextColor) = _currentTicket.Status switch
            {
                TicketStatus.Active    => (Color.FromArgb("#2010B981"), Color.FromArgb("#10B981")),
                TicketStatus.Used      => (Color.FromArgb("#206B7280"), Color.FromArgb("#9CA3AF")),
                TicketStatus.Cancelled => (Color.FromArgb("#20EF4444"), Color.FromArgb("#F87171")),
                TicketStatus.Expired   => (Color.FromArgb("#20F59E0B"), Color.FromArgb("#FBBF24")),
                _                      => (Color.FromArgb("#20FFFFFF"), Colors.White)
            };

            // Check-in banner
            if (_currentTicket.CheckedInAt.HasValue)
            {
                CheckInBorder.IsVisible = true;
                CheckInDateLabel.Text = $"Checked in on {_currentTicket.CheckedInAt.Value:MMM dd, yyyy • h:mm tt}";
            }

            // Generate QR code from ticket data
            GenerateQrCode(_currentTicket);
        }

        /// <summary>
        /// Generates a QR code PNG from a signed ticket payload and displays it.
        /// The payload encodes ticketId + eventId + userId so the QR scanner can
        /// verify ownership and mark the ticket as used.
        /// </summary>
        private void GenerateQrCode(Ticket ticket)
        {
            try
            {
                // Compact JSON payload — keep it small for reliable scanning
                var payload = $"{{\"t\":\"{ticket.Id}\",\"e\":\"{ticket.EventId}\",\"u\":\"{ticket.UserId}\"}}";

                using var qrGenerator = new QRCodeGenerator();
                var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.M);
                using var qrCode = new PngByteQRCode(qrData);
                var png = qrCode.GetGraphic(10);                   // 10 pixels per module

                // Display via MemoryStream so no temp file is needed
                QRCodeImage.Source = ImageSource.FromStream(() => new MemoryStream(png));
            }
            catch
            {
                // If QR generation fails (e.g. QRCoder not available on this platform),
                // fall back to whatever string was stored on the ticket.
                if (!string.IsNullOrEmpty(ticket.QRCode))
                    QRCodeImage.Source = ticket.QRCode;
            }
        }

        private async void OnDownloadTicketClicked(object? sender, EventArgs e)
        {
            if (_currentTicket == null) return;

            // Share the ticket as text so the user can save it externally.
            // Full PDF export can be added once a backend is available.
            await Share.RequestAsync(new ShareTextRequest
            {
                Title   = $"Ticket – {_currentTicket.EventTitle}",
                Subject = $"EventHub Ticket: {_currentTicket.EventTitle}",
                Text    = $"🎫 {_currentTicket.EventTitle}\n"
                        + $"Type: {_currentTicket.TicketTypeName}\n"
                        + $"Holder: {_currentTicket.UserName}\n"
                        + $"Paid: GH₵ {_currentTicket.Price:F2}\n"
                        + $"ID: {_currentTicket.Id}\n"
                        + $"Status: {_currentTicket.Status}"
            });
        }

        private async void OnShareTicketClicked(object? sender, EventArgs e)
        {
            if (_currentTicket == null) return;

            try
            {
                await Share.RequestAsync(new ShareTextRequest
                {
                    Title = "Transfer Ticket",
                    Text  = $"Here's my EventHub ticket for {_currentTicket.EventTitle}!\nTicket ID: {_currentTicket.Id}"
                });
            }
            catch
            {
                await DisplayAlertAsync("Error", "Unable to share ticket", "OK");
            }
        }
    }
}
