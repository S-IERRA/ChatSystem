using System.Text;

namespace ChatSystem.Logic.Helpers;

public static class EnumToString
{
    public static string Convert<TGenericEnum>(this TGenericEnum enumType) where TGenericEnum : Enum
    {
        string enumString = enumType.ToString();
        var stringBuilder = new StringBuilder(enumString.Length * 2);

        foreach (char c in enumString)
        {
            if (Char.IsUpper(c) && stringBuilder.Length > 0)
                stringBuilder.Append(' ');

            stringBuilder.Append(c);
        }

        return stringBuilder.ToString();
    }
}