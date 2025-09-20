using UnityEngine;

namespace CrystalFrost.Client.Credentials
{
    /// <summary>
    /// Defines the interface for a factory that creates credentials stores.
    /// </summary>
    public interface ICredentialsStoreFactory
    {
        /// <summary>
        /// Gets a credentials store that is appropriate for the current operating system.
        /// </summary>
        /// <returns>A credentials store.</returns>
        ICredentialsStore GetCredentialsStore();
    }

    /// <summary>
    /// A factory that creates credentials stores.
    /// </summary>
    public class CredentialsStoreFactory : ICredentialsStoreFactory
    {
        /// <inheritdoc/>
        public ICredentialsStore GetCredentialsStore()
        {
            // Get a creditals store that is appropriate for the operating system.
            // if there is not an operating system specific one, return the 
            // default store. (The default store does not persist data).
            return Application.platform switch
            {
                RuntimePlatform.WindowsPlayer or
                RuntimePlatform.WindowsEditor
                    => Services.GetService<IWindowsCredentialsStore>(),
                // default - don't try to store credentials securly cause we
                // we don't yet know how for that OS.
                _ => Services.GetService<IDefaultCredentialsStore>(),
            };
        }
    }
}
