using UnityEngine.Networking;

namespace R2API.Networking.Interfaces {

    public interface ISerializableObject {

        void Serialize(NetworkWriter writer);

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
