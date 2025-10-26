using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Domain.DTOs.Authentication
{
    public class TwoFactorSetupDto
    {
        public string Secret { get; set; }
        public string QrCodeUri { get; set; }
        public string ManualEntryKey { get; set; }
        public List<string> BackupCodes { get; set; }
    }

}
