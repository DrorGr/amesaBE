using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AmesaBackend.Auth.Configuration;

public static class GoogleCredentialsConfiguration
{
    /// <summary>
    /// Sets up Google service account credentials file from environment variable.
    /// Handles BOM removal, base64 decoding, JSON validation, directory creation, and file permissions.
    /// </summary>
    public static IConfiguration SetupGoogleCredentials(this IConfiguration configuration, IHostEnvironment environment)
    {
        var isProduction = environment.IsProduction();
        var googleCredentialsPath = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
        var googleServiceAccountJson = Environment.GetEnvironmentVariable("GOOGLE_SERVICE_ACCOUNT_JSON");

        if (!string.IsNullOrEmpty(googleCredentialsPath) && !string.IsNullOrEmpty(googleServiceAccountJson) && !File.Exists(googleCredentialsPath))
        {
            try
            {
                // BOM byte constants
                const byte UTF8_BOM_BYTE1 = 0xEF;
                const byte UTF8_BOM_BYTE2 = 0xBB;
                const byte UTF8_BOM_BYTE3 = 0xBF;
                
                // Helper function to remove BOM from a string (reusable for initial and base64-decoded content)
                string RemoveBomFromString(string input)
                {
                    if (string.IsNullOrEmpty(input))
                        return input;
                    
                    // Convert to bytes to detect BOM accurately regardless of how it's stored
                    byte[] bytes = System.Text.Encoding.UTF8.GetBytes(input);
                    
                    // Pattern 1: UTF-8 encoding of ï»¿ characters (C3-AF-C2-BB-C2-BF) - 6 bytes
                    // This is what we see in the hex dump from AWS Secrets Manager
                    if (bytes.Length >= 6 && 
                        bytes[0] == 0xC3 && bytes[1] == 0xAF &&  // UTF-8 for ï (U+00EF)
                        bytes[2] == 0xC2 && bytes[3] == 0xBB &&  // UTF-8 for » (U+00BB)
                        bytes[4] == 0xC2 && bytes[5] == 0xBF)   // UTF-8 for ¿ (U+00BF)
                    {
                        return System.Text.Encoding.UTF8.GetString(bytes, 6, bytes.Length - 6).TrimStart();
                    }
                    // Pattern 2: Raw BOM bytes (0xEF 0xBB 0xBF) - 3 bytes
                    else if (bytes.Length >= 3 && 
                             bytes[0] == UTF8_BOM_BYTE1 && 
                             bytes[1] == UTF8_BOM_BYTE2 && 
                             bytes[2] == UTF8_BOM_BYTE3)
                    {
                        return System.Text.Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3).TrimStart();
                    }
                    // Pattern 3: Unicode BOM character (\uFEFF) - single character
                    else if (input.Length > 0 && input[0] == '\uFEFF')
                    {
                        return input.Substring(1).TrimStart();
                    }
                    
                    return input;
                }
                
                // Step 1: Initial trim (preserve potential BOM at start)
                string processedJson = googleServiceAccountJson.TrimStart();
                
                // Step 2: Remove BOM from initial string
                string beforeBomRemoval = processedJson;
                processedJson = RemoveBomFromString(processedJson);
                if (processedJson != beforeBomRemoval)
                {
                    Console.WriteLine("Removed BOM from Google service account JSON");
                }
                
                // Step 3: Try base64 decode if it doesn't start with '{' or '['
                string trimmedJson = processedJson.TrimStart();
                if (!trimmedJson.StartsWith("{") && !trimmedJson.StartsWith("["))
                {
                    try
                    {
                        byte[] decodedBytes = Convert.FromBase64String(trimmedJson);
                        string decodedJson = System.Text.Encoding.UTF8.GetString(decodedBytes);
                        
                        // Step 4: Remove BOM from base64-decoded content (if present)
                        string beforeDecodedBomRemoval = decodedJson;
                        decodedJson = RemoveBomFromString(decodedJson);
                        if (decodedJson != beforeDecodedBomRemoval)
                        {
                            Console.WriteLine("Removed BOM from base64-decoded Google service account JSON");
                        }
                        
                        processedJson = decodedJson;
                        Console.WriteLine("Google service account JSON was base64 encoded, decoded successfully");
                    }
                    catch (FormatException)
                    {
                        // Not base64, continue with original string
                        Console.WriteLine("Google service account JSON is not base64 encoded, using as-is");
                    }
                }
                
                // Step 5: Final trim after all processing
                processedJson = processedJson.Trim();
                
                // Step 5: Validate JSON structure before writing
                if (string.IsNullOrWhiteSpace(processedJson))
                {
                    throw new InvalidOperationException("Google service account JSON is empty after processing");
                }
                
                using (JsonDocument.Parse(processedJson))
                {
                    // JSON is valid, continue
                }

                // Step 6: Ensure directory exists
                var directory = Path.GetDirectoryName(googleCredentialsPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    try
                    {
                        Directory.CreateDirectory(directory);
                        Console.WriteLine($"Created directory: {directory}");
                    }
                    catch (Exception dirEx)
                    {
                        throw new InvalidOperationException($"Failed to create directory {directory}: {dirEx.Message}", dirEx);
                    }
                }
                
                // Step 7: Write file with UTF-8 encoding (no BOM)
                // Use UTF8Encoding(false) to explicitly avoid BOM
                var utf8NoBom = new System.Text.UTF8Encoding(false);
                File.WriteAllText(googleCredentialsPath, processedJson, utf8NoBom);
                
                // Step 8: Set restrictive file permissions on Linux/Unix
                if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
                {
                    try
                    {
                        File.SetUnixFileMode(googleCredentialsPath, 
                            System.IO.UnixFileMode.UserRead | System.IO.UnixFileMode.UserWrite);
                        Console.WriteLine($"Set file permissions for: {googleCredentialsPath}");
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // Not on Unix, ignore
                    }
                    catch (Exception permEx)
                    {
                        // Log but don't fail - file is written, permissions are nice-to-have
                        Console.WriteLine($"Warning: Failed to set file permissions: {permEx.Message}");
                    }
                }
                
                Console.WriteLine($"Google service account credentials written to: {googleCredentialsPath}");
            }
            catch (JsonException ex)
            {
                var preview = string.IsNullOrEmpty(googleServiceAccountJson) 
                    ? "null or empty" 
                    : googleServiceAccountJson.Length > 100 
                        ? googleServiceAccountJson.Substring(0, 100) 
                        : googleServiceAccountJson;
                var errorMsg = $"Invalid Google service account JSON: {ex.Message}. First 100 chars (hex): {BitConverter.ToString(System.Text.Encoding.UTF8.GetBytes(preview).Take(100).ToArray())}";
                Console.WriteLine($"ERROR: {errorMsg}");
                
                // In production, make this non-blocking to allow service to start
                // CAPTCHA will be disabled, but service remains functional
                if (isProduction)
                {
                    Console.WriteLine("WARNING: Service will start without Google credentials. CAPTCHA features will be disabled.");
                    // Don't throw - allow service to start
                }
                else
                {
                    throw new InvalidOperationException(errorMsg, ex);
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to write Google service account credentials file: {ex.Message}";
                Console.WriteLine($"ERROR: {errorMsg}");
                
                // In production, make this non-blocking
                if (isProduction)
                {
                    Console.WriteLine("WARNING: Service will start without Google credentials. CAPTCHA features will be disabled.");
                    // Don't throw - allow service to start
                }
                else
                {
                    throw new InvalidOperationException(errorMsg, ex);
                }
            }
        }

        return configuration;
    }
}






