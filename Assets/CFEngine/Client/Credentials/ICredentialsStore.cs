using System.Collections.Generic;

namespace CrystalFrost.Client.Credentials
{
    /// <summary>
    /// Defines the interface for a credentials store.
    /// </summary>
    public interface ICredentialsStore : IList<LoginCredential>
    {
        /// <summary>
        /// Loads the credentials from the store.
        /// </summary>
        void Load();
        /// <summary>
        /// Saves the credentials to the store.
        /// </summary>
        void Save();
    }
}
