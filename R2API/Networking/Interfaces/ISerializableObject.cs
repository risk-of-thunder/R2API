using UnityEngine.Networking;

namespace R2API.Networking.Interfaces {

    /// <summary>
    /// Must implement Serialize for how to serialize the class / struct through a NetworkWriter
    /// and how to deserialize them through a NetworkReader
    /// </summary>
    public interface ISerializableObject {

        /// <summary>
        /// How the class / struct should be serialized over the network
        /// </summary>
        /// <param name="writer"></param>
        void Serialize(NetworkWriter writer);

        /// <summary>
        /// How the class / struct should be deserialized over the network
        /// </summary>
        /// <param name="reader"></param>
        void Deserialize(NetworkReader reader);
    }

    public static class SerializableObjectExtensions {

        public static void Write<TObject>(this NetworkWriter? writer, TObject target) where TObject : ISerializableObject => target.Serialize(writer);

        public static TObject Read<TObject>(this NetworkReader? reader, TObject destination) where TObject : ISerializableObject {
            destination.Deserialize(reader);
            return destination;
        }

        public static TObject Read<TObject>(this NetworkReader? reader) where TObject : ISerializableObject, new() {
            var obj = new TObject();
            obj.Deserialize(reader);
            return obj;
        }
    }
}
