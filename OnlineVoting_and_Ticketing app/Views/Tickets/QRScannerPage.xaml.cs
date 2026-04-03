using Microsoft.Extensions.Logging;
using OnlineVoting_and_Ticketing_app.Services;

namespace OnlineVoting_and_Ticketing_app.Views.Tickets
{
    public partial class QRScannerPage : ContentPage
    {
        private readonly ITicketService _ticketService;
        private readonly ILogger<QRScannerPage> _logger;

        public QRScannerPage(ITicketService ticketService, ILogger<QRScannerPage> logger)
        {
            InitializeComponent();
            _ticketService = ticketService;
            _logger = logger;
        }

        private async void OnCheckInClicked(object sender, EventArgs e)
        {
            var ticketId = TicketIdEntry.Text?.Trim();

            if (string.IsNullOrWhiteSpace(ticketId))
            {
                ShowStatus("Please enter a ticket ID.", isSuccess: false);
                return;
            }

            CheckInButton.IsEnabled = false;
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            StatusBorder.IsVisible = false;

            try
            {
                var success = await _ticketService.CheckInByQRCodeAsync(ticketId);

                if (success)
                {
                    ShowStatus("✓  Check-in Successful", isSuccess: true);
                    TicketIdEntry.Text = string.Empty;
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(200));
                }
                else
                {
                    var isValid = await _ticketService.ValidateTicketAsync(ticketId);
                    ShowStatus(isValid
                        ? "✗  Ticket already used or invalid"
                        : "✗  Ticket not found or expired", isSuccess: false);
                    Vibration.Default.Vibrate(TimeSpan.FromMilliseconds(400));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Check-in failed for ticket {TicketId}", ticketId);
                ShowStatus("✗  Error processing ticket", isSuccess: false);
            }
            finally
            {
                LoadingIndicator.IsRunning = false;
                LoadingIndicator.IsVisible = false;
                CheckInButton.IsEnabled = true;
            }
        }

        private async void OnPasteClicked(object sender, EventArgs e)
        {
            var text = await Clipboard.Default.GetTextAsync();
            if (!string.IsNullOrWhiteSpace(text))
                TicketIdEntry.Text = text.Trim();
        }

        private void ShowStatus(string message, bool isSuccess)
        {
            StatusLabel.Text = message;
            StatusLabel.TextColor = isSuccess ? Color.FromArgb("#10B981") : Color.FromArgb("#EF4444");
            StatusBorder.BackgroundColor = isSuccess
                ? Color.FromArgb("#1010B981")
                : Color.FromArgb("#10EF4444");
            StatusBorder.IsVisible = true;
        }
    }
}
