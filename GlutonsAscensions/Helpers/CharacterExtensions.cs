using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;

namespace GlutonsAscensions.Helpers;

public static partial class CharacterExtensions {
    [GeneratedRegex(@"^\[sine\]\[(\w+)\]")]
    private static partial Regex AromaColorCodePattern();

    extension(CharacterModel characterModel) {
        public string GetAromaColorCode() {
            var aromaPrinciple = new LocString("characters", characterModel.Id.Entry + ".aromaPrinciple");
            var match = AromaColorCodePattern().Match(aromaPrinciple.GetRawText());
            return match.Groups[1].Value;
        }
        
        public CharacterStats? Stats => SaveManager.Instance.Progress.CharacterStats.GetValueOrDefault(characterModel.Id);
    }

    public static CharacterModel? SelectedCharacter(this NAscensionPanel ascensionPanel) =>
        ascensionPanel.GetParent() is NCharacterSelectScreen { _selectedButton._character: { } selectedCharacter }
            ? selectedCharacter
            : null;
}