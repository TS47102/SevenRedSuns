using System;
using System.Linq;
using UnityEngine;

namespace SevenRedSuns
{
    [RequireComponent(typeof(Camera))]
    public class OrbitalCamera : MonoBehaviour
    {
        [Serializable]
        public class InputSettings
        {
            [Tooltip("The axis to use for rotating the view around.")]
            public string HorizontalAxisName = "Mouse X";

            [Tooltip("The axis to use for looking up and down.")]
            public string VerticalAxisName = "Mouse Y";

            [Tooltip("Whether or not to invert the horizontal axis.")]
            public bool InvertHorizontal = false;

            [Tooltip("Whether or not to invert the vertical axis.")]
            public bool InvertVertical = false;

            [Tooltip("The camera's movement speed.")]
            public float Sensitivity = 0f;

            [Tooltip("Whether or not camera movement will be smoothed.")]
            public bool Smoothing = false;

            [Tooltip("How fast the smoothing is, if enabled."), Range(0, 1)]
            public float SmoothSpeed = 0f;
        }

        [Serializable]
        public class BehaviourSettings
        {
            [Tooltip("The constant distance the camera will keep from the focus.")]
            public float OrbitRadius = 0f;

            [Tooltip("How close, in Degrees, that the camera is allowed to get to pointing vertically up or down.")]
            public float VerticalThreshhold = 0f;

            [Tooltip("How the Camera will try to avoid clipping into Colliders.")]
            public ClipAvoidance ClipAvoidance = ClipAvoidance.KeepLineOfSight;

            [Tooltip("The minimum distance the Camera will be kept from Colliders by the Clip Avoidance.")]
            public float ClipRadius = 0.5f;

            [Tooltip("The Layers that the Clip Avoidance interacts with.")]
            public LayerMask ClipMask = -1;

            [Tooltip("How the Clip Avoidance interacts with Triggers.")]
            public QueryTriggerInteraction ClipTriggerInteraction;
        }

        [Serializable]
        public enum ClipAvoidance
        {
            Disabled,
            Enabled,
            KeepLineOfSight
        }

        [Tooltip("The transform that the camera will orbit and track.")]
        public Transform OrbitFocus;
        public InputSettings Input;
        public BehaviourSettings Behaviour;

        [NonSerialized] public Quaternion CurrentOrientation;
        [NonSerialized] public Quaternion TargetOrientation;
        [NonSerialized] public bool Dirty;

        public void Start()
        {
            TargetOrientation = OrbitFocus.rotation;
            Dirty = true;
            Update();
        }

        public void Update()
        {
            UpdateOrientation();
            UpdateTransform();
        }

        public void UpdateOrientation()
        {
            Vector2 rotation = GetInputVector();

            if(rotation != Vector2.zero)
            {
                TargetOrientation = Quaternion.AngleAxis(rotation.x, Vector3.up) * TargetOrientation;

                if(rotation.y > 0)
                    rotation.y = Math.Min(rotation.y, Math.Abs(TargetOrientation.eulerAngles.x - 270) - Behaviour.VerticalThreshhold);
                else if(rotation.y < 0)
                    rotation.y = Math.Max(rotation.y, Behaviour.VerticalThreshhold - Math.Abs(90 - TargetOrientation.eulerAngles.x));

                TargetOrientation = Quaternion.AngleAxis(rotation.y, transform.right) * TargetOrientation;
            }

            if(CurrentOrientation != TargetOrientation)
            {
                CurrentOrientation = Input.Smoothing ? Quaternion.Slerp(CurrentOrientation, TargetOrientation, Input.SmoothSpeed) : TargetOrientation;
                Dirty = true;
            }
        }

        public Vector2 GetInputVector()
        {
            return new Vector2(UnityEngine.Input.GetAxis(Input.HorizontalAxisName) * Input.Sensitivity * (Input.InvertHorizontal ? -1 : 1),
                               UnityEngine.Input.GetAxis(Input.VerticalAxisName) * Input.Sensitivity * (Input.InvertVertical ? 1 : -1));
        }

        public void UpdateTransform()
        {
            if(OrbitFocus.hasChanged)
            {
                Dirty = true;
                OrbitFocus.hasChanged = false;
            }

            switch(Behaviour.ClipAvoidance)
            {
                case ClipAvoidance.Disabled:
                    UpdateTransformDisabled();
                    break;
                case ClipAvoidance.Enabled:
                    UpdateTransformEnabled();
                    break;
                case ClipAvoidance.KeepLineOfSight:
                    UpdateTransformKeepLoS();
                    break;
                default:
                    throw new InvalidOperationException($"Invalid {nameof(ClipAvoidance)} setting: '{Behaviour.ClipAvoidance}'!");
            }
        }

        private void UpdateTransformDisabled()
        {
            if(Dirty)
            {
                UpdatePosAndLook(OrbitFocus.position + (CurrentOrientation * Vector3.forward * Behaviour.OrbitRadius));
                Dirty = false;
            }
        }

        private void UpdateTransformEnabled()
        {
            Vector3 direction = CurrentOrientation * Vector3.forward;
            Vector3 targetPos = OrbitFocus.position + (direction * Behaviour.OrbitRadius);

            bool wouldOverlap = Physics.CheckSphere(targetPos,
                                                    Behaviour.ClipRadius,
                                                    Behaviour.ClipMask,
                                                    Behaviour.ClipTriggerInteraction);

            if(wouldOverlap)
            {
                RaycastHit[] results = Physics.SphereCastAll(OrbitFocus.position,
                                                                Behaviour.ClipRadius,
                                                                direction,
                                                                Behaviour.OrbitRadius,
                                                                Behaviour.ClipMask,
                                                                Behaviour.ClipTriggerInteraction);

                RaycastHit? ideal = results.OrderByDescending(hit => hit.distance)
                                           .Cast<RaycastHit?>()
                                           .FirstOrDefault(hit => !Physics.OverlapSphere(hit.Value.point + Behaviour.ClipRadius * hit.Value.normal,
                                                                                            Behaviour.ClipRadius,
                                                                                            Behaviour.ClipMask,
                                                                                            Behaviour.ClipTriggerInteraction)
                                                                   .Any(collider => collider != hit.Value.collider));

                UpdatePosAndLook(ideal.HasValue ? ideal.Value.point + Behaviour.ClipRadius * ideal.Value.normal : OrbitFocus.position);
            }
            else if(Dirty)
                UpdatePosAndLook(targetPos);

            Dirty = wouldOverlap;
        }

        private void UpdateTransformKeepLoS()
        {
            Vector3 direction = CurrentOrientation * Vector3.forward;
            bool LoSBroken = Physics.SphereCast(OrbitFocus.position,
                                                Behaviour.ClipRadius,
                                                direction,
                                                out RaycastHit hitResult,
                                                Behaviour.OrbitRadius,
                                                Behaviour.ClipMask,
                                                Behaviour.ClipTriggerInteraction);

            if(LoSBroken)
                UpdatePosAndLook(hitResult.point + Behaviour.ClipRadius * hitResult.normal);
            else if(Dirty)
                UpdatePosAndLook(OrbitFocus.position + (direction * Behaviour.OrbitRadius));

            Dirty = LoSBroken;
        }

        private void UpdatePosAndLook(Vector3 newPos)
        {
            transform.position = newPos;
            if(newPos != OrbitFocus.position)
                transform.LookAt(OrbitFocus);
        }
    }
}
