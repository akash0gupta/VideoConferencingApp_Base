using Moq;
using VideoConferencingApp.Application.Common.Abstractions;
using VideoConferencingApp.Application.Common.Interfaces;
using VideoConferencingApp.Application.Services;
using VideoConferencingApp.Domain.Entities;
using VideoConferencingApp.Domain.Enums;
using VideoConferencingApp.Domain.Interfaces;
using Xunit;

namespace VideoConferencingApp.Application.Test.Services
{
    public class ContactServiceTests
    {
        private readonly Mock<IRepository<User>> _userRepositoryMock;
        private readonly Mock<IRepository<Contact>> _contactRepositoryMock;
        private readonly Mock<IEventPublisher> _eventPublisherMock;
        private readonly Mock<INotificationService> _notificationServiceMock;
        private readonly ContactService _sut;

        public ContactServiceTests()
        {
            _userRepositoryMock = new Mock<IRepository<User>>();
            _contactRepositoryMock = new Mock<IRepository<Contact>>();
            _eventPublisherMock = new Mock<IEventPublisher>();
            _notificationServiceMock = new Mock<INotificationService>();

            _sut = new ContactService(
                _userRepositoryMock.Object,
                _contactRepositoryMock.Object,
                _eventPublisherMock.Object,
                _notificationServiceMock.Object
            );
        }

        [Fact]
        public async Task SearchUsersAsync_WithValidQuery_ReturnsMatchingUsers()
        {
            // Arrange
            const long currentUserId = 1;
            const string query = "john";
            var users = new List<User>
            {
                new() { Id = 2, Username = "john_doe", Email = "john@example.com" },
                new() { Id = 3, Username = "johnny", Email = "johnny@example.com" },
                new() { Id = currentUserId, Username = "johnson", Email = "johnson@example.com" }
            };

            _userRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
                .ReturnsAsync(users.Where(u => u.Id != currentUserId).ToList());

            // Act
            var result = await _sut.SearchUsersAsync(currentUserId, query);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.DoesNotContain(result, u => u.Id == currentUserId);
            Assert.All(result, u => Assert.Contains(query, u.Username, StringComparison.OrdinalIgnoreCase));
        }

        [Fact]
        public async Task SendRequestAsync_WithValidData_CreatesContactRequest()
        {
            // Arrange
            const long requesterId = 1;
            const long addresseeId = 2;
            var newContact = new Contact
            {
                RequesterId = requesterId,
                AddresseeId = addresseeId,
                Status = ContactStatus.Pending,
                AddresseeUserName = "test",
                RequesterUserName = "test",
                IsActive = true
            };

            _contactRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Contact, bool>>>()))
                .ReturnsAsync(new List<Contact>());

            _contactRepositoryMock.Setup(x => x.InsertAsync(It.IsAny<Contact>()))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _sut.SendRequestAsync(requesterId, addresseeId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(requesterId, result.RequesterId);
            Assert.Equal(addresseeId, result.AddresseeId);
            Assert.Equal(ContactStatus.Pending, result.Status);
            _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<ContactRequestSentEvent>()), Times.Once);
        }

        [Theory]
        [InlineData(1, 1)] // Same user
        public async Task SendRequestAsync_WithInvalidData_ThrowsException(long requesterId, long addresseeId)
        {
            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() => 
                _sut.SendRequestAsync(requesterId, addresseeId));
        }

        [Fact]
        public async Task AcceptRequestAsync_WithValidRequest_UpdatesContactStatus()
        {
            // Arrange
            const long contactId = 1;
            const long currentUserId = 2;
            var contact = new Contact
            {
                Id = contactId,
                RequesterId = 1,
                AddresseeId = currentUserId,
                Status = ContactStatus.Pending
            };

            _contactRepositoryMock.Setup(x => x.GetByIdAsync(contactId))
                .ReturnsAsync(contact);

            // Act
            var result = await _sut.AcceptRequestAsync(contactId, currentUserId);

            // Assert
            Assert.True(result);
            Assert.Equal(ContactStatus.Accepted, contact.Status);
            _contactRepositoryMock.Verify(x => x.UpdateAsync(contact), Times.Once);
            _eventPublisherMock.Verify(x => x.PublishAsync(It.IsAny<ContactRequestAcceptedEvent>()), Times.Once);
        }

        [Fact]
        public async Task GetContactsAsync_ReturnsAcceptedContacts()
        {
            // Arrange
            const long userId = 1;
            var contacts = new List<Contact>
            {
                new() { RequesterId = userId, AddresseeId = 2, Status = ContactStatus.Accepted },
                new() { RequesterId = 3, AddresseeId = userId, Status = ContactStatus.Accepted },
                new() { RequesterId = userId, AddresseeId = 4, Status = ContactStatus.Pending }
            };

            _contactRepositoryMock.Setup(x => x.FindAsync(It.IsAny<Expression<Func<Contact, bool>>>()))
                .ReturnsAsync(contacts.Where(c => 
                    (c.RequesterId == userId || c.AddresseeId == userId) && 
                    c.Status == ContactStatus.Accepted).ToList());

            // Act
            var result = await _sut.GetContactsAsync(userId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.All(result, c => Assert.Equal(ContactStatus.Accepted, c.Status));
            Assert.All(result, c => Assert.True(c.RequesterId == userId || c.AddresseeId == userId));
        }
    }
}