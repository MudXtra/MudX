using System.ComponentModel;
using System.Reflection;

namespace MudX.Extensions
{
    public static class EnumExtensions
    {
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
