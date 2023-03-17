using System;
using System.IO;
using System.Windows.Forms;
using LiteDB;

namespace Clipple.Types;

public abstract class PersistentDataHelper
{
    public static T? Load<T>()
    {
        var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.ProductName, nameof(T));
        try
        {
            return BsonMapper.Global.ToObject<T>(BsonSerializer.Deserialize(File.ReadAllBytes(fileName)));
        }
        catch (FileNotFoundException e)
        {
            return default;
        }
    }

    public static void Save<T>(T data)
    {
        var fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Application.ProductName, nameof(T));
        File.WriteAllBytes(fileName, BsonSerializer.Serialize(BsonMapper.Global.ToDocument(data)));
    }
}