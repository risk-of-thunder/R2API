using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace R2API.SpawnCardCloning;
public class GameObjectArrayDictionary<T>
{
    private class Entry
    {
        public GameObject[] gameObjects;
        public T t;
        public Entry(GameObject[] gameObjects, T t)
        {
            this.gameObjects = gameObjects;
            this.t = t;
        }
    }
    private List<Entry> _entries = [];
    public void Add(GameObject[] keys, T t)
    {
        if (keys == null) return;
        _entries.Add(new Entry(keys, t));
    }
    public bool Remove(GameObject[] gameObjects, bool matchOrder, bool removeAllMatches = false)
    {
        if (gameObjects == null) return false;
        if (removeAllMatches)
        {
            int removedCount = _entries.RemoveAll(entry => Match(entry.gameObjects, gameObjects, matchOrder));
            return removedCount > 0;
        }
        else
        {
            for (int i = 0; i < _entries.Count; i++)
            {
                if (Match(_entries[i].gameObjects, gameObjects, matchOrder))
                {
                    _entries.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }
    }
    public void Clear() => _entries.Clear();
    public bool ContainsKey(GameObject[] gameObjects, bool matchOrder)
    {
        foreach (var entry in _entries)
        {
            if (!Match(entry.gameObjects, gameObjects, matchOrder)) continue;
            return true;
        }
        return false;
    }
    public bool TryGetValue(GameObject[] gameObjects, bool matchOrder, out T t)
    {
        foreach (Entry entry in _entries)
        {
            if (!Match(entry.gameObjects, gameObjects, matchOrder)) continue;
            t = entry.t;
            return true;
        }
        t = default;
        return false;
    }
    private bool Match(GameObject[] entryGameObjects, GameObject[] inputGameObjects, bool matchOrder)
    {
        if (entryGameObjects == null || inputGameObjects == null || entryGameObjects.Length != inputGameObjects.Length) return false;
        if (matchOrder)
        {
            for (int i = 0; i < entryGameObjects.Length; i++)
            {
                if (entryGameObjects[i] != inputGameObjects[i]) return false;
            }
            return true;
        }
        else
        {
            var entryGrameObejctsSorted = entryGameObjects.Select(go => go != null ? go.GetInstanceID() : 0).OrderBy(id => id);
            var inputGameObjectsSorted = inputGameObjects.Select(go => go != null ? go.GetInstanceID() : 0).OrderBy(id => id);
            return entryGrameObejctsSorted.SequenceEqual(inputGameObjectsSorted);
        }
    }
}
