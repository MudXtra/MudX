using System.ComponentModel;
using System.Reflection;

namespace MudX.Extensions
{
    /// <summary>
    /// Provides extension methods for working with enumeration types.
    /// </summary>
    /// <remarks>This class includes methods that enhance the functionality of enums, such as retrieving
    /// custom descriptions defined by the <see cref="DescriptionAttribute"/>.</remarks>
    public static class EnumExtensions
    {
        /// <summary>
        /// Retrieves the description associated with an enumeration value.
        /// </summary>
        /// <remarks>This method is useful for obtaining human-readable descriptions of enumeration
        /// values when the <see cref="DescriptionAttribute"/> is used to provide metadata for the
        /// values.</remarks>
        public static string ToDescription(this Enum value)
        {
            if (value == null)
                return string.Empty;

            var field = value.GetType().GetField(value.ToString());
            if (field == null)
                return value.ToString();

            var attribute = field.GetCustomAttribute<DescriptionAttribute>();
            return attribute?.Description ?? value.ToString();
        }
    }
}
