using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using System.IO;
using System.ComponentModel.Composition;
using System.Diagnostics.Contracts;
using System.Threading;
using System.Collections;

namespace MongoDB.Driver
{
  public struct MessageHeader
  {
    public int Length;
    public int Id;
    public int ResponseTo;
    public Operation Operation;
  }

  public enum Operation
  {
    Reply = 1,
    Msg = 1000,
    Update = 2001,
    Insert = 2002,
    GetByOid = 2003,
    Query = 2004,
    GetMore = 2005,
    Delete = 2006,
    KillCursor = 2007
  }

  [Flags]
  public enum QueryOption
  {
    None = 0,
    TailableCursor = 2,
    SlaveOk = 4,
    NoTimeout = 16
  }

  [Flags]
  public enum UpdateOption
  {
    None = 0,
    Upsert = 1,
    MultiUpdate = 2
  }

  public class MessageWriter : BsonWriter
  {
    private static int id = 1;
    private readonly Stream _output;

    private static int NextId()
    {
      return Interlocked.Increment(ref id);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
    public MessageWriter(Stream output)
      : base(new MemoryStream(0x1000)) 
    {
      _output = output;
    }

    public void WriteGetLastError()
    {
      WriteQuery(new Doc { { "getlasterror", 1 } }, "admin.$cmd");
    }

    public void WriteQuery(
      IDictionary<string, object> query,
      string fullName,
      int limit = -1,
      int skip = 0,
      IDictionary<string, object> fields = null,
      QueryOption options = QueryOption.None)
    {
      Write(new MessageHeader { Id = NextId(), Operation = Operation.Query });
      Write((int)options);
      Write(fullName);
      Write(skip);
      Write(limit);
      Write(query);
      if (fields != null)
      {
        Write(fields);
      }
      var len = (int)BaseStream.Position;
      Seek(0, SeekOrigin.Begin);
      Write(len);
      WriteStreamTo(_output);
      Reset();
    }

    public void WriteGetMore(
      long cursorId,
      string fullName,
      int limit = 0)
    {
      Write(new MessageHeader { Id = NextId(), Operation = Operation.GetMore });
      Write(0);
      Write(fullName);
      Write(limit);
      Write(cursorId);
      var len = (int)BaseStream.Position;
      Seek(0, SeekOrigin.Begin);
      Write(len);
      WriteStreamTo(_output);
      Reset();
    }

    public void WriteInsert(
      string fullName,
      IEnumerable<IDictionary<string, object>> docs)
    {
      Write(new MessageHeader { Id = NextId(), Operation = Operation.Insert });
      Write(0);
      Write(fullName);
      foreach (var doc in docs)
      {
        Write(doc);
      }
      var len = (int)BaseStream.Position;
      Seek(0, SeekOrigin.Begin);
      Write(len);
      WriteStreamTo(_output);
      Reset();
    }

    public void WriteUpdate(
      string fullName,
      IDictionary<string, object> selector,
      IDictionary<string, object> doc,
      UpdateOption options = UpdateOption.None)
    {
      Write(new MessageHeader { Id = NextId(), Operation = Operation.Update });
      Write(0);
      Write(fullName);
      Write((int)options);
      Write(selector);
      Write(doc);
      var len = (int)BaseStream.Position;
      Seek(0, SeekOrigin.Begin);
      Write(len);
      WriteStreamTo(_output);
      Reset();
    }

    public void WriteDelete(
      string fullName,
      IDictionary<string, object> selector)
    {
      Write(new MessageHeader { Id = NextId(), Operation = Operation.Delete });
      Write(0);
      Write(fullName);
      Write(0);
      Write(selector);
      var len = (int)BaseStream.Position;
      Seek(0, SeekOrigin.Begin);
      Write(len);
      WriteStreamTo(_output);
      Reset();
    }

    public void WriteKillCursors(
      int count,
      IEnumerable<long> cursors)
    {
      Write(new MessageHeader { Id = NextId(), Operation = Operation.KillCursor });
      Write(0);
      Write(count);
      foreach (var cur in cursors)
      {
        Write(cur);
      }
      var len = (int)BaseStream.Position;
      Seek(0, SeekOrigin.Begin);
      Write(len);
      WriteStreamTo(_output);
      Reset();
    }
  }
}
