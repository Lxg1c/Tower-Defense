using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class BuildingLevelRow : MonoBehaviour
{
    [SerializeField] private TMP_Text levelLabel;
    [SerializeField] private TMP_Text healthLabel;
    [SerializeField] private TMP_Text damageLabel;
    [SerializeField] private Image background;

    public void Bind(int level, BuildingLevelList.Level stats, int current, bool income)
    {
        levelLabel.text = level.ToString();
        healthLabel.text = Number(stats.Health);
        damageLabel.text = Number(income ? stats.Income : stats.Damage);
        var color = level == current ? new Color(.18f, .91f, .91f)
            : level == current + 1 ? new Color(.91f, .96f, .97f) : new Color(.55f, .7f, .76f);
        levelLabel.color = healthLabel.color = damageLabel.color = color;
        background.color = level == current ? new Color(.07f, .32f, .36f, 1)
            : new Color(.07f, .18f, .22f, level % 2 == 0 ? .6f : .25f);
    }

    private static string Number(float? value) => value.HasValue
        ? value.Value.ToString("0.#", CultureInfo.InvariantCulture) : "—";
}
