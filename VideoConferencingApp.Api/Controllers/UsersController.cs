using Microsoft.AspNetCore.Mvc;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.Events;
using VideoConferencingApp.Application.Interfaces.Common.ICommonServices;
using VideoConferencingApp.Application.Interfaces.Common.IUserServices;
using VideoConferencingApp.Domain.Entities;


namespace VideoConferencingApp.API.Controllers
{
    public class UsersController : BaseController
    {
        private readonly IUserService _users;
        private readonly IMessageProducer _messageBus;

        public UsersController(IUserService users, IMessageProducer messageBus, ILogger<UsersController> logger)
            : base(logger)
        {
            _users = users;
            _messageBus = messageBus;
        }
    }
}