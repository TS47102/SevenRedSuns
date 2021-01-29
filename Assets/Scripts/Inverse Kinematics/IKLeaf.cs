using System;
using System.Collections.Generic;
using UnityEngine;

public class IKLeaf : MonoBehaviour
{
    [SerializeField] private Transform _target;
    [SerializeField] private int _priority;
    [SerializeField] private float _tolerance;
    [SerializeField] private bool _canMoveRootBone;

    public Transform target { get { return _target; } set { _target = value; dirty = true; } }
    public int priority { get { return _priority; } set { _priority = value; dirty = true; } }
    public float tolerance { get { return _tolerance; } set { _tolerance = value; } }
    public bool canMoveRootBone { get { return _canMoveRootBone; } set { _canMoveRootBone = value; } }
    public bool dirty { get; set; }

    public List<Transform> parents { get; set; }
    public Transform root { get; set; }
    public Transform effectiveRoot { get; set; }
}
