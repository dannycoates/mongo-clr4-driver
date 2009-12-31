using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using MongoDB.Types;
using System.Text.RegularExpressions;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;

namespace MongoDB.Driver
{
  public class BsonReader : BinaryReader
  {
    [SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
    public static readonly Encoding encoding = new UTF8Encoding();
    private readonly byte[] _stringBuffer = new byte[1024];
    private readonly byte[] _byteBuffer = new byte[1];

    public BsonReader(Stream stream)
      :base(stream)
    {
    }

    public override byte ReadByte()
    {
      Read(_byteBuffer, 0, 1);
      return _byteBuffer[0];
    }

    /// <summary>
    /// Reads a string of unknown length but less than 1024 bytes
    /// </summary>
    /// <remarks>
    /// In the BSON spec there are two cases where the string length
    /// is not given: Element names of objects and regex strings.
    /// Since element names are very common, and are most likely
    /// short this method WILL FAIL unappologetically for strings
    /// longer than 1024 bytes. You have been warned!
    /// </remarks>
    /// <returns>a new string</returns>
    public string ReadShortString()
    {
      int i = -1;
      do
      {
        Read(_byteBuffer, 0, 1);
        _stringBuffer[++i] = _byteBuffer[0];
      }
      while (_stringBuffer[i] != 0);
      return encoding.GetString(_stringBuffer, 0, i);
    }

    public string ReadString(int length)
    {
      Contract.Requires(length > 0);
      byte[] buf;
      var strlen = length - 1;
      if (length < _stringBuffer.Length)
      {
        Read(_stringBuffer, 0, strlen);
        buf = _stringBuffer;
      }
      else
      {
        buf = ReadBytes(strlen);
      }      
      Read(_byteBuffer, 0, 1); // trailing NULL
      return encoding.GetString(buf, 0, strlen);
    }

    public MessageHeader ReadMessageHeader()
    {
      return new MessageHeader
      {
        Length = ReadInt32(),
        Id = ReadInt32(),
        ResponseTo = ReadInt32(),
        Operation = (Operation)ReadInt32()
      };
    }

    public ReplyMessage ReadReplyMessage()
    {
      var header = ReadMessageHeader();
      var flags = ReadInt32();
      var cursorId = ReadInt64();
      var startIndex = ReadInt32();
      var numberReturned = ReadInt32();
      var docs = new Doc[numberReturned];
      for (int i = 0; i < numberReturned; i++)
      {
        docs[i] = ReadDoc();
      }
      return new ReplyMessage(header, flags, cursorId, startIndex, numberReturned, docs);
    }

    public DateTime ReadDate()
    {
      return ReadInt64().BsonToDate();
    }

    public ObjectId ReadObjectId()
    {
      return new ObjectId(ReadBytes(12));
    }

    public DBRef ReadDBRef()
    {
      return new DBRef(ReadString(ReadInt32()), ReadObjectId());
    }

    public Doc ReadDoc()
    {
      var doc = new Doc();
      ReadInt32(); //size (ignored)
      while(true)
      {
        var type = (BsonType)ReadByte();
        if (type == BsonType.EOO)
        {
          return doc;
        }
        var name = ReadShortString();
        object o = null;
        switch (type)
        {
          case BsonType.NUMBER:
            o = ReadDouble();
            break;
          case BsonType.STRING:
            o = ReadString(ReadInt32());
            break;
          case BsonType.OBJECT:
            o = ReadDoc();
            break;
          case BsonType.ARRAY:
            o = ReadDoc().ToList();
            break;
          case BsonType.BINARY:
            var count = ReadInt32();
            ReadByte(); //TODO: handle 'subtype'
            o = ReadBytes(count);
            break;
          case BsonType.UNDEFINED:
            break;
          case BsonType.OID:
            o = ReadObjectId();
            break;
          case BsonType.BOOLEAN:
            o = ReadBoolean();
            break;
          case BsonType.DATE:
            o = ReadDate();
            break;
          case BsonType.NULL:
            break;
          case BsonType.REGEX:
            o = new Regex(ReadShortString());
            ReadShortString(); //TODO: options
            break;
          case BsonType.REF:
            o = ReadDBRef();
            break;
          case BsonType.CODE:
            o = new Code(ReadString(ReadInt32()));
            break;
          case BsonType.SYMBOL:
            o = new Symbol(ReadString(ReadInt32()));
            break;
          case BsonType.CODE_W_SCOPE:
            ReadInt32();
            o = new ScopedCode(ReadString(ReadInt32()), ReadDoc());
            break;
          case BsonType.NUMBER_INT:
            o = ReadInt32();
            break;
          case BsonType.TIMESTAMP:
            o = new TimeStamp(ReadInt64());
            break;
          case BsonType.NUMBER_LONG:
            o = ReadInt64();
            break;
          case BsonType.MINKEY:
            break;
          case BsonType.MAXKEY:
            break;
          default:
            break;
        }
        doc.Add(name, o);
      }
    }
  }
}
