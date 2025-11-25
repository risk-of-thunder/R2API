using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace R2API.Networking.Messages;

internal struct DotMessage : INetMessage
{
    static readonly bool[] _nullableFieldsSharedHasValueArray = new bool[3];

    public void Serialize(NetworkWriter writer)
    {
        writer.Write(_victim);
        writer.Write(_attacker);
        writer.WritePackedIndex32((int)_dotIndex);
        writer.Write(_duration);
        writer.Write(_damageMultiplier);

        int bitIndex = 0;
        _nullableFieldsSharedHasValueArray[bitIndex++] = _maxStacksFromAttacker.HasValue;
        _nullableFieldsSharedHasValueArray[bitIndex++] = _totalDamage.HasValue;
        _nullableFieldsSharedHasValueArray[bitIndex++] = _preUpgradeDotIndex.HasValue;
        writer.WriteBitArray(_nullableFieldsSharedHasValueArray);

        if (_maxStacksFromAttacker.HasValue)
        {
            writer.WritePackedUInt32(_maxStacksFromAttacker.Value);
        }

        if (_totalDamage.HasValue)
        {
            writer.Write(_totalDamage.Value);
        }

        if (_preUpgradeDotIndex.HasValue)
        {
            writer.WritePackedIndex32((int)_preUpgradeDotIndex.Value);
        }

        writer.Write(_hitHurtBox);
    }

    public void Deserialize(NetworkReader reader)
    {
        _victim = reader.ReadGameObject();
        _attacker = reader.ReadGameObject();
        _dotIndex = (DotController.DotIndex)reader.ReadPackedIndex32();
        _duration = reader.ReadSingle();
        _damageMultiplier = reader.ReadSingle();

        reader.ReadBitArray(_nullableFieldsSharedHasValueArray);

        int bitIndex = 0;
        bool maxStacksFromAttackerHasValue = _nullableFieldsSharedHasValueArray[bitIndex++];
        bool totalDamageHasValue = _nullableFieldsSharedHasValueArray[bitIndex++];
        bool preUpgradeDotIndexHasValue = _nullableFieldsSharedHasValueArray[bitIndex++];

        if (maxStacksFromAttackerHasValue)
        {
            _maxStacksFromAttacker = reader.ReadPackedUInt32();
        }

        if (totalDamageHasValue)
        {
            _totalDamage = reader.ReadSingle();
        }

        if (preUpgradeDotIndexHasValue)
        {
            _preUpgradeDotIndex = (DotController.DotIndex)reader.ReadPackedIndex32();
        }

        _hitHurtBox = reader.ReadHurtBoxReference();
    }

    public void OnReceived()
    {
        InflictDotInfo inflictDotInfo = new InflictDotInfo()
        {
            victimObject = _victim,
            attackerObject = _attacker,
            dotIndex = _dotIndex,
            duration = _duration,
            damageMultiplier = _damageMultiplier,
            maxStacksFromAttacker = _maxStacksFromAttacker,
            totalDamage = _totalDamage,
            preUpgradeDotIndex = _preUpgradeDotIndex,
            hitHurtBox = _hitHurtBox.ResolveHurtBox(),
        };

        DotController.InflictDot(ref inflictDotInfo);
    }

    internal DotMessage(InflictDotInfo inflictDotInfo)
    {
        _victim = inflictDotInfo.victimObject;
        _attacker = inflictDotInfo.attackerObject;
        _dotIndex = inflictDotInfo.dotIndex;
        _duration = inflictDotInfo.duration;
        _damageMultiplier = inflictDotInfo.damageMultiplier;
        _maxStacksFromAttacker = inflictDotInfo.maxStacksFromAttacker;
        _totalDamage = inflictDotInfo.totalDamage;
        _preUpgradeDotIndex = inflictDotInfo.preUpgradeDotIndex;
        _hitHurtBox = HurtBoxReference.FromHurtBox(inflictDotInfo.hitHurtBox);
    }

    private GameObject _victim;
    private GameObject _attacker;
    private DotController.DotIndex _dotIndex;
    private float _duration;
    private float _damageMultiplier;
    private uint? _maxStacksFromAttacker;
    private float? _totalDamage;
    private DotController.DotIndex? _preUpgradeDotIndex;
    private HurtBoxReference _hitHurtBox;
}
