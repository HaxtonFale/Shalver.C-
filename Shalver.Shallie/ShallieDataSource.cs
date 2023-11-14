using Microsoft.Extensions.Logging;
using Shalver.Model;
using Shalver.IO;

namespace Shalver.Shallie;

public class ShallieDataSource : IDataSource
{
    private readonly IDictionary<string, Category> _categories = new Dictionary<string, Category>();
    
    private readonly IDictionary<string, Item> _items = new Dictionary<string, Item>();
    
    private readonly IDictionary<string, Recipe> _recipes = new Dictionary<string, Recipe>();

    private readonly Dictionary<string, Disassembly> _disassemblies = new();

    private readonly ILogger<ShallieDataSource> _logger;

    public ShallieDataSource(ILogger<ShallieDataSource> logger)
    {
        _logger = logger;

        _logger.LogInformation("Loading data for Atelier Shallie...");
        var loader = new CsvLoader("Shalver.Shallie.Data", typeof(ShallieDataSource).Assembly);

        _logger.LogDebug("Loading Category data...");
        foreach (var categoryRow in loader.LoadCsv("Categories", true))
        {
            var internalName = categoryRow[0];
            var displayName = categoryRow[1];
            _logger.LogTrace("Loaded Category {CategoryName} [{InternalName}]", displayName, internalName);
            _categories[internalName] = new Category(internalName, displayName);
        }
        _logger.LogDebug("Loaded {Categories} categories.", _categories.Count);

        _logger.LogDebug("Loading Item data...");
        foreach (var itemRow in loader.LoadCsv("Items", true))
        {
            var internalName = itemRow[1];
            var displayName = itemRow[24];
            _logger.LogTrace("Loading Item {DisplayName} ({InternalName})...", displayName, internalName);
            var categories = itemRow[13].Split(',').Where(c => !string.IsNullOrWhiteSpace(c))
                .Select(c => _categories[c]).ToList();
            _logger.LogTrace("Categories: {Categories}", string.Join(", ", categories.Select(c => c.DisplayName)));
            var itemType = Enum.Parse<ItemType>(itemRow[3]);
            var isStackable = itemRow[28] == "True";
            _items[internalName] = new Item(internalName, displayName, itemType, categories, isStackable);
        }
        _logger.LogDebug("Loaded {Items} items.", _items.Count);

        _logger.LogDebug("Loading Recipe data...");
        foreach (var recipeRow in loader.LoadCsv("Ingredients", true))
        {
            var item = _items[recipeRow[0]];
            _logger.LogTrace("Loading Recipe for {Item}...", item.DisplayName);
            var ingredients = new List<Ingredient>(4);
            for (var i = 0; i < 4; i++)
            {
                var ingredientName = recipeRow[3 * i + 1];
                if (string.IsNullOrWhiteSpace(ingredientName)) break;
                if (recipeRow[3 * i + 2] == "1")
                {
                    _logger.LogTrace("Ingredient {Ingredient} identified as Category.", ingredientName);
                    ingredients.Add(_categories[ingredientName]);
                }
                else
                {
                    _logger.LogTrace("Ingredient {Ingredient} identified as Item.", ingredientName);
                    ingredients.Add(_items[ingredientName]);
                }
            }

            item.Ingredients = ingredients;
            _recipes[item.InternalName] = new Recipe(item, ingredients);
        }
        _logger.LogDebug("Loaded {Recipes} recipes.", _recipes.Count);

        _logger.LogDebug("Loading Disassembly data...");
        foreach (var disassemblyRow in loader.LoadCsv("Disassembly", true))
        {
            var item = _items[disassemblyRow[0]];
            _logger.LogTrace("Loading Disassembly for {Item}...", item.DisplayName);
            var results = new List<Item> { _items[disassemblyRow[1]] };
            if (disassemblyRow[2] != "")
            {
                results.Add(_items[disassemblyRow[2]]);
            }

            item.DisassemblesInto = results;
            _disassemblies[item.InternalName] = new Disassembly(item, results);
        }
        _logger.LogDebug("Loaded {Disassemblies} disassemblies.", _disassemblies.Count);

        _logger.LogInformation("Atelier Shallie data successfully loaded.");
    }

    public IEnumerable<Item> GetCategoryItems(Category category) => _items.Values.Where(i => i.Categories.Contains(category));

    public Item GetItem(string itemName)
    {
        _logger.LogTrace("Looking up item \"{ItemName}\"...", itemName);
        if (_items.TryGetValue(itemName, out var item)) return item;
        return _items.Values.SingleOrDefault(i => i.DisplayName == itemName) ??
               throw new ArgumentException($"Item with name {itemName} does not exist.");
    }

    public bool HasRecipe(Item item) => _recipes.ContainsKey(item.InternalName);
    public Recipe GetRecipe(Item item) => _recipes[item.InternalName];
    public IEnumerable<Ingredient> GetIngredients(Item item) => GetRecipe(item).Ingredients;
    public IEnumerable<Recipe> GetRecipesUsing(Ingredient ingredient) =>
        _recipes.Values.Where(r => r.Ingredients.Contains(ingredient));

    public bool HasDisassembly => false;
    public IEnumerable<Item> GetDisassemblySources(Item item) => GetDisassemblySources(item, false);
    public IEnumerable<Item> GetDisassemblySources(Item item, bool includeAll) => _disassemblies
        .Values.Where(d => d.Results.Contains(item) && (includeAll || item.IsStackable == d.Item.IsStackable))
        .Select(d => d.Item);

    public IEnumerable<Item> GetValidSources() => _items.Values.Where(i => !i.IsStackable);
    public IEnumerable<Item> GetValidDestinations() => _recipes.Values.Select(r => r.Item);
}