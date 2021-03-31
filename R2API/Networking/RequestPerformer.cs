using R2API.Networking.Interfaces;
using UnityEngine.Networking;

namespace R2API.Networking {

    internal abstract class RequestPerformerBase {

        internal abstract ISerializableObject PerformRequest(NetworkReader reader);

        internal abstract void PerformReply(NetworkReader reader);
    }

    internal sealed class RequestPerformer<TRequest, TReply> : RequestPerformerBase
        where TRequest : INetRequest<TRequest, TReply>
        where TReply : INetRequestReply<TRequest, TReply> {

        internal RequestPerformer(TRequest request, TReply reply) {
            _request = request;
            _reply = reply;
        }

        private readonly TRequest _request;
        private readonly TReply _reply;

        internal override ISerializableObject PerformRequest(NetworkReader reader) {
            _request.Deserialize(reader);
            return _request.OnRequestReceived();
        }

        internal override void PerformReply(NetworkReader reader) {
            _reply.Deserialize(reader);
            _reply.OnReplyReceived();
        }
    }
}
