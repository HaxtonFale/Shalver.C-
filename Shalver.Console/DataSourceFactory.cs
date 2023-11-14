using Shalver.Ayesha;
using Shalver.Escha;
using Shalver.Model;
using Shalver.Shallie;

namespace Shalver.Console;

public class DataSourceFactory
{
    private readonly IEnumerable<IDataSource> _dataSources;

    public DataSourceFactory(IEnumerable<IDataSource> dataSources)
    {
        _dataSources = dataSources;
    }

    public IDataSource GetDataSource(Atelier atelier)
    {
        var matchType = atelier switch {
            Atelier.Ayesha => typeof(AyeshaDataSource),
            Atelier.Escha => typeof(EschaDataSource),
            Atelier.Shallie => typeof(ShallieDataSource),
            _ => throw new ArgumentOutOfRangeException(nameof(atelier))
        };
        var dataSource = _dataSources.FirstOrDefault(s => s.GetType() == matchType);
        return dataSource ?? throw new ArgumentOutOfRangeException(nameof(atelier));
    }
}