using System.Text;

namespace AdPlay.Api.Middleware;

// Passes every write straight to the real response stream and keeps a copy of
// only the first N bytes. Big responses are not held in memory.
internal sealed class CaptureStream : Stream
{
    private readonly Stream _inner;
    private readonly int _limit;
    private readonly MemoryStream _copy = new();
    private long _total;

    public CaptureStream(Stream inner, int limit)
    {
        _inner = inner;
        _limit = limit;
    }

    public bool Truncated => _total > _limit;

    public string GetText() => Encoding.UTF8.GetString(_copy.GetBuffer(), 0, (int)_copy.Length);

    private void Capture(ReadOnlySpan<byte> data)
    {
        _total += data.Length;
        var room = _limit - (int)_copy.Length;
        if (room > 0)
            _copy.Write(data[..Math.Min(room, data.Length)]);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        Capture(buffer.AsSpan(offset, count));
        _inner.Write(buffer, offset, count);
    }

    public override void Write(ReadOnlySpan<byte> buffer)
    {
        Capture(buffer);
        _inner.Write(buffer);
    }

    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default)
    {
        Capture(buffer.Span);
        await _inner.WriteAsync(buffer, ct);
    }

    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) =>
        WriteAsync(buffer.AsMemory(offset, count), ct).AsTask();

    public override void Flush() => _inner.Flush();
    public override Task FlushAsync(CancellationToken ct) => _inner.FlushAsync(ct);

    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
