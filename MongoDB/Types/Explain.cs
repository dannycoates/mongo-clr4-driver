using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Types
{
  public sealed class Explain
  {
    private readonly Doc _doc;
    internal Explain(Doc doc)
    {
      _doc = doc;
    }

    public Doc Doc { get { return _doc; } }
    public int Scanned { get { return Convert.ToInt32(_doc["nscanned"]); } }
    public int Returned { get { return Convert.ToInt32(_doc["n"]); } }
    public int Milliseconds { get { return Convert.ToInt32(_doc["millis"]); } }
    public string Cursor { get { return _doc["cursor"] as string; } }
  }
}
