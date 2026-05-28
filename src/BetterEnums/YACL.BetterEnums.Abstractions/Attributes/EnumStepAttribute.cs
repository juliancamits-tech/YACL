namespace BetterEnums.YACL.Abstractions.Attributes;

/// <summary>
/// Specified step names for next, previous, and error transitions.
/// </summary>
/// <param name="nextStep">Next step in the enum list or null if there no next step</param>
/// <param name="previousStep">Previous step in the enum list or null if there no previous step</param>
/// <param name="errorStep">Error step in the enum list</param>
/// <code>
/// public enum MyEnum
/// {
///  [EnumStepAttribute(WorkFlow.Procesing,null,WorkFlow.Error)]
///  Pending = 0,
///  [EnumStepAttribute(WorkFlow.Sucess,WorkFlow.Pending,WorkFlow.Error)]
///  Procesing = 1,
///  Sucess = 2,
///  Error = 3
///  }
/// </code>
[AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
#pragma warning disable CS9113 // Parameter is unread.
public class EnumStepAttribute(object? nextStep, object? previousStep, object? errorStep) : Attribute
#pragma warning restore CS9113 // Parameter is unread.
{
}