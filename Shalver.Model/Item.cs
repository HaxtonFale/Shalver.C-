namespace Shalver.Model;

public class Item : Ingredient
{
    public ItemType Type { get; init; }
    public IList<Category> Categories { get; init; }
    public bool IsStackable { get; init; }

    public IList<Ingredient>? Ingredients { get; set; }
    public bool HasIngredients => Ingredients?.Count > 0;
    public IList<Item>? DisassemblesInto { get; set; }
    public bool Disassembles => DisassemblesInto?.Count > 0;

    public Item(string internalName, string displayName, ItemType type, IList<Category> categories,
        bool isStackable) : base(internalName, displayName)
    {
        Type = type;
        Categories = categories;
        IsStackable = isStackable;
    }
}