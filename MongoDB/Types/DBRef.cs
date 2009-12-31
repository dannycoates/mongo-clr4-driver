namespace MongoDB.Types
{
  public sealed class DBRef
  {
    private readonly string _fullName;
    private readonly ObjectId _id;
    public DBRef(string fullName, ObjectId id)
    {
      _fullName = fullName;
      _id = id;
    }

    public string FullName { get { return _fullName; } }
    public ObjectId Id { get { return _id; } }
  }
}
