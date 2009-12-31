using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Dynamic;
using MongoDB.Driver;
using MongoDB.Types;
using System.Collections;
using System.Diagnostics;

namespace MongoDB
{
  public class Doc : DynamicObject, IDictionary<string, object>
  {
    protected readonly IDictionary<string, object> _properties =
      new Dictionary<string, object>();

    public Doc()
    {
    }

    public Doc(IDictionary<string, object> obj)
    {
      _properties["_id"] = ObjectId.Create();
      foreach (var pair in obj)
      {
        _properties[pair.Key] = pair.Value;
      }
    }

    public override bool TrySetMember(SetMemberBinder binder, object value)
    {
      _properties[binder.Name] = value;
      return true;
    }

    public override bool TryGetMember(GetMemberBinder binder, out object result)
    {
      if (_properties.ContainsKey(binder.Name))
      {
        result = _properties[binder.Name];
      }
      else
      {
        result = null;
      }
      return true;
    }

    public void Add(string key, object value)
    {
      _properties.Add(key, value);
    }

    public bool ContainsKey(string key)
    {
      return _properties.ContainsKey(key);
    }

    public ICollection<string> Keys
    {
      get { return _properties.Keys; }
    }

    public bool Remove(string key)
    {
      return _properties.Remove(key);
    }

    public bool TryGetValue(string key, out object value)
    {
      return _properties.TryGetValue(key, out value);
    }

    public ICollection<object> Values
    {
      get { return _properties.Values; }
    }

    public object this[string key]
    {
      get
      {
        return _properties[key];
      }
      set
      {
        _properties[key] = value;
      }
    }

    public void Add(KeyValuePair<string, object> item)
    {
      _properties.Add(item.Key, item.Value);
    }

    public void Clear()
    {
      _properties.Clear();
    }

    public bool Contains(KeyValuePair<string, object> item)
    {
      return _properties.Contains(item);
    }

    public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex)
    {
      _properties.CopyTo(array, arrayIndex);
    }

    public int Count
    {
      get { return _properties.Count; }
    }

    public bool IsReadOnly
    {
      get { return _properties.IsReadOnly; }
    }

    public bool Remove(KeyValuePair<string, object> item)
    {
      return _properties.Remove(item.Key);
    }

    public virtual IEnumerator<KeyValuePair<string, object>> GetEnumerator()
    {
      // _id must be first if present
      if ("_id".Equals(_properties.Keys.FirstOrDefault()) || !_properties.ContainsKey("_id"))
      {
        // this is the 'normal' case
        return _properties.GetEnumerator();
      }
      // 'safe' case
      return
        _properties
        .OrderBy(x => !"_id".Equals(x.Key))
        .GetEnumerator();
    }

    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
    {
      return GetEnumerator();
    }

    public IDictionary<string, object> CheckBsonTypes()
    {
      var invalids = new Dictionary<string, object>();
      foreach (var item in _properties)
      {
        try
        {
          Bson.TypeOf(item.Value);
        }
        catch (MongoTypeException)
        {
          invalids[item.Key] = item.Value;
        }
      }
      return invalids;
    }

    public IList ToList()
    {
      var list = new ArrayList();
      foreach (var item in this)
      {
        int i;
        if (int.TryParse(item.Key, out i))
        {
          list.Insert(i, item.Value);
        }
      }
      return list;
    }
  }
}
