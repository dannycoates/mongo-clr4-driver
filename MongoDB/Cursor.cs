using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Types;

namespace MongoDB
{
  public sealed class Cursor : IEnumerable<Doc>, IDisposable
  {
    private readonly IDictionary<string, object> _query;
    private readonly IEnumerable<string> _fields;
    private readonly Collection _collection;
    private IEnumerable<string> _hint;
    private Index _sort;
    private bool _explain = false;
    private int _limit;
    private int _skip;
    private bool _snapshot = false; //TODO: expose
    private long _cursorId;
    
    internal Cursor(
      IDictionary<string, object> query,
      IEnumerable<string> fields,
      Collection collection)
    {
      _query = query;
      _fields = fields;
      _collection = collection;
      SlaveOk = true;
    }

    public bool SlaveOk { get; set; }

    public void Close()
    {
      if (_cursorId == 0L) return;
      _collection.Database.Connection.Say(msg => msg.WriteKillCursors(1, new[] { _cursorId }));
      _cursorId = 0L;
    }

    public static void Close(IEnumerable<Cursor> cursors)
    {
      Contract.Requires(cursors != null);
      var ids =
        cursors
        .Where(x => x._cursorId != 0L)
        .Select(x => x._cursorId);
      var count = ids.Count();
      if (count > 0)
      {
        cursors.First()._collection.Database.Connection.Say(msg => msg.WriteKillCursors(count, ids));
      }
    }

    public int Count(bool withLimitAndSkip = false)
    {
      var command = new Command("count", _collection.Name) { 
                              {"query", _query},
                              {"fields", _fields.ToDoc()}};
      if (withLimitAndSkip)
      {
        command["limit"] = _limit;
        command["skip"] = _skip;
      }
      var reply = _collection.Database.ExecuteCommand(command);
      return Convert.ToInt32((double)reply["n"]);
    }

    public Cursor Limit(int n)
    {
      Contract.Requires(n > -1);
      _limit = n;
      return this;
    }

    public Cursor Skip(int n)
    {
      Contract.Requires(n > -1);
      _skip = n;
      return this;
    }

    public Cursor Sort(Index index)
    {
      _sort = index;
      return this;
    }

    public Cursor Hint(IEnumerable<string> keys)
    {
      _hint = keys;
      return this;
    }

    public Cursor Where(string code)
    {
      Contract.Requires(!string.IsNullOrWhiteSpace(code));
      _query["$where"] = new Code(code);
      return this;
    }

    [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
    public Explain Explain()
    {
      var c = new Cursor(_query, _fields, _collection) { _explain = true, _limit = 1 };
      return new Explain(c.First());
    }

    private IDictionary<string, object> FullQuery()
    {
      if (_hint == null && _sort == null && !_explain)
      {
        return _query;
      }
      var q = new Doc { { "query", _query } };
      if (_sort != null)
      {
        q["orderby"] = _sort.ToDoc();
      }
      if (_explain)
      {
        q["$explain"] = _explain;
      }
      if (_hint != null)
      {
        q["$hint"] = _hint.ToDoc();
      }
      if (_snapshot)
      {
        q["$snapshot"] = _snapshot;
      }
      return q;
    }

    public IEnumerator<Doc> GetEnumerator()
    {
      var reply = _collection.Database.Connection.Call(msg => msg.WriteQuery(FullQuery(), _collection.FullName, _limit, _skip, _fields.ToDoc()));
      if (!reply.Ok) throw new MongoOperationException("Error querying Mongo", reply.FirstOrDefault());
      _cursorId = reply.CursorId;
      foreach (var doc in reply)
      {
        yield return doc;
      }
      var seen = reply.NReturned;
      while (reply.HasMore)
      {
        var count = (_limit < 1) ? -1 : Math.Max(_limit - seen, 0);
        if (count == 0) break;
        reply = _collection.Database.Connection.Call(msg => msg.WriteGetMore(reply.CursorId, _collection.FullName, count));
        if (!reply.Ok) throw new MongoOperationException("Error retrieving more documents", reply.FirstOrDefault());
        foreach (var doc in reply)
        {
          yield return doc;
        }
        seen += reply.NReturned;
      }
      Close();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public void Dispose()
    {
      Close();
    }
  }
}
