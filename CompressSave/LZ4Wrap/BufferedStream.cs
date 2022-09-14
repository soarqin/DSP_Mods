namespace CompressSave.LZ4Wrap;

//public class BufferedFileStream : FileStream
//{
//    public override bool CanTimeout => base.CanTimeout;

//    public override int ReadTimeout { get => base.ReadTimeout; set => base.ReadTimeout = value; }
//    public override int WriteTimeout { get => base.WriteTimeout; set => base.WriteTimeout = value; }

//    public override long Position { get => base.Position; set => base.Position = value; }

//    public override SafeFileHandle SafeFileHandle => base.SafeFileHandle;

//    public override IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
//    {
//        return base.BeginRead(array, offset, numBytes, userCallback, stateObject);
//    }

//    public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback userCallback, object stateObject)
//    {
//        return base.BeginWrite(array, offset, numBytes, userCallback, stateObject);
//    }

//    public override void Close()
//    {
//        var bs = new BufferedStream(this);
            

//        base.Close();
//    }

//    public override bool Equals(object obj)
//    {
//        return base.Equals(obj);
//    }

//    public override void Flush()
//    {
//        base.Flush();
//    }

//    public override void Flush(bool flushToDisk)
//    {
//        base.Flush(flushToDisk);
//    }

//    public override Task FlushAsync(CancellationToken cancellationToken)
//    {
//        return base.FlushAsync(cancellationToken);
//    }

//    public override int GetHashCode()
//    {
//        return base.GetHashCode();
//    }

//    public override object InitializeLifetimeService()
//    {
//        return base.InitializeLifetimeService();
//    }

//    public override void Lock(long position, long length)
//    {
//        base.Lock(position, length);
//    }

//    public override int Read(byte[] array, int offset, int count)
//    {
//        return base.Read(array, offset, count);
//    }

//    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
//    {
//        return base.ReadAsync(buffer, offset, count, cancellationToken);
//    }

//    public override long Seek(long offset, SeekOrigin origin)
//    {
//        return base.Seek(offset, origin);
//    }

//    public override void SetLength(long value)
//    {
//        base.SetLength(value);
//    }

//    public override string ToString()
//    {
//        return base.ToString();
//    }

//    public override void Unlock(long position, long length)
//    {
//        base.Unlock(position, length);
//    }

//    public override void Write(byte[] array, int offset, int count)
//    {
//        base.Write(array, offset, count);
//    }

//    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
//    {
//        return base.WriteAsync(buffer, offset, count, cancellationToken);
//    }

//    public override void WriteByte(byte value)
//    {
//        base.WriteByte(value);
//    }

//    protected override void Dispose(bool disposing)
//    {
//        base.Dispose(disposing);
//    }

//}