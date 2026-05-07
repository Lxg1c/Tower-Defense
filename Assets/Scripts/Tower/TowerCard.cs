using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single card in the tower selection modal. Attach to the card prefab root
/// and wire its inspector slots — modal calls <see cref="Bind"/> with data.
/// </summary>
[DisallowMultipleComponent]
public class TowerCard : MonoBehaviour
{
    [SerializeField] private Image    iconImage;
    [SerializeField] private TMP_Text nameLabel;
    [SerializeField] private TMP_Text costLabel;
    [SerializeField] private Button   button;

    [Tooltip("Format used for the cost label. {0} = cost.")]
    [SerializeField] private string costFormat = "{0}";

    public void Bind(TowerOption option, bool affordable, Action onClick)
    {
        if (iconImage != null)
        {
            iconImage.sprite  = option.icon;
            iconImage.enabled = option.icon != null;
        }
        if (nameLabel != null) nameLabel.text = option.displayName;
        if (costLabel != null) costLabel.text = string.Format(costFormat, option.cost);

        if (button != null)
        {
            button.interactable = affordable;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
