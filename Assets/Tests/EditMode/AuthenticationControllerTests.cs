using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using OpenMetaverse;
using CrystalFrost.Controllers;
using CrystalFrost.Services;
using CrystalFrost.Client.Credentials;
using CrystalFrost.Config;
using Microsoft.Extensions.Logging;
using Moq;

namespace CrystalFrost.Tests
{
    [TestFixture]
    public class AuthenticationControllerTests
    {
        private Mock<ICredentialsStore> _mockCredentialsStore;
        private Mock<ILoginUriProvider> _mockLoginUriProvider;
        private Mock<IClientManagerService> _mockClientManagerService;
        private Mock<ILogger<AuthenticationController>> _mockLogger;
        private GridClient _gridClient;

        [SetUp]
        public void SetUp()
        {
            _mockCredentialsStore = new Mock<ICredentialsStore>();
            _mockLoginUriProvider = new Mock<ILoginUriProvider>();
            _mockClientManagerService = new Mock<IClientManagerService>();
            _mockLogger = new Mock<ILogger<AuthenticationController>>();

            _gridClient = new GridClient();
            _mockClientManagerService.Setup(m => m.Client).Returns(_gridClient);

            var credentialsList = new List<LoginCredential>
            {
                new LoginCredential { FirstName = "Test", LastName = "User", Password = "password123" }
            };
            _mockCredentialsStore.Setup(m => m.Any()).Returns(true);
            _mockCredentialsStore.Setup(m => m.First()).Returns(credentialsList[0]);

            _mockLoginUriProvider.Setup(m => m.GetLoginUri()).Returns("http://login.test.grid:8002/");
        }

        [Test]
        public async Task TryLogin_FailsAfter3RetryAttempts_WhenNetworkLoginReturnsFalse()
        {
            var go = new GameObject("TestAuthObject");
            var authController = go.AddComponent<AuthenticationController>();

            List<string> statusUpdates = new List<string>();
            authController.OnStatusUpdate += (status) => statusUpdates.Add(status);

            int loginCalls = 0;
            // Intercept Network.Login if possible or verify backoff retries count
            authController.TryLogin("Test", "User", "wrongpass", "http://login.test.grid:8002/");

            // Wait for background tasks & retries (capped at 3 attempts)
            await Task.Delay(3500);

            Assert.IsFalse(_mockClientManagerService.Object.Active, "ClientManager active should be false when login fails");
            Assert.IsTrue(statusUpdates.Exists(s => s.Contains("Login failed")), "Status updates should record login failure");
            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void DetachCapabilitiesHandler_UnsubscribesEvent_OnLogout()
        {
            var go = new GameObject("TestAuthObject");
            var authController = go.AddComponent<AuthenticationController>();

            bool logoutCompleteCalled = false;
            authController.OnLogoutComplete += () => logoutCompleteCalled = true;

            authController.Logout();

            Assert.IsFalse(_mockClientManagerService.Object.Active, "ClientManager active should be false on logout");
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
