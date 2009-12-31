using System;
using System.Diagnostics.Contracts;
using System.IO;
using MongoDB.Types;

namespace MongoDB.GridFS
{
  public sealed class GridStream : Stream
  {
    private MemoryStream _buffer;
    private readonly FileInfo _fileInfo;
    private readonly Collection _collection;
    private readonly Collection _chunks;
    private Doc _currentChunk;
    private int _totalChunks;
    private int _currentIndex = -1;
    private bool _canWrite;
    private bool _canRead;
    private bool _canSeek;
    private long _position;

    internal GridStream(
      Collection collection, 
      string name, 
      Doc fileInfo, 
      FileAccess access)
    {
      Contract.Requires(fileInfo != null);
      _collection = collection;
      _chunks = _collection.ChunkCollection();
      _fileInfo = new FileInfo(fileInfo, _collection);

      long rem;
      _totalChunks = (int)Math.DivRem(_fileInfo.Length, _fileInfo.ChunkSize, out rem);
      _totalChunks = rem > 0 ? _totalChunks + 1 : _totalChunks;
      _chunks.CreateIndex(new Index { { "files_id", Mongo.Dir.Asc }, { "n", Mongo.Dir.Asc } }, true);
      LoadChunk(0);
      _position = 0L;
      switch (access)
      {
        case FileAccess.Read:
          _canRead = true;
          break;
        case FileAccess.ReadWrite:
          _canRead = true;
          _canWrite = true;
          break;
        case FileAccess.Write:
          _canWrite = true;
          break;
      }
      _canSeek = true;
    }

    private Doc CreateChunk(int n)
    {
      return new Doc { {"_id", ObjectId.Create()},
                       {"files_id", _fileInfo.Id},
                       {"n", n},
                       {"data", new byte[0]} };
    }

    private void LoadChunk(int n)
    {
      Contract.Requires(n > -1);
      Contract.Requires(n <= _totalChunks);
      if (n == _currentIndex) return;

      if (n >= _totalChunks)
      {
        _currentChunk = CreateChunk(n);
        _totalChunks = n + 1;
      }
      else
      {
        _currentChunk = _chunks.FindOne(new Doc {{"files_id", _fileInfo.Id}, {"n", n}});
      }
      
      _currentIndex = n;
      var data = _currentChunk["data"] as byte[];
      if (!IsFull(_currentChunk)) //non-full chunk
      {
        _buffer = new MemoryStream(new byte[_fileInfo.ChunkSize]);
        _buffer.Write(data, 0, data.Length);
      }
      else
      {
        _buffer = new MemoryStream(data);
      }      
    }

    private bool IsFull(Doc chunk)
    {
      var data = chunk["data"] as byte[];
      if (data == null) throw new NullReferenceException("chunk data is null");
      return data.Length == _fileInfo.ChunkSize;
    }

    public override bool CanRead { get { return _canRead; } }

    public override bool CanSeek { get { return _canSeek; } }

    public override bool CanWrite { get { return _canWrite; } }

    public override long Length { get { return _fileInfo.Length; } }

    public override long Position
    {
      get { return _position; }
      set { Seek(value, SeekOrigin.Begin); }
    }

    public override void Flush()
    {
      //_fileInfo.Save();
      if (!IsFull(_currentChunk))
      {
        var b = new byte[_buffer.Position];
        _buffer.Seek(0, SeekOrigin.Begin);
        _buffer.Read(b, 0, b.Length);
        _currentChunk["data"] = b;
      }
      _chunks.Save(_currentChunk);
    }    

    public override int Read(byte[] buffer, int offset, int count)
    {
      var end = Math.Min(_position + offset + count, _fileInfo.Length);

      var nRead = 0;
      var bufOffset = offset;
      var totalRead = 0;

      while (_position < end)
      {
        Seek(_position, SeekOrigin.Begin);
        var nToRead = Math.Min((int)(end - _position), _buffer.Capacity - (int)_buffer.Position);
        nRead = _buffer.Read(buffer, bufOffset, Math.Min(nToRead, count));
        count -= nRead;
        bufOffset += nRead;
        totalRead += nRead;
        _position += nRead;
      }
      return totalRead;
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
      int nextChunk;
      long nextPosition = 0L;
      switch (origin)
      {
        case SeekOrigin.Begin:          
          nextPosition = Math.Min(offset, _fileInfo.Length);          
          break;
        case SeekOrigin.Current:
          nextPosition = Math.Min(_position + offset, _fileInfo.Length);
          break;
        case SeekOrigin.End:
          nextPosition = Math.Min(_fileInfo.Length + offset, _fileInfo.Length);
          break;
      }
      nextChunk = (int)(nextPosition / _fileInfo.ChunkSize);
      if (nextChunk != _currentIndex)
      {
        LoadChunk(nextChunk);
        _currentIndex = nextChunk;
      }
      var boffset = nextPosition % _fileInfo.ChunkSize;
      _buffer.Position = boffset;
      _position = nextPosition;
      return _position;
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
      var nToWrite = 0;
      var bufOffset = offset;

      while (count > 0)
      {
        Seek(_position, SeekOrigin.Begin);
        nToWrite = Math.Min(count, _buffer.Capacity - (int)_buffer.Position);
        _buffer.Write(buffer, bufOffset, nToWrite);
        count -= nToWrite;
        bufOffset += nToWrite;
        _position += nToWrite;
        _fileInfo.Length = Math.Max(_position, _fileInfo.Length);
        Flush();
      }
    }

    public override void SetLength(long value)
    {
      throw new NotImplementedException();
    }

    protected override void Dispose(bool disposing)
    {
      
      _canRead = false;
      _canWrite = false;
      _canSeek = false;
      if (disposing)
      {
        _fileInfo.Save();
        _buffer.Dispose();
      }
      base.Dispose(disposing);
    }
  }
}
