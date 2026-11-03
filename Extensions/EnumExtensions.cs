using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace Borderland.Dreams.Extensions;

public static class EnumExtensions
{
    public static string GetDisplayName<TEnum>(this TEnum value)
    where TEnum : struct, Enum
    {
        return typeof(TEnum)
            .GetMember(value.ToString())
            .FirstOrDefault()?
            .GetCustomAttribute<DisplayAttribute>()?
            .GetName()
            ?? value.ToString();
    }
}