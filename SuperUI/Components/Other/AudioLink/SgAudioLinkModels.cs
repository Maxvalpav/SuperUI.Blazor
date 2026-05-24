namespace SuperUI.Components;

// ── Enums ──────────────────────────────────────────────────────────────────────

public enum SgAudioLinkState
{
    Idle,
    Transmitting,
    Listening,
    PreambleDetected,
    Receiving,
    Error
}

// ── Event args ─────────────────────────────────────────────────────────────────

public sealed class SgAudioLinkDataReceivedEventArgs
{
    public string Text       { get; init; } = string.Empty;
    public int    ByteCount  { get; init; }
    public int    Crc        { get; init; }
    public DateTime ReceivedAt { get; init; } = DateTime.Now;
}

public sealed class SgAudioLinkTxCompleteEventArgs
{
    public string Text        { get; init; } = string.Empty;
    public int    SymbolCount { get; init; }
    public DateTime SentAt    { get; init; } = DateTime.Now;
}

public sealed class SgAudioLinkErrorEventArgs
{
    public string Message   { get; init; } = string.Empty;
    public string ErrorType { get; init; } = string.Empty;
}

// ── Protocol info (from JS) ────────────────────────────────────────────────────

public sealed class SgAudioLinkProtocolInfo
{
    public int    BaseFreq        { get; init; }
    public int    FreqStep        { get; init; }
    public int    NumCarriers     { get; init; }
    public int    SymbolMs        { get; init; }
    public int    GuardMs         { get; init; }
    public int    SyncA           { get; init; }
    public int    SyncB           { get; init; }
    public int    StartTone       { get; init; }
    public int    FftSize         { get; init; }
    public int    SampleRate      { get; init; }
    public int    BitsPerSymbol   { get; init; }
    public int    MaxPayloadBytes { get; init; }
}

// ── Log entry ──────────────────────────────────────────────────────────────────

public sealed class SgAudioLinkLogEntry
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string   Direction { get; init; } = string.Empty; // TX / RX / SYS
    public string   Message   { get; init; } = string.Empty;
    public string   Level     { get; init; } = "info";       // info / warn / error / success
}
