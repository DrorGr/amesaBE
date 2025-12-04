using AmesaBackend.Notification.Data;
using AmesaBackend.Notification.Constants;
using AmesaBackend.Shared.Caching;
using AmesaBackend.Shared.Rest;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AmesaBackend.Notification.Services
{
    public class TemplateEngine : ITemplateEngine
    {
        private readonly NotificationDbContext _context;
        private readonly ICache _cache;
        private readonly IHttpRequest _httpRequest;
        private readonly IConfiguration _configuration;
        private readonly ILogger<TemplateEngine> _logger;

        public TemplateEngine(
            NotificationDbContext context,
            ICache cache,
            IHttpRequest httpRequest,
            IConfiguration configuration,
            ILogger<TemplateEngine> logger)
        {
            _context = context;
            _cache = cache;
            _httpRequest = httpRequest;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<string> RenderTemplateAsync(string templateName, string language, string channel, Dictionary<string, object> variables)
        {
            try
            {
                // Check cache first
                var cacheKey = $"template:{templateName}:{language}:{channel}";
                var cachedTemplate = await _cache.GetRecordAsync<string>(cacheKey);
                
                string templateContent;
                if (!string.IsNullOrEmpty(cachedTemplate))
                {
                    templateContent = cachedTemplate;
                }
                else
                {
                    // Fetch template from database
                    var template = await _context.NotificationTemplates
                        .FirstOrDefaultAsync(t => t.Name == templateName && 
                                                  t.Language == language && 
                                                  (t.Channel == channel || t.Channel == NotificationChannelConstants.DefaultChannel) &&
                                                  t.IsActive);

                    if (template == null)
                    {
                        // Fallback to English if template not found in requested language
                        if (language != NotificationChannelConstants.DefaultLanguage)
                        {
                            _logger.LogInformation("Template {TemplateName} not found for language {Language}, falling back to English", 
                                templateName, language);
                            template = await _context.NotificationTemplates
                                .FirstOrDefaultAsync(t => t.Name == templateName && 
                                                          t.Language == NotificationChannelConstants.DefaultLanguage && 
                                                          (t.Channel == channel || t.Channel == NotificationChannelConstants.DefaultChannel) &&
                                                          t.IsActive);
                        }

                        if (template == null)
                        {
                            _logger.LogWarning("Template not found: {TemplateName} for language {Language} and channel {Channel}", 
                                templateName, language, channel);
                            return string.Empty;
                        }
                    }

                    templateContent = template.Message;
                    
                    // Cache template for 1 hour
                    await _cache.SetRecordAsync(cacheKey, templateContent, TimeSpan.FromHours(1));
                }

                // Format variables by locale before substitution
                var formattedVariables = FormatVariablesByLocale(variables, language);

                // Perform variable substitution with formatted variables
                var rendered = SubstituteVariables(templateContent, formattedVariables);

                return rendered;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error rendering template {TemplateName}", templateName);
                return string.Empty;
            }
        }

        /// <summary>
        /// Format variables by locale (dates, numbers, currency)
        /// </summary>
        private Dictionary<string, object> FormatVariablesByLocale(Dictionary<string, object> variables, string language)
        {
            var formatted = new Dictionary<string, object>(variables);
            var locale = GetLocaleFromLanguage(language);
            var culture = new System.Globalization.CultureInfo(locale);

            foreach (var kvp in variables)
            {
                if (kvp.Value is DateTime dateTime)
                {
                    formatted[kvp.Key] = dateTime.ToString("d", culture);
                }
                else if (kvp.Value is decimal decimalValue)
                {
                    // Check if it's a currency field
                    if (kvp.Key.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Contains("price", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Contains("cost", StringComparison.OrdinalIgnoreCase))
                    {
                        formatted[kvp.Key] = decimalValue.ToString("C", culture);
                    }
                    else
                    {
                        formatted[kvp.Key] = decimalValue.ToString("N", culture);
                    }
                }
                else if (kvp.Value is double doubleValue)
                {
                    formatted[kvp.Key] = doubleValue.ToString("N", culture);
                }
                else if (kvp.Value is int intValue)
                {
                    formatted[kvp.Key] = intValue.ToString("N0", culture);
                }
            }

            return formatted;
        }

        /// <summary>
        /// Get locale code from language code
        /// </summary>
        private string GetLocaleFromLanguage(string language)
        {
            return language.ToLower() switch
            {
                "en" => "en-US",
                "es" => "es-ES",
                "fr" => "fr-FR",
                "pl" => "pl-PL",
                _ => "en-US"
            };
        }

        private string SubstituteVariables(string template, Dictionary<string, object>? variables)
        {
            if (variables == null || variables.Count == 0)
            {
                return template;
            }

            var result = template;

            // Support both {{variable}} and {variable} syntax
            var pattern = @"\{\{?(\w+)\}?\}";
            result = Regex.Replace(result, pattern, match =>
            {
                var varName = match.Groups[1].Value;
                if (variables.TryGetValue(varName, out var value))
                {
                    return FormatValue(value);
                }
                return match.Value; // Keep original if variable not found
            });

            return result;
        }

        private string FormatValue(object? value)
        {
            if (value == null)
                return string.Empty;

            if (value is string str)
                return str;

            if (value is DateTime dateTime)
                return dateTime.ToString("yyyy-MM-dd HH:mm:ss");

            if (value is decimal or double or float)
                return value.ToString() ?? string.Empty;

            // For complex objects, serialize to JSON
            try
            {
                return JsonSerializer.Serialize(value);
            }
            catch
            {
                return value.ToString() ?? string.Empty;
            }
        }
    }
}

