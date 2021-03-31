using R2API.Networking.Interfaces;
using RoR2;
using UnityEngine.Networking;

namespace R2API.Networking.Messages {

    internal struct BuffMessage : INetMessage {

        public void Serialize(NetworkWriter writer) {
            writer.Write(_body.gameObject);
            writer.WriteBuffIndex(_buff);
            writer.Write(_stacks);
            writer.Write(_duration);
        }

        public void Deserialize(NetworkReader reader) {
            _body = reader.ReadGameObject().GetComponent<CharacterBody>();
            _buff = reader.ReadBuffIndex();
            _stacks = reader.ReadInt32();
            _duration = reader.ReadSingle();
        }

        public void OnReceived() => _body.ApplyBuff(_buff, _stacks, _duration);

        internal BuffMessage(CharacterBody body, BuffIndex buff, int stacks, float duration) {
            _body = body;
            _buff = buff;
            _stacks = stacks;
            _duration = duration;
        }

        private CharacterBody _body;
        private BuffIndex _buff;
        private int _stacks;
        private float _duration;
    }
}
