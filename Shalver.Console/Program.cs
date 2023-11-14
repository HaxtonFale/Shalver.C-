using Cocona;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;
using Shalver.Ayesha;
using Shalver.Console;
using Shalver.Escha;
using Shalver.Model;
using Shalver.Shallie;

using MSLogger = Microsoft.Extensions.Logging.ILogger;

static void PrintTimeTaken(MSLogger logger, DateTime start, DateTime end)
{
    var span = end - start;
    logger.LogInformation("Search took {Seconds:F3} seconds.", span.TotalSeconds);
}

var builder = CoconaApp.CreateBuilder(args);
builder.Host.UseSerilog((_, _, config) =>
{
    config.MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});
builder.Services
    .AddSingleton<IDataSource, ShallieDataSource>()
    .AddSingleton<IDataSource, EschaDataSource>()
    .AddSingleton<IDataSource, AyeshaDataSource>()
    .AddTransient<DataSourceFactory>()
    .AddTransient(provider =>
    {
        var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
        return loggerFactory.CreateLogger("Shalver.Console");
    });

var app = builder.Build();
app.AddSubCommand("find", b => {
    b.AddCommand("path", (MSLogger logger, DataSourceFactory dsf, Atelier atelier, string source, string destination) => {
        var dataSource = dsf.GetDataSource(atelier);
        var sourceItem = dataSource.GetItem(source);
        var destinationItem = dataSource.GetItem(destination);

        var solver = new BFS(dataSource);

        logger.LogInformation("Finding a shortest synthesis/disassembly chain from {Source} to {Destination}...",
            sourceItem.DisplayName, destinationItem.DisplayName);
        var start = DateTime.Now;
        var solution = solver.TrySolve(sourceItem, destinationItem);
        var end = DateTime.Now;

        if (solution != null)
        {
            logger.LogInformation("Solution found:");
            var step = 1;
            foreach (var line in solution.RenderSolution())
            {
                logger.LogInformation("{Step}. {Line}", step++, line);
            }
        }
        else
        {
            logger.LogInformation("No solution found.");
        }

        PrintTimeTaken(logger, start, end);
    });
    b.AddCommand("longest", (MSLogger logger, DataSourceFactory dsf, Atelier atelier) => {
        var dataSource = dsf.GetDataSource(atelier);

        var sources = dataSource.GetValidSources().ToList();
        var destinations = dataSource.GetValidDestinations().ToList();

        var start = DateTime.Now;
        var solutions = sources.SelectMany(s => destinations.Select(d => (s, d)))
            .AsParallel()
            .Select(tuple => {
                var solver = new BFS(dataSource);
                var solution = solver.TrySolve(tuple.s, tuple.d);
                return (solution, tuple.s, tuple.d);
            })
            .Where(tuple => tuple.solution != null)
            .ToList();
        var end = DateTime.Now;

        var longest = solutions.GroupBy(tuple => tuple.solution!.Length).MaxBy(grp => grp.Key)!.ToList();
        logger.LogInformation("Found {Longest} longest solutions:", longest.Count);
        foreach (var (sol, src, dst) in longest)
        {
            logger.LogInformation("{Source} to {Destination} ({Length} steps).", src.DisplayName, dst.DisplayName, sol!.Length);
        }

        PrintTimeTaken(logger, start, end);
    });
});

app.Run();