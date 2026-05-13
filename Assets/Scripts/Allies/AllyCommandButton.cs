using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class AllyCommandButton : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private GameObject activeRoot;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        if (button != null)
            button.onClick.AddListener(AllyCommand.ToggleFollowPlayer);
    }

    private void OnEnable()
    {
        AllyCommand.OnFollowPlayerChanged += Refresh;
        Refresh(AllyCommand.FollowPlayer);
    }

    private void OnDisable()
    {
        AllyCommand.OnFollowPlayerChanged -= Refresh;
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(AllyCommand.ToggleFollowPlayer);
    }

    private void Refresh(bool followPlayer)
    {
        if (activeRoot != null)
            activeRoot.SetActive(followPlayer);

    }
}
