using System;
using System.Collections;
using UnityEngine;
using OpenMetaverse;
using CrystalFrost.Client.Credentials;
using CrystalFrost.Config;
using Microsoft.Extensions.Logging;
using Bunny;

namespace CrystalFrost.Controllers
{
    /// <summary>
    /// Handles authentication logic including login, logout, and credential management
    /// </summary>
    public class AuthenticationController : MonoBehaviour
    {
        private ILogger<AuthenticationController> _logger;
        private ICredentialsStore _credentials;
        private ILoginUriProvider _loginUriProvider;
        private LoginCredential _currentCredential;

        public System.Action OnLoginSuccess;
        public System.Action OnLogoutComplete;
        public System.Action<string> OnStatusUpdate;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<AuthenticationController>>();
            _loginUriProvider = Services.GetService<ILoginUriProvider>();
            _credentials = Services.GetService<ICredentialsStore>();
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
                    ClientManager.client,
                    firstName,
                    lastName, 
                    password,
                    "CrystalFrost",
                    "0.2",
                    loginUri
                );

                if (loginParams.URI != loginUri) ClientManager.isOpenSim = true;

                // Perform login
                bool loginSuccess = ClientManager.client.Network.Login(loginParams);
                
                if (loginSuccess)
                {
                    Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + ClientManager.client.Network.LoginMessage);
                    Console.WriteLine("Logging in. The viewer might appear to lock up for a short while, while the sim floods it with new objects.");

                    _logger.LogInformation("Login successful");
                    OnStatusUpdate?.Invoke("Login successful!");
                    
                    // Set up client state - matching original logic
                    ClientManager.client.Network.CurrentSim.Caps.CapabilitiesReceived += ((sender, e) => {
                        ClientManager.active = true;
                    });
                    ClientManager.client.Estate.RequestInfo();

                    // Re-initialize the SimManager for the new session
                    if (ClientManager.simManager != null)
                    {
                        (ClientManager.simManager as SimManager).Initialize();
                    }

                    // Update credential last used time
                    _currentCredential.LastUsed = System.DateTime.UtcNow;

                    OnLoginSuccess?.Invoke();
                }
                else
                {
                    Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + ClientManager.client.Network.LoginMessage);
                    _logger.LogError($"Login failed: {ClientManager.client.Network.LoginMessage}");
                    OnStatusUpdate?.Invoke($"Login failed: {ClientManager.client.Network.LoginMessage}");
                    ClientManager.active = false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception during login");
                OnStatusUpdate?.Invoke($"Login error: {ex.Message}");
                ClientManager.active = false;
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

            return Settings.AGNI_LOGIN_SERVER;
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
                if (ClientManager.client != null && ClientManager.client.Network.Connected)
                {
                    ClientManager.client.Network.Logout();
                }

                ClientManager.active = false;

                // Dispose managers to clean up state
                if (ClientManager.assetManager != null)
                {
                    ClientManager.assetManager.Dispose();
                    ClientManager.assetManager = new CrystalFrost.CFAssetManager(); // Re-initialize for next session
                }

                if (ClientManager.simManager != null)
                {
                    (ClientManager.simManager as SimManager).Dispose();
                }

                if (ClientManager.currentOutfitFolder != null)
                {
                    ClientManager.currentOutfitFolder.Dispose();
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