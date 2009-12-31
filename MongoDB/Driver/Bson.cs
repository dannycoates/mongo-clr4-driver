using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using MongoDB.Types;
using System.Text.RegularExpressions;

namespace MongoDB.Driver
{
  public enum BsonType : sbyte
  {
    EOO = 0,
    NUMBER = 1,
    STRING = 2,
    OBJECT = 3,
    ARRAY = 4,
    BINARY = 5,
    UNDEFINED = 6,
    OID = 7,
    BOOLEAN = 8,
    DATE = 9,
    NULL = 10,
    REGEX = 11,
    REF = 12,
    CODE = 13,
    SYMBOL = 14,
    CODE_W_SCOPE = 15,
    NUMBER_INT = 16,
    TIMESTAMP = 17,
    NUMBER_LONG = 18,

    MINKEY = -1,
    MAXKEY = 127
  }
  public static class Bson
  {
    public static readonly DateTime Epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static long ToBson(this DateTime time)
    {
      return Convert.ToInt64(((time.ToUniversalTime() - Bson.Epoch).TotalMilliseconds));
    }

    public static DateTime BsonToDate(this long time)
    {
      return Epoch + TimeSpan.FromMilliseconds(Convert.ToDouble(time));
    }

    public static Doc ToDoc(this IEnumerable<string> fields)
    {
      var d = new Doc();
      if (fields == null) return d;
      foreach (var item in fields)
      {
        d[item] = 1;
      }
      return d;
    }

    public static BsonType TypeOf(object o)
    {
      if (o == null)
      {
        return BsonType.NULL;
      }
      else if (o is string)
      {
        return BsonType.STRING;
      }
      else if (o is bool)
      {
        return BsonType.BOOLEAN;
      }
      else if (o is byte[])
      {
        return BsonType.BINARY;
      }
      else if (o is Int32)
      {
        return BsonType.NUMBER_INT;
      }
      else if (o is double)
      {
        return BsonType.NUMBER;
      }
      else if (o is long)
      {
        return BsonType.NUMBER_LONG;
      }
      else if (o is DateTime)
      {
        return BsonType.DATE;
      }
      else if (o is IDictionary<string, object>)
      {
        return BsonType.OBJECT;
      }
      else if (o is IList)
      {
        return BsonType.ARRAY;
      }
      else if (o is ObjectId)
      {
        return BsonType.OID;
      }
      else if (o is DBRef)
      {
        return BsonType.REF;
      }
      else if (o is TimeStamp)
      {
        return BsonType.TIMESTAMP;
      }
      else if (o is Symbol)
      {
        return BsonType.SYMBOL;
      }
      else if (o is Regex)
      {
        return BsonType.REGEX;
      }
      else if (o is ScopedCode)
      {
        return BsonType.CODE_W_SCOPE;
      }
      else if (o is Code)
      {
        return BsonType.CODE;
      }
      throw new MongoTypeException("No BSON type for " + o.GetType().FullName, o.GetType());
    }
  }
}
