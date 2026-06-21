using BaseLib.Utils;
using GlutonsAscensions.Helpers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Entities.UI;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Characters;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.CharacterSelect;
using MegaCrit.Sts2.Core.Saves;
using static GlutonsAscensions.GlutonsAscensionsMod;

namespace GlutonsAscensions.Nodes;

[HarmonyPatch]
internal partial class NAscensionLock : TextureRect {
    private const float ArrowUnfocusedAlpha = 0.5f;
    
    private static readonly string AscensionLockedPath = ModResource("images/ui/ascension_locked.png");
    private static readonly string AscensionUnlockPath = ModResource("images/ui/ascension_unlock.png");
    
    private static readonly SpireField<NGoldArrowButton, NAscensionLock?> AscensionLockNode = new(() => null);
    private static readonly SpireField<NGoldArrowButton, NAscensionPanel?> AscensionPanelNode = new(() => null);
    
    private bool IsLocked { get; set {
        if (field == value) return;
        field = value;
        Texture = ResourceLoader.Load<Texture2D>(value ? AscensionLockedPath : AscensionUnlockPath);
    }}

    public override void _Ready() {
        base._Ready();
        
        Name = "AscensionLock";
        Visible = false;
        IsLocked = true;
        ExpandMode = ExpandModeEnum.IgnoreSize;
        StretchMode = StretchModeEnum.KeepAspect;
        LayoutMode = 1; // Anchors
        SetAnchorsPreset(LayoutPreset.Center);
        CustomMinimumSize = new Vector2(30f, 30f);
        Size = new Vector2(30f, 30f);
        Position = new Vector2(34f, 64f);
        PivotOffset = new Vector2(-6f, -6f);
    }

    private static void AttachLock(NGoldArrowButton arrowButton, NAscensionPanel ascensionPanel) {
        var ascensionLock = new NAscensionLock();
        arrowButton._icon.AddChild(ascensionLock);
        AscensionLockNode[arrowButton] = ascensionLock;
        AscensionPanelNode[arrowButton] = ascensionPanel;
    }

    private static void EnableLock(NGoldArrowButton arrowButton) {
        if (AscensionLockNode[arrowButton] is not { } ascensionLock) throw new Exception("Tried to enable ascension lock on button that does not have one");

        ascensionLock.Visible = true; // show lock
        arrowButton.Visible = true; // force visible
        arrowButton.SetModulateAlpha(ArrowUnfocusedAlpha); // set unfocused alpha
    }

    private static void DisableLock(NGoldArrowButton arrowButton) {
        if (AscensionLockNode[arrowButton] is not { } ascensionLock) throw new Exception("Tried to disable ascension lock on button that does not have one");
        
        if (!ascensionLock.Visible) return;
        
        ascensionLock.Visible = false; // hide lock
        arrowButton.SetModulateAlpha(1.0f); // reset alpha
        NHoverTipSet.Remove(arrowButton); // clear hover tips
    }
 
    [HarmonyPatch(typeof(NGoldArrowButton), "OnFocus")]
    [HarmonyPostfix]
    static void OnFocusPostfix(NGoldArrowButton __instance) {
        if (AscensionLockNode[__instance] is not { Visible: true } ascensionLock) return;
        
        ascensionLock.IsLocked = false;
        OnArrowFocused(__instance);
    }   
    
    [HarmonyPatch(typeof(NGoldArrowButton), "OnUnfocus")]
    [HarmonyPostfix]
    static void OnUnfocusPostfix(NGoldArrowButton __instance) {
        if (AscensionLockNode[__instance] is not { Visible: true } ascensionLock) return;
        
        ascensionLock.IsLocked = true;
        OnArrowUnfocused(__instance);
    }
       
    private static void OnArrowFocused(NGoldArrowButton arrowButton) {
        if (AscensionPanelNode[arrowButton] is not { } ascensionPanel) {
            throw new Exception("Tried to focus arrow button with ascension lock that does not have an ascension panel");
        }
        
        arrowButton.SetModulateAlpha(1f);

        if (CreateHoverTip(ascensionPanel) is { } hoverTip) {
            var hoverTipSet = NHoverTipSet.CreateAndShow(arrowButton, hoverTip, HoverTipAlignment.Right);
            foreach (var child in hoverTipSet?._textHoverTipContainer.GetChildren() ?? []) {
                child.GetNodeOrNull<MegaRichTextLabel>("%Description")?.AddThemeFontSizeOverride("bold_font_size", 22);
            }
        }
    }
    
    private static void OnArrowUnfocused(NGoldArrowButton arrowButton) {
        arrowButton.SetModulateAlpha(ArrowUnfocusedAlpha);

        NHoverTipSet.Remove(arrowButton);
    }

    private static HoverTip? CreateHoverTip(NAscensionPanel ascensionPanel) {
        var target = ascensionPanel._mode switch {
            MultiplayerUiMode.Host => null,
            MultiplayerUiMode.Singleplayer when ascensionPanel.SelectedCharacter() is { } selectedCharacter => selectedCharacter,
            _ => null
        };
        
        if (ascensionPanel._mode is not MultiplayerUiMode.Host && target is null) return null;
        
        var description = ModLocString("main_menu_ui", "UNLOCK_ASCENSION_ARROW.description");
        description.Add("Level", GlutonsAscensionLevel.FirstGlutonAscensionLevel);
        description.Add("Target", GetTargetString(target));
        
        return new HoverTip(description);
    }
    
    private static string GetTargetString(CharacterModel? targetCharacter) {
        if (targetCharacter is null) {
            var multiplayerString = new LocString("main_menu_ui", "MULTIPLAYER").GetFormattedText();
            return $"[gold]{multiplayerString}[/gold]";
        } else {
            var bbCode = targetCharacter is RandomCharacter ? "jitter" : targetCharacter.GetAromaColorCode();
            return $"[{bbCode}]{targetCharacter.Title.GetFormattedText()}[/{bbCode}]";
        }
    }
    
    [HarmonyPatch(typeof(NAscensionPanel), nameof(NAscensionPanel._Ready))]
    [HarmonyPostfix]
    static void _ReadyPostfix(NAscensionPanel __instance) {
        if (__instance._rightArrow is NGoldArrowButton rightArrow) {
            AttachLock(rightArrow, __instance);
        }
    }
    
    [HarmonyPatch(typeof(NAscensionPanel), nameof(NAscensionPanel.RefreshArrowVisibility))]
    [HarmonyPostfix]
    static void RefreshArrowVisibilityPostfix(NAscensionPanel __instance) {
        if (__instance._rightArrow is not NGoldArrowButton rightArrow) return;
        
        if (__instance._arrowsVisible &&
            __instance.Ascension == GlutonsAscensionLevel.BaseMaxAscensionAllowed &&
            __instance._maxAscension == GlutonsAscensionLevel.BaseMaxAscensionAllowed &&
            (__instance._mode == MultiplayerUiMode.Host) == (SaveManager.Instance.Progress.MaxMultiplayerAscension == GlutonsAscensionLevel.BaseMaxAscensionAllowed)
        ) {
            EnableLock(rightArrow);
        } else {
            DisableLock(rightArrow);
        }
    }
    
    [HarmonyPatch(typeof(NAscensionPanel), nameof(NAscensionPanel.IncrementAscension))]
    [HarmonyPrefix]
    static bool IncrementAscensionPrefix(NAscensionPanel __instance) {
        if (__instance.Ascension != GlutonsAscensionLevel.BaseMaxAscensionAllowed ||
            __instance._maxAscension != GlutonsAscensionLevel.BaseMaxAscensionAllowed
        ) {
            return HarmonyExtensions.PrefixRunOriginal;
        }

        var confirmationPopup = NConfirmUnlockAscensionPopup.Create(
            GlutonsAscensionLevel.FirstGlutonAscensionLevel,
            GetTargetString(__instance._mode == MultiplayerUiMode.Host ? null : __instance.SelectedCharacter()),
            __instance
        );
        if (confirmationPopup is not null) {
            NModalContainer.Instance?.Add(confirmationPopup);
        }
            
        return HarmonyExtensions.PrefixSkipOriginal;
    }
}