namespace AmesaBackend.Shared.Authentication.Config
{
    public class CryptographyConfig
    {
        public Boolean EncryptionEnabled { get; set; }
        public AES Aes { get; set; } = new AES();
    }

    public class AES
    {
        public string key { get; set; } = string.Empty;
        public string salt { get; set; } = string.Empty;
    }
}

