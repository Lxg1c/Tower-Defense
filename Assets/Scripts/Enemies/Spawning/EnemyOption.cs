using UnityEngine;

[CreateAssetMenu(menuName = "Enemies/Option", fileName = "EnemyOption")]
public class EnemyOption : ScriptableObject
{
    [Tooltip("Enemy prefab spawned by waves.")]
    public MobCore prefab;

    [Tooltip("Name shown in editor and optional UI.")]
    public string displayName;

    [Tooltip("Icon shown in wave direction preview.")]
    public Sprite icon;
}
