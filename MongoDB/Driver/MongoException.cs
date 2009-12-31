using System;
using System.Runtime.Serialization;

namespace MongoDB.Driver
{
  [Serializable]
  public class MongoException : Exception
  {
    public MongoException(string message, Exception innerException)
      : base(message, innerException)
    { }
    public MongoException(string message)
      : base(message)
    { }
  }

  [Serializable]
  public sealed class MongoTypeException : MongoException
  {
    public MongoTypeException(string message, Type type)
      : base(message)
    {
      BadType = type;
    }

    public Type BadType { get; private set; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("BadType", BadType);
      base.GetObjectData(info, context);
    }
  }

  [Serializable]
  public sealed class MongoOperationException : MongoException
  {
    public MongoOperationException(string message, Doc serverReply)
      : base(message)
    {
      Details = serverReply;
    }

    public Doc Details { get; private set; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Details", Details);
      base.GetObjectData(info, context);
    }
  }

  [Serializable]
  public sealed class MongoSecurityException : MongoException
  {
    public MongoSecurityException(string message, string username, string db)
      : base(message)
    {
      Username = username;
      Database = db;
    }

    public string Username { get; private set; }
    public string Database { get; private set; }

    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
      info.AddValue("Username", Username);
      info.AddValue("Database", Database);
      base.GetObjectData(info, context);
    }
  }
}
