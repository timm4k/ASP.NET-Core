namespace DependencyInjectionLab.Traffic;

internal interface ITrafficCounter
{
    int Count { get; }
    void RecordTraffic();
}

internal sealed class TrafficCounter : ITrafficCounter
{
    public TrafficCounter()
    {
        Count++;
    }

    public int Count { get; private set; }

    public void RecordTraffic()
    {
        Count++;
    }
}
