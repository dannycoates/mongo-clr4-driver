using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MongoDB.Driver;
using MongoDB.Types;
using System.IO;
using System.Collections;

namespace MongoDB.GridFS
{
  public sealed class FileInfo
  {
    private readonly Collection _collection;
    public object Id { get; internal set; }
    public string Filename { get; set; }
    public string ContentType { get; set; }
    public long Length { get; internal set; }
    public int ChunkSize { get; internal set; }
    public DateTime UploadDate { get; internal set; }
    public IList<string> Aliases { get; internal set; }
    public IDictionary<string, object> MetaData { get; set; }
    public string MD5 { get; internal set; } //TODO

    internal FileInfo(string name, Collection collection)
    {
      Id = ObjectId.Create();
      ChunkSize = 0x40000;
      Filename = name;
      UploadDate = DateTime.UtcNow;
      Aliases = new List<string>();
      _collection = collection;
    }

    internal FileInfo(Doc doc, Collection collection)
    {
      Id = doc["_id"];
      Filename = doc["filename"] as string;
      ContentType = doc["contentType"] as string;
      Length = Convert.ToInt64(doc["length"]);
      ChunkSize = Convert.ToInt32(doc["chunkSize"]);
      UploadDate = ((DateTime)doc["uploadDate"]);
     // Aliases = (doc["aliases"] as IList).OfType<string>() as IList<string>;
      MetaData = doc["metadata"] as Doc;
      MD5 = doc["md5"] as string;
      _collection = collection;
    }

    internal Doc ToDoc()
    {
      return new Doc { {"_id", Id},
                       {"filename", Filename},
                       {"contentType", ContentType},
                       {"length", Length},
                       {"chunkSize", ChunkSize},
                       {"uploadDate", UploadDate},
                       {"aliases", Aliases},
                       {"metadata", MetaData},
                       {"md5", MD5} };
    }

    public void Save()
    {
      _collection.FilesCollection().Save(this.ToDoc());
    }

    public Stream Open(FileAccess access = FileAccess.Read)
    {
      return new GridStream(_collection, Filename, this.ToDoc(), access);
    }

    public void Delete()
    {
      _collection.ChunkCollection().Remove(new Doc { { "files_id", Id } });
      _collection.FilesCollection().Remove(new Doc { { "_id", Id } });
    }

    public bool Exists { get { return _collection.FileExists(Filename); } }
  }
}
