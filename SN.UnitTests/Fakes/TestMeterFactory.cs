using System.Diagnostics.Metrics;

namespace SN.UnitTests.Fakes;

public sealed class TestMeterFactory : IMeterFactory
{
    private readonly List<Meter> meters = [];

    public Meter Create(MeterOptions options)
    {
        var meter = new Meter(options.Name, options.Version, options.Tags, scope: this);
        meters.Add(meter);
        return meter;
    }

    public void Dispose()
    {
        foreach (var meter in meters)
        {
            meter.Dispose();
        }
    }
}