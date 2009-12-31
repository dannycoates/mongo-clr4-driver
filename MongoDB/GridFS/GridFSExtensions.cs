using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace MongoDB.GridFS
{
  public static class GridFSExtensions
  {
    internal static Doc FindFile(this Collection col, string name)
    {
      var files = col.FilesCollection();
      files.CreateIndex(new Index { { "filename", Mongo.Dir.Asc } }, true);
      return files.FindOne(new Doc { { "filename", name } });
    }

    internal static Collection FilesCollection(this Collection col)
    {
      return col.Database.GetCollection(col.Name + ".files");
    }

    internal static Collection ChunkCollection(this Collection col)
    {
      return col.Database.GetCollection(col.Name + ".chunks");
    }

    public static bool FileExists(this Collection col, string name)
    {
      return col.FindFile(name) != null;
    }

    public static FileInfo GetFile(this Collection col, string name)
    {
      var doc = col.FindFile(name);
      return (doc == null) ? new FileInfo(name, col) : new FileInfo(doc, col);
    }

    public static void RemoveFile(this Collection col, string name)
    {
      var f = col.GetFile(name);
      f.Delete();
    }

    public static Collection GridFS(this Database db)
    {
      return db.GetCollection("fs");
    }

  }
}
