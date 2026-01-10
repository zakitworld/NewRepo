using Firebase.Database;
using Firebase.Database.Query;
using OnlineVoting_and_Ticketing_app.Constants;
using OnlineVoting_and_Ticketing_app.Models;
using QRCoder;
using System.Text;

namespace OnlineVoting_and_Ticketing_app.Services
{
    public class FirebaseTicketService : ITicketService
    {
        private readonly FirebaseClient _firebaseClient;
        private readonly IEventService _eventService;

        public FirebaseTicketService(IEventService eventService)
        {
            _firebaseClient = new FirebaseClient(FirebaseConfig.DatabaseUrl);
            _eventService = eventService;
        }

        public async Task<List<Ticket>> GetUserTicketsAsync(string userId)
        {
            try
            {
                var tickets = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionTickets)
                    .OnceAsync<Ticket>();

                return tickets
                    .Where(t => t.Object.UserId == userId)
                    .Select(t => new Ticket
                    {
                        Id = t.Key,
                        EventId = t.Object.EventId,
                        EventTitle = t.Object.EventTitle,
                        UserId = t.Object.UserId,
                        UserName = t.Object.UserName,
                        UserEmail = t.Object.UserEmail,
                        TicketTypeId = t.Object.TicketTypeId,
                        TicketTypeName = t.Object.TicketTypeName,
                        Price = t.Object.Price,
                        QRCode = t.Object.QRCode,
                        Status = t.Object.Status,
                        PurchasedAt = t.Object.PurchasedAt,
                        CheckedInAt = t.Object.CheckedInAt,
                        TransactionId = t.Object.TransactionId,
                        PaymentStatus = t.Object.PaymentStatus
                    })
                    .OrderByDescending(t => t.PurchasedAt)
                    .ToList();
            }
            catch
            {
                return new List<Ticket>();
            }
        }

        public async Task<Ticket?> GetTicketByIdAsync(string ticketId)
        {
            try
            {
                var ticket = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionTickets)
                    .Child(ticketId)
                    .OnceSingleAsync<Ticket>();

                if (ticket == null)
                    return null;

                ticket.Id = ticketId;
                return ticket;
            }
            catch
            {
                return null;
            }
        }

        public async Task<(bool Success, string? Error, Ticket? Ticket)> PurchaseTicketAsync(string eventId, string ticketTypeId, string userId)
        {
            try
            {
                var eventData = await _eventService.GetEventByIdAsync(eventId);
                if (eventData == null)
                    return (false, "Event not found", null);

                var ticketType = eventData.TicketTypes.FirstOrDefault(t => t.Id == ticketTypeId);
                if (ticketType == null)
                    return (false, "Ticket type not found", null);

                if (ticketType.AvailableQuantity <= 0)
                    return (false, "Tickets sold out", null);

                var user = Preferences.Get(AppConstants.Preferences.UserName, "User");
                var email = Preferences.Get(AppConstants.Preferences.UserEmail, "");

                var ticket = new Ticket
                {
                    EventId = eventId,
                    EventTitle = eventData.Title,
                    UserId = userId,
                    UserName = user,
                    UserEmail = email,
                    TicketTypeId = ticketTypeId,
                    TicketTypeName = ticketType.Name,
                    Price = ticketType.Price,
                    Status = TicketStatus.Active,
                    PurchasedAt = DateTime.UtcNow,
                    PaymentStatus = PaymentStatus.Pending
                };

                var result = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionTickets)
                    .PostAsync(ticket);

                ticket.Id = result.Key;
                ticket.QRCode = await GenerateQRCodeAsync(ticket.Id);

                await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionTickets)
                    .Child(ticket.Id)
                    .PutAsync(ticket);

                ticketType.AvailableQuantity--;
                eventData.AvailableTickets--;
                await _eventService.UpdateEventAsync(eventData);

                return (true, null, ticket);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, null);
            }
        }

        public async Task<bool> ValidateTicketAsync(string ticketId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                return ticket != null && ticket.Status == TicketStatus.Active;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CheckInTicketAsync(string ticketId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                if (ticket == null || ticket.Status != TicketStatus.Active)
                    return false;

                ticket.Status = TicketStatus.Used;
                ticket.CheckedInAt = DateTime.UtcNow;

                await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionTickets)
                    .Child(ticketId)
                    .PutAsync(ticket);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> CancelTicketAsync(string ticketId)
        {
            try
            {
                var ticket = await GetTicketByIdAsync(ticketId);
                if (ticket == null)
                    return false;

                ticket.Status = TicketStatus.Cancelled;

                await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionTickets)
                    .Child(ticketId)
                    .PutAsync(ticket);

                var eventData = await _eventService.GetEventByIdAsync(ticket.EventId);
                if (eventData != null)
                {
                    var ticketType = eventData.TicketTypes.FirstOrDefault(t => t.Id == ticket.TicketTypeId);
                    if (ticketType != null)
                    {
                        ticketType.AvailableQuantity++;
                        eventData.AvailableTickets++;
                        await _eventService.UpdateEventAsync(eventData);
                    }
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        public Task<string> GenerateQRCodeAsync(string ticketId)
        {
            try
            {
                using var qrGenerator = new QRCodeGenerator();
                var qrCodeData = qrGenerator.CreateQrCode(ticketId, QRCodeGenerator.ECCLevel.Q);
                using var qrCode = new PngByteQRCode(qrCodeData);
                var qrCodeBytes = qrCode.GetGraphic(20);
                var base64String = Convert.ToBase64String(qrCodeBytes);
                return Task.FromResult($"data:image/png;base64,{base64String}");
            }
            catch
            {
                return Task.FromResult(string.Empty);
            }
        }

        public async Task<List<Ticket>> GetEventTicketsAsync(string eventId)
        {
            try
            {
                var tickets = await _firebaseClient
                    .Child(AppConstants.Firebase.CollectionTickets)
                    .OnceAsync<Ticket>();

                return tickets
                    .Where(t => t.Object.EventId == eventId)
                    .Select(t => new Ticket
                    {
                        Id = t.Key,
                        EventId = t.Object.EventId,
                        EventTitle = t.Object.EventTitle,
                        UserId = t.Object.UserId,
                        UserName = t.Object.UserName,
                        UserEmail = t.Object.UserEmail,
                        TicketTypeId = t.Object.TicketTypeId,
                        TicketTypeName = t.Object.TicketTypeName,
                        Price = t.Object.Price,
                        QRCode = t.Object.QRCode,
                        Status = t.Object.Status,
                        PurchasedAt = t.Object.PurchasedAt,
                        CheckedInAt = t.Object.CheckedInAt,
                        TransactionId = t.Object.TransactionId,
                        PaymentStatus = t.Object.PaymentStatus
                    })
                    .OrderByDescending(t => t.PurchasedAt)
                    .ToList();
            }
            catch
            {
                return new List<Ticket>();
            }
        }
    }
}
