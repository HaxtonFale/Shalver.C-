namespace Shalver.Model;

public class Recipe
{
    public Item Item { get; init; }
    public IList<Ingredient> Ingredients { get; init; }

    public Recipe(Item item, IList<Ingredient> ingredients)
    {
        Item = item;
        Ingredients = ingredients;
    }
}