using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.Interfaces.Common.ICommonServices
{
    public interface IInputValidator
    {
        bool IsValidUserId(string userId);
        bool IsValidUsername(string username);
        bool IsValidSdp(string sdp);
        bool IsValidIceCandidate(string candidate);
        string SanitizeInput(string input);
    }
}
