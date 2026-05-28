#nullable enable
using System.Collections.Generic;
using System.Linq;

namespace YACL.BetterEnums.SourceGeneration;

internal class EnumInfo
{
    public string NameSpace { get; set; } = string.Empty;
    public string EnumName { get; set; } = string.Empty;
    public string TypeOfEnum { get; set; } = string.Empty;

    public string CleanNameSpace => EnumName.Replace(".", string.Empty);
    public string CleanName => EnumName.Split('.').LastOrDefault();
    public List<EnumItem> EnumItems { get; private set; } = [];

    public void AddEnumItem(EnumItem item)
    {
        this.EnumItems.Add(item);
    }
}

public class EnumItem
{
    public string TextValue { get; set; } = string.Empty;
    public string NumericValue { get; set; } = string.Empty;

    public string NextItem { get; set; } = string.Empty;
    public string PrevItem { get; set; } = string.Empty;
    public string ErrorItem { get; set; } = string.Empty;

}

