using Shalver.Model;

namespace Shalver.Console;

public abstract class Solver
{
    protected readonly IDataSource DataSource;

    protected Solver(IDataSource dataSource)
    {
        DataSource = dataSource;
    }

    protected abstract Solution GetNextSolution();
    protected abstract bool CanGetNextSolution();
    protected abstract void StoreSolution(Solution solution);

    public Solution? TrySolve(Item addedItem, Item destinationItem)
    {
        StoreSolution(new Solution(destinationItem));

        var visitedItems = new HashSet<Item>();
        var visitedCategories = new HashSet<Category>();

        void AddSolution(Solution solution)
        {
            var ingredient = solution.CurrentItem;
            if (DataSource.HasRecipe(ingredient))
            {
                StoreSolution(solution);
            }
        }

        while (CanGetNextSolution())
        {
            var solution = GetNextSolution();
            foreach (var ingredient in DataSource.GetIngredients(solution.CurrentItem))
            {
                switch (ingredient)
                {
                    case Item item when !visitedItems.Add(item):
                        continue;
                    case Item item:
                        var itemSolution = solution.AddStep(item, StepType.Synthesize);
                        if (itemSolution.CurrentItem == addedItem)
                        {
                            return itemSolution;
                        }

                        AddSolution(itemSolution);
                        break;
                    case Category cat when !visitedCategories.Add(cat):
                        continue;
                    case Category cat:
                        {
                            foreach (var item in DataSource.GetCategoryItems(cat))
                            {
                                if (!visitedItems.Add(item)) continue;
                                var catSolution = solution.AddStep(item, StepType.SynthesizeAsCategory, cat);
                                if (catSolution.CurrentItem == addedItem)
                                {
                                    return catSolution;
                                }
                                AddSolution(catSolution);
                            }

                            break;
                        }
                }
            }

            if (DataSource.HasDisassembly) continue;
            foreach (var item in DataSource.GetDisassemblySources(solution.CurrentItem))
            {
                if (!visitedItems.Add(item)) continue;
                var newSolution = solution.AddStep(item, StepType.Disassemble);
                if (newSolution.CurrentItem == addedItem)
                {
                    return newSolution;
                }
                AddSolution(newSolution);
            }
        }

        return null;
    }
}