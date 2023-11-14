namespace Shalver.Model;

public interface IDataSource
{
    IEnumerable<Item> GetCategoryItems(Category category);
    Item GetItem(string itemName);
    bool HasRecipe(Item item);
    IEnumerable<Ingredient> GetIngredients(Item item);
    IEnumerable<Recipe> GetRecipesUsing(Ingredient ingredient);
    bool HasDisassembly { get; }
    IEnumerable<Item> GetDisassemblySources(Item item);
    IEnumerable<Item> GetValidSources();
    IEnumerable<Item> GetValidDestinations();
}