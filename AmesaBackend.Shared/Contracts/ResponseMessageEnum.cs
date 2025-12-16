using System.ComponentModel;
using System.Reflection;

namespace AmesaBackend.Shared.Contracts
{
    /// <summary>
    /// Enumeration of standard API response message types.
    /// </summary>
    public enum ResponseMessageEnum
    {
        /// <summary>
        /// Request successful.
        /// </summary>
        [Description("Request successful.")]
        Success,

        /// <summary>
        /// Request not found. The specified uri does not exist.
        /// </summary>
        [Description("Request not found. The specified uri does not exist.")]
        NotFound,

        /// <summary>
        /// Request responded with 'Method Not Allowed'.
        /// </summary>
        [Description("Request responded with 'Method Not Allowed'.")]
        MethodNotAllowed,

        /// <summary>
        /// Request no content. The specified uri does not contain any content.
        /// </summary>
        [Description("Request no content. The specified uri does not contain any content.")]
        NotContent,

        /// <summary>
        /// Request responded with exceptions.
        /// </summary>
        [Description("Request responded with exceptions.")]
        Exception,

        /// <summary>
        /// Request denied. Unauthorized access.
        /// </summary>
        [Description("Request denied. Unauthorized access.")]
        UnAuthorized,

        /// <summary>
        /// Request responded with validation error(s). Please correct the specified validation errors and try again.
        /// </summary>
        [Description("Request responded with validation error(s). Please correct the specified validation errors and try again.")]
        ValidationError,

        /// <summary>
        /// Request cannot be processed. Please contact a support.
        /// </summary>
        [Description("Request cannot be processed. Please contact a support.")]
        Unknown,

        /// <summary>
        /// Unhandled Exception occured. Unable to process the request.
        /// </summary>
        [Description("Unhandled Exception occured. Unable to process the request.")]
        Unhandled
    }

    /// <summary>
    /// Extension methods for <see cref="ResponseMessageEnum"/>.
    /// </summary>
    public static class ResponseMessageEnumExtensions
    {
        /// <summary>
        /// Gets the description associated with the enum value via the <see cref="DescriptionAttribute"/>.
        /// </summary>
        /// <param name="enumValue">The enum value to get the description for.</param>
        /// <returns>The description from the <see cref="DescriptionAttribute"/>, or the enum value name if no description is found.</returns>
        public static string GetDescription(this ResponseMessageEnum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field != null)
            {
                var attribute = (DescriptionAttribute?)Attribute.GetCustomAttribute(field, typeof(DescriptionAttribute));
                return attribute?.Description ?? enumValue.ToString();
            }
            return enumValue.ToString();
        }
    }
}

