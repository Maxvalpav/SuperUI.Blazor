namespace SuperUI.Components;

// ── Enums ──────────────────────────────────────────────────────────────────────

/// <summary>Represents the current state of the acoustic data link.</summary>
public enum SgAudioLinkState
{
    /// <summary>No operation in progress.</summary>
    Idle,
    /// <summary>Currently transmitting data acoustically.</summary>
    Transmitting,
    /// <summary>Listening for incoming acoustic signals.</summary>
    Listening,
    /// <summary>Preamble (start of frame) has been detected.</summary>
    PreambleDetected,
    /// <summary>Actively receiving data from an acoustic signal.</summary>
    Receiving,
    /// <summary>An error occurred during operation.</summary>
    Error
}

// ── Event args ─────────────────────────────────────────────────────────────────

/// <summary>Event arguments for acoustic data transmission.</summary>
public sealed class SgAudioLinkDataReceivedEventArgs
{
    /// <summary>Received text content.</summary>
    public string Text       { get; init; } = string.Empty;
    /// <summary>Number of bytes received.</summary>
    public int    ByteCount  { get; init; }
    /// <summary>CRC checksum value.</summary>
    public int    Crc        { get; init; }
    /// <summary>Timestamp when the data was received.</summary>
    public DateTime ReceivedAt { get; init; } = DateTime.Now;
}

/// <summary>Event arguments raised when an acoustic transmission completes.</summary>
public sealed class SgAudioLinkTxCompleteEventArgs
{
    /// <summary>Transmitted text content.</summary>
    public string Text        { get; init; } = string.Empty;
    /// <summary>Number of symbols transmitted.</summary>
    public int    SymbolCount { get; init; }
    /// <summary>Timestamp when the transmission was sent.</summary>
    public DateTime SentAt    { get; init; } = DateTime.Now;
}

/// <summary>Event arguments for acoustic data link errors.</summary>
public sealed class SgAudioLinkErrorEventArgs
{
    /// <summary>Error description message.</summary>
    public string Message   { get; init; } = string.Empty;
    /// <summary>Error type/category identifier.</summary>
    public string ErrorType { get; init; } = string.Empty;
}

// ── Protocol info (from JS) ────────────────────────────────────────────────────

/// <summary>Protocol configuration information for the acoustic data link.</summary>
public sealed class SgAudioLinkProtocolInfo
{
    /// <summary>Base carrier frequency in Hz.</summary>
    public int    BaseFreq        { get; init; }
    /// <summary>Frequency step between carriers in Hz.</summary>
    public int    FreqStep        { get; init; }
    /// <summary>Number of carrier frequencies used.</summary>
    public int    NumCarriers     { get; init; }
    /// <summary>Symbol duration in milliseconds.</summary>
    public int    SymbolMs        { get; init; }
    /// <summary>Guard interval between symbols in ms.</summary>
    public int    GuardMs         { get; init; }
    /// <summary>Sync pattern A value.</summary>
    public int    SyncA           { get; init; }
    /// <summary>Sync pattern B value.</summary>
    public int    SyncB           { get; init; }
    /// <summary>Start tone index.</summary>
    public int    StartTone       { get; init; }
    /// <summary>FFT size used for spectral analysis.</summary>
    public int    FftSize         { get; init; }
    /// <summary>Audio sample rate in Hz.</summary>
    public int    SampleRate      { get; init; }
    /// <summary>Bits encoded per symbol.</summary>
    public int    BitsPerSymbol   { get; init; }
    /// <summary>Maximum payload size in bytes.</summary>
    public int    MaxPayloadBytes { get; init; }
}

// ── Log entry ──────────────────────────────────────────────────────────────────

/// <summary>A single event log entry for the audio data link.</summary>
public sealed class SgAudioLinkLogEntry
{
    /// <summary>Timestamp of the log entry.</summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
    /// <summary>Direction: TX, RX, or SYS.</summary>
    public string   Direction { get; init; } = string.Empty;
    /// <summary>Log message text.</summary>
    public string   Message   { get; init; } = string.Empty;
    /// <summary>Severity level: info, warn, error, or success.</summary>
    public string   Level     { get; init; } = "info";
}
