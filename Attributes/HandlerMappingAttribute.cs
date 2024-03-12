[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class HandlerMappingAttribute : Attribute
{
    public string Action { get; }

    public HandlerMappingAttribute(string action)
    {
        Action = action;
    }
}