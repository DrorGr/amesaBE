using System.ComponentModel;
using System.Reflection;

namespace AmesaBackend.Shared.Contracts
{
    public enum ResponseMessageEnum
    {
        [Description("Request successful.")]
        Success,
        [Description("Request not found. The specified uri does not exist.")]
        NotFound,
        [Description("Request responded with 'Method Not Allowed'.")]
        MethodNotAllowed,
        [Description("Request no content. The specified uri does not contain any content.")]
        NotContent,
        [Description("Request responded with exceptions.")]
        Exception,
        [Description("Request denied. Unauthorized access.")]
        UnAuthorized,
        [Description("Request responded with validation error(s). Please correct the specified validation errors and try again.")]
        ValidationError,
        [Description("Request cannot be processed. Please contact a support.")]
        Unknown,
        [Description("Unhandled Exception occured. Unable to process the request.")]
        Unhandled
    }

    public static class ResponseMessageEnumExtensions
    {
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

