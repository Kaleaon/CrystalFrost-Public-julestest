using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace CrystalFrost.Config
{
    /// <summary>
    /// Contains configuration for the logging system.
    /// </summary>
    public class LoggingConfig
    {
        // Logging config is handled differently than other config sections since
        // this configuraion is passed directly into the logging system.
        // This creates the presets that can be modified by the user.
        /// <summary>
        /// The default logging values.
        /// </summary>
        public static Dictionary<string,string> DefaultValues = new Dictionary<string, string>()
        {
            { "Logging:LogLevel:Default", "Information" },
            { "Logging:LogLevel:CrystalFrost.GridClientFactory", "Information" },
            { "Logging:LogLevel:CrystalFrost:Logging:LMVLogger", "Information" }
        };

        /// <summary>
        /// The name of the configuration subsection.
        /// </summary>
        public const string subsectionName = "Logging";

        /// <summary>
        /// The directory where logs will be stored.
        /// </summary>
        public string LogDirectory { get; set; } = "./Logs";

        /// <summary>
        /// The log levels for different parts of the application.
        /// </summary>
        public object LogLevel = new {
            Default = "Information",
            CrystalFrost = new {
                GridClientFactory = "Information",
                Logging = new {
                    LMVLogger = "Information"
                }
            }
        };

    }
}
