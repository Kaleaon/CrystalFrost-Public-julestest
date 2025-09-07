using UnityEngine;
using OpenMetaverse;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Controllers
{
    /// <summary>
    /// Handles all grid and simulator events including region crossing, connection status, and grid updates
    /// </summary>
    public class GridEventController : MonoBehaviour
    {
        private ILogger<GridEventController> _logger;

        public System.Action<RegionCrossedEventArgs> OnRegionCrossed;
        public System.Action<SimConnectedEventArgs> OnSimConnected;
        public System.Action<SimConnectingEventArgs> OnSimConnecting;
        public System.Action<SimChangedEventArgs> OnSimChanged;
        public System.Action<SimDisconnectedEventArgs> OnSimDisconnected;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<GridEventController>>();
        }

        public void RegisterEventHandlers()
        {
            if (ClientManager.client?.Network != null)
            {
                ClientManager.client.Network.SimConnected += SimConnectedEventHandler;
                ClientManager.client.Network.SimConnecting += SimConnectingEventHandler;
                ClientManager.client.Network.SimDisconnected += SimDisconnectedEventHandler;
                ClientManager.client.Network.SimChanged += SimChangedEventHandler;
                
                // Add other grid events as needed
                ClientManager.client.Grid.GridRegion += GridRegionEventHandler;
                ClientManager.client.Grid.GridItems += GridItemsEventHandler;
                ClientManager.client.Grid.CoarseLocationUpdate += GridCourseLocationUpdateEventHandler;
            }
        }

        public void UnregisterEventHandlers()
        {
            if (ClientManager.client?.Network != null)
            {
                ClientManager.client.Network.SimConnected -= SimConnectedEventHandler;
                ClientManager.client.Network.SimConnecting -= SimConnectingEventHandler;
                ClientManager.client.Network.SimDisconnected -= SimDisconnectedEventHandler;
                ClientManager.client.Network.SimChanged -= SimChangedEventHandler;
                
                ClientManager.client.Grid.GridRegion -= GridRegionEventHandler;
                ClientManager.client.Grid.GridItems -= GridItemsEventHandler;
                ClientManager.client.Grid.CoarseLocationUpdate -= GridCourseLocationUpdateEventHandler;
            }
        }

        private void OnDestroy()
        {
            UnregisterEventHandlers();
        }

        public void RegionCrossedEventHandler(object sender, RegionCrossedEventArgs e)
        {
            if (ClientManager.IsMainThread)
            {
                _logger.LogInformation($"RegionCrossed: From {e.OldSimulator.Name} ({e.OldSimulator.Handle}) to {e.NewSimulator.Name} ({e.NewSimulator.Handle})");
                OnRegionCrossed?.Invoke(e);
            }
        }

        public void SimConnectedEventHandler(object sender, SimConnectedEventArgs e)
        {
            if (ClientManager.IsMainThread)
            {
                _logger.LogInformation($"Connected to sim: {e.Simulator.Name} / {e.Simulator.Handle}");
                OnSimConnected?.Invoke(e);
            }
        }

        public void SimConnectingEventHandler(object sender, SimConnectingEventArgs e)
        {
            if (ClientManager.IsMainThread)
            {
                _logger.LogInformation($"Connecting to sim: {e.Simulator.Name} / {e.Simulator.Handle}");
                OnSimConnecting?.Invoke(e);
            }
        }

        public void SimChangedEventHandler(object sender, SimChangedEventArgs e)
        {
            if (ClientManager.IsMainThread)
            {
                _logger.LogInformation($"Sim changed to {ClientManager.client.Network.CurrentSim.Name}");
                OnSimChanged?.Invoke(e);
            }
        }

        public void SimDisconnectedEventHandler(object sender, SimDisconnectedEventArgs e)
        {
            if (ClientManager.IsMainThread)
            {
                _logger.LogInformation($"Disconnected from sim: {e.Simulator.Name} / {e.Simulator.Handle} / {e.Reason}");
                OnSimDisconnected?.Invoke(e);
            }
        }

        public void GridRegionEventHandler(object sender, GridRegionEventArgs e)
        {
            if (ClientManager.IsMainThread)
            {
                _logger.LogInformation($"GridRegionEvent {e.Region.Name}, {e.Region.RegionHandle}, <{e.Region.X},{e.Region.Y}>");
            }
        }

        public void GridItemsEventHandler(object sender, GridItemsEventArgs e)
        {
            if (ClientManager.IsMainThread)
            {
                _logger.LogInformation($"GridItemsEvent");
            }
        }

        public void GridCourseLocationUpdateEventHandler(object sender, CoarseLocationUpdateEventArgs e)
        {
            if (ClientManager.IsMainThread)
            {
                _logger.LogInformation($"CourseLocationUpdate new entries: {e.NewEntries.Count}, removed entries: {e.RemovedEntries.Count}");
            }
        }
    }
}