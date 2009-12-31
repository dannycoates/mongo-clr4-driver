using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;

namespace MongoDB.Driver
{
  public sealed class ReplyMessage : IEnumerable<Doc>
  {
    private readonly MessageHeader _header;
    private readonly int _responseFlag;
    private readonly long _cursorId;
    private readonly int _startingFrom;
    private readonly int _numberReturned;
    private readonly Doc[] _docs;

    public ReplyMessage(
      MessageHeader header,
      int responseFlag,
      long cursorId,
      int startingFrom,
      int numberReturned,
      Doc[] docs)
    {
      _header = header;
      _responseFlag = responseFlag;
      _cursorId = cursorId;
      _startingFrom = startingFrom;
      _numberReturned = numberReturned;
      _docs = docs;
    }

    public bool Ok { get { return _responseFlag == 0; } }

    public bool HasMore { get { return _cursorId != 0 && _numberReturned > 0; } }

    public long CursorId { get { return _cursorId; } }

    public int NReturned { get { return _numberReturned; } }

    public IEnumerator<Doc> GetEnumerator()
    {
      return _docs.AsEnumerable().GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return _docs.GetEnumerator();
    }
  }
}
