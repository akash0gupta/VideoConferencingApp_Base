namespace VideoConferencingApp.Application.Interfaces.Common.INotificationServices
{
    /// <summary>
    /// Defines methods for sending real-time notifications to users, groups, or all clients via SignalR.
    /// </summary>
    public interface INotificationService
    {
        /// <summary>
        /// Sends a real-time notification to a specific user.
        /// </summary>
        /// <param name="userId">The ID of the user to notify.</param>
        /// <param name="method">The name of the SignalR method to invoke on the client.</param>
        /// <param name="payload">The data to send to the client.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NotifyUserAsync(string userId, string method, object payload);

        /// <summary>
        /// Sends a real-time notification to a specific group of users.
        /// </summary>
        /// <param name="groupName">The name of the group to notify.</param>
        /// <param name="method">The name of the SignalR method to invoke on the clients.</param>
        /// <param name="payload">The data to send to the clients.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NotifyGroupAsync(string groupName, string method, object payload);

        /// <summary>
        /// Sends a real-time notification to all connected clients.
        /// </summary>
        /// <param name="method">The name of the SignalR method to invoke on the clients.</param>
        /// <param name="payload">The data to send to the clients.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task NotifyAllAsync(string method, object payload);
    }
}
