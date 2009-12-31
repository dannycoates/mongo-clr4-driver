using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MongoDB
{
  [Serializable]
  public class Index : Dictionary<string, Mongo.Dir>
  {
    protected Index(SerializationInfo info, StreamingContext context)
      :base(info, context) { }

    public Index() { }

    public Doc ToDoc()
    {
      var d = new Doc();
      foreach (var item in this)
      {
        d[item.Key] = (int)item.Value;
      }
      return d;
    }
  }
}
