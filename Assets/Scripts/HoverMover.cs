using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HoverMover : MonoBehaviour
{
    public Transform Perspective;

    public string ForwardAxisName = "Vertical";
    public string StrafeAxisName = "Horizontal";

    public float HoverHeight;
    public bool HoverSmoothing;
    [Range(0, 1)] public float HoverSmoothingSpeed;

    public float Speed;

    public ForceMode RigidbodyForceMode;

    private Rigidbody Rigidbody;

	public void Start()
	{
        Rigidbody = GetComponent<Rigidbody>();
	}

	public void FixedUpdate()
    {
        Vector3 movementVector = GetInputVector() * Speed * Time.deltaTime;
        Vector3 position = GetPosition();

        Terrain t = Terrain.activeTerrain;
        movementVector.y = (t.SampleHeight(position) + t.GetPosition().y + HoverHeight) - position.y;
        if(HoverSmoothing)
            movementVector.y *= HoverSmoothingSpeed;

		if(Rigidbody == null)
            transform.Translate(movementVector);
        else if(Rigidbody.isKinematic)
            Rigidbody.MovePosition(movementVector + position);
        else
            Rigidbody.AddForce(movementVector, RigidbodyForceMode);
    }

	public Vector3 GetInputVector()
    {
        Vector3 inputVector = (Perspective.forward * Input.GetAxis(ForwardAxisName))
                            + (Perspective.right * Input.GetAxis(StrafeAxisName));
        inputVector.y = 0;
        return inputVector.normalized;
    }

	public Vector3 GetPosition() => Rigidbody == null ? transform.position : Rigidbody.position;
}
