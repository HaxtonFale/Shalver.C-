namespace Shalver.Model;

public class Category : Ingredient
{
    public Category(string internalName, string displayName) : base(internalName, displayName)
    {
        InternalName = internalName;
        DisplayName = displayName;
    }
}