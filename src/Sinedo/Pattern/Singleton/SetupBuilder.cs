using Sinedo.Models;

namespace Sinedo.Background
{

    public class SetupBuilder
    {
        #region Properties

        public DiskSpaceRecord DiskInfo { get; set; }

        /// <summary>
        /// Verlauf der Auslastung.
        /// </summary>
        public BandwidthRecord BandwidthInfo { get; set; }

        #endregion
    }
}