using System;
using UnityEngine;

[DefaultExecutionOrder(-200)]
[DisallowMultipleComponent]
public sealed class LevelSetup : MonoBehaviour
{
    [Tooltip("Explicit level to run when entering this scene directly in the Editor.")]
    [SerializeField] private LevelDefinition directPlayLevel;
    [SerializeField] private WaveSpawner spawner;
    [SerializeField] private PlayerWallet wallet;

    private void Awake()
    {
        try
        {
            if (spawner == null || wallet == null) throw new ArgumentException("Assign the spawner and wallet.");
            var level = RunSelection.Resolve(gameObject.scene.name, directPlayLevel);
            spawner.ConfigureLevel(level);
            wallet.ConfigureStartingCoins(level.startingCoins);
        }
        catch (ArgumentException exception)
        {
            if (spawner != null) spawner.enabled = false;
            Debug.LogError($"[LevelSetup] {exception.Message}", this);
            enabled = false;
        }
    }
}
