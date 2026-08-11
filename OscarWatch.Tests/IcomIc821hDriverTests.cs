using OscarWatch.Core.Models;
using OscarWatch.Rig;

namespace OscarWatch.Tests;

public sealed class IcomIc821hDriverTests
{
    private static readonly byte[] CivNak = [0xFE, 0xFE, 0x60, 0x00, 0xFA, 0xFD];

    [Fact]
    public void RigType_is_IcomIc821h()
    {
        var driver = new IcomIc821hDriver("COM1", 19200, "4C");
        Assert.Equal(RigType.IcomIc821h, driver.RigType);
    }

    [Fact]
    public void Factory_creates_IcomIc821h_driver()
    {
        var driver = RigDriverFactory.Create(new RigSettings { Type = RigType.IcomIc821h });
        Assert.IsType<IcomIc821hDriver>(driver);
    }

    [Fact]
    public void SupportsVfoExchange_is_false()
    {
        var driver = new IcomIc821hDriver("COM1", 19200, "4C");
        Assert.False(driver.SupportsVfoExchange);
    }

    [Fact]
    public void Satellite_mode_inverts_main_sub_band_access_bytes()
    {
        var transport = new RecordingIcomCivTransport();
        var driver = new IcomIc821hDriver(transport);
        transport.Open();
        driver.Open();

        driver.SetSatelliteMode(true);
        driver.SelectVfo(RigVfo.Main, force: true);
        Assert.Contains("07d1", transport.SentCommandBodies);

        driver.SelectVfo(RigVfo.Sub, force: true);
        Assert.Contains("07d0", transport.SentCommandBodies);
    }

    [Fact]
    public void Non_satellite_mode_uses_normal_main_sub_bytes()
    {
        var transport = new RecordingIcomCivTransport();
        var driver = new IcomIc821hDriver(transport);
        transport.Open();
        driver.Open();

        driver.SetSatelliteMode(false);
        driver.SelectVfo(RigVfo.Main, force: true);
        Assert.Contains("07d0", transport.SentCommandBodies);

        driver.SelectVfo(RigVfo.Sub, force: true);
        Assert.Contains("07d1", transport.SentCommandBodies);
    }

    [Fact]
    public void SetSatelliteMode_does_not_send_unsupported_satellite_command()
    {
        var transport = new RecordingIcomCivTransport();
        var driver = new IcomIc821hDriver(transport);
        transport.Open();
        driver.Open();

        var before = transport.CommandCount;
        driver.SetSatelliteMode(true);
        driver.SetSatelliteMode(false);

        Assert.Equal(before, transport.CommandCount);
        Assert.DoesNotContain("1a0701", transport.SentCommandBodies);
        Assert.DoesNotContain("1a0700", transport.SentCommandBodies);
    }

    [Fact]
    public void SetFrequencyHz_without_ack_sends_once_and_updates_cached_read()
    {
        var transport = new RecordingIcomCivTransport { MainHz = 435_750_000 };
        var driver = new IcomIc821hDriver(transport);
        driver.Open();
        driver.SelectVfo(RigVfo.Main);

        transport.SetFrequencyResponses.Enqueue([]);
        Assert.True(driver.SetFrequencyHz(435_751_000));
        Assert.Equal(1, transport.SetFrequencyCommandCount);

        transport.NextReadResponse = [];
        Assert.Equal(435_751_000, driver.ReadFrequencyHz(RigVfo.Main));
    }

    [Fact]
    public void SetFrequencyHz_nak_is_rejected_without_retry()
    {
        var transport = new RecordingIcomCivTransport { MainHz = 435_750_000 };
        var driver = new IcomIc821hDriver(transport);
        driver.Open();
        driver.SelectVfo(RigVfo.Main);

        transport.SetFrequencyResponses.Enqueue(CivNak);
        Assert.False(driver.SetFrequencyHz(435_751_000));
        Assert.Equal(1, transport.SetFrequencyCommandCount);
    }

    [Fact]
    public void SelectVfo_without_ack_does_not_retry_on_ic821()
    {
        var transport = new RecordingIcomCivTransport();
        var driver = new IcomIc821hDriver(transport);
        driver.Open();
        driver.SetSatelliteMode(true);
        transport.CommandResponses.Enqueue([]);

        var before = transport.CommandCount;
        driver.SelectVfo(RigVfo.Main, force: true);

        Assert.Equal(before + 1, transport.CommandCount);
        Assert.Equal("07d1", transport.SentCommandBodies[^1]);
    }

    [Fact]
    public void SetSplitOn_does_not_send_split_command()
    {
        var transport = new RecordingIcomCivTransport();
        var driver = new IcomIc821hDriver(transport);
        transport.Open();
        driver.Open();

        var before = transport.CommandCount;
        driver.SetSplitOn(true);
        driver.SetSplitOn(false);
        Assert.Equal(before, transport.CommandCount);
    }

    [Fact]
    public void EstablishSatelliteVfoState_selects_main_rx_band()
    {
        var transport = new RecordingIcomCivTransport();
        var driver = new IcomIc821hDriver(transport);
        transport.Open();
        driver.Open();

        driver.SetSatelliteMode(true);
        transport.SentCommandBodies.Clear();
        driver.EstablishSatelliteVfoState();
        Assert.Equal("07d1", transport.SentCommandBodies[^1]);
    }
}
