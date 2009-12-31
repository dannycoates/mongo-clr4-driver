using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Types
{
  public sealed class ScopedCode
  {
    private readonly string _code;
    private readonly IDictionary<string, object> _scope;
    public ScopedCode(string code, IDictionary<string, object> scope)
    {
      _code = code;
      _scope = scope;
    }

    public string Code { get { return _code; } }
    public IDictionary<string, object> Scope { get { return _scope; } }
  }
}
