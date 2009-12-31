using System;
using System.Collections.Generic;

namespace MongoDB.Types
{
  public class MapReduce
  {
    private readonly Code _map;
    private readonly Code _reduce;

    public MapReduce(string map, string reduce)
    {
      _map = new Code(map);
      _reduce = new Code(reduce);
    }

    public IDictionary<string,object> Query { get; set; }
    public Index Sort { get; set; }
    public int Limit { get; set; }
    public string OutputCollection { get; set; }
    public bool KeepTemp { get; set; }
    public Code Finalize { get; set; }
    public bool Verbose { get; set; }

    internal Doc ToDoc(string collection)
    {
      throw new NotImplementedException();
    }
  }
}
