using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace R2API.Networking;

internal class Header : ISerializableObject
{

    public Header()
    {
    }

    internal Header(int typeCode, NetworkDestination dest)
    {
        TypeCode = typeCode;
        Destination = dest;
    }

    internal int TypeCode
    {
        get;
        private set;
    }

    internal NetworkDestination Destination
    {
        get;
        private set;
    }

    internal void RemoveDestination(NetworkDestination destination) => Destination &= ~destination;

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(TypeCode);
        writer.Write((byte)Destination);
    }

    public void Deserialize(NetworkReader reader)
    {
        TypeCode = reader.ReadInt32();
        Destination = (NetworkDestination)reader.ReadByte();
    }
}
