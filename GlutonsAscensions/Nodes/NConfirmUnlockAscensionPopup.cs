using GlutonsAscensions.Helpers;
using Godot;
using MegaCrit.Sts2.Core.Assets;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.GodotExtensions;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Nodes.Screens.ScreenContext;
using MegaCrit.Sts2.Core.TestSupport;

namespace GlutonsAscensions.Nodes;

using static GlutonsAscensionsMod;

public partial class NConfirmUnlockAscensionPopup : NVerticalPopup, IScreenContext {
    private int _ascensionLevel;
    private string _targetString = "";
    private NAscensionPanel? _ascensionPanel;
    
    private NVerticalPopup _verticalPopup = PreloadManager.Cache.GetScene(_scenePath).Instantiate<NVerticalPopup>();

    public Control? DefaultFocusedControl => null;
    
    public NConfirmUnlockAscensionPopup() {}

    private NConfirmUnlockAscensionPopup(
        int ascensionLevel,
        NAscensionPanel? ascensionPanel,
        string targetString
    ) {
        AddChild(_verticalPopup);
        
        _ascensionLevel = ascensionLevel;
        _ascensionPanel = ascensionPanel;
        _targetString = targetString;
        
        SetAnchorsPreset(LayoutPreset.FullRect);
        
        _verticalPopup.SetAnchorsPreset(LayoutPreset.Center);
    }

    public override void _Ready() {
        var body = ModLocString("main_menu_ui", "CONFIRM_UNLOCK_ASCENSION_POPUP.body");
        body.Add("Level", _ascensionLevel);
        body.Add("Target", _targetString);
        
        _verticalPopup.SetText(
            ModLocString("main_menu_ui", "CONFIRM_UNLOCK_ASCENSION_POPUP.title"),
            body
        );
        _verticalPopup.InitYesButton(ModLocString("main_menu_ui", "CONFIRM_UNLOCK_ASCENSION_POPUP.yes"), OnYesButtonPressed);
        _verticalPopup.InitNoButton(ModLocString("main_menu_ui", "CONFIRM_UNLOCK_ASCENSION_POPUP.no"), OnNoButtonPressed);
        
        Logger.Info($"Size: {Size}");
        Logger.Info($"Position: {Position}");
        Logger.Info($"_verticalPopup.Size: {_verticalPopup.Size}");
        Logger.Info($"_verticalPopup.Position: {_verticalPopup.Position}");
    }

    private void OnNoButtonPressed(NButton _) {
        this.QueueFreeSafely();
        NModalContainer.Instance?.HideBackstop();
    }

    private void OnYesButtonPressed(NButton _) {
        this.QueueFreeSafely();
        NModalContainer.Instance?.HideBackstop();
        _ascensionPanel?.UnlockAscension(_ascensionLevel);
        _ascensionPanel?.SetMaxAscension(_ascensionLevel);
        _ascensionPanel?.SetAscensionLevel(_ascensionLevel);
    }
    
    public static NConfirmUnlockAscensionPopup? Create(
        int ascensionLevel,
        string targetString,
        NAscensionPanel ascensionPanel
    ) => TestMode.IsOn ? null : new NConfirmUnlockAscensionPopup(ascensionLevel, ascensionPanel, targetString);
}