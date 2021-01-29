using System.Collections.Generic;
using UnityEngine;

namespace SevenRedSuns.InverseKinematics
{
    public class IKLeaf : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private int priority;
        [SerializeField] private float tolerance;
        [SerializeField] private bool canMoveRootBone;

        public Transform Target { get => target; set { target = value; Dirty = true; } }
        public int Priority { get => priority; set { priority = value; Dirty = true; } }
        public float Tolerance { get => tolerance; set => tolerance = value; }
        public bool CanMoveRootBone { get => canMoveRootBone; set => canMoveRootBone = value; }
        public bool Dirty { get; set; }

        public List<Transform> Parents { get; set; }
        public Transform Root { get; set; }
        public Transform EffectiveRoot { get; set; }
    }
}
