namespace MongoDB.Types
{
  public sealed class Code
  {
    private readonly string _code;
    public Code(string code)
    {
      _code = code;
    }

    public override string ToString()
    {
      return _code;
    }
  }
}
