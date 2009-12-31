using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Dynamic;
using MongoDB.Driver;

namespace MongoDB
{
  public sealed class Mongo : DynamicObject, IDisposable
  {
    private readonly string _host;
    private readonly int _port;
    private readonly ConcurrentDictionary<string, Database> _databases =
      new ConcurrentDictionary<string, Database>();

    public enum Dir
    {
      Asc = 1,
      Desc = -1
    }

    public Mongo(string host, int port)
    {
      _host = host;
      _port = port;
    }

    ~Mongo()
    {
      Dispose(false);
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      result = GetDB(binder.Name);
      return true;
    }

    public IEnumerable<string> GetDatabaseNames()
    {
      Contract.Ensures(Contract.Result<IEnumerable<string>>() != null);
      var names = new List<string>();
      dynamic cmd = new Command("listDatabases", 1);
      dynamic result = GetDB("admin").ExecuteCommand(cmd);
      foreach (var db in result.databases)
      {
        names.Add(db.name as string);
      }
      return names;
    }

    public Database GetDB(string name)
    {
      Contract.Requires(!string.IsNullOrWhiteSpace(name));
      return _databases.GetOrAdd(name, x => new Database(x, _host, _port));
    }

    private Doc AdminCommand(string cmd)
    {
      return GetDB("admin").ExecuteCommand(new Command(cmd, 1));
    }

    public void ShutdownServer()
    {
      try
      {
        AdminCommand("shutdown");
      }
      catch (MongoException ex)
      {
        //this is the desired path
        if (ex.Message.Equals("Server connection failed"))
          return; //server should be down
      }
      throw new MongoException("ShutdownServer failed");
    }

    public Doc ServerBuildInfo()
    {
      return AdminCommand("buildinfo");
    }

    public string ServerVersion()
    {
      return ServerBuildInfo()["version"] as string;
    }

    private void Dispose(bool disposing)
    {
      if (disposing)
      {
        foreach (var db in _databases.Values)
        {
          db.Dispose();
        }
        _databases.Clear();
      }
    }

    public void Dispose()
    {
      Dispose(true);
      GC.SuppressFinalize(this);
    }
  }
}
