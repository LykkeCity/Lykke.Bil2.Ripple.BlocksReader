using Lykke.Bil2.Sdk.BlocksReader;
using Lykke.Bil2.Sdk.BlocksReader.Settings;
using Lykke.Bil2.Sdk.Services;
using Lykke.SettingsReader.Attributes;

namespace Lykke.Bil2.Ripple.BlocksReader.Settings
{
    /// <summary>
    /// Specific blockchain settings
    /// </summary>
    public class AppSettings : BaseBlocksReaderSettings<DbSettings>
    {
        public string NodeRpcUrl { get; set; }

        [Optional]
        public string NodeRpcUsername { get; set; }

        [Optional]
        [SecureSettings]
        public string NodeRpcPassword { get; set; }
    }
}
