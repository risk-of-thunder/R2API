using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API.Networking.Messages;

internal struct DotMessage : INetMessage
{

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(_victim);
        writer.Write(_attacker);
        writer.WritePackedIndex32((int)_dotIndex);
        writer.Write(_duration);
        writer.Write(_damageMultiplier);
    }

    public void Deserialize(NetworkReader reader)
    {
        _victim = reader.ReadGameObject();
        _attacker = reader.ReadGameObject();
        _dotIndex = (DotController.DotIndex)reader.ReadPackedIndex32();
        _duration = reader.ReadSingle();
        _damageMultiplier = reader.ReadSingle();
    }

    public void OnReceived() => DotController.InflictDot(_victim, _attacker, _dotIndex, _duration, _damageMultiplier);

    internal DotMessage(GameObject victimObject, GameObject attackerObject, DotController.DotIndex dotIndex, float duration, float damageMultiplier)
    {
        _victim = victimObject;
        _attacker = attackerObject;
        _dotIndex = dotIndex;
        _duration = duration;
        _damageMultiplier = damageMultiplier;
    }

    private GameObject _victim;
    private GameObject _attacker;
    private DotController.DotIndex _dotIndex;
    private float _duration;
    private float _damageMultiplier;
}
