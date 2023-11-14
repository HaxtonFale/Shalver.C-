namespace Shalver.Model;

public abstract class Ingredient
{
    public string InternalName { get; init; }
    public string DisplayName { get; init; }

    internal Ingredient(string internalName, string displayName)
    {
        InternalName = internalName;
        DisplayName = displayName;
    }

    public override string ToString() => DisplayName;
}