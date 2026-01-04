using UnityEngine;

public class SurfaceManager : MonoBehaviour
{
    public static SurfaceManager Instance;

    public SurfaceProfile[] surfaces;

    private void Awake()
    {
        Instance = this;
    }

    public bool TryGetSurface(PhysicsMaterial material, out SurfaceProfile profile)
    {
        profile = null;

        if (material == null)
            return false;

        foreach (var s in surfaces)
        {
            foreach (var m in s.materials)
            {
                if (m == material)
                {
                    profile = s;
                    return true;
                }
            }
        }

        return false;
    }
}

