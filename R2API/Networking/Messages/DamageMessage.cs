using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine.Networking;

namespace R2API.Networking.Messages {

    internal struct DamageMessage : INetMessage {

        public void Serialize(NetworkWriter writer) {
            writer.Write(_damage);
            writer.Write(HurtBoxReference.FromHurtBox(_target));

            byte flags = 0b0000;
            flags |= _callHitWorld ? (byte)0b0001 : (byte)0b0000;
            flags <<= 1;
            flags |= _callHitEnemy ? (byte)0b0001 : (byte)0b0000;
            flags <<= 1;
            flags |= _callDamage ? (byte)0b0001 : (byte)0b0000;
            writer.Write(flags);
        }

        public void Deserialize(NetworkReader reader) {
            _damage = reader.ReadDamageInfo();
            _target = reader.ReadHurtBoxReference().ResolveHurtBox();

            byte flags = reader.ReadByte();
            const byte mask = 0b0001;
            _callDamage = (flags & mask) > 0;
            flags >>= 1;
            _callHitEnemy = (flags & mask) > 0;
            flags >>= 1;
            _callHitWorld = (flags & mask) > 0;
        }

        public void OnReceived() => _damage.DealDamage(_target, _callDamage, _callHitEnemy, _callHitWorld);

        internal DamageMessage(DamageInfo damage, HurtBox target, bool callDamage, bool callHitEnemy, bool callHitWorld) {
            _damage = damage;
            _target = target;
            _callDamage = callDamage;
            _callHitEnemy = callHitEnemy;
            _callHitWorld = callHitWorld;
        }

        private DamageInfo _damage;
        private HurtBox _target;
        private bool _callDamage;
        private bool _callHitEnemy;
        private bool _callHitWorld;
    }
}
