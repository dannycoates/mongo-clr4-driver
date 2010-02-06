using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MongoDB.Driver;
using System.IO;
using MongoDB.Types;
using MongoDB;
using System.Collections;

namespace TestMongoDB
{
  [TestClass]
  public class ReaderWriterTest
  {
    private BsonReader reader;
    private BsonWriter writer;
    private MemoryStream stream;

    [TestInitialize()]
    public void MyTestInitialize()
    {
      stream = new MemoryStream();
      writer = new BsonWriter(stream);
      reader = new BsonReader(stream);
    }
    
    [TestCleanup()]
    public void MyTestCleanup()
    {
      stream.Dispose();
    }

    [TestMethod]
    public void TestShortString()
    {
      var s = "short string test";
      writer.Write(s);
      stream.Seek(0, SeekOrigin.Begin);
      var x = reader.ReadShortString();
      Assert.AreEqual(s, x);
    }

    [TestMethod]
    public void TestString()
    {
      var s = "test string";
      writer.Write(s, true);
      stream.Seek(0, SeekOrigin.Begin);
      var len = reader.ReadInt32();
      var x = reader.ReadString(len);
      Assert.AreEqual(s, x);
    }

    [TestMethod]
    public void WriteTooLongShortString()
    {
      var sb = new StringBuilder();
      for (int i = 0; i < 1025; i++)
      {
        sb.Append('A');
      }
      try
      {
        writer.Write(sb.ToString());
        Assert.Fail();
      }
      catch (ArgumentOutOfRangeException e)
      {
        Assert.AreEqual(
          "Strings without length prefix cannot be > 1024 bytes\r\nParameter name: value", 
          e.Message);
      }
    }

    [TestMethod]
    public void TestMessageHeader()
    {
      var h = new MessageHeader { 
        Id = 1, 
        Length = 12, 
        Operation = Operation.Query, 
        ResponseTo = 10 
      };

      writer.Write(h);
      stream.Seek(0, SeekOrigin.Begin);

      var x = reader.ReadMessageHeader();
      Assert.AreEqual(h, x);
    }

    [TestMethod]
    public void TestObjectId()
    {
      var o = ObjectId.Create();
      writer.Write(o);
      stream.Seek(0, SeekOrigin.Begin);
      var x = reader.ReadObjectId();
      Assert.AreEqual(o, x);
    }

    [TestMethod]
    public void TestDoc()
    {
      var d = new Doc { 
        {"_id", ObjectId.Create()},
        {"string", "a string"},
        {"double", 12.34},
        {"int", 42},
        {"long", 1L},
        {"null", null},
        {"bool", true},
        {"binary", new byte[] {0xDE, 0xAD, 0xBE, 0xEF}},
        {"date", new DateTime(2010, 1, 1)},
        {"obj", new Doc { {"i", 1}}},
        {"list", new List<int> {1,2,3,4,5}},
        {"dbref", "deprecated"},
        {"timestamp", new TimeStamp(4L)},
        {"symbol", new Symbol("symbol")},
        {"regex", "todo"},
        {"code", new Code("code")},
        {"scode", new ScopedCode("scode", new Doc())}
      };
      writer.Write(d);
      stream.Seek(0, SeekOrigin.Begin);
      var x = reader.ReadDoc();
      Assert.IsInstanceOfType(x["_id"], typeof(ObjectId));
      Assert.AreEqual("a string", x["string"]);
      Assert.AreEqual(12.34, x["double"]);
      Assert.AreEqual(42, x["int"]);
      Assert.AreEqual(1L, x["long"]);
      Assert.IsNull(x["null"]);
      Assert.AreEqual(true, x["bool"]);
      Assert.IsInstanceOfType(x["binary"], typeof(byte[]));
      Assert.AreEqual(new DateTime(2010, 1, 1).ToUniversalTime(), x["date"]);
      Assert.IsInstanceOfType(x["obj"], typeof(Doc));
      Assert.IsInstanceOfType(x["list"], typeof(IList));
    }

  }
}
