using System.Reflection;
using Microsoft.Extensions.Logging;
using Shalver.IO;
using Shalver.Model;

namespace Shalver.Ayesha;

public class AyeshaDataSource : IDataSource
{
    private readonly IDictionary<string, Category> _categories = new Dictionary<string, Category>();

    private readonly IDictionary<string, Item> _items = new Dictionary<string, Item>();

    private readonly IDictionary<string, Recipe> _recipes = new Dictionary<string, Recipe>();

    private readonly ILogger<AyeshaDataSource> _logger;

    public AyeshaDataSource(ILogger<AyeshaDataSource> logger)
    {
        _logger = logger;

        _logger.LogInformation("Loading data for Atelier Ayesha...");
        var loader = new CsvLoader("Shalver.Ayesha.Data", Assembly.GetAssembly(typeof(AyeshaDataSource))!);
        
        _logger.LogDebug("Loading Ingredient Item data...");
        foreach (var itemRow in loader.LoadCsv("IngredientItems", true))
        {
            var itemName = itemRow[1];
            _logger.LogTrace("Loading Item {ItemName}...", itemName);
            var categories = ProcessCategories(itemRow, 3);
            _logger.LogTrace("Categories: {Categories}", string.Join(", ", categories.Select(c => c.DisplayName)));
            _items[itemName] = new Item(itemName, itemName, ItemType.Material, categories, false);
        }
        _logger.LogDebug("Loaded {Count} Ingredient Items.", _items.Count);

        _logger.LogDebug("Loading Synthesis Item data...");
        var deferredRecipes = new HashSet<(string item, List<string> ingredients)>();
        var itemCount = 0;
        var recipeCount = 0;

        void ProcessItemRow(string[] itemRow, ItemType itemType)
        {
            var itemName = itemRow[1];
            _logger.LogTrace("Loading Item {ItemName}...", itemName);
            var categories = ProcessCategories(itemRow, 3);
            _logger.LogTrace("Categories: {Categories}", string.Join(", ", categories.Select(c => c.DisplayName)));
            var item = new Item(itemName, itemName, itemType, categories, false);
            _items[itemName] = item;
            itemCount++;
            _logger.LogTrace("Loading Item ingredients...");
            var ingredients = new List<Ingredient>(4);
            var defer = false;
            for (var i = 0; i < 4; i++)
            {
                var ingredientName = itemRow[8 + i];
                _logger.LogTrace("Ingredient: {IngredientName}", ingredientName);
                if (ingredientName == "") break;
                if (ingredientName.StartsWith('('))
                {
                    _logger.LogTrace("Identified as Category");
                    if (_categories.TryGetValue(ingredientName, out var ingCat))
                    {
                        _logger.LogTrace("Category already created. Retrieving..."); 
                        ingredients.Add(ingCat);
                    }
                    else
                    {
                        _logger.LogTrace("Category not yet created. Creating...");
                        var newCat = new Category(ingredientName, ingredientName);
                        _categories[ingredientName] = newCat;
                        ingredients.Add(newCat);
                    }
                }
                else
                {
                    _logger.LogTrace("Identified as Item");
                    if (_items.TryGetValue(ingredientName, out var ingItem))
                    {
                        _logger.LogTrace("Item already created. Retrieving...");
                        ingredients.Add(ingItem);
                    }
                    else
                    {
                        _logger.LogTrace("Item not yet created. Deferring...");
                        defer = true;
                        break;
                    }
                }
            }

            if (defer)
            {
                deferredRecipes.Add((itemName, itemRow[new Range(8, 12)].Where(i => i != "").ToList()));
                return;
            }

            recipeCount++;
            _recipes[itemName] = new Recipe(item, ingredients);
            item.Ingredients = ingredients;
        }

        foreach (var itemRow in loader.LoadCsv("SynthesisItems", true))
        {
            ProcessItemRow(itemRow, ItemType.Synthesis);
        }
        _logger.LogDebug("Loaded {Items} Synthesis Items and {Recipes} recipes.", itemCount, recipeCount);

        _logger.LogDebug("Loading Usable Item data...");
        itemCount = recipeCount = 0;
        foreach (var itemRow in loader.LoadCsv("UsableItems", true))
        {
            ProcessItemRow(itemRow, ItemType.Usable);
        }
        _logger.LogDebug("Loaded {Items} Usable Items and {Recipes} recipes.", itemCount, recipeCount);

        _logger.LogDebug("{Deferred} recipes have been deferred. Retrying...", deferredRecipes.Count);
        foreach (var (itemName, ingredientNames) in deferredRecipes)
        {
            var item = _items[itemName];
            var ingredients = new List<Ingredient>(4);
            _logger.LogTrace("Item: {Item}, ingredients: {Ingredients}", itemName, string.Join(", ", ingredientNames));
            foreach (var ingredientName in ingredientNames)
            {
                if (ingredientName.StartsWith('('))
                {
                    ingredients.Add(_categories[ingredientName]);
                }
                else
                {
                    ingredients.Add(_items[ingredientName]);
                }
            }

            _recipes[itemName] = new Recipe(item, ingredients);
            item.Ingredients = ingredients;
        }

        _logger.LogDebug("Loading Weapon data...");
        itemCount = 0;
        foreach (var itemRow in loader.LoadCsv("Weapons", true))
        {
            var itemName = itemRow[0];
            _logger.LogTrace("Loading Item {ItemName}...", itemName);
            var category = _categories[itemRow[3]];
            _logger.LogTrace("Category: {Category}", category.DisplayName);
            _items[itemName] = new Item(itemName, itemName, ItemType.Equipment, new List<Category> {category}, false);
            itemCount++;
        }
        _logger.LogDebug("Loaded {Count} Weapons.", itemCount);

        _logger.LogDebug("Loading Armor data...");
        itemCount = 0;
        foreach (var itemRow in loader.LoadCsv("Armor", true))
        {
            var itemName = itemRow[1];
            _logger.LogTrace("Loading Item {ItemName}...", itemName);
            var category = _categories[itemRow[3]];
            _logger.LogTrace("Category: {Category}", category.DisplayName);
            _items[itemName] = new Item(itemName, itemName, ItemType.Equipment, new List<Category> { category }, false);
            itemCount++;
        }
        _logger.LogDebug("Loaded {Count} Armors.", itemCount);

        _logger.LogInformation("Atelier Ayesha data successfully loaded.");
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
    public IEnumerable<Item> GetDisassemblySources(Item item) => Enumerable.Empty<Item>();

    public IEnumerable<Item> GetValidSources() => _items.Values;

    public IEnumerable<Item> GetValidDestinations() => _recipes.Values.Select(r => r.Item);

    private IList<Category> ProcessCategories(string[] itemRow, int startingIndex)
    {
        var categories = new List<Category>(4);
        for (var i = 0; i < 4; i++)
        {
            if (itemRow[startingIndex + i] == "") break;
            var categoryName = itemRow[3 + i];
            _logger.LogTrace("Checking Category {Category}...", categoryName);
            if (_categories.TryGetValue(categoryName, out var category))
            {
                _logger.LogTrace("Category already created. Retrieving...");
                categories.Add(category);
            }
            else
            {
                _logger.LogTrace("Category not yet created. Creating...");
                var newCat = new Category(categoryName, categoryName);
                _categories[categoryName] = newCat;
                categories.Add(newCat);
            }
        }
        return categories;
    }
}