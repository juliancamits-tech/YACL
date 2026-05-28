using ConsoleTestApp;

Console.WriteLine("Hello, World!");

var p1 = MyFirstEnum.Item1;
var enumValue = "item1".ToMyFirstEnum();
p1.ToStringV2();
MyFirstEnum.FromString("item1");

var workflow = WorkFlow.Pending;
Console.WriteLine(workflow.ToStringV2());
workflow = workflow.NextStep();
Console.WriteLine(workflow.ToStringV2());
workflow = workflow.PreviousStep();
Console.WriteLine(workflow.ToStringV2());
workflow = workflow.ErrorStep();
Console.WriteLine(workflow.ToStringV2());