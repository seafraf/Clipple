using System;
using System.IO;
using System.Windows.Forms;
using LiteDB;

namespace Clipple.Types;

public abstract class PersistentDataHelper
{
    public static T? Load<T>()
    {
        var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.ProductName, typeof(T).Name + ".bson");
        try
        {
            return BsonMapper.Global.ToObject<T>(BsonSerializer.Deserialize(File.ReadAllBytes(fileName)));
        }
        catch (FileNotFoundException)
        {
            return default;
        }
    }

    public static void Save<T>(T data)
    {
        var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.ProductName, typeof(T).Name + ".bson");
        File.WriteAllBytes(fileName, BsonSerializer.Serialize(BsonMapper.Global.ToDocument(data)));
    }
}