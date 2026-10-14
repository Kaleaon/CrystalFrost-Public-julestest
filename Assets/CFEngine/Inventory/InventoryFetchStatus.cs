using System;

namespace CrystalFrost.Inventory
{
    /// <summary>
    /// Tracks the synchronization status of inventory category nodes / folders.
    /// </summary>
    public enum InventoryFetchStatus
    {
        /// <summary>
        /// Folder contents have not yet been requested or retrieved from the server.
        /// </summary>
        Unfetched = 0,

        /// <summary>
        /// Folder contents request is currently active in the pre-fetch queue.
        /// </summary>
        Fetching = 1,

        /// <summary>
        /// Folder contents have been successfully fetched and indexed locally.
        /// </summary>
        Fetched = 2
    }

    /// <summary>
    /// Priority level for folder fetch requests.
    /// </summary>
    public enum FetchPriority
    {
        Low = 0,
        Normal = 1,
        High = 2
    }
}
