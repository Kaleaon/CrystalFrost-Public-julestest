using UnityEngine;
using OpenMetaverse;
using CrystalFrost.Services;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Controllers
{
    /// <summary>
    /// Handles all grid and simulator events including region crossing, connection status, and grid updates
    /// Uses service-based ClientManager for better dependency management
    /// </summary>
    public class GridEventController : MonoBehaviour
    {
        private ILogger<GridEventController> _logger;
        private IClientManagerService _clientManagerService;

        public System.Action<RegionCrossedEventArgs> OnRegionCrossed;
        public System.Action<SimConnectedEventArgs> OnSimConnected;
        public System.Action<SimConnectingEventArgs> OnSimConnecting;
        public System.Action<SimChangedEventArgs> OnSimChanged;
        public System.Action<SimDisconnectedEventArgs> OnSimDisconnected;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<GridEventController>>();
            _clientManagerService = ClientManager.GetService();
        }

        public void RegisterEventHandlers()
        {
            if (_clientManagerService.Client?.Network != null)
            {
                _clientManagerService.Client.Network.SimConnected += SimConnectedEventHandler;
                _clientManagerService.Client.Network.SimConnecting += SimConnectingEventHandler;
                _clientManagerService.Client.Network.SimDisconnected += SimDisconnectedEventHandler;
                _clientManagerService.Client.Network.SimChanged += SimChangedEventHandler;
                
                // Add other grid events as needed
                _clientManagerService.Client.Grid.GridRegion += GridRegionEventHandler;
                _clientManagerService.Client.Grid.GridItems += GridItemsEventHandler;
                _clientManagerService.Client.Grid.CoarseLocationUpdate += GridCourseLocationUpdateEventHandler;
            }
        }

        public void UnregisterEventHandlers()
        {
            if (_clientManagerService.Client?.Network != null)
            {
                _clientManagerService.Client.Network.SimConnected -= SimConnectedEventHandler;
                _clientManagerService.Client.Network.SimConnecting -= SimConnectingEventHandler;
                _clientManagerService.Client.Network.SimDisconnected -= SimDisconnectedEventHandler;
                _clientManagerService.Client.Network.SimChanged -= SimChangedEventHandler;
                
                _clientManagerService.Client.Grid.GridRegion -= GridRegionEventHandler;
                _clientManagerService.Client.Grid.GridItems -= GridItemsEventHandler;
                _clientManagerService.Client.Grid.CoarseLocationUpdate -= GridCourseLocationUpdateEventHandler;
            }
        }

        private void OnDestroy()
        {
            UnregisterEventHandlers();
        }

        public void RegionCrossedEventHandler(object sender, RegionCrossedEventArgs e)
        {
            if (_clientManagerService.IsMainThread)
            {
                _logger.LogInformation($"RegionCrossed: From {e.OldSimulator.Name} ({e.OldSimulator.Handle}) to {e.NewSimulator.Name} ({e.NewSimulator.Handle})");
                OnRegionCrossed?.Invoke(e);
            }
        }

        public void SimConnectedEventHandler(object sender, SimConnectedEventArgs e)
        {
            if (_clientManagerService.IsMainThread)
            {
                _logger.LogInformation($"Connected to sim: {e.Simulator.Name} / {e.Simulator.Handle}");
                OnSimConnected?.Invoke(e);
            }
        }

        public void SimConnectingEventHandler(object sender, SimConnectingEventArgs e)
        {
            if (_clientManagerService.IsMainThread)
            {
                _logger.LogInformation($"Connecting to sim: {e.Simulator.Name} / {e.Simulator.Handle}");
                OnSimConnecting?.Invoke(e);
            }
        }

        public void SimChangedEventHandler(object sender, SimChangedEventArgs e)
        {
            if (_clientManagerService.IsMainThread)
            {
                _logger.LogInformation($"Sim changed to {_clientManagerService.Client.Network.CurrentSim.Name}");
                OnSimChanged?.Invoke(e);
            }
        }

        public void SimDisconnectedEventHandler(object sender, SimDisconnectedEventArgs e)
        {
            if (_clientManagerService.IsMainThread)
            {
                _logger.LogInformation($"Disconnected from sim: {e.Simulator.Name} / {e.Simulator.Handle} / {e.Reason}");
                OnSimDisconnected?.Invoke(e);
            }
        }

        public void GridRegionEventHandler(object sender, GridRegionEventArgs e)
        {
            if (_clientManagerService.IsMainThread)
            {
                _logger.LogInformation($"GridRegionEvent {e.Region.Name}, {e.Region.RegionHandle}, <{e.Region.X},{e.Region.Y}>");
            }
        }

        public void GridItemsEventHandler(object sender, GridItemsEventArgs e)
        {
            if (_clientManagerService.IsMainThread)
            {
                _logger.LogInformation($"GridItemsEvent");
            }
        }

        public void GridCourseLocationUpdateEventHandler(object sender, CoarseLocationUpdateEventArgs e)
        {
            if (_clientManagerService.IsMainThread)
            {
                _logger.LogInformation($"CourseLocationUpdate new entries: {e.NewEntries.Count}, removed entries: {e.RemovedEntries.Count}");
            }
        }
    }
}