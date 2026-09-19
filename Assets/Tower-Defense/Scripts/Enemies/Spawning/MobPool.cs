using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Per-prefab mob pool. Reuses instances to avoid allocations during heavy waves.
/// The spawner is the only owner; mobs report death via MobCore.OnDeathHandled
/// and the spawner returns them here.
/// </summary>
public class MobPool
{
    private readonly MobCore prefab;
    private readonly Transform parent;
    private readonly Stack<MobCore> idle = new();

    public MobPool(MobCore prefab, Transform parent = null)
    {
        this.prefab = prefab;
        this.parent = parent;
    }

    public MobCore Get(Vector3 position, Quaternion rotation)
    {
        MobCore instance = null;
        while (idle.Count > 0 && instance == null)
            instance = idle.Pop();

        if (instance == null)
        {
            instance = Object.Instantiate(prefab, position, rotation, parent);
        }
        else
        {
            instance.transform.SetPositionAndRotation(position, rotation);
        }

        instance.gameObject.SetActive(true);
        return instance;
    }

    public void Return(MobCore instance)
    {
        if (instance == null) return;
        instance.gameObject.SetActive(false);
        idle.Push(instance);
    }

    public void Prewarm(int count, Vector3 position)
    {
        for (int i = 0; i < count; i++)
        {
            var inst = Object.Instantiate(prefab, position, Quaternion.identity, parent);
            inst.gameObject.SetActive(false);
            idle.Push(inst);
        }
    }
}
