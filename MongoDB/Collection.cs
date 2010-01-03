using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Dynamic;
using System.Linq;
using MongoDB.Driver;
using MongoDB.Types;

namespace MongoDB
{
  public sealed class Collection : DynamicObject
  {
    public string Name { get; private set; }
    public Database Database { get; private set; }
    public string FullName { get; private set; }
    
    public Collection(string name, Database db, Doc options = null)
    {
      Name = name;
      Database = db;
      FullName = db.Name + "." + name;
      //TODO: handle options
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      result = Database.GetCollection(string.Format("{0}.{1}", Name, binder.Name));
      return true;
    }

    /// <summary>
    /// Drop this collection from its Database
    /// </summary>
    public void Drop()
    {
      Database.ExecuteCommand(new Command("drop", Name));
    }

    public Doc Stats()
    {
      return Database.ExecuteCommand(new Command("collstats", Name));
    }

    public long DataSize()
    {
      return Convert.ToInt64(Stats()["size"]);
    }

    public long StorageSize()
    {
      return Convert.ToInt64(Stats()["storageSize"]);
    }

    public long TotalIndexSize()
    {
      return 
        GetIndexes()
        .Aggregate(0L, 
          (size, doc) =>
          {
            var coll = Database.GetCollection(Name + ".$" + doc["name"]);
            return size + coll.DataSize();
          });
    }

    #region CRUD

    public object Insert(IDictionary<string, object> obj, bool safe = false)
    {
      Contract.Requires(obj != null);
      if (!"_id".Equals(obj.Keys.FirstOrDefault()))
      {
        obj = new Doc(obj);
      }
      Database.Connection.Say(msg => msg.WriteInsert(FullName, new IDictionary<string, object>[] { obj }), safe);
      return obj["_id"];
    }

    public void BulkInsert(params Doc[] docs)
    {
      Contract.Requires(docs.All(x => x.ContainsKey("_id")));
      Database.Connection.Say(msg => msg.WriteInsert(FullName, docs));
    }

    public void Update(
      IDictionary<string, object> spec,
      IDictionary<string, object> doc,
      bool upsert = false,
      bool multi = false, 
      bool safe = false)
    {
      var options = UpdateOption.None;
      if (upsert) options |= UpdateOption.Upsert;
      if (multi) options |= UpdateOption.MultiUpdate;
      Database.Connection.Say(msg => msg.WriteUpdate(FullName, spec, doc, options), safe);
    }

    public object Save(IDictionary<string, object> obj, bool safe = false)
    {
      if (!obj.ContainsKey("_id"))
      {
        return Insert(obj, safe);
      }
      Update(new Doc { { "_id", obj["_id"] } }, obj, true, false, safe);
      return obj["_id"];
    }

    public void Remove(IDictionary<string, object> spec, bool safe = false)
    {
      Database.Connection.Say(msg => msg.WriteDelete(FullName, spec), safe);
    }

    #endregion

    #region Query

    public int Count()
    {
      return Find().Count();
    }

    public IList Distinct(string key)
    {
      return Database.ExecuteCommand(new Command("distinct", Name){ { "key", key } })["values"] as IList;
    }

    [SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
    public Cursor Find(
      IDictionary<string, object> query = null,
      int limit = 0,
      IEnumerable<string> fields = null, 
      int skip = 0)
    {
      if (query == null)
      {
        query = new Doc();
      }
      return 
        new Cursor(query, fields, this)
        .Limit(limit)
        .Skip(skip);
    }

    public Doc FindOne(IDictionary<string, object> obj = null)
    {
      return Find(obj, 1).FirstOrDefault();
    }

    //public Doc MapReduce(MapReduce mr)
    //{
    //  return Database.ExecuteCommand(mr.ToDoc(Name));
    //}
    #endregion

    #region Indexes

    private static string IndexName(Index index)
    {
      return string.Join("_", index.Select(x => string.Join("_", x.Key, (int)x.Value)));
    }

    [Obsolete("Use CreateIndex")]
    public string EnsureIndex(Index index)
    {
      /* Since Mongo 1.1.3 the insert function on the server check whether
       * the index already exists in pdfile.cpp so tracking indexes here isn't 
       * worth the effort. So here, Ensure can just be an alias for Create.
       */
      return CreateIndex(index);
    }

    public string CreateIndex(Index index, bool unique = false)
    {
      var indexName = IndexName(index);
      var doc = new Doc { {"name", indexName},
                          {"ns", FullName},
                          {"key", index.ToDoc()},
                          {"unique", unique} };
      Database.GetCollection("system.indexes").Insert(doc);
      return indexName;
    }

    public void DropIndex(Index index)
    {
      DropIndex(IndexName(index));
    }

    public void DropIndex(string name)
    {
      Database.ExecuteCommand(new Command("deleteIndexes", Name){ { "index", name } });
    }

    public Cursor GetIndexes()
    {
      return Database.GetCollection("system.indexes").Find(new Doc { { "ns", FullName } });
    }

    #endregion
  }
}
