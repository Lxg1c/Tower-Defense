using UnityEngine;

[DisallowMultipleComponent]
public class InputManager : MonoBehaviour
{
    public static InputManager Instance { get; private set; }
    public InputSystem_Actions actions;
    private bool restorePlayerMap = true;
    private bool restoreUiMap = true;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Init()
    {
        if (Instance != null ) return;

        GameObject go = new GameObject("InputManager");
        Instance = go.AddComponent<InputManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        if (Instance != null && Instance != this) return;
        Instance = this;
        actions ??= new InputSystem_Actions();
        if (restorePlayerMap) actions.Player.Enable();
        if (restoreUiMap) actions.UI.Enable();
    }

    private void OnDisable()
    {
        if (actions == null) return;
        restorePlayerMap = actions.Player.enabled;
        restoreUiMap = actions.UI.enabled;
        actions.Disable();
    }

    private void OnDestroy()
    {
        actions?.Disable();
        actions?.Dispose();
        actions = null;
        if (Instance == this) Instance = null;
    }

    public void ChangeInputMap(InputType inputType)
    {
        switch(inputType)
        {
            case InputType.Player:
            {
                actions.Player.Enable();
                actions.UI.Disable();
                break;
            }
            case InputType.UI:
            {
                actions.UI.Enable();
                actions.Player.Disable();
                break;
            }
        }
    }

    public enum InputType
    {
        Player, UI
    }
}
