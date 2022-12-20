using System.Runtime.Serialization;
using System.Text;
using System.Xml.Serialization;

namespace TestTool
{
    public static class Tools
    {
        public static string Serialize(this object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj), "Serializable object cannot be null");
            }

            using var MS = new MemoryStream();
            var ser = new XmlSerializer(obj.GetType());
            ser.Serialize(MS, obj);
            return Encoding.UTF8.GetString(MS.ToArray());
        }

        public static T Deserialize<T>(this string s)
        {
            if (string.IsNullOrWhiteSpace(s))
            {
                throw new ArgumentException($"'{nameof(s)}' cannot be null or whitespace.", nameof(s));
            }

            using var MS = new MemoryStream(Encoding.UTF8.GetBytes(s));
            var ser = new XmlSerializer(typeof(T));
            var obj = ser.Deserialize(MS);
            if (obj == null)
            {
                throw new SerializationException($"Unable to deserialize type {typeof(T)} into non-null value");
            }
            return (T)obj;
        }
    }
}
