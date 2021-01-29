using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(IKLeaf))]
public class IKLeafStepper : MonoBehaviour
{
    public Transform ActiveTarget;
    public Transform IdealTarget;

    public float StepLength;
    public float StepHeight;
    public float StepSpeed;
    public float StepTolerance;

    public float StepLengthSquared => StepLength * StepLength;
    public float StepToleranceSquared => StepTolerance * StepTolerance;

    private IKLeaf Leaf;
    private Transform WorkingTarget;
    private bool IsStepping;

    public void Start()
    {
        Leaf = GetComponent<IKLeaf>();

        if(ActiveTarget == null)
        {
            ActiveTarget = new GameObject($"{Leaf.name} IKLeaf Target").transform;
            ActiveTarget.position = Leaf.transform.position;
        }

        Leaf.Target = ActiveTarget;

        WorkingTarget = new GameObject($"{Leaf.name} Working Target").transform;
        WorkingTarget.position = IdealTarget.position;
    }

    public void FixedUpdate()
    {
        UpdateWorkingTarget();
        Vector3 difference = IdealTarget.position - ActiveTarget.position;
        if(difference.sqrMagnitude > StepLengthSquared)
            IsStepping = true;

        if(IsStepping)
        {
            difference = WorkingTarget.position - ActiveTarget.position;
            float distanceSquared = StepSpeed * StepSpeed * Time.deltaTime * Time.deltaTime;

            if(distanceSquared >= difference.sqrMagnitude)
            {
                ActiveTarget.position = WorkingTarget.position;
                IsStepping = false;
            }
            else
            {
                ActiveTarget.Translate(difference.normalized * StepSpeed * Time.deltaTime);
                if(difference.sqrMagnitude - distanceSquared <= StepToleranceSquared)
                    IsStepping = false;
            }
        }
    }

    public void UpdateWorkingTarget()
    {
        Vector3 idealPosition = IdealTarget.position;
        if(ActiveTarget.position.y <= idealPosition.y)
            idealPosition.y += StepHeight * 2;
        else if(ActiveTarget.position.y < idealPosition.y + StepHeight)
            idealPosition.y = WorkingTarget.position.y;

        WorkingTarget.position = idealPosition;
    }
}
