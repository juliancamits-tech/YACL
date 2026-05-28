namespace ConsoleTestApp;

internal static class StaticExtension
{
    extension(ConsoleTestApp.MySecondEnum value)
    {
        public string ToStringV2() => value switch
        {
            ConsoleTestApp.MySecondEnum.Item3 => "Item3",
            ConsoleTestApp.MySecondEnum.Item4 => "Item4",
            ConsoleTestApp.MySecondEnum.Item5 => "Item5",
            _ => value.ToString()
        };
    }


}