using System;
using System.Collections;
using UnityEngine;
using OpenMetaverse;
using CrystalFrost;
using CrystalFrost.Scripts;
using CrystalFrost.Client.Credentials;
using CrystalFrost.Config;
using CrystalFrost.Services;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Controllers
{
    /// <summary>
    /// Handles authentication logic including login, logout, and credential management
    /// Uses service-based ClientManager instead of static access for better testability
    /// </summary>
    public class AuthenticationController : MonoBehaviour
    {
        private ILogger<AuthenticationController> _logger;
        private ICredentialsStore _credentials;
        private ILoginUriProvider _loginUriProvider;
        private IClientManagerService _clientManagerService;
        private LoginCredential _currentCredential;

        public System.Action OnLoginSuccess;
        public System.Action OnLogoutComplete;
        public System.Action<string> OnStatusUpdate;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<AuthenticationController>>();
            _loginUriProvider = Services.GetService<ILoginUriProvider>();
            _credentials = Services.GetService<ICredentialsStore>();
            _clientManagerService = ClientManager.GetService(); // Use service instead of static access
        }

        public void Initialize()
        {
            _credentials.Load();
            if (!_credentials.Any()) 
                _credentials.Add(new());
            _currentCredential = _credentials.First();
        }

        public LoginCredential GetCurrentCredential()
        {
            return _currentCredential;
        }

        public void TryLogin(string firstName, string lastName, string password, string customGridURL)
        {
            StartCoroutine(LoginCoroutine(firstName, lastName, password, customGridURL));
        }

        private IEnumerator LoginCoroutine(string firstName, string lastName, string password, string customGridURL)
        {
            _logger.LogInformation($"Logging in as {firstName} {lastName}");
            OnStatusUpdate?.Invoke($"Connecting as {firstName} {lastName}...");

            // Update credentials
            _currentCredential.FirstName = firstName;
            _currentCredential.LastName = lastName;
            _currentCredential.Password = password;

            yield return null;

            // Determine login URI
            string loginUri = DetermineLoginUri(customGridURL);
            _logger.LoggingIn(firstName, lastName, loginUri);

            try
            {
                // Create login parameters - matching original constructor signature
                LoginParams loginParams = new(
                    _clientManagerService.Client,
                    firstName,
                    lastName, 
                    password,
                    "CrystalFrost",
                    "0.2",
                    loginUri
                );

                if (loginParams.URI != loginUri) _clientManagerService.IsOpenSim = true;

                // Perform login
                bool loginSuccess = _clientManagerService.Client.Network.Login(loginParams);
                
                if (loginSuccess)
                {
                    Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + _clientManagerService.Client.Network.LoginMessage);
                    Console.WriteLine("Logging in. The viewer might appear to lock up for a short while, while the sim floods it with new objects.");

                    _logger.LogInformation("Login successful");
                    OnStatusUpdate?.Invoke("Login successful!");
                    
                    // Set up client state - matching original logic
                    _clientManagerService.Client.Network.CurrentSim.Caps.CapabilitiesReceived += ((sender, e) => {
                        _clientManagerService.Active = true;
                    });
                    _clientManagerService.Client.Estate.RequestInfo();

                    // Re-initialize the SimManager for the new session
                    if (_clientManagerService.SimManager != null)
                    {
                        (_clientManagerService.SimManager as SimManager).Initialize();
                    }

                    // Update credential last used time
                    _currentCredential.LastUsed = System.DateTime.UtcNow;

                    OnLoginSuccess?.Invoke();
                }
                else
                {
                    Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + _clientManagerService.Client.Network.LoginMessage);
                    _logger.LogError($"Login failed: {_clientManagerService.Client.Network.LoginMessage}");
                    OnStatusUpdate?.Invoke($"Login failed: {_clientManagerService.Client.Network.LoginMessage}");
                    _clientManagerService.Active = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login");
                OnStatusUpdate?.Invoke($"Login error: {ex.Message}");
                _clientManagerService.Active = false;
            }
        }

        private string DetermineLoginUri(string customGridURL)
        {
            if (!string.IsNullOrEmpty(customGridURL))
            {
                return customGridURL;
            }

            var defaultUri = _loginUriProvider.GetLoginUri();
            if (!string.IsNullOrEmpty(defaultUri))
            {
                return defaultUri;
            }

            return OpenMetaverse.Settings.AGNI_LOGIN_SERVER;
        }

        public void Logout()
        {
            StartCoroutine(LogoutCoroutine());
        }

        private IEnumerator LogoutCoroutine()
        {
            _logger.LoggingOut();
            OnStatusUpdate?.Invoke("Logging out...");

            yield return null;

            try
            {
                // Gracefully disconnect from the network
                if (_clientManagerService.Client != null && _clientManagerService.Client.Network.Connected)
                {
                    _clientManagerService.Client.Network.Logout();
                }

                _clientManagerService.Active = false;

                // Dispose managers to clean up state
                if (_clientManagerService.AssetManager != null)
                {
                    _clientManagerService.AssetManager.Dispose();
                    _clientManagerService.AssetManager = new CrystalFrost.CFAssetManager(); // Re-initialize for next session
                }

                if (_clientManagerService.SimManager != null)
                {
                    (_clientManagerService.SimManager as SimManager).Dispose();
                }

                if (_clientManagerService.CurrentOutfitFolder != null)
                {
                    _clientManagerService.CurrentOutfitFolder.Dispose();
                }

                // Clean up any remaining assets
                Resources.UnloadUnusedAssets();

                _logger.LogInformation("Logout complete");
                OnStatusUpdate?.Invoke("Logged out successfully");
                OnLogoutComplete?.Invoke();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during logout");
                OnStatusUpdate?.Invoke($"Logout error: {ex.Message}");
            }
        }

        public void SaveCredentials()
        {
            if (_credentials != null && _currentCredential != null)
            {
                _credentials.Save();
            }
        }
    }
}