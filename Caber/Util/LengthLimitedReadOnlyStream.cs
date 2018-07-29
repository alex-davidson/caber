using System;
using System.IO;

namespace Caber.Util
{
    /// <summary>
    /// Wraps a readable stream, allowing only the first `length` bytes to be read.
    /// </summary>
    /// <remarks>
    /// If 'takeOwnership' is false, the underlying stream will not be disposed by this one. If
    /// true or omitted, it will be disposed when this one is.
    /// </remarks>
    public class LengthLimitedReadOnlyStream  : Stream
    {
        private Stream stream;
        private readonly long length;
        private readonly bool takeOwnership;

        public LengthLimitedReadOnlyStream(Stream stream, long length, bool takeOwnership = true)
        {
            if (!stream.CanRead) throw new ArgumentException("Stream is not readable.", nameof(stream));
            if (length < 0) throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
            if (stream.CanSeek)
            {
                if (length > stream.Length) throw new ArgumentOutOfRangeException(nameof(length), "Stream is shorter then the specified length.");
            }

            this.stream = stream;
            this.length = length;
            this.takeOwnership = takeOwnership;
        }

        public override bool CanRead => stream.CanRead;
        public override bool CanSeek => stream.CanSeek;
        public override bool CanWrite => false;
        public override bool CanTimeout => stream.CanTimeout;

        public override long Length => length;

        public override long Position
        {
            get => stream.Position;
            set => stream.Position = Math.Min(value, length);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    var clampedBeginOffset = Math.Max(Math.Min(length, offset), 0);
                    stream.Seek(clampedBeginOffset, origin);
                    break;
                case SeekOrigin.End:
                    var clampedEndOffset = Math.Max(Math.Min(length, offset), 0);
                    stream.Seek(length - clampedEndOffset, origin);
                    break;
                default:
                    var max = length - Position;
                    var min = -Position;
                    var clampedRelativeOffset = Math.Max(Math.Min(max, offset), min);
                    stream.Seek(clampedRelativeOffset, origin);
                    break;
            }
            return Position;
        }

        public override void Flush()
        {
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            var bytesAvailable = Length - Position;
            if (bytesAvailable <= 0) return 0;
            if (bytesAvailable < count)
            {
                return stream.Read(buffer, offset, Math.Min((int)bytesAvailable, count));
            }
            return stream.Read(buffer, offset, count);
        }

        public override IAsyncResult BeginRead(byte[] buffer, int offset, int count, AsyncCallback callback, object state)
        {
            var bytesAvailable = Length - Position;
            if (bytesAvailable < count)
            {
                return stream.BeginRead(buffer, offset, Math.Min((int)bytesAvailable, count), callback, state);
            }
            return stream.BeginRead(buffer, offset, count, callback, state);
        }

        public override int EndRead(IAsyncResult asyncResult) => stream.EndRead(asyncResult);

        protected sealed override void Dispose(bool disposing)
        {
            try
            {
                if (!disposing) return;
                if (takeOwnership) stream?.Dispose();
                stream = null;
            }
            finally
            {
                base.Dispose(disposing);
            }
        }

        public override int ReadTimeout
        {
            get => stream.ReadTimeout;
            set => throw new NotSupportedException("Cannot adjust ReadTimeout because the stream is read-only.");
        }

        public override int WriteTimeout
        {
            get => stream.ReadTimeout;
            set => throw new NotSupportedException("Cannot adjust WriteTimeout because the stream is read-only.");
        }

        public override void SetLength(long value) => throw StreamIsReadOnly();

        public override void Write(byte[] buffer, int offset, int count) => throw StreamIsReadOnly();
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback callback, object state) => throw StreamIsReadOnly();
        public override void EndWrite(IAsyncResult asyncResult) => throw StreamIsReadOnly();

        private static NotSupportedException StreamIsReadOnly() => new NotSupportedException("Stream is read-only.");
    }
}
