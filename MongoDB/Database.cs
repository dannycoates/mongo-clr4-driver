using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using MongoDB.Driver;
using MongoDB.Types;

namespace MongoDB
{
  public class Database : DynamicObject, IDisposable
  {
    private readonly string _name;
    private readonly Connection _connection;
    private readonly ConcurrentDictionary<string, Collection> _collections =
      new ConcurrentDictionary<string, Collection>();

    public string Name { get { return _name; } }
    internal Connection Connection { get { return _connection; } }

    internal Database(string name, string host, int port)
    {
      _name = name;
      _connection = new Connection(host, port);
      Connection.Open();
    }

    ~Database()
    {
      Dispose(false);
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        _connection.Dispose();
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      result = GetCollection(binder.Name);
      return true;
    }

    public IEnumerable<string> GetCollectionNames()
    {
      Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);
      var list = new List<string>();
      var result = GetCollection("system.namespaces").Find();
      foreach (var doc in result)
      {
        list.Add(doc["name"] as string);
      }
      return list;
    }

    public bool Authenticate(string username, string password)
    {
      return Connection.Authenticate(username, password, Name);
    }

    public void Logout()
    {
      ExecuteCommand(new Command("logout", 1));
    }

    //public void AddUser(string username, string password)
    //{
    //  var users = GetCollection("system.users");
    //  var existing = users.FindOne(new Doc { { "user", username } });
    //  if (existing == null)
    //  {
    //    users.Insert(new Doc { { "user", username }, { "pwd", Hash(username, password) } });
    //  }
    //}

    public Collection CreateCollection(string name, Doc options = null)
    {
      /* valid options are:
       * size - initial size (bytes), also max if capped
       * capped - true if capped
       * max - max object count if capped (optional)
       */
      return new Collection(name, this, options);
    }

    public Collection GetCollection(string name)
    {
      Contract.Requires(!string.IsNullOrWhiteSpace(name));
      return _collections.GetOrAdd(name, x => new Collection(x, this));
    }

    public Doc ExecuteCommand(Command cmd)
    {
      return GetCollection("$cmd").FindOne(cmd);
    }

    public string Eval(string code, params string[] args)
    {
      var cmd = new Command("$eval", new Code(code)) { { "args", args } };
      return ExecuteCommand(cmd)["retval"] as string;
    }

    public string Eval(ScopedCode code, params string[] args)
    {
      var cmd = new Command("$eval", code) { { "args", args } };
      return ExecuteCommand(cmd)["retval"] as string;
    }

    public void Drop()
    {
      ExecuteCommand(new Command("dropDatabase", 1));
    }

    public int ProfilingLevel 
    {
      get 
      {
        return Convert.ToInt32(ExecuteCommand(new Command("profile", -1))["was"]);
      }
      set
      {
        Contract.Requires(value > -1 && value < 3);
        ExecuteCommand(new Command("profile", value));
      }
    }
  }
}
