using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using MongoDB.Types;

namespace MongoDB.Driver
{
  public class BsonWriter : BinaryWriter
  {
    private static readonly Encoding encoding = new UTF8Encoding();
    private static readonly MD5 md5 = MD5.Create();

    public static string MD5HashString(string input)
    {
      var bytes = MD5Hash(input);
      var sb = new StringBuilder(bytes.Length);
      for (int i = 0; i < bytes.Length; i++)
      {
        sb.Append(bytes[i].ToString("x2"));
      }
      return sb.ToString();
    }

    public static byte[] MD5Hash(string input)
    {
      Contract.Requires(!string.IsNullOrWhiteSpace(input));
      return md5.ComputeHash(encoding.GetBytes(input));
    }

    public BsonWriter(Stream stream)
      : base(stream) { }

    public override void Write(string value)
    {
      Write(value, false);
    }

    public void Write(string value, bool withLength)
    {
      var data = encoding.GetBytes(value);
      if (withLength)
      {
        Write(data.Length + 1); //+1 for terminating null
      }
      else if (data.Length > 1024)
      {
        //NOTE: this is a design decision specific to this library, not MongoDB
        throw new ArgumentOutOfRangeException(
          "value",
          "Strings without length prefix cannot be > 1024 bytes");
      }
      Write(data);
      Write((byte)0);
    }

    public void Write(MessageHeader header)
    {
      Write(header.Length);
      Write(header.Id);
      Write(header.ResponseTo);
      Write((int)header.Operation);
    }

    public void Write(IDictionary<string, object> doc)
    {
      var start = (int)BaseStream.Position;
      base.Write(0); // spot for length
      foreach (var item in doc)
      {
        Write(Bson.TypeOf(item.Value), item.Key, item.Value);
      }
      Write((sbyte)BsonType.EOO);
      var len = (int)(BaseStream.Position - start);
      Seek(start, SeekOrigin.Begin);
      Write(len);
      Seek(start + len, SeekOrigin.Begin);
    }

    private void Write(BsonType t, string name, object o)
    {
      Write((sbyte)t);
      Write(name);
      switch (t)
      {
        case BsonType.NUMBER:
          Write((double)o);
          break;
        case BsonType.STRING:
          Write(o as string, true);
          break;
        case BsonType.OBJECT:
          Write(o as IDictionary<string, object>);
          break;
        case BsonType.ARRAY:
          WriteList(o as IList);
          break;
        case BsonType.BINARY:
          var buf = o as byte[];
          Write(buf.Length);
          Write((sbyte)0x02);
          Write(buf);
          break;
        case BsonType.OID:
          Write(o as ObjectId);
          break;
        case BsonType.BOOLEAN:
          Write((bool)o);
          break;
        case BsonType.DATE:
          Write(((DateTime)o).ToBson());
          break;
        case BsonType.NULL:
          break;
        case BsonType.REGEX:
          Write(o as Regex);
          break;
        case BsonType.REF:
          Write(o as DBRef);
          break;
        case BsonType.CODE:
          Write(o.ToString(), true);
          break;
        case BsonType.SYMBOL:
          Write(o.ToString(), true);
          break;
        case BsonType.CODE_W_SCOPE:
          var sc = o as ScopedCode;
          var pos = (int)BaseStream.Position;
          Write(0);
          Write(sc.Code, true);
          Write(sc.Scope);
          var end = (int)BaseStream.Position;
          Seek(pos, SeekOrigin.Begin);
          Write(end - pos);
          Seek(end, SeekOrigin.Begin);
          break;
        case BsonType.NUMBER_INT:
          Write((int)o);
          break;
        case BsonType.TIMESTAMP:
          Write(((TimeStamp)o).Value);
          break;
        case BsonType.NUMBER_LONG:
          Write((long)o);
          break;
        default:
          break;
      }
    }

    private void Write(DBRef dbref)
    {
      Write(dbref.FullName, true);
      Write(dbref.Id);
    }

    private void Write(Regex regex)
    {
      Write(regex.ToString());
      var options = regex.Options;
      var opt = "";
      if (options.HasFlag(RegexOptions.IgnoreCase))
      {
        opt += "i";
      }
      if (options.HasFlag(RegexOptions.Multiline))
      {
        opt += "m";
      }
      //TODO: wrap Regex with new class to preserve option "x"
      Write(opt);
    }

    public void Write(ObjectId id)
    {
      Write(id.GetBytes());
    }

    public void WriteList(IList list)
    {
      var i = 0;
      var start = (int)BaseStream.Position;
      base.Write(0); // spot for length
      foreach (var item in list)
      {
        Write(Bson.TypeOf(item), i.ToString(), item);
        i++;
      }
      Write((sbyte)BsonType.EOO);
      var len = (int)(BaseStream.Position - start);
      Seek(start, SeekOrigin.Begin);
      Write(len);
      Seek(start + len, SeekOrigin.Begin);
    }

    internal void WriteStreamTo(Stream output)
    {
      Contract.Requires(BaseStream is MemoryStream);
      (BaseStream as MemoryStream).WriteTo(output);
    }

    public void Reset()
    {
      BaseStream.SetLength(0);
    }
  }
}
