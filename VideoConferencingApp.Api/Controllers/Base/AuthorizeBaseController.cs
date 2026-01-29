using MapsterMapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using VideoConferencingApp.API.Controllers.Base;
using VideoConferencingApp.Application.Common.IAuthServices;
using VideoConferencingApp.Application.Services.UserServices;
using VideoConferencingApp.Controllers;
using VideoConferencingApp.Infrastructure.Services.AuthServices;

namespace VideoConferencingApp.Api.Controllers.Base
{
    [Authorize]
    public class AuthorizeBaseController: BaseController
    {
        public AuthorizeBaseController(
            ILogger logger,
            ICurrentUserService currentUserService,
            IHttpContextService httpContextService,
            IResponseHeaderService responseHeaderService
            ) : base(logger, currentUserService, httpContextService, responseHeaderService)
        {

        }
    }
}
