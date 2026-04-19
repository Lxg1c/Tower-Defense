using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class PhaseDisplay : MonoBehaviour
{
    [SerializeField] private WaveManager waveManager;
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button startWaveButton;
    [SerializeField] private string prepText = "Press to start wave!";
    [SerializeField] private string waveFormat = "Wave {0}";
    [SerializeField] private string victoryText = "Victory!";

    private void Start()
    {
        if (startWaveButton != null)
            startWaveButton.onClick.AddListener(OnStartWaveClicked);
    }

    private void Update()
    {
        if (waveManager == null || label == null)
            return;

        switch (waveManager.CurrentPhase)
        {
            case WaveManager.Phase.Prep:
                label.text = prepText;
                if (startWaveButton != null)
                    startWaveButton.gameObject.SetActive(true);
                break;
            case WaveManager.Phase.Combat:
                label.text = string.Format(waveFormat, waveManager.CurrentWave);
                if (startWaveButton != null)
                    startWaveButton.gameObject.SetActive(false);
                break;
            case WaveManager.Phase.Finished:
                label.text = victoryText;
                if (startWaveButton != null)
                    startWaveButton.gameObject.SetActive(false);
                break;
        }
    }

    private void OnStartWaveClicked()
    {
        if (waveManager != null)
            waveManager.StartNextWave();
    }
}
