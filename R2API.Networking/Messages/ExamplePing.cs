using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace R2API.Networking.Messages;

internal struct ExamplePing : INetRequest<ExamplePing, ExamplePingReply>
{
    internal int Integer;

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(Integer);
    }

    public void Deserialize(NetworkReader reader)
    {
        Integer = reader.ReadInt32();
    }

    public ExamplePingReply OnRequestReceived()
    {
        NetworkingPlugin.Logger.LogWarning("ExamplePing - Received this request : " + Integer);
        return new ExamplePingReply { Str = "I'm answering you back this string" };
    }
}

internal struct ExamplePingReply : INetRequestReply<ExamplePing, ExamplePingReply>
{
    internal string Str;

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(Str);
    }

    public void Deserialize(NetworkReader reader)
    {
        Str = reader.ReadString();
    }

    public void OnReplyReceived()
    {
        NetworkingPlugin.Logger.LogWarning("ExamplePingReply - Received this Reply : " + Str);
    }
}
