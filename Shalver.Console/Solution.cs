using Shalver.Model;

namespace Shalver.Console;

public class Solution
{
    public Item CurrentItem { get; init; }
    public (Solution Solution, StepType StepType, Category? Category)? NextStep { get; init; }
    public uint Length { get; }

    public Solution(Item item)
    {
        CurrentItem = item;
        Length = 0;
    }

    private Solution(Item item, (Solution Solution, StepType StepType, Category? Category) nextStep)
    {
        CurrentItem = item;
        NextStep = nextStep;
        Length = 1 + nextStep.Solution.Length;
    }

    public Solution AddStep(Item item, StepType stepType, Category? category = null)
    {
        if ((stepType == StepType.SynthesizeAsCategory) == (category == null))
        {
            throw new ArgumentException($"Cannot supply category if Step Type is not {StepType.SynthesizeAsCategory}",
                nameof(category));
        }

        return new Solution(item, (this, stepType, category));
    }

    public IEnumerable<string> RenderSolution()
    {
        var current = this;
        while (current.NextStep != null)
        {
            var (nextStep, stepType, category) = current.NextStep.Value;
            yield return stepType switch
            {
                StepType.Synthesize => $"Synthesize {nextStep.CurrentItem} with {current.CurrentItem}.",
                StepType.SynthesizeAsCategory =>
                    $"Synthesize {nextStep.CurrentItem} with {current.CurrentItem} as {category}",
                StepType.Disassemble => $"Disassemble {current.CurrentItem} into {nextStep.CurrentItem}.",
                _ => throw new ArgumentOutOfRangeException()
            };
            current = nextStep;
        }
    }
}