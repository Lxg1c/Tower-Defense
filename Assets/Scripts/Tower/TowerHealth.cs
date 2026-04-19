using UnityEngine;

public class TowerHealth : Damageable
{
    protected override void OnDeath()
    {
        Destroy(gameObject);
    }
}
