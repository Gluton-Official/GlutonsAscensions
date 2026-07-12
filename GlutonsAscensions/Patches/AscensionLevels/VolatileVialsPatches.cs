using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Utils;
using GlutonsAscensions.Helpers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Potions;

namespace GlutonsAscensions.Patches.AscensionLevels;

[HarmonyPatch]
public class VolatileVialsPatches {
    private const int COMBATS_BEFORE_INERT = 3;

    private static readonly Shader GrayscaleShader = new() {
        // lang=gdshader
        Code = """
            shader_type canvas_item;
            
            void fragment() {
                vec4 tex_color = texture(TEXTURE, UV);
                
                float gray = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114)) * 0.9;
                
                COLOR = vec4(vec3(gray), tex_color.a);
            }
        """
    };
    
    private static readonly SavedSpireField<PotionModel, int> VolatileCombatCountdown = new (() => COMBATS_BEFORE_INERT, GlutonsAscensionsMod.ModNamespace(nameof(VolatileCombatCountdown)));
    
    private static bool IsInert(PotionModel potion) => VolatileCombatCountdown[potion] <= 0;

    private static IEnumerable<IHoverTip> CreateVolatilePotionHoverTip(PotionModel potion) {
        var combats = VolatileCombatCountdown[potion];
        if (combats <= 0) return [InertPotionHoverTip];
        
        var descriptionKey = combats == 1 && CombatManager.Instance.IsInProgress ? "VOLATILE_POTION.description.0" : "VOLATILE_POTION.description";
        var description = GlutonsAscensionsMod.ModLocString("static_hover_tips", descriptionKey);
        description.Add("Combats", combats);
        var volatileHoverTip = new HoverTip(
            GlutonsAscensionsMod.ModLocString("static_hover_tips", "VOLATILE_POTION.title"),
            description
        ) {
            IsDebuff = true,
        };
        return [volatileHoverTip, InertPotionHoverTip];
    }

    private static HoverTip InertPotionHoverTip => new(
        GlutonsAscensionsMod.ModLocString("static_hover_tips", "INERT_POTION.title"),
        GlutonsAscensionsMod.ModLocString("static_hover_tips", "INERT_POTION.description")
    ) {
        IsDebuff = true,
    };

    private static void SetGrayscaleShader(NPotion potion) {
        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = GrayscaleShader;
        potion.Image.Material = shaderMaterial;
    }

    private static void UpdateLocalPotionHolders() {
        if (NRun.Instance is not { } run) return;
        
        var potions = run.GlobalUi.TopBar.PotionContainer._holders
            .Where(holder => holder.HasPotion)
            .Select(holder => holder.Potion!)
            .ToList();

        // Stop any almost inert animations
        foreach (var potion in potions) {
            AlmostInertTween[potion]?.Kill();
        }
        
        // Make inert potions grayscale
        foreach (var inertPotion in potions.Where(potion => VolatileCombatCountdown[potion.Model] <= 0)) {
            SetGrayscaleShader(inertPotion);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.ResetCombatState))]
    [HarmonyPostfix]
    static void ResetCombatStatePostfix(Player __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;

        if (!LocalContext.IsMe(__instance)) return;
        if (NRun.Instance is not { } run) return;

        var potions = run.GlobalUi.TopBar.PotionContainer._holders
            .Where(holder => holder.HasPotion)
            .Select(holder => holder.Potion!)
            .ToList();

        foreach (var potion in potions.Where(potion => VolatileCombatCountdown[potion.Model] == 1)) {
            StartAlmostInertAnimation(potion);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.AfterCombatEnd))]
    [HarmonyPostfix]
    static void AfterCombatEndPostfix(Player __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        foreach (var potion in __instance.Potions) {
            var combats = VolatileCombatCountdown[potion];
            if (combats <= 0) continue;
            VolatileCombatCountdown[potion] = combats - 1;
        }
        
        UpdateLocalPotionHolders();
    }

    [HarmonyPatch(typeof(NPotionContainer), nameof(NPotionContainer.Add))]
    [HarmonyPostfix]
    static void OnLoadPotion(NPotionContainer __instance, PotionModel potion, bool isInitialization) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        if (!isInitialization) return;

        // Check if potion was successfully added
        var potionHolder = __instance._holders.Find(holder => holder.Potion?.Model == potion);
        if (potionHolder is null) return;

        if (CombatManager.Instance.IsInProgress && VolatileCombatCountdown[potion] == 1) {
            StartAlmostInertAnimation(potionHolder.Potion!);
        }

        if (VolatileCombatCountdown[potion] <= 0) {
            SetGrayscaleShader(potionHolder.Potion!);
        }
    }

    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.OnUseWrapper), MethodType.Async)]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> SkipOnUseIfInertTranspiler(IEnumerable<CodeInstruction> instructions, ILGenerator generator) {
        var codeMatcher = new CodeMatcher(instructions);
        
        var afterUseLabel = generator.DefineLabel();

        // await potionModel.OnUse(choiceContext, target);
        // 
        // ldloc.1      // potionModel
        // ldarg.0      // this
        // ldfld        class MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext MegaCrit.Sts2.Core.Models.PotionModel/'<OnUseWrapper>d__71'::choiceContext
        // ldarg.0      // this
        // ldfld        class MegaCrit.Sts2.Core.Entities.Creatures.Creature MegaCrit.Sts2.Core.Models.PotionModel/'<OnUseWrapper>d__71'::target
        // callvirt     instance class [System.Runtime]System.Threading.Tasks.Task MegaCrit.Sts2.Core.Models.PotionModel::OnUse(class MegaCrit.Sts2.Core.GameActions.Multiplayer.PlayerChoiceContext, class MegaCrit.Sts2.Core.Entities.Creatures.Creature)
        codeMatcher
            .MatchStartForward(
                CodeMatch.Calls(AccessTools.Method(typeof(PotionModel), "OnUse"))
            )
            .ThrowIfInvalid("Could not find call to PotionModel.OnUse");

        codeMatcher
            .MatchStartBackwards(
                new CodeMatch(OpCodes.Ldloc_1)
            )
            .ThrowIfInvalid("Could not find ldloc.1 opcode");

        codeMatcher
            .InsertAndAdvance(
                CodeInstruction.LoadLocal(1),
                CodeInstruction.Call(() => IsInert(null!)),
                new CodeInstruction(OpCodes.Brtrue_S, afterUseLabel)
            );

        codeMatcher
            .MatchStartForward(
                new CodeMatch(OpCodes.Leave_S)
            )
            .ThrowIfInvalid("Could not find leave.s opcode");
        
        codeMatcher.AddLabels([afterUseLabel]);

        return codeMatcher.Instructions();
    }
    
    [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.Discard))]
    [HarmonyPostfix]
    static void DiscardPostfix(PotionModel __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        VolatileCombatCountdown.Remove(__instance);
    }

    [HarmonyPatch]
    class PotionHoverTipsPatch {
        [HarmonyTargetMethods]
        static IEnumerable<MethodBase> ExtraHoverTips() =>
            AccessTools.DeclaredPropertyGetter(typeof(PotionModel), nameof(PotionModel.ExtraHoverTips)).FindOverrides(searchAllTypes: true);

        [HarmonyPostfix]
        static void AddVolatileToExtraHoverTipsPostfix(PotionModel __instance, ref IEnumerable<IHoverTip> __result) {
            if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
            __result = __result.Concat(CreateVolatilePotionHoverTip(__instance));
        }
    }
    
    private static readonly SpireField<NPotion, Tween> AlmostInertTween = new(() => null);

    private static void StartAlmostInertAnimation(NPotion potion) {
        AlmostInertTween[potion]?.Kill();
        
        var tween = potion.GetTree().CreateTween().SetLoops();
        tween.TweenCallback(Callable.From(potion.DoBounce)).SetDelay(5.0);
        AlmostInertTween[potion] = tween;
    }

    [HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder.AddPotion))]
    [HarmonyPostfix]
    static void AddPotionPostfix(NPotionHolder __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (!CombatManager.Instance.IsInProgress) return;
        if (__instance.Potion is not { } potion) return;
        if (VolatileCombatCountdown[potion.Model] != 1) return;

        StartAlmostInertAnimation(potion);
    }

    [HarmonyPatch(typeof(NPotionHolder), "OnFocus")]
    [HarmonyPostfix]
    static void OnFocusPostfix(NPotionHolder __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (__instance.Potion is not { } potion) return;
        
        AlmostInertTween[potion]?.Kill();
    }
    
    [HarmonyPatch(typeof(NPotionHolder), "OnUnfocus")]
    [HarmonyPostfix]
    static void OnUnfocusPostfix(NPotionHolder __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (!CombatManager.Instance.IsInProgress) return;
        if (__instance.Potion is not { } potion) return;
        if (VolatileCombatCountdown[potion.Model] != 1) return;
        
        StartAlmostInertAnimation(potion);
    }

    [HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder.DiscardPotion))]
    [HarmonyPrefix]
    static void DiscardPotionPrefix(NPotionHolder __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (__instance.Potion is not { } potion) return;
        
        AlmostInertTween[potion]?.Kill();
    }

    [HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder.RemoveUsedPotion))]
    [HarmonyPrefix]
    static void RemoveUsedPotionPrefix(NPotionHolder __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (__instance.Potion is not { } potion) return;
        
        AlmostInertTween[potion]?.Kill();
    }
}