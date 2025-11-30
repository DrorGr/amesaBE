namespace AmesaBackend.Shared.Authentication.Config
{
    /// <summary>
    /// Configuration class for cryptography settings, including encryption enablement and AES configuration.
    /// </summary>
    public class CryptographyConfig
    {
        /// <summary>
        /// Gets or sets a value indicating whether encryption is enabled.
        /// </summary>
        public Boolean EncryptionEnabled { get; set; }
        
        /// <summary>
        /// Gets or sets the AES encryption configuration settings.
        /// </summary>
        public AES Aes { get; set; } = new AES();
    }

    /// <summary>
    /// Configuration class for AES encryption settings, including key and salt values.
    /// </summary>
    public class AES
    {
        /// <summary>
        /// Gets or sets the AES encryption key.
        /// </summary>
        public string key { get; set; } = string.Empty;
        
        /// <summary>
        /// Gets or sets the AES encryption salt value.
        /// </summary>
        public string salt { get; set; } = string.Empty;
    }
}

