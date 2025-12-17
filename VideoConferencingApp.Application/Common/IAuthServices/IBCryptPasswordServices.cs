using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoConferencingApp.Application.Common.IAuthServices
{
    public interface IBCryptPasswordServices
    {
        /// <summary>
        /// Hash password using BCrypt
        /// </summary>
        string HashPassword(string password);


        /// <summary>
        /// Verify password against hash
        /// </summary>
        bool VerifyPassword(string password, string hash);

    }
}
