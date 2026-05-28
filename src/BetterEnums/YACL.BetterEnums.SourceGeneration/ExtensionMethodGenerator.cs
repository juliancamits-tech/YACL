using System;
using System.Linq;
using System.Text;

namespace YACL.BetterEnums.SourceGeneration
{
    internal static class ExtensionMethodGenerator
    {
        public static string GenerateExtensionFile(EnumInfo enumInfo)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System.Collections.Frozen;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine($"//{DateTime.Now}");
            sb.AppendLine($"namespace {enumInfo.NameSpace};");
            sb.AppendLine();

            sb.AppendLine($"public static class {enumInfo.CleanNameSpace}BetterEnumsExtensions");
            sb.AppendLine("{");


            #region Extension ToDto (?)

            sb.AppendLine($"    private static readonly System.Lazy<FrozenDictionary<{enumInfo.TypeOfEnum}, string>> _dtoDicInt = new System.Lazy<FrozenDictionary<{enumInfo.TypeOfEnum}, string>>(() => CreateDictionary());");
            sb.AppendLine();

            sb.AppendLine($"    private static FrozenDictionary<{enumInfo.TypeOfEnum},string> CreateDictionary()");
            sb.AppendLine("     {");
            sb.AppendLine($"         return new Dictionary<{enumInfo.TypeOfEnum}, string>()");
            sb.AppendLine("         {");
            foreach (var enumItem in enumInfo.EnumItems)
            {
                sb.AppendLine($"            {{{enumItem.NumericValue},\"{enumItem.TextValue}\"}},");
            }
            sb.AppendLine("         }.ToFrozenDictionary();");
            sb.AppendLine("     }");
            sb.AppendLine();

            #endregion

            sb.AppendLine();

            #region Direct use Enum

            sb.AppendLine($"    extension({enumInfo.EnumName})");
            sb.AppendLine("     {");

            #region Extension FromString

            sb.AppendLine($"         public static {enumInfo.EnumName} FromString(string value) => value switch");
            sb.AppendLine("         {");
            foreach (var members in enumInfo.EnumItems)
            {
                sb.AppendLine($"                \"{members.TextValue}\" => {enumInfo.EnumName}.{members.TextValue},");
                sb.AppendLine($"                \"{members.TextValue.ToLowerInvariant()}\" => {enumInfo.EnumName}.{members.TextValue},");
                sb.AppendLine($"                \"{members.TextValue.ToUpperInvariant()}\" => {enumInfo.EnumName}.{members.TextValue},");
            }
            sb.AppendLine($"                 _ =>   System.Enum.TryParse<{enumInfo.EnumName}>(value, true ,out {enumInfo.EnumName} r) ? r : throw new System.ArgumentException($\"Invalid value '{{value}}'\")");
            sb.AppendLine("         };");

            #endregion

            #region Extension ToDto (?)

            sb.AppendLine();
            sb.AppendLine($"        public static FrozenDictionary<{enumInfo.TypeOfEnum},string> ToDictionary() => _dtoDicInt.Value;");

            sb.AppendLine();
            #endregion

            sb.AppendLine("     }");
            sb.AppendLine();

            #endregion

            #region Use Enum Value

            sb.AppendLine($"    extension({enumInfo.EnumName} value)");
            sb.AppendLine("     {");

            #region Extension ToStringV2

            sb.AppendLine($"         public string ToStringV2() => value switch");
            sb.AppendLine("         {");
            foreach (var members in enumInfo.EnumItems)
            {
                sb.AppendLine($"            {enumInfo.EnumName}.{members.TextValue} => \"{members.TextValue}\",");
            }
            sb.AppendLine($"                 _ => value.ToString()");
            sb.AppendLine("         };");

            #endregion

            #region Next

            sb.AppendLine();
            sb.AppendLine($"         public {enumInfo.EnumName} NextStep() => value switch");
            sb.AppendLine("         {");
            foreach (var members in enumInfo.EnumItems)
            {
                if (string.IsNullOrEmpty(members.NextItem))
                    continue;

                sb.AppendLine($"            {enumInfo.EnumName}.{members.TextValue} => {enumInfo.EnumName}.{enumInfo.EnumItems.FirstOrDefault(x => x.NumericValue == members.NextItem).TextValue},");
            }
            sb.AppendLine($"                 _ => value");
            sb.AppendLine("         };");

            #endregion

            #region Previous

            sb.AppendLine();
            sb.AppendLine($"         public {enumInfo.EnumName} PreviousStep() => value switch");
            sb.AppendLine("         {");
            foreach (var members in enumInfo.EnumItems)
            {
                if (string.IsNullOrEmpty(members.PrevItem))
                    continue;

                sb.AppendLine($"            {enumInfo.EnumName}.{members.TextValue} => {enumInfo.EnumName}.{enumInfo.EnumItems.FirstOrDefault(x => x.NumericValue == members.PrevItem).TextValue},");
            }
            sb.AppendLine($"                 _ => value");
            sb.AppendLine("         };");

            #endregion

            #region Previous

            sb.AppendLine();
            sb.AppendLine($"         public {enumInfo.EnumName} ErrorStep() => value switch");
            sb.AppendLine("         {");
            foreach (var members in enumInfo.EnumItems)
            {
                if (string.IsNullOrEmpty(members.ErrorItem))
                    continue;

                sb.AppendLine($"            {enumInfo.EnumName}.{members.TextValue} => {enumInfo.EnumName}.{enumInfo.EnumItems.FirstOrDefault(x => x.NumericValue == members.ErrorItem).TextValue},");
            }
            sb.AppendLine($"                 _ => value");
            sb.AppendLine("         };");

            #endregion

            sb.AppendLine("     }");
            sb.AppendLine();

            #endregion

            #region Direct use String

            sb.AppendLine("     extension(string value)");
            sb.AppendLine("     {");

            #region String To Enum

            sb.AppendLine($"         public {enumInfo.EnumName} To{enumInfo.CleanName}() => value switch");
            sb.AppendLine("         {");
            foreach (var members in enumInfo.EnumItems)
            {
                sb.AppendLine($"                \"{members.TextValue}\" => {enumInfo.EnumName}.{members.TextValue},");
                sb.AppendLine($"                \"{members.TextValue.ToLowerInvariant()}\" => {enumInfo.EnumName}.{members.TextValue},");
                sb.AppendLine($"                \"{members.TextValue.ToUpperInvariant()}\" => {enumInfo.EnumName}.{members.TextValue},");
            }
            sb.AppendLine($"                 _ =>   System.Enum.TryParse<{enumInfo.EnumName}>(value, true ,out {enumInfo.EnumName} r) ? r : throw new System.ArgumentException($\"Invalid value '{{value}}'\")");
            sb.AppendLine("         };");

            #endregion

            sb.AppendLine("     }");
            sb.AppendLine();

            #endregion

            sb.AppendLine("}");
            return sb.ToString();
        }
    }
}
