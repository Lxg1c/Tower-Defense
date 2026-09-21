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
    [SerializeField] private TMP_Text actionLabel;
    [SerializeField] private TMP_Text levelStatus;
    [SerializeField] private RectTransform levelContent;
    [SerializeField] private BuildingLevelRow levelRowPrefab;
    [SerializeField] private GameObject damageIcon;
    [SerializeField] private TMP_Text incomeHeader;

    [Tooltip("Format used for the cost label. {0} = cost.")]
    [SerializeField] private string costFormat = "{0}";

    public void Bind(TowerOption option, bool affordable, Action onClick)
        => Bind(option, 0, option.cost, "BUILD", affordable, onClick);

    public void Bind(TowerOption option, int currentLevel, int? cost, string action,
        bool available, Action onClick)
    {
        if (iconImage != null)
        {
            iconImage.sprite  = option.icon;
            iconImage.enabled = option.icon != null;
        }
        if (nameLabel != null) nameLabel.text = option.displayName;
        if (costLabel != null) costLabel.text = cost.HasValue ? string.Format(costFormat, cost.Value) : "";
        if (actionLabel != null) actionLabel.text = action;
        if (levelStatus != null) levelStatus.text = currentLevel == 0 ? "LEVEL PROGRESSION" : $"CURRENT LEVEL {currentLevel}";
        if (levelContent != null && levelRowPrefab != null)
        {
            foreach (Transform child in levelContent)
                if (child != levelRowPrefab.transform) { child.gameObject.SetActive(false); Destroy(child.gameObject); }
            var levels = BuildingLevelList.Read(option);
            bool income = levels[0].Income.HasValue;
            if (damageIcon != null) damageIcon.SetActive(!income);
            if (incomeHeader != null) incomeHeader.gameObject.SetActive(income);
            for (int i = 0; i < levels.Count; i++)
            {
                var row = Instantiate(levelRowPrefab, levelContent);
                row.gameObject.SetActive(true);
                row.Bind(i + 1, levels[i], currentLevel, income);
            }
        }

        if (button != null)
        {
            button.interactable = available;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => onClick?.Invoke());
        }
    }
}
