using Microsoft.AspNetCore.SignalR;
using Moq;
using VideoConferencingApp.Infrastructure.RealTime;
using Xunit;

namespace VideoConferencingApp.Infrastructure.Test.RealTime
{
    public class SignalRNotificationServiceTests
    {
        private readonly Mock<IHubContext<AppHub>> _mockHubContext;
        private readonly Mock<IClientProxy> _mockClientProxy;
        private readonly SignalRNotificationService _service;

        public SignalRNotificationServiceTests()
        {
            _mockHubContext = new Mock<IHubContext<AppHub>>();
            _mockClientProxy = new Mock<IClientProxy>();

            // Setup hub clients mock chain
            var mockClients = new Mock<IHubClients>();
            _mockHubContext.Setup(x => x.Clients).Returns(mockClients.Object);

            // Setup various client group scenarios
            mockClients
                .Setup(x => x.Group(It.IsAny<string>()))
                .Returns(_mockClientProxy.Object);

            mockClients
                .Setup(x => x.All)
                .Returns(_mockClientProxy.Object);

            _service = new SignalRNotificationService(_mockHubContext.Object);
        }

        [Fact]
        public async Task NotifyUserAsync_ShouldSendToUserGroup()
        {
            // Arrange
            var userId = "123";
            var method = "TestMethod";
            var payload = new { data = "test" };

            _mockClientProxy
                .Setup(x => x.SendCoreAsync(
                    method,
                    It.Is<object[]>(o => o[0] == payload),
                    default))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyUserAsync(userId, method, payload);

            // Assert
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    method,
                    It.Is<object[]>(o => o[0] == payload),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task NotifyGroupAsync_ShouldSendToSpecifiedGroup()
        {
            // Arrange
            var groupName = "testGroup";
            var method = "TestMethod";
            var payload = new { data = "test" };

            _mockClientProxy
                .Setup(x => x.SendCoreAsync(
                    method,
                    It.Is<object[]>(o => o[0] == payload),
                    default))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyGroupAsync(groupName, method, payload);

            // Assert
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    method,
                    It.Is<object[]>(o => o[0] == payload),
                    default),
                Times.Once);
        }

        [Fact]
        public async Task NotifyAllAsync_ShouldSendToAllClients()
        {
            // Arrange
            var method = "TestMethod";
            var payload = new { data = "test" };

            _mockClientProxy
                .Setup(x => x.SendCoreAsync(
                    method,
                    It.Is<object[]>(o => o[0] == payload),
                    default))
                .Returns(Task.CompletedTask);

            // Act
            await _service.NotifyAllAsync(method, payload);

            // Assert
            _mockClientProxy.Verify(
                x => x.SendCoreAsync(
                    method,
                    It.Is<object[]>(o => o[0] == payload),
                    default),
                Times.Once);
        }
    }
}