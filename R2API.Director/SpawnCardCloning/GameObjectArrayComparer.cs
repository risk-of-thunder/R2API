using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace R2API.SpawnCardCloning;
public class GameObjectArrayComparer : IEqualityComparer<GameObject[]>
{
    public bool Equals(GameObject[] x, GameObject[] y)
    {
        if (ReferenceEquals(x, y)) return true;
        if (x == null || y == null || x.Length != y.Length) return false;
        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i]) return false;
        }
        return true;
    }
    public int GetHashCode(GameObject[] obj)
    {
        if (obj == null) return 0;
        int hash = 17;
        foreach (var item in obj)
        {
            hash = hash * 31 + (item != null ? item.GetHashCode() : 0);
        }
        return hash;
    }
}
