using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MongoDB.Types
{
  public struct TimeStamp
  {
    public long Value;
    public TimeStamp(long value)
    {
      Value = value;
    }
  }
}
