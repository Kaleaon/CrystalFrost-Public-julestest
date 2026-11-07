using System;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using CrystalFrost.Exceptions;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Security
{
    /// <summary>
    /// Exception thrown when TLS policy verification fails.
    /// </summary>
    public class TlsPolicyException : Exception
    {
        public UserFriendlyMessage UserMessage { get; }

        public TlsPolicyException(string message, UserFriendlyMessage userMessage, Exception innerException = null)
            : base(message, innerException)
        {
            UserMessage = userMessage;
        }
    }

    /// <summary>
    /// Contract for evaluating TLS certificate security policies.
    /// </summary>
    public interface ITlsPolicy
    {
        bool AllowUntrustedCertificates { get; set; }

        /// <summary>
        /// Validates a server certificate according to current grid security policy.
        /// </summary>
        bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors,
            out UserFriendlyMessage userMessage);

        /// <summary>
        /// Evaluates certificate policy and throws a <see cref="TlsPolicyException"/> with user-friendly security guidance if invalid.
        /// </summary>
        void EvaluateAndVerifyServerCertificate(
            string hostName,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors);
    }

    /// <summary>
    /// Implements grid TLS certificate validation and converts certificate errors into plain security guidance.
    /// </summary>
    public class TlsPolicy : ITlsPolicy
    {
        private readonly ILogger<TlsPolicy> _logger;
        private readonly IUserFriendlyExceptionMapper _exceptionMapper;

        public bool AllowUntrustedCertificates { get; set; } = false;

        public TlsPolicy(ILogger<TlsPolicy> logger = null, IUserFriendlyExceptionMapper exceptionMapper = null)
        {
            _logger = logger;
            _exceptionMapper = exceptionMapper ?? new UserFriendlyExceptionMapper();
        }

        public bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors,
            out UserFriendlyMessage userMessage)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
            {
                userMessage = default;
                return true;
            }

            if (AllowUntrustedCertificates)
            {
                _logger?.LogWarning($"TLS Certificate policy warning ignored (AllowUntrustedCertificates=true): {sslPolicyErrors}");
                userMessage = default;
                return true;
            }

            // Technical details logged for developers
            string techDetail = $"TLS certificate error for server: {sslPolicyErrors} (Subject: {certificate?.Subject})";
            _logger?.LogError(techDetail);

            // Convert certificate error to plain security message before logging or throwing
            userMessage = _exceptionMapper.Map($"Certificate security error: {sslPolicyErrors}");
            return false;
        }

        public void EvaluateAndVerifyServerCertificate(
            string hostName,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (!ValidateServerCertificate(hostName, certificate, chain, sslPolicyErrors, out var userMessage))
            {
                string message = $"TLS certificate validation failed for host '{hostName}': {sslPolicyErrors}";
                throw new TlsPolicyException(message, userMessage);
            }
        }
    }
}
