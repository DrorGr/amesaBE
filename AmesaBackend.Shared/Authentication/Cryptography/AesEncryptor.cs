using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;
using AmesaBackend.Shared.Exceptions;

namespace AmesaBackend.Shared.Authentication.Cryptography
{
    public class AesEncryptor : IAesEncryptor
    {
        private const int KeySizeInBits = 256;
        private const int IvSizeInBytes = 16; // AES IV size is 128 bits (16 bytes)

        private string _encryptionKey;

        private readonly ILogger<AesEncryptor> _logger;

        public AesEncryptor(ILogger<AesEncryptor> logger, IConfiguration configuration)
        {
            _logger = logger;
            _encryptionKey = configuration.GetValue<string>("CryptographyConfig:AES:key") 
                ?? throw new InvalidOperationException("AES encryption key is not configured");
        }

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

