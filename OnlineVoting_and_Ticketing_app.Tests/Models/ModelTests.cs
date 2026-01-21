using FluentAssertions;
using OnlineVoting_and_Ticketing_app.Models;
using OnlineVoting_and_Ticketing_app.Tests.Helpers;
using Xunit;

namespace OnlineVoting_and_Ticketing_app.Tests.Models
{
    public class EventModelTests
    {
        [Fact]
        public void Event_DefaultValues_AreCorrect()
        {
            // Act
            var eventModel = new Event();

            // Assert
            eventModel.Id.Should().BeEmpty();
            eventModel.Title.Should().BeEmpty();
            eventModel.Description.Should().BeEmpty();
            eventModel.Status.Should().Be(EventStatus.Draft);
            eventModel.TicketTypes.Should().BeEmpty();
            eventModel.IsPublished.Should().BeFalse();
        }

        [Fact]
        public void Event_CreatedAt_IsSetToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var eventModel = new Event();

            // Assert
            eventModel.CreatedAt.Should().BeOnOrAfter(before);
            eventModel.UpdatedAt.Should().BeOnOrAfter(before);
        }

        [Theory]
        [InlineData(EventCategory.Music)]
        [InlineData(EventCategory.Sports)]
        [InlineData(EventCategory.Conference)]
        [InlineData(EventCategory.Workshop)]
        [InlineData(EventCategory.Festival)]
        public void Event_Category_CanBeSet(EventCategory category)
        {
            // Act
            var eventModel = new Event { Category = category };

            // Assert
            eventModel.Category.Should().Be(category);
        }

        [Theory]
        [InlineData(EventType.InPerson)]
        [InlineData(EventType.Virtual)]
        [InlineData(EventType.Hybrid)]
        public void Event_Type_CanBeSet(EventType eventType)
        {
            // Act
            var eventModel = new Event { Type = eventType };

            // Assert
            eventModel.Type.Should().Be(eventType);
        }

        [Theory]
        [InlineData(EventStatus.Draft)]
        [InlineData(EventStatus.Published)]
        [InlineData(EventStatus.Ongoing)]
        [InlineData(EventStatus.Completed)]
        [InlineData(EventStatus.Cancelled)]
        public void Event_Status_CanBeSet(EventStatus status)
        {
            // Act
            var eventModel = new Event { Status = status };

            // Assert
            eventModel.Status.Should().Be(status);
        }
    }

    public class TicketTypeModelTests
    {
        [Fact]
        public void TicketType_DefaultValues_AreCorrect()
        {
            // Act
            var ticketType = new TicketType();

            // Assert
            ticketType.Id.Should().BeEmpty();
            ticketType.Name.Should().BeEmpty();
            ticketType.Description.Should().BeEmpty();
            ticketType.Price.Should().Be(0);
            ticketType.TotalQuantity.Should().Be(0);
            ticketType.AvailableQuantity.Should().Be(0);
        }

        [Fact]
        public void TicketType_Price_CanBeDecimal()
        {
            // Act
            var ticketType = new TicketType { Price = 99.99m };

            // Assert
            ticketType.Price.Should().Be(99.99m);
        }
    }

    public class PollModelTests
    {
        [Fact]
        public void Poll_DefaultValues_AreCorrect()
        {
            // Act
            var poll = new Poll();

            // Assert
            poll.Id.Should().BeEmpty();
            poll.Title.Should().BeEmpty();
            poll.Status.Should().Be(PollStatus.Draft);
            poll.Options.Should().BeEmpty();
            poll.TotalVotes.Should().Be(0);
            poll.RequireAuthentication.Should().BeTrue();
            poll.AllowMultipleChoices.Should().BeFalse();
            poll.IsAnonymous.Should().BeFalse();
        }

        [Theory]
        [InlineData(PollStatus.Draft)]
        [InlineData(PollStatus.Active)]
        [InlineData(PollStatus.Closed)]
        [InlineData(PollStatus.Archived)]
        public void Poll_Status_CanBeSet(PollStatus status)
        {
            // Act
            var poll = new Poll { Status = status };

            // Assert
            poll.Status.Should().Be(status);
        }

        [Theory]
        [InlineData(PollType.SingleChoice)]
        [InlineData(PollType.MultipleChoice)]
        [InlineData(PollType.Rating)]
        [InlineData(PollType.OpenEnded)]
        public void Poll_Type_CanBeSet(PollType pollType)
        {
            // Act
            var poll = new Poll { Type = pollType };

            // Assert
            poll.Type.Should().Be(pollType);
        }

        [Fact]
        public void Poll_EventId_CanBeNull()
        {
            // Act
            var poll = new Poll { EventId = null };

            // Assert
            poll.EventId.Should().BeNull();
        }
    }

    public class PollOptionModelTests
    {
        [Fact]
        public void PollOption_DefaultValues_AreCorrect()
        {
            // Act
            var option = new PollOption();

            // Assert
            option.Id.Should().BeEmpty();
            option.PollId.Should().BeEmpty();
            option.Text.Should().BeEmpty();
            option.VoteCount.Should().Be(0);
            option.Order.Should().Be(0);
        }

        [Fact]
        public void PollOption_ImageUrl_CanBeNull()
        {
            // Act
            var option = new PollOption { ImageUrl = null };

            // Assert
            option.ImageUrl.Should().BeNull();
        }
    }

    public class VoteModelTests
    {
        [Fact]
        public void Vote_DefaultValues_AreCorrect()
        {
            // Act
            var vote = new Vote();

            // Assert
            vote.Id.Should().BeEmpty();
            vote.PollId.Should().BeEmpty();
            vote.UserId.Should().BeEmpty();
            vote.SelectedOptionIds.Should().BeEmpty();
        }

        [Fact]
        public void Vote_VotedAt_IsSetToUtcNow()
        {
            // Arrange
            var before = DateTime.UtcNow;

            // Act
            var vote = new Vote();

            // Assert
            vote.VotedAt.Should().BeOnOrAfter(before);
        }

        [Fact]
        public void Vote_SelectedOptionIds_CanHaveMultipleItems()
        {
            // Act
            var vote = new Vote
            {
                SelectedOptionIds = new List<string> { "option-1", "option-2", "option-3" }
            };

            // Assert
            vote.SelectedOptionIds.Should().HaveCount(3);
        }
    }

    public class TicketModelTests
    {
        [Fact]
        public void Ticket_DefaultValues_AreCorrect()
        {
            // Act
            var ticket = new Ticket();

            // Assert
            ticket.Id.Should().BeEmpty();
            ticket.EventId.Should().BeEmpty();
            ticket.UserId.Should().BeEmpty();
            ticket.Status.Should().Be(TicketStatus.Active);
            ticket.PaymentStatus.Should().Be(PaymentStatus.Pending);
            ticket.CheckedInAt.Should().BeNull();
        }

        [Theory]
        [InlineData(TicketStatus.Active)]
        [InlineData(TicketStatus.Used)]
        [InlineData(TicketStatus.Cancelled)]
        [InlineData(TicketStatus.Expired)]
        public void Ticket_Status_CanBeSet(TicketStatus status)
        {
            // Act
            var ticket = new Ticket { Status = status };

            // Assert
            ticket.Status.Should().Be(status);
        }

        [Theory]
        [InlineData(PaymentStatus.Pending)]
        [InlineData(PaymentStatus.Processing)]
        [InlineData(PaymentStatus.Completed)]
        [InlineData(PaymentStatus.Failed)]
        [InlineData(PaymentStatus.Refunded)]
        public void Ticket_PaymentStatus_CanBeSet(PaymentStatus paymentStatus)
        {
            // Act
            var ticket = new Ticket { PaymentStatus = paymentStatus };

            // Assert
            ticket.PaymentStatus.Should().Be(paymentStatus);
        }

        [Fact]
        public void Ticket_CheckedInAt_CanBeSet()
        {
            // Arrange
            var checkedInTime = DateTime.UtcNow;

            // Act
            var ticket = new Ticket { CheckedInAt = checkedInTime };

            // Assert
            ticket.CheckedInAt.Should().Be(checkedInTime);
        }
    }

    public class UserModelTests
    {
        [Fact]
        public void User_DefaultValues_AreCorrect()
        {
            // Act
            var user = new User();

            // Assert
            user.Id.Should().BeEmpty();
            user.Email.Should().BeEmpty();
            user.FullName.Should().BeEmpty();
            user.Role.Should().Be(UserRole.User);
            user.IsActive.Should().BeTrue();
            user.IsEmailVerified.Should().BeFalse();
        }

        [Theory]
        [InlineData(UserRole.User)]
        [InlineData(UserRole.EventOrganizer)]
        [InlineData(UserRole.Admin)]
        public void User_Role_CanBeSet(UserRole role)
        {
            // Act
            var user = new User { Role = role };

            // Assert
            user.Role.Should().Be(role);
        }
    }
}
