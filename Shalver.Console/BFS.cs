using Shalver.Model;

namespace Shalver.Console;

// ReSharper disable once InconsistentNaming
public class BFS : Solver
{
    private readonly Queue<Solution> _solutions = new ();
    public BFS(IDataSource dataSource) : base(dataSource)
    {
    }

    protected override Solution GetNextSolution() => _solutions.Dequeue();

    protected override bool CanGetNextSolution() => _solutions.Count > 0;

    protected override void StoreSolution(Solution solution) => _solutions.Enqueue(solution);
}