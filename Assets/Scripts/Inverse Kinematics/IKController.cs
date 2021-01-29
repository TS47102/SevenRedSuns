using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class IKController : MonoBehaviour
{
    public uint iterations = 1;
    public bool usePhysics = false;

    [NonSerialized] public IKLeaf[] leafBones;
    [NonSerialized] public ILookup<Transform, IKLeaf> sharedRoots;

    private void Start()
    {
        findLeafBones();
    }

    private void findLeafBones()
    {
        leafBones = GetComponentsInChildren<IKLeaf>();
        sharedRoots = leafBones.ToLookup(initLeaf);
        foreach(IGrouping<Transform, IKLeaf> group in sharedRoots)
            recalculateGroup(group);
    }

    private void recalculateGroup(IEnumerable<IKLeaf> group)
    {
        foreach (IKLeaf leaf in group)
        {
            leaf.dirty = false;
            foreach (IKLeaf otherLeaf in group)
                if (leaf != otherLeaf && otherLeaf.priority >= leaf.priority && otherLeaf.target != null)
                    leaf.effectiveRoot = leaf.parents.Except(otherLeaf.parents).LastOrDefault();
        }
    }

    private Transform initLeaf(IKLeaf leaf)
    {
        List<Transform> parents = getLeafParents(leaf.transform, new List<Transform>());
        leaf.parents = parents;
        if(usePhysics)
            initLeafParents(leaf);

        // The root bone is always the last transform in the list.
        leaf.root = parents.Last();
        leaf.effectiveRoot = leaf.root;
        parents.RemoveAt(parents.Count - 1);
        return leaf.root;
    }

    private void initLeafParents(IKLeaf leaf)
    {
        foreach (Transform t in leaf.parents.Reverse<Transform>())
        {
            Rigidbody r;
            if (!t.TryGetComponent(out r))
                r = t.gameObject.AddComponent<Rigidbody>();

            r.useGravity = false;
            r.drag = 1;
            r.angularDrag = 1;

            CharacterJoint c;
            if (!t.TryGetComponent(out c))
                c = t.gameObject.AddComponent<CharacterJoint>();

            c.connectedBody = t.parent?.GetComponent<Rigidbody>();
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

    private List<Transform> getLeafParents(Transform current, List<Transform> into)
    {
        if(current.parent != null && current.parent != transform)
        {
            into.Add(current.parent);
            return getLeafParents(current.parent, into);
        }

        return into;
    }

    private void FixedUpdate()
    {
        foreach(IKLeaf leaf in leafBones)
            solve(leaf);
    }

    private void solve(IKLeaf leaf)
    {
        if(leaf.dirty)
            recalculateGroup(sharedRoots[leaf.root]);

        if(leaf.target == null)
            return;

        for(uint i = 0; i < iterations; ++i)
        {
            if(Vector3.Distance(leaf.transform.position, leaf.target.position) <= leaf.tolerance)
                return;

            solve_once(leaf, usePhysics);
        }
    }

    private static void solve_once(IKLeaf leaf, bool usePhysics)
    {
        foreach(Transform t in leaf.parents)
        {
            solve_bone(leaf, t, usePhysics);
            if(t == leaf.effectiveRoot)
                return;
        }

        if(leaf.canMoveRootBone)
            solve_bone(leaf, leaf.root, usePhysics);
    }

    private static void solve_bone(IKLeaf leaf, Transform bone, bool usePhysics)
    {
        Vector3 end = leaf.transform.position - bone.position;
        Vector3 target = leaf.target.position - bone.position;
        Quaternion rot = Quaternion.FromToRotation(end, target);
        if(usePhysics)
        {
            float angle;
            Vector3 axis;
            rot.ToAngleAxis(out angle, out axis);
            bone.GetComponent<Rigidbody>().AddTorque(axis * angle, ForceMode.VelocityChange);
        }
        else
            bone.rotation = rot * bone.rotation;
    }
}
