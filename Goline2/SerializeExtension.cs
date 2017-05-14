using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace Logic
{
    internal static class SerializeExtension
    {
        public static bool Serialize<T>(this T obj, MemoryStream ms) where T : ISerializable
        {
            if (obj != null)
            {
                var bformatter = new BinaryFormatter();
                try
                {
                    bformatter.Serialize(ms, obj);
                    return true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    return false;
                }
            }
            else return false;
        }

        public static bool Deserialize<T>(this MemoryStream ms, out T obj) where T : ISerializable
        {
            obj = default(T);
            if (ms.Capacity > 0)
            {
                var bformatter = new BinaryFormatter();
                try
                {
                    ms.Position = 0;
                    obj = (T)bformatter.Deserialize(ms);
                    return true;
                }
                catch (Exception e)
                {
                    System.Diagnostics.Debug.WriteLine(e.Message);
                    return false;
                }
            }
            else
            {
                return false;
            }
        }
    }
}
