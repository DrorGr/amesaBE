using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using AmesaBackend.Shared.Exceptions;

namespace AmesaBackend.Shared.Authentication.Cryptography
{
    /// <summary>
    /// Provides AES encryption and decryption functionality for text data.
    /// Uses SHA256 hashing for key derivation and supports salt-based encryption.
    /// </summary>
    public class AesEncryptor : IAesEncryptor
    {
        private const int KeySizeInBits = 256;
        private const int IvSizeInBytes = 16; // AES IV size is 128 bits (16 bytes)

        private string _encryptionKey;

        private readonly ILogger<AesEncryptor> _logger;

        /// <summary>
        /// Initializes a new instance of the <see cref="AesEncryptor"/> class.
        /// </summary>
        /// <param name="logger">The logger instance for logging operations.</param>
        /// <param name="configuration">The configuration instance to retrieve encryption key.</param>
        /// <exception cref="InvalidOperationException">Thrown when the AES encryption key is not configured.</exception>
        public AesEncryptor(ILogger<AesEncryptor> logger, IConfiguration configuration)
        {
            _logger = logger;
            _encryptionKey = configuration.GetValue<string>("CryptographyConfig:AES:key") 
                ?? throw new InvalidOperationException("AES encryption key is not configured");
        }

        /// <summary>
        /// Encrypts the specified input text using AES encryption.
        /// </summary>
        /// <param name="input">The plain text to encrypt.</param>
        /// <param name="saltKey">When this method returns, contains the base64-encoded salt key used for encryption.</param>
        /// <param name="providedSalt">Optional base64-encoded salt. If not provided, a new salt will be generated.</param>
        /// <returns>The base64-encoded encrypted text.</returns>
        public string EncryptText(string input, out string saltKey, string providedSalt = "")
        {
            byte[] encryptionKeyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
            byte[] saltBytes = null!;

            if (!string.IsNullOrEmpty(providedSalt))
            {
                saltBytes = Convert.FromBase64String(providedSalt);
            }

            // Hash the password with SHA256
            encryptionKeyBytes = SHA256.Create().ComputeHash(encryptionKeyBytes);

            byte[] bytesEncrypted = AesEncrypt(input, encryptionKeyBytes, saltBytes!, out saltBytes);
            saltKey = Convert.ToBase64String(saltBytes);

            string result = Convert.ToBase64String(bytesEncrypted);

            return result;
        }

        /// <summary>
        /// Decrypts the specified encrypted text using AES decryption.
        /// </summary>
        /// <param name="input">The base64-encoded encrypted text to decrypt.</param>
        /// <param name="saltKey">The base64-encoded salt key used for decryption.</param>
        /// <returns>The decrypted plain text, or the original input if decryption fails or saltKey is null.</returns>
        public string DecryptText(string input, string saltKey)
        {
            try
            {
                if (saltKey != null)
                {
                    // Get the bytes of the string
                    byte[] bytesToBeDecrypted = Convert.FromBase64String(input);
                    byte[] encryptionKeyBytes = Encoding.UTF8.GetBytes(_encryptionKey);
                    byte[] saltBytes = Convert.FromBase64String(saltKey);

                    encryptionKeyBytes = SHA256.Create().ComputeHash(encryptionKeyBytes);

                    string result = AesDecrypt(bytesToBeDecrypted, encryptionKeyBytes, saltBytes);

                    return result;
                }

                return input;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Decryption failed");
                return input;
            }
        }

        private byte[] AesEncrypt(string plainText, byte[] encryptionKeyBytes, byte[] providedSalt,
            out byte[] saltBytes)
        {
            byte[]? encryptedBytes = null;

            using (var AES = Aes.Create())
            {
                AES.Key = encryptionKeyBytes;
                if (providedSalt != null)
                {
                    AES.IV = providedSalt;
                }

                saltBytes = AES.IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = AES.CreateEncryptor(AES.Key, AES.IV);

                using (MemoryStream ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(cs))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                    }

                    encryptedBytes = ms.ToArray();
                }
            }

            return encryptedBytes;
        }


        private string AesDecrypt(byte[] cipherText, byte[] encryptionKeyBytes, byte[] saltBytes)
        {
            try
            {
                // Check arguments.
                if (cipherText == null || cipherText.Length <= 0)
                    throw new ArgumentNullException(nameof(cipherText));
                if (encryptionKeyBytes == null || encryptionKeyBytes.Length <= 0)
                    throw new ArgumentNullException(nameof(encryptionKeyBytes));
                if (saltBytes == null || saltBytes.Length <= 0)
                    throw new ArgumentNullException(nameof(saltBytes));

                // Declare the string used to hold
                // the decrypted text.
                string plainText = string.Empty;

                using (var AES = Aes.Create())
                {
                    AES.Key = encryptionKeyBytes;
                    AES.IV = saltBytes;

                    // Create a decryptor to perform the stream transform.
                    ICryptoTransform decryptor = AES.CreateDecryptor(AES.Key, AES.IV);

                    using (MemoryStream ms = new MemoryStream(cipherText))
                    {
                        using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(cs))
                            {
                                // Read the decrypted bytes from the decrypting stream
                                // and place them in a string.
                                plainText = srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }

                return plainText;
            }
            catch (Exception ex)
            {
                throw new CustomFaultException(ServiceError.GeneralError, "Decryption failed " + ex.Message);
            }
        }


        public bool IsDecrypt(string input, string saltKey)
        {
            string textAfterDecrypt = DecryptText(input, saltKey);

            if (String.Equals(textAfterDecrypt, input))
            {
                return false;
            }

            return true;
        }
    }
}

