using BetterEnums.YACL.Abstractions.Attributes;

namespace ConsoleTestApp;

public enum MyFirstEnum
{
    Item1 = 1,
    Item2 = 2,
    Item3 = 3
}

public enum MySecondEnum : short
{
    Item3,
    Item4,
    Item5
}

public enum WorkFlow
{
    [EnumStepAttribute(WorkFlow.Procesing, null, WorkFlow.Error)]
    Pending = 0,
    [EnumStepAttribute(WorkFlow.Sucess, WorkFlow.Pending, WorkFlow.Error)]
    Procesing = 1,
    Sucess = 2,
    Error = 3
}
