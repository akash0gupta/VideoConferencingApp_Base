using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Enums
{
    public enum MessageType
    {
        Text = 0,
        Image = 1,
        Video = 2,
        Audio = 3,
        File = 4,
        Location = 5,
        Contact = 6,
        System = 99
    }

    public enum MessageStatus
    {
        Sending = 0,
        Sent = 1,
        Delivered = 2,
        Read = 3,
        Failed = 4
    }

    public enum CallsType
    {
        Voice = 0,
        Video = 1,
        ScreenShare = 2
    }

    public enum CallStatus
    {
        Initiating = 0,
        Ringing = 1,
        Accepted = 2,
        Rejected = 3,
        Ended = 4,
        Missed = 5,
        Failed = 6,
        Busy = 7
    }

    public enum UserPresenceStatus
    {
        Online = 0,
        Offline = 1,
        Away = 2,
        Busy = 3,
        DoNotDisturb = 4
    }

    public enum GroupRole
    {
        Owner = 0,
        Admin = 1,
        Member = 2
    }

    public enum SignalType
    {
        Offer = 0,
        Answer = 1,
        IceCandidate = 2,
        HangUp = 3
    }


}
