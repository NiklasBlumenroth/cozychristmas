using System;
using System.Collections.Generic;
using UnityEngine;

namespace CozySanta.Runtime.Items
{
    /// <summary>Eine gespeicherte Item-Platzierung: welches Prefab, an welcher Pose (Weltkoordinaten).</summary>
    [Serializable]
    public struct ItemPlacement
    {
        public string key;
        public Vector3 position;
        public Quaternion rotation;
    }

    /// <summary>Serialisierbarer Container aller Item-Platzierungen eines Bereichs (für JsonUtility).</summary>
    [Serializable]
    public sealed class AreaItemData
    {
        public string area = "";
        public List<ItemPlacement> items = new List<ItemPlacement>();
    }
}
