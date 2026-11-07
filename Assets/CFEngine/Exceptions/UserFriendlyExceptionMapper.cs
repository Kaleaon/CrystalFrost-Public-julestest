using System;
using System.Net.Sockets;
using System.Security.Authentication;
using Microsoft.Extensions.Logging;

namespace CrystalFrost.Exceptions
{
    /// <summary>
    /// Represents a user-facing error message with a clear headline and actionable recovery guidance.
    /// </summary>
    public readonly struct UserFriendlyMessage
    {
        public string Headline { get; }
        public string RecoveryStep { get; }

        public UserFriendlyMessage(string headline, string recoveryStep)
        {
            Headline = headline ?? "Notice";
            RecoveryStep = recoveryStep ?? "Please try again.";
        }

        public override string ToString()
        {
            return $"{Headline}: {RecoveryStep}";
        }
    }

    /// <summary>
    /// Service for mapping raw technical exceptions and protocol errors into conversational, user-friendly messages.
    /// </summary>
    public interface IUserFriendlyExceptionMapper
    {
        /// <summary>
        /// Converts a technical exception into a user-friendly error message with headline and recovery guidance.
        /// </summary>
        UserFriendlyMessage Map(Exception ex);

        /// <summary>
        /// Converts a raw error string (e.g., login failure message or protocol string) into a user-friendly error message.
        /// </summary>
        UserFriendlyMessage Map(string rawErrorMessage);
    }

    /// <summary>
    /// Implementation of <see cref="IUserFriendlyExceptionMapper"/> providing standardized, plain-language user messages.
    /// </summary>
    public class UserFriendlyExceptionMapper : IUserFriendlyExceptionMapper
    {
        private readonly ILogger<UserFriendlyExceptionMapper> _logger;

        public UserFriendlyExceptionMapper(ILogger<UserFriendlyExceptionMapper> logger = null)
        {
            _logger = logger;
        }

        public UserFriendlyMessage Map(Exception ex)
        {
            if (ex == null)
            {
                return new UserFriendlyMessage(
                    "Notice",
                    "An unexpected error occurred. Please try again."
                );
            }

            // Unwrap AggregateException if present
            if (ex is AggregateException aggEx && aggEx.InnerException != null)
            {
                ex = aggEx.InnerException;
            }

            // Technical logging preserving full exception details and stack trace
            _logger?.LogError(ex, "Mapping technical exception to user-friendly message");

            // Check exception types
            if (ex is SocketException || ex is System.Net.WebException || ex.GetType().Name.Contains("XmlRpc") || ex.GetType().Name.Contains("Http"))
            {
                return MapNetworkError(ex.Message);
            }

            if (ex is AuthenticationException || ex.GetType().Name.Contains("Tls") || ex.GetType().Name.Contains("Cert") || ex.GetType().Name.Contains("Ssl"))
            {
                return MapSecurityError(ex.Message);
            }

            if (ex.GetType().Name.Contains("Asset") || ex is NullReferenceException || ex is System.IO.InvalidDataException || ex.GetType().Name.Contains("Decode"))
            {
                return MapAssetError(ex.Message);
            }

            // Fallback to string-based pattern matching
            return Map(ex.Message);
        }

        public UserFriendlyMessage Map(string rawErrorMessage)
        {
            if (string.IsNullOrWhiteSpace(rawErrorMessage))
            {
                return new UserFriendlyMessage(
                    "Notice",
                    "An unexpected issue occurred. Please try your request again."
                );
            }

            string lower = rawErrorMessage.ToLowerInvariant();

            // Check for TLS / Security certificate failures
            if (lower.Contains("cert") || lower.Contains("ssl") || lower.Contains("tls") || lower.Contains("untrusted") || lower.Contains("security"))
            {
                return MapSecurityError(rawErrorMessage);
            }

            // Check for authentication / credentials / login errors
            if (lower.Contains("password") || lower.Contains("credential") || lower.Contains("authentication") || lower.Contains("login") || lower.Contains("user") || lower.Contains("key"))
            {
                if (lower.Contains("invalid") || lower.Contains("failed") || lower.Contains("incorrect") || lower.Contains("unknown") || lower.Contains("denied"))
                {
                    return new UserFriendlyMessage(
                        "Unable to Sign In",
                        "Please check your username and password, then try logging in again."
                    );
                }
            }

            // Check for network connection / timeout / socket / XML-RPC failures
            if (lower.Contains("socket") || lower.Contains("xmlrpc") || lower.Contains("connect") || lower.Contains("timeout") || lower.Contains("unreachable") || lower.Contains("network") || lower.Contains("dns") || lower.Contains("offline"))
            {
                return MapNetworkError(rawErrorMessage);
            }

            // Check for asset / decoding failures
            if (lower.Contains("asset") || lower.Contains("mesh") || lower.Contains("texture") || lower.Contains("sculpt") || lower.Contains("decode") || lower.Contains("corrupt"))
            {
                return MapAssetError(rawErrorMessage);
            }

            // Default conversational fallback
            return new UserFriendlyMessage(
                "Action Couldn't Be Completed",
                "An unexpected issue occurred. Please try again or restart the application."
            );
        }

        private static UserFriendlyMessage MapNetworkError(string rawDetail)
        {
            return new UserFriendlyMessage(
                "Connection Problem",
                "Please verify your internet connection and check that the grid server URL is correct, then try again."
            );
        }

        private static UserFriendlyMessage MapSecurityError(string rawDetail)
        {
            return new UserFriendlyMessage(
                "Security Verification Failed",
                "The server's security certificate could not be verified. Connection was blocked to safeguard your security."
            );
        }

        private static UserFriendlyMessage MapAssetError(string rawDetail)
        {
            return new UserFriendlyMessage(
                "Content Loading Notice",
                "Some world content could not be displayed completely. Re-entering the region or checking your network connection may help."
            );
        }
    }
}
