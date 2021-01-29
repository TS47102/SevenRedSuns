using System;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class OrbitalCamera : MonoBehaviour
{
    [Serializable]
    public class InputSettings
    {
        [Tooltip("The axis to use for rotating the view around.")]
        public string horizontalAxisName = "Mouse X";

        [Tooltip("The axis to use for looking up and down.")]
        public string verticalAxisName = "Mouse Y";

        [Tooltip("Whether or not to invert the horizontal axis.")]
        public bool invertHorizontal = false;

        [Tooltip("Whether or not to invert the vertical axis.")]
        public bool invertVertical = false;

        [Tooltip("The camera's movement speed.")]
        public float sensitivity = 0f;

        [Tooltip("Whether or not camera movement will be smoothed.")]
        public bool smoothing = false;

        [Tooltip("How fast the smoothing is, if enabled."), Range(0, 1)]
        public float smoothSpeed = 0f;
    }

    [Serializable]
    public class BehaviourSettings
    {
        [Tooltip("The constant distance the camera will keep from the focus.")]
        public float orbitRadius = 0f;

        [Tooltip("How close, in Degrees, that the camera is allowed to get to pointing vertically up or down.")]
        public float verticalThreshhold = 0f;

        [Tooltip("How the Camera will try to avoid clipping into Colliders.")]
        public ClipAvoidance clipAvoidance = ClipAvoidance.KeepLineOfSight;

        [Tooltip("The minimum distance the Camera will be kept from Colliders by the Clip Avoidance.")]
        public float clipRadius = 0.5f;

        [Tooltip("The Layers that the Clip Avoidance interacts with.")]
        public LayerMask clipMask = -1;

        [Tooltip("How the Clip Avoidance interacts with Triggers.")]
        public QueryTriggerInteraction clipTriggerInteraction;
    }

    [Serializable]
    public enum ClipAvoidance
    {
        Disabled,
        Enabled,
        KeepLineOfSight
    }

    [Tooltip("The transform that the camera will orbit and track.")]
    public Transform orbitFocus;
    public InputSettings input;
    public BehaviourSettings behaviour;

    [NonSerialized] public Quaternion currentOrientation;
    [NonSerialized] public Quaternion targetOrientation;
    [NonSerialized] public bool dirty;

    public void Start()
    {
        targetOrientation = orbitFocus.rotation;
        dirty = true;
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
            targetOrientation = Quaternion.AngleAxis(rotation.x, Vector3.up) * targetOrientation;

            if(rotation.y > 0)
                rotation.y = Math.Min(rotation.y, Math.Abs(targetOrientation.eulerAngles.x - 270) - behaviour.verticalThreshhold);
            else if(rotation.y < 0)
                rotation.y = Math.Max(rotation.y, behaviour.verticalThreshhold - Math.Abs(90 - targetOrientation.eulerAngles.x));

            targetOrientation = Quaternion.AngleAxis(rotation.y, transform.right) * targetOrientation;
        }

        if(currentOrientation != targetOrientation)
        {
            currentOrientation = input.smoothing ? Quaternion.Slerp(currentOrientation, targetOrientation, input.smoothSpeed) : targetOrientation;
            dirty = true;
        }
    }

    public Vector2 GetInputVector()
    {
        return new Vector2(Input.GetAxis(input.horizontalAxisName) * input.sensitivity * (input.invertHorizontal ? -1 : 1),
                           Input.GetAxis(input.verticalAxisName) * input.sensitivity * (input.invertVertical ? 1 : -1));
    }

    public void UpdateTransform()
    {
        if(orbitFocus.hasChanged)
        {
            dirty = true;
            orbitFocus.hasChanged = false;
        }

        switch(behaviour.clipAvoidance)
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
                throw new InvalidOperationException($"Invalid {nameof(ClipAvoidance)} setting: '{behaviour.clipAvoidance}'!");
        }
    }

    private void UpdateTransformDisabled()
    {
        if(dirty)
        {
            UpdatePosAndLook(orbitFocus.position + (currentOrientation * Vector3.forward * behaviour.orbitRadius));
            dirty = false;
        }
    }

    private void UpdateTransformEnabled()
    {
        Vector3 direction = currentOrientation * Vector3.forward;
        Vector3 targetPos = orbitFocus.position + (direction * behaviour.orbitRadius);

        bool wouldOverlap = Physics.CheckSphere(targetPos,
                            behaviour.clipRadius,
                            behaviour.clipMask,
                            behaviour.clipTriggerInteraction);

        if(wouldOverlap)
        {
            RaycastHit[] results = Physics.SphereCastAll(orbitFocus.position,
                                                             behaviour.clipRadius,
                                                             direction,
                                                             behaviour.orbitRadius,
                                                             behaviour.clipMask,
                                                             behaviour.clipTriggerInteraction);

            RaycastHit? ideal = results.OrderByDescending(hit => hit.distance)
                                           .Cast<RaycastHit?>()
                                           .FirstOrDefault(hit => !Physics.OverlapSphere(hit.Value.point + behaviour.clipRadius * hit.Value.normal,
                                                                   behaviour.clipRadius,
                                                                   behaviour.clipMask,
                                                                   behaviour.clipTriggerInteraction)
                                                                   .Any(collider => collider != hit.Value.collider));

            UpdatePosAndLook(ideal.HasValue ? ideal.Value.point + behaviour.clipRadius * ideal.Value.normal : orbitFocus.position);
        }
        else if(dirty)
            UpdatePosAndLook(targetPos);

        dirty = wouldOverlap;
    }

    private void UpdateTransformKeepLoS()
    {
        Vector3 direction = currentOrientation * Vector3.forward;
        bool LoSBroken = Physics.SphereCast(orbitFocus.position,
                                behaviour.clipRadius,
                                direction,
                                out RaycastHit hitResult,
                                behaviour.orbitRadius,
                                behaviour.clipMask,
                                behaviour.clipTriggerInteraction);

        if(LoSBroken)
            UpdatePosAndLook(hitResult.point + behaviour.clipRadius * hitResult.normal);
        else if(dirty)
            UpdatePosAndLook(orbitFocus.position + (direction * behaviour.orbitRadius));

        dirty = LoSBroken;
    }

    private void UpdatePosAndLook(Vector3 newPos)
    {
        transform.position = newPos;
        if(newPos != orbitFocus.position)
            transform.LookAt(orbitFocus);
    }
}
