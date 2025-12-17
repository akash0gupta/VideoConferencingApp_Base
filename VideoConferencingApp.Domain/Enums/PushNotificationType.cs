using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.Enums
{

    public enum PushNotificationType { 
        SingleDevice,
        MultipleDevices,
        Topic,
        Batch
    }
}
