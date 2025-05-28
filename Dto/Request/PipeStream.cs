using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

public class PipeStream : Stream
{
    private readonly BlockingCollection<byte[]> _buffers = new BlockingCollection<byte[]>();
    private byte[]? _currentBuffer;
    private int _currentIndex;

    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Flush() { }

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (_currentBuffer == null || _currentIndex >= _currentBuffer.Length)
        {
            if (!_buffers.TryTake(out _currentBuffer, Timeout.Infinite))
                return 0; // No data and stream closed
            _currentIndex = 0;
        }

        int bytesToCopy = Math.Min(count, _currentBuffer.Length - _currentIndex);
        Array.Copy(_currentBuffer, _currentIndex, buffer, offset, bytesToCopy);
        _currentIndex += bytesToCopy;
        return bytesToCopy;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var data = new byte[count];
        Array.Copy(buffer, offset, data, 0, count);
        _buffers.Add(data);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _buffers.CompleteAdding(); // Signal no more data
        }
        base.Dispose(disposing);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();

    public override void SetLength(long value) => throw new NotSupportedException();
}
