
using System;

namespace CrystalFrost.Client.Credentials
{
    /// <summary>
    /// Defines the information needed login.
    /// </summary>
    public class LoginCredential
    {
        /// <summary>
        /// The login server URL.
        /// </summary>
        public string LoginServer { get; set; } = string.Empty;
        /// <summary>
        /// The user's first name.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;
        /// <summary>
        /// The user's last name.
        /// </summary>
        public string LastName { get; set; } = string.Empty;
        /// <summary>
        /// The user's password.
        /// </summary>
        public string Password { get; set; } = string.Empty;
        /// <summary>
        /// The last time the credential was used.
        /// </summary>
        public DateTime LastUsed { get; set; } = DateTime.MinValue;
    }
}
