using UnityEngine;

/// <summary>
/// Attached at spawn time to remember which pool an instance came from,
/// so it can be returned without a dictionary lookup by prefab reference.
/// </summary>
public class MobPoolMember : MonoBehaviour
{
    public MobPool Pool;
}
