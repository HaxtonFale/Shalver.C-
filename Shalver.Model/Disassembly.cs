namespace Shalver.Model;

public class Disassembly
{
    public Item Item { get; init; }
    public IList<Item> Results { get; init; }

    public Disassembly(Item item, IList<Item> results)
    {
        Item = item;
        Results = results;
    }
}