using OscarWatch.Core.Models;

namespace OscarWatch.Rig;

/// <summary>
/// IC-821H CI-V. Satellite mode uses Main=RX / Sub=TX, but band-access bytes D0/D1 are
/// inverted vs IC-910/9700 while SAT is on. Satellite mode itself is operator controlled;
/// there is no reliable IC-821H CI-V command to toggle it. No split CAT; uplink CTCSS is front-panel only.
/// </summary>
public sealed class IcomIc821hDriver : IcomCivDriverBase
{
    private bool _satelliteModeActive;

    public IcomIc821hDriver(string port, int baudRate, string civAddressHex, int catDelayMs = 50)
        : base(RigType.IcomIc821h, port, baudRate, civAddressHex, catDelayMs)
    {
    }

    internal IcomIc821hDriver(IIcomCivTransport transport)
        : base(RigType.IcomIc821h, transport)
    {
    }

    public override bool SupportsTracking => true;

    public bool SupportsVfoExchange => false;

    /// <summary>
    /// IC-821H CI-V is commonly used at low baud rates on an echoing single-wire bus. A complete
    /// command echo can be observed before the radio's acknowledgement, so retrying solely because
    /// FB was not present can send the same stale Doppler target multiple times.
    /// </summary>
    protected override bool FrequencyWritesRequireAck => false;

    /// <summary>
    /// Main/Sub band access commands may not return an explicit FB in satellite operation. Do not
    /// repeat the selector simply because the first completed CI-V frame was the command echo.
    /// </summary>
    protected override bool RetryVfoSelectWithoutAck => false;

    public override void SetSatelliteMode(bool on)
    {
        // IC-821H satellite/full-duplex mode is configured from the front panel. Keep only the
        // software mapping state here; sending the IC-910-era 1A 07 command adds retries/delay and
        // is not a supported IC-821H satellite-mode control.
        _satelliteModeActive = on;
    }

    public override void SetSplitOn(bool on)
    {
        // Manual: sub band cannot perform split/duplex/offset in satellite layout.
    }

    public override void SetToneOn(bool on)
    {
    }

    public override void SetToneSquelchOn(bool on)
    {
    }

    public override void SetToneHz(double hz, bool squelchTone)
    {
    }

    /// <summary>
    /// After SAT is enabled in OscarWatch, select Main (RX/downlink) so software VFO state matches the radio.
    /// The operator must already have enabled satellite/full-duplex mode on the IC-821H front panel.
    /// </summary>
    public void EstablishSatelliteVfoState()
    {
        SelectVfo(RigVfo.Main, force: true);
    }

    /// <summary>
    /// In satellite mode, CI-V Main band access (D0) selects TX/uplink; Sub band access (D1) selects RX/downlink.
    /// </summary>
    protected override RigVfo MapOperationalVfo(RigVfo vfo) =>
        !_satelliteModeActive
            ? vfo
            : vfo switch
            {
                RigVfo.Main => RigVfo.Sub,
                RigVfo.Sub => RigVfo.Main,
                _ => vfo
            };
}
