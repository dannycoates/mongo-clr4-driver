using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace MongoDB.Driver
{
  internal class Connection : IDisposable
  {
    private readonly ConcurrentQueue<Port> _ports = new ConcurrentQueue<Port>();
    private readonly int _min;
    private readonly int _max;
    private readonly TimeSpan _portTimeout = TimeSpan.FromSeconds(10.0);
    
    private string _username;
    private string _pwhash;
    private string _dbname;
    private bool _authenticate = false;

    private int _ncreated;
    private string _host;
    private int _port;

    internal class Port : IDisposable
    {
      private readonly BsonReader _reader;
      internal readonly MessageWriter Writer;
      internal bool Authorized { get; private set; }
      private Port(NetworkStream stream)
      {
        _reader = new BsonReader(stream);
        Writer = new MessageWriter(stream);
      }

      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      internal Port(string host, int port) 
        : this(CreateStream(host, port)) { }

      [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
      private static NetworkStream CreateStream(string host, int port)
      {
        var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        var addresses = Dns.GetHostAddresses(host);
        Exception ex = null;
        foreach (var address in addresses)
        {
          try
          {
            socket.Connect(address, port);
            ex = null;
          }
          catch (Exception e)
          {
            ex = e;
          }
        }
        if (ex != null)
        {
          socket.Close();
          throw ex;
        }
        var stream = new NetworkStream(socket, true);
        return stream;
      }

      public void Auth(bool doAuth, string dbname, string username, string pwhash)
      {
        if (Authorized || !doAuth) return;
        var cmd = dbname + ".$cmd";
        Writer.WriteQuery(new Doc { { "getnonce", 1 } }, cmd, 1);
        dynamic reply = Receive().First();
        if (reply.ok != 1d)
        {
          throw new MongoSecurityException("Failed getting nonce", username, dbname);
        }
        var nonce = reply.nonce;
        var key = nonce + username + pwhash;
        Writer.WriteQuery(new Doc 
                      { 
                        {"authenticate", 1},
                        {"user", username},
                        {"nonce", nonce},
                        {"key", BsonWriter.MD5HashString(key)}
                      }, cmd, 1);
        dynamic auth = Receive().First();
        Authorized = (auth.ok == 1d);
        if (!Authorized)
        {
          throw new MongoSecurityException("Authentication failed or user not authorized", username, dbname);
        }
      }

      public ReplyMessage Receive()
      {
        return _reader.ReadReplyMessage();
      }

      ~Port()
      {
        Dispose(false);
      }

      private void Dispose(bool disposing)
      {
        if (disposing)
        {
          _reader.Dispose();
          Writer.Dispose();
        }
      }

      public void Dispose()
      {
        Dispose(true);
        GC.SuppressFinalize(this);
      }
    }

    internal Connection(string host, int port, int min = 1, int max = 10)
    {
      _host = host;
      _port = port;
      _min = min;
      _max = max;
    }

    internal void AddHost(string name, int port)
    {
      throw new NotImplementedException();
    }

    ~Connection()
    {
      Dispose(false);
    }

    public void Close()
    {
      Dispose();
    }

    [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
    public void Open()
    {
      for (int i = 0; i < _min; i++)
      {
        _ports.Enqueue(new Port(_host, _port));
        Interlocked.Increment(ref _ncreated);
      }
    }

    private static string Hash(string username, string password)
    {
      return BsonWriter.MD5HashString(string.Format("{0}:mongo:{1}", username, password));
    }

    public bool Authenticate(string username, string password, string dbname)
    {
      _username = username;
      _pwhash = Hash(username, password);
      _dbname = dbname;
      _authenticate = false;
      WithPort(port => { 
        port.Auth(true, _dbname, _username, _pwhash);
        _authenticate = port.Authorized;
      });
      return _authenticate;
    }

    private Port CheckOut(TimeSpan timeout)
    {
      Port port;
      var sw = Stopwatch.StartNew();
      if (_ports.TryDequeue(out port))
      {
        return port;
      }
      else if (_ncreated < _max)
      {
        var c = Interlocked.Increment(ref _ncreated);
        if (c <= _max)
        {
          return new Port(_host, _port);
        }
      }
      if (timeout > TimeSpan.Zero)
      {
        SpinWait.SpinUntil(() => _ports.Count > 0, 100);
        return CheckOut(timeout - sw.Elapsed);
      }
      else
      {
        throw new MongoException("Checkout timed out");
      }
    }

    private void CheckIn(Port port)
    {
      _ports.Enqueue(port);
    }

    /// <summary>
    /// Do something with a port, then return it to the queue
    /// </summary>
    /// <param name="action">the "something" to do with the port</param>
    internal void WithPort(Action<Port> action)
    {
      var port = CheckOut(_portTimeout);
      try
      {
        port.Auth(_authenticate, _dbname, _username, _pwhash);
        action(port);
        CheckIn(port);
      }
      catch (IOException)
      {
        port.Dispose(); // eject!   
      }
    }

    /// <summary>
    /// Send a message to the server and receive a response
    /// </summary>
    /// <param name="msgWriter"></param>
    /// <returns>a ReplyMessage from the server</returns>
    public ReplyMessage Call(Action<MessageWriter> msgWriter)
    {
      ReplyMessage reply = null;
      WithPort(port =>
        {
          msgWriter(port.Writer);
          reply = port.Receive();
        });
      if (reply == null)
      {
        throw new MongoException("Server connection failed");
      }
      return reply;
    }

    /// <summary>
    /// Send a message to the server without receiving a response
    /// </summary>
    /// <param name="msgWriter"></param>
    /// <param name="safe">if true, ensures the message executed without error on the server</param>
    public void Say(Action<MessageWriter> msgWriter, bool safe = false)
    {
      if (safe)
      {
        WithPort(port =>
        {
          msgWriter(port.Writer);
          port.Writer.WriteGetLastError();
          var reply = port.Receive();
          var err = reply.First()["err"];
          if (err != null)
          {
            throw new MongoOperationException(err.ToString(), reply.First());
          }
        });
      }
      else
      {
        WithPort(port => msgWriter(port.Writer));
      }      
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        while (_ports.Count > 0)
        {
          Port port;
          if (_ports.TryDequeue(out port))
          {
            port.Dispose();
          }
        }
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}
