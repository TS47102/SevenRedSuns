using UnityEngine;

public class TerrainFollower : MonoBehaviour
{
    public float HoverHeight;
    [Range(0, 1)] public float HoverSmoothingSpeed;

    public virtual void FixedUpdate()
    {
        Vector3 movementVector = new Vector3(0, GetHeightOffset(Terrain.activeTerrain, transform.position));
        transform.Translate(movementVector, Space.World);
    }

	public float GetHeightOffset(Terrain t, Vector3 position) => (t.SampleHeight(position) + t.GetPosition().y + HoverHeight - position.y) * HoverSmoothingSpeed;
}
