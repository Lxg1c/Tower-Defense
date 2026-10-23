using TMPro;
using UnityEngine;

/// <summary>Displays the three permanent upgrades in the main menu.</summary>
public sealed class SkillTreePanel : MonoBehaviour
{
    [SerializeField] private MainMenuScreens menu;
    [SerializeField] private UnityEngine.UI.Button backButton;
    [SerializeField] private TMP_Text pointsLabel;
    [SerializeField] private UnityEngine.UI.Button[] upgradeButtons;
    [SerializeField] private TMP_Text[] details;
    [SerializeField] private TMP_Text[] prices;

    private static readonly PlayerSkill[] Skills =
        { PlayerSkill.Vitality, PlayerSkill.Firepower, PlayerSkill.RapidFire };
    private static readonly string[] Names = { "VITALITY", "FIREPOWER", "RAPID FIRE" };
    private static readonly string[] Bonuses = { "+15% MAX HP", "+10% SHOT DAMAGE", "+8% FIRE RATE" };

    private void OnEnable()
    {
        if (menu == null || backButton == null || pointsLabel == null ||
            upgradeButtons == null || upgradeButtons.Length != Skills.Length ||
            details == null || details.Length != Skills.Length ||
            prices == null || prices.Length != Skills.Length)
        {
            Debug.LogError("[SkillTreePanel] Assign the menu, Back button and three skill rows.", this);
            enabled = false;
            return;
        }

        for (int i = 0; i < Skills.Length; i++)
        {
            if (upgradeButtons[i] == null || details[i] == null || prices[i] == null)
            {
                Debug.LogError("[SkillTreePanel] Each skill needs a button, description and price.", this);
                enabled = false;
                return;
            }
        }

        upgradeButtons[0].onClick.AddListener(BuyVitality);
        upgradeButtons[1].onClick.AddListener(BuyFirepower);
        upgradeButtons[2].onClick.AddListener(BuyRapidFire);
        backButton.onClick.AddListener(menu.ShowHome);
        PlayerProgression.Changed += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (upgradeButtons != null && upgradeButtons.Length == Skills.Length)
        {
            if (upgradeButtons[0] != null) upgradeButtons[0].onClick.RemoveListener(BuyVitality);
            if (upgradeButtons[1] != null) upgradeButtons[1].onClick.RemoveListener(BuyFirepower);
            if (upgradeButtons[2] != null) upgradeButtons[2].onClick.RemoveListener(BuyRapidFire);
        }
        if (backButton != null && menu != null) backButton.onClick.RemoveListener(menu.ShowHome);
        PlayerProgression.Changed -= Refresh;
    }

    private void BuyVitality() => Buy(PlayerSkill.Vitality);
    private void BuyFirepower() => Buy(PlayerSkill.Firepower);
    private void BuyRapidFire() => Buy(PlayerSkill.RapidFire);
    private void Buy(PlayerSkill skill) => PlayerProgression.TryUpgrade(skill);

    private void Refresh()
    {
        SkillTreeState state = PlayerProgression.State;
        pointsLabel.text = $"MONEY  {state.Money}";
        for (int i = 0; i < Skills.Length; i++)
        {
            int rank = state.Rank(Skills[i]);
            int cost = state.NextCost(Skills[i]);
            bool unlocked = state.IsUnlocked(Skills[i]);
            details[i].text = $"{Names[i]}   {rank}/{SkillTreeState.MaxRank}\n" +
                (unlocked ? $"{Bonuses[i]} PER RANK" : "REQUIRES VITALITY 1");
            prices[i].text = cost == 0 ? "MAX" : unlocked ? $"UPGRADE  ·  {cost}" : "LOCKED";
            upgradeButtons[i].interactable = unlocked && cost > 0 && state.Money >= cost;
        }
    }
}
