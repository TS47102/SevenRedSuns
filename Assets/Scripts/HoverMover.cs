using UnityEngine;

namespace SevenRedSuns
{
    public class HoverMover : TerrainFollower
    {
        public Transform Perspective;
        public Transform MovementLean;

        public string ForwardAxisName = "Vertical";
        public string StrafeAxisName = "Horizontal";

        public float Speed;

        public float LeanAmount;

        [Range(0, 1)] public float TurnSmoothingSpeed;

        public override void FixedUpdate()
        {
            Vector3 inputVector = GetInputVector();
            Vector3 movementVector = inputVector * Speed * Time.deltaTime;
            movementVector.y = GetHeightOffset(Terrain.activeTerrain, transform.position);

            Vector3 look = Perspective.forward;
            look.y = 0;

            if(MovementLean != null)
                MovementLean.localPosition = MovementLean.InverseTransformDirection(inputVector * LeanAmount);

            transform.Translate(movementVector, Space.World);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(look), TurnSmoothingSpeed);
        }

        public Vector3 GetInputVector()
        {
            Vector3 inputVector = (Perspective.forward * Input.GetAxis(ForwardAxisName))
                                + (Perspective.right * Input.GetAxis(StrafeAxisName));
            inputVector.y = 0;
            return inputVector.normalized;
        }
    }
}
