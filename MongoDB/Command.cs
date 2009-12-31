using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB
{
  public sealed class Command : Doc
  {
    private readonly KeyValuePair<string, object> _command;

    public Command(string command, object param)
    {
      _command = new KeyValuePair<string, object>(command, param);
    }

    public override IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
      yield return _command;
      foreach (var item in _properties)
      {
        yield return item;
      }
    }
  }
}
