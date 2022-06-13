using System;
using UnityEngine.Networking;

namespace R2API.Networking;

internal class Writer : IDisposable
{

    internal Writer(NetworkWriter writer, short messageIndex, NetworkConnection target, QosType qos)
    {
        _netWriter = writer;
        _target = target;
        _qos = qos;
        writer.StartMessage(messageIndex);
    }

    private readonly NetworkWriter _netWriter;
    private readonly NetworkConnection _target;
    private readonly QosType _qos;

    public static implicit operator NetworkWriter(Writer writer) => writer._netWriter;

    public void Dispose()
    {
        _netWriter.FinishMessage();
        _target.SendWriter(_netWriter, (int)_qos);
    }
}
