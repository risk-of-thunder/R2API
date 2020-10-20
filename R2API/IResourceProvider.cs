using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace R2API {
    public interface IResourceProvider {
        string? ModPrefix { get; }

        Object Load(string? path, Type? type);

        ResourceRequest LoadAsync(string? path, Type? type);

        Object[] LoadAll(Type? type);
    }
}
