using System;
using UnityEngine;
using System.Linq;
using UnityEngine.Splines;
using System.Collections.Generic;

namespace BurstRope
{
    public class RopeTarget : MonoBehaviour
    {
        Dictionary<Rope, int> RopeIndices = new Dictionary<Rope, int>();

        public void Init(Rope rope, int index)
        {
            if (!RopeIndices.ContainsKey(rope))
                RopeIndices.Add(rope, index);
        }

        public void StartDragging()
        {
            foreach (var item in RopeIndices)
                item.Key.StartDragging(item.Value);
        }

        public void StopDragging()
        {
            foreach (var item in RopeIndices)
                item.Key.StopDragging();
        }
    }
}