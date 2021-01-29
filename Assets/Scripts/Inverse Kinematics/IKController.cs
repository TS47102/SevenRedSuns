using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace SevenRedSuns.InverseKinematics
{
    public class IKController : MonoBehaviour
    {
        public uint Iterations = 1;
        public bool UsePhysics = false;

        [NonSerialized] public IKLeaf[] LeafBones;
        [NonSerialized] public ILookup<Transform, IKLeaf> SharedRoots;

        private void Start()
        {
            FindLeafBones();
        }

        private void FindLeafBones()
        {
            LeafBones = GetComponentsInChildren<IKLeaf>();
            SharedRoots = LeafBones.ToLookup(InitLeaf);
            foreach(IGrouping<Transform, IKLeaf> group in SharedRoots)
                RecalculateGroup(group);
        }

        private void RecalculateGroup(IEnumerable<IKLeaf> group)
        {
            foreach(IKLeaf leaf in group)
            {
                leaf.Dirty = false;
                foreach(IKLeaf otherLeaf in group)
                {
                    if(leaf != otherLeaf && otherLeaf.Priority >= leaf.Priority && otherLeaf.Target != null)
                        leaf.EffectiveRoot = leaf.Parents.Except(otherLeaf.Parents).LastOrDefault();
                }
            }
        }

        private Transform InitLeaf(IKLeaf leaf)
        {
            List<Transform> parents = GetLeafParents(leaf.transform, new List<Transform>());
            leaf.Parents = parents;
            if(UsePhysics)
                InitLeafParents(leaf);

            // The root bone is always the last transform in the list.
            leaf.Root = parents.Last();
            leaf.EffectiveRoot = leaf.Root;
            parents.RemoveAt(parents.Count - 1);
            return leaf.Root;
        }

        private void InitLeafParents(IKLeaf leaf)
        {
            foreach(Transform t in leaf.Parents.Reverse<Transform>())
            {
                if(!t.TryGetComponent(out Rigidbody r))
                    r = t.gameObject.AddComponent<Rigidbody>();

                r.useGravity = false;
                r.drag = 1;
                r.angularDrag = 1;

                if(!t.TryGetComponent(out CharacterJoint c))
                    c = t.gameObject.AddComponent<CharacterJoint>();

                c.connectedBody = t.parent == null ? null : t.parent.GetComponent<Rigidbody>();
                c.enableCollision = true;

                SoftJointLimit s = c.highTwistLimit;
                s.limit = float.PositiveInfinity;
                c.highTwistLimit = s;

                s = c.lowTwistLimit;
                s.limit = float.PositiveInfinity;
                c.lowTwistLimit = s;

                s = c.swing1Limit;
                s.limit = float.PositiveInfinity;
                c.swing1Limit = s;

                s = c.swing2Limit;
                s.limit = float.PositiveInfinity;
                c.swing2Limit = s;
            }
        }

        private List<Transform> GetLeafParents(Transform current, List<Transform> into)
        {
            if(current.parent != null && current.parent != transform)
            {
                into.Add(current.parent);
                return GetLeafParents(current.parent, into);
            }

            return into;
        }

        private void FixedUpdate()
        {
            foreach(IKLeaf leaf in LeafBones)
                Solve(leaf);
        }

        private void Solve(IKLeaf leaf)
        {
            if(leaf.Dirty)
                RecalculateGroup(SharedRoots[leaf.Root]);

            if(leaf.Target == null)
                return;

            for(uint i = 0; i < Iterations; ++i)
            {
                if(Vector3.Distance(leaf.transform.position, leaf.Target.position) <= leaf.Tolerance)
                    return;

                SolveOnce(leaf, UsePhysics);
            }
        }

        private static void SolveOnce(IKLeaf leaf, bool usePhysics)
        {
            foreach(Transform t in leaf.Parents)
            {
                SolveBone(leaf, t, usePhysics);
                if(t == leaf.EffectiveRoot)
                    return;
            }

            if(leaf.CanMoveRootBone)
                SolveBone(leaf, leaf.Root, usePhysics);
        }

        private static void SolveBone(IKLeaf leaf, Transform bone, bool usePhysics)
        {
            Vector3 end = leaf.transform.position - bone.position;
            Vector3 target = leaf.Target.position - bone.position;
            Quaternion rot = Quaternion.FromToRotation(end, target);
            if(usePhysics)
            {
                rot.ToAngleAxis(out float angle, out Vector3 axis);
                bone.GetComponent<Rigidbody>().AddTorque(axis * angle, ForceMode.VelocityChange);
            }
            else
                bone.rotation = rot * bone.rotation;
        }
    }
}
