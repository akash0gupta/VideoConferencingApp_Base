using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Enums
{
    public enum NotificationErrorCode
    {
        Unknown,
        InvalidArgument,
        Unregistered,
        SenderIdMismatch,
        QuotaExceeded,
        Unavailable,
        Internal,
        ThirdPartyAuthError
    }
}
