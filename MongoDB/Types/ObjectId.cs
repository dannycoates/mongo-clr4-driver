using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using MongoDB.Driver;

namespace MongoDB.Types
{
  public sealed class ObjectId
  {
    private static readonly byte[] procid = BitConverter.GetBytes(Process.GetCurrentProcess().Id);
    private static readonly byte[] machineid = BsonWriter.MD5Hash(Dns.GetHostName());
    private static readonly Regex oidString = new Regex(@"[A-Fa-f0-9]{24}", RegexOptions.Compiled);
    private static int counter;    

    private readonly byte[] _bytes;

    public static ObjectId Create()
    {
      var c = Interlocked.Increment(ref counter);
      var bytes = new byte[12];
      Array.Copy(
        BitConverter.GetBytes(
          Convert.ToInt32(
          DateTime.Now.ToUniversalTime().TimeOfDay.TotalMilliseconds)), 0, bytes, 0, 4);
      Array.Copy(machineid, 0, bytes, 4, 3);
      Array.Copy(procid, 0, bytes, 7, 2);
      Array.Copy(BitConverter.GetBytes(c), 0, bytes, 9, 3);
      return new ObjectId(bytes);
    }

    public static bool TryParse(string value, out ObjectId oid)
    {
      oid = null;
      if (!IsOidString(value)) return false;
      try
      {
        oid = FromString(value);
      }
      catch
      {
        return false;
      }      
      return true;
    }

    public static bool IsOidString(string hex)
    {
      return hex != null && oidString.IsMatch(hex);
    }

    private static ObjectId FromString(string hex)
    {
      Contract.Requires(hex != null);
      Contract.Requires(oidString.IsMatch(hex));
      var bytes = new byte[12];
      for (int i = 0; i < 24; i += 2)
      {
        bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
      }
      return new ObjectId(bytes); 
    }

    public ObjectId(byte[] bytes)
    {
      Contract.Requires(bytes.Length == 12);
      _bytes = bytes;
    }

    public override string ToString()
    {
      var sb = new StringBuilder(_bytes.Length);
      for (int i = 0; i < _bytes.Length; i++)
      {
        sb.Append(_bytes[i].ToString("x2"));
      }
      return sb.ToString();
    }

    public byte[] GetBytes() { return _bytes; }
  }
}
