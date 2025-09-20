using System.Collections;
using System.Collections.Generic;

namespace CrystalFrost.Config
{
    /// <summary>
    /// Contains configuration about the grid.
    /// </summary>
    public class GridConfig
    {
        /// <summary>
        /// The name of the configuration subsection.
        /// </summary>
        public const string subsectionName = "Grid";

        /// <summary>
        /// The login URI for the grid.
        /// </summary>
        public string LoginURI { get; set; } = OpenMetaverse.Settings.AGNI_LOGIN_SERVER;
    }
}
