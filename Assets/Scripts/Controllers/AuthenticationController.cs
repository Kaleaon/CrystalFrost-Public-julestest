using System;
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using OpenMetaverse;
using CrystalFrost;
using CrystalFrost.Client.Credentials;
using CrystalFrost.Config;
using CrystalFrost.Scripts;
using Microsoft.Extensions.Logging;
using LoginCredential = CrystalFrost.Client.Credentials.LoginCredential;

namespace CrystalFrost.Controllers
{
    /// <summary>
    /// Handles authentication logic including login, logout, and credential management.
    /// Executes network operations asynchronously in background tasks with exponential backoff retries,
    /// marshals UI updates back to the main thread, and maintains clean delegate lifecycles.
    /// </summary>
    public class AuthenticationController : MonoBehaviour
    {
        private const int MaxRetryAttempts = 3;
        private const int InitialBackoffDelayMs = 1000;

        private ILogger<AuthenticationController> _logger;
        private ICredentialsStore _credentials;
        private ILoginUriProvider _loginUriProvider;
        private IClientManagerService _clientManagerService;
        private LoginCredential _currentCredential;

        private EventHandler<CapabilitiesReceivedEventArgs> _capabilitiesReceivedHandler;
        private bool _isLoggingIn = false;

        public System.Action OnLoginSuccess;
        public System.Action OnLogoutComplete;
        public System.Action<string> OnStatusUpdate;

        private void Awake()
        {
            _logger = Services.GetService<ILogger<AuthenticationController>>();
            _loginUriProvider = Services.GetService<ILoginUriProvider>();
            _credentials = Services.GetService<ICredentialsStore>();
            _clientManagerService = ClientManager.GetService();
            _capabilitiesReceivedHandler = OnCapabilitiesReceived;
        }

        public void Initialize()
        {
            if (_capabilitiesReceivedHandler == null)
            {
                _capabilitiesReceivedHandler = OnCapabilitiesReceived;
            }

            _credentials?.Load();
            if (_credentials != null && !_credentials.Any())
            {
                _credentials.Add(new());
            }
            if (_credentials != null)
            {
                _currentCredential = _credentials.First();
            }
        }

        public LoginCredential GetCurrentCredential()
        {
            return _currentCredential;
        }

        public void TryLogin(string firstName, string lastName, string password, string customGridURL)
        {
            if (_isLoggingIn)
            {
                _logger?.LogWarning("Login process already in progress.");
                return;
            }

            _isLoggingIn = true;
            Task.Run(async () => await PerformLoginWithRetryAsync(firstName, lastName, password, customGridURL));
        }

        private async Task PerformLoginWithRetryAsync(string firstName, string lastName, string password, string customGridURL)
        {
            _logger?.LogInformation($"Logging in as {firstName} {lastName}");
            DispatchToMainThread(() => OnStatusUpdate?.Invoke($"Connecting as {firstName} {lastName}..."));

            // Update credentials
            if (_currentCredential != null)
            {
                _currentCredential.FirstName = firstName;
                _currentCredential.LastName = lastName;
                _currentCredential.Password = password;
            }

            // Determine login URI
            string loginUri = DetermineLoginUri(customGridURL);
            _logger?.LoggingIn(firstName, lastName, loginUri);

            int currentDelay = InitialBackoffDelayMs;
            bool loginSuccess = false;
            string lastErrorMessage = string.Empty;

            // Ensure any previous handler is detached prior to login attempts
            DetachCapabilitiesReceivedHandler();

            for (int attempt = 1; attempt <= MaxRetryAttempts; attempt++)
            {
                if (attempt > 1)
                {
                    _logger?.LogInformation($"Retrying login (attempt {attempt}/{MaxRetryAttempts}) after {currentDelay}ms backoff...");
                    DispatchToMainThread(() => OnStatusUpdate?.Invoke($"Retrying connection (attempt {attempt}/{MaxRetryAttempts})..."));
                    await Task.Delay(currentDelay);
                    currentDelay *= 2;
                }

                try
                {
                    LoginParams loginParams = new(
                        _clientManagerService.Client,
                        firstName,
                        lastName,
                        password,
                        "CrystalFrost",
                        "0.2",
                        loginUri
                    );

                    if (loginParams.URI != loginUri && _clientManagerService != null)
                    {
                        _clientManagerService.IsOpenSim = true;
                    }

                    loginSuccess = _clientManagerService.Client.Network.Login(loginParams);

                    if (loginSuccess)
                    {
                        _logger?.LogInformation($"Login succeeded on attempt {attempt}");
                        break;
                    }

                    lastErrorMessage = _clientManagerService.Client.Network.LoginMessage;
                    _logger?.LogWarning($"Login attempt {attempt}/{MaxRetryAttempts} failed: {lastErrorMessage}");

                    if (attempt < MaxRetryAttempts)
                    {
                        DispatchToMainThread(() => OnStatusUpdate?.Invoke($"Attempt {attempt} failed: {lastErrorMessage}. Retrying..."));
                    }
                }
                catch (Exception ex)
                {
                    lastErrorMessage = ex.Message;
                    _logger?.LogError(ex, $"Exception during login attempt {attempt}/{MaxRetryAttempts}");

                    if (attempt < MaxRetryAttempts)
                    {
                        DispatchToMainThread(() => OnStatusUpdate?.Invoke($"Attempt {attempt} error: {ex.Message}. Retrying..."));
                    }
                }
            }

            if (loginSuccess)
            {
                DispatchToMainThread(() =>
                {
                    if (_clientManagerService?.Client?.Network != null)
                    {
                        Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + _clientManagerService.Client.Network.LoginMessage);
                    }
                    Console.WriteLine("Logging in. The viewer might appear to lock up for a short while, while the sim floods it with new objects.");

                    _logger?.LogInformation("Login successful");
                    OnStatusUpdate?.Invoke("Login successful!");

                    // Clean up existing handler reference and subscribe named handler
                    DetachCapabilitiesReceivedHandler();
                    if (_clientManagerService?.Client?.Network?.CurrentSim?.Caps != null)
                    {
                        _clientManagerService.Client.Network.CurrentSim.Caps.CapabilitiesReceived += _capabilitiesReceivedHandler;
                    }

                    _clientManagerService?.Client?.Estate?.RequestInfo();

                    // Re-initialize the SimManager for the new session
                    if (_clientManagerService?.SimManager != null)
                    {
                        (_clientManagerService.SimManager as SimManager).Initialize();
                    }

                    // Update credential last used time
                    if (_currentCredential != null)
                    {
                        _currentCredential.LastUsed = System.DateTime.UtcNow;
                    }

                    _isLoggingIn = false;
                    OnLoginSuccess?.Invoke();
                });
            }
            else
            {
                DispatchToMainThread(() =>
                {
                    Console.WriteLine(System.DateTime.UtcNow.ToShortTimeString() + ": " + lastErrorMessage);
                    _logger?.LogError($"Login failed after {MaxRetryAttempts} attempts: {lastErrorMessage}");
                    OnStatusUpdate?.Invoke($"Login failed: {lastErrorMessage}");

                    DetachCapabilitiesReceivedHandler();
                    if (_clientManagerService != null)
                    {
                        _clientManagerService.Active = false;
                    }

                    _isLoggingIn = false;
                });
            }
        }

        private void OnCapabilitiesReceived(object sender, CapabilitiesReceivedEventArgs e)
        {
            DispatchToMainThread(() =>
            {
                if (_clientManagerService != null)
                {
                    _clientManagerService.Active = true;
                }
                _logger?.LogInformation("Capabilities received; ClientManager active set to true");
            });
        }

        private void DetachCapabilitiesReceivedHandler()
        {
            try
            {
                if (_capabilitiesReceivedHandler != null &&
                    _clientManagerService?.Client?.Network?.CurrentSim?.Caps != null)
                {
                    _clientManagerService.Client.Network.CurrentSim.Caps.CapabilitiesReceived -= _capabilitiesReceivedHandler;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Failed to detach CapabilitiesReceived handler");
            }
        }

        private void DispatchToMainThread(Action action)
        {
            if (action == null) return;

            if (UnityMainThreadDispatcher.Exists())
            {
                UnityMainThreadDispatcher.Instance().Enqueue(action);
            }
            else
            {
                action();
            }
        }

        private string DetermineLoginUri(string customGridURL)
        {
            if (!string.IsNullOrEmpty(customGridURL))
            {
                return customGridURL;
            }

            var defaultUri = _loginUriProvider?.GetLoginUri();
            if (!string.IsNullOrEmpty(defaultUri))
            {
                return defaultUri;
            }

            return OpenMetaverse.Settings.AGNI_LOGIN_SERVER;
        }

        public void Logout()
        {
            DetachCapabilitiesReceivedHandler();
            StartCoroutine(LogoutCoroutine());
        }

        private IEnumerator LogoutCoroutine()
        {
            _logger?.LoggingOut();
            OnStatusUpdate?.Invoke("Logging out...");

            yield return null;

            try
            {
                DetachCapabilitiesReceivedHandler();

                // Gracefully disconnect from the network
                if (_clientManagerService?.Client != null && _clientManagerService.Client.Network.Connected)
                {
                    _clientManagerService.Client.Network.Logout();
                }

                if (_clientManagerService != null)
                {
                    _clientManagerService.Active = false;
                }

                // Dispose managers to clean up state
                if (_clientManagerService?.AssetManager != null)
                {
                    _clientManagerService.AssetManager.Dispose();
                    _clientManagerService.AssetManager = new CrystalFrost.CFAssetManager();
                }

                if (_clientManagerService?.SimManager != null)
                {
                    (_clientManagerService.SimManager as SimManager).Dispose();
                }

                if (_clientManagerService?.CurrentOutfitFolder != null)
                {
                    _clientManagerService.CurrentOutfitFolder.Dispose();
                }

                // Clean up any remaining assets
                Resources.UnloadUnusedAssets();

                _logger?.LogInformation("Logout complete");
                OnStatusUpdate?.Invoke("Logged out successfully");
                OnLogoutComplete?.Invoke();
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Exception during logout");
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

        private void OnDestroy()
        {
            DetachCapabilitiesReceivedHandler();
        }
    }
}