using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Utils;
using GlutonsAscensions.Helpers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.addons.mega_text;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Entities.Potions;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes;
using MegaCrit.Sts2.Core.Nodes.Potions;
using MegaCrit.Sts2.Core.Nodes.Rewards;
using MegaCrit.Sts2.Core.Nodes.Screens.Shops;

namespace GlutonsAscensions.Patches.AscensionLevels;

[HarmonyPatch]
public class VolatileVialsPatches {
    private static readonly SavedSpireField<PotionModel, int> VolatileCombatCountdown = new(
        potion => potion.Rarity switch {
            PotionRarity.Uncommon => 4,
            PotionRarity.Rare => 5,
            PotionRarity.Event => 6,
            _ => 3,
        },
        GlutonsAscensionsMod.ModNamespace(nameof(VolatileCombatCountdown))
    );
    
    private static readonly NotNullSpireField<NPotion, MegaLabel> CombatCountLabel = new(AddCombatCountLabel);
    
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

    private static readonly Shader RustShader = new() {
        // lang=gdshader
        Code = """
            shader_type canvas_item;

            uniform vec4 rust_color_dark : source_color = vec4(0.35, 0.12, 0.03, 1.0);
            uniform vec4 rust_color_mid  : source_color = vec4(0.75, 0.28, 0.06, 1.0);

            uniform float rust_width : hint_range(0.0, 0.2) = 0.125;
            uniform float rust_strength : hint_range(0.0, 2.0) = 1.4;
            uniform float noise_scale = 45.0;
            uniform float edge_threshold : hint_range(0.0, 1.0) = 0.99;

            float hash(vec2 p) {
                return fract(sin(dot(p, vec2(127.1, 311.7))) * 43758.5453123);
            }

            float noise(vec2 p) {
                vec2 i = floor(p);
                vec2 f = fract(p);

                float a = hash(i);
                float b = hash(i + vec2(1.0, 0.0));
                float c = hash(i + vec2(0.0, 1.0));
                float d = hash(i + vec2(1.0, 1.0));

                vec2 u = f * f * (3.0 - 2.0 * f);

                return mix(a, b, u.x) +
                    (c - a) * u.y * (1.0 - u.x) +
                    (d - b) * u.x * u.y;
            }

            float fbm(vec2 p) {
                float value = 0.0;
                float amplitude = 0.5;

                for (int i = 0; i < 5; i++) {
                    value += amplitude * noise(p);
                    p *= 2.0;
                    amplitude *= 0.5;
                }

                return value;
            }

            void fragment() {
                vec4 tex = texture(TEXTURE, UV);

                vec2 px = TEXTURE_PIXEL_SIZE * rust_width * 100.0;

                float alpha_center = tex.a;

                float alpha_left  = texture(TEXTURE, UV + vec2(-px.x, 0.0)).a;
                float alpha_right = texture(TEXTURE, UV + vec2(px.x, 0.0)).a;
                float alpha_up    = texture(TEXTURE, UV + vec2(0.0, -px.y)).a;
                float alpha_down  = texture(TEXTURE, UV + vec2(0.0, px.y)).a;

                float edge = 0.0;
                edge += abs(alpha_center - alpha_left);
                edge += abs(alpha_center - alpha_right);
                edge += abs(alpha_center - alpha_up);
                edge += abs(alpha_center - alpha_down);

                edge = smoothstep(edge_threshold, 1.0, edge);

                float inward_mask = smoothstep(0.0, rust_width, alpha_center);
                float rust_mask = edge * inward_mask;

                float n1 = fbm(UV * noise_scale);

                rust_mask *= smoothstep(0.25, 0.95, n1);
                rust_mask *= rust_strength;

                vec4 rust_color = mix(rust_color_dark, rust_color_mid, n1);

                vec3 final_color = mix(tex.rgb, rust_color.rgb, rust_mask);

                COLOR = vec4(final_color, tex.a);
            }
            """
    };

    /// <param name="potion">Must have been added to the scene tree, since <see cref="NPotion.Image"/> is set in <see cref="NPotion._Ready"/></param>
    private static void SetShader(NPotion potion, Shader shader) {
        if (potion.Image.Material is ShaderMaterial { Shader: var currentShader} && currentShader == shader) return;
        
        var shaderMaterial = new ShaderMaterial();
        shaderMaterial.Shader = shader;
        potion.Image.Material = shaderMaterial;
    }
    
    private static MegaLabel AddCombatCountLabel(NPotion potion) {
        potion.Model.AssertMutable();
        var label = CreateCombatCountLabel();
        potion.AddChild(label);
        label.SetTextAutoSize(VolatileCombatCountdown[potion.Model].ToString());
        return label;
    }

    private static MegaLabel CreateCombatCountLabel() {
        var label = new MegaLabel {
            AutoSizeEnabled = false,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            LayoutMode = 1, // Anchors
        };
        label.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        label.AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.cream);
        label.AddThemeColorOverride(ThemeConstants.Label.FontShadowColor, Colors.Black with { A = 0.1875f });
        label.AddThemeColorOverride(ThemeConstants.Label.FontOutlineColor, new Color("26241C"));
        label.AddThemeConstantOverride("shadow_offset_x", 2);
        label.AddThemeConstantOverride("shadow_offset_y", 2);
        label.AddThemeConstantOverride("outline_size", 10);
        label.AddThemeConstantOverride("shadow_outline_size", 10);
        label.AddThemeFontOverride(ThemeConstants.Label.Font, ResourceLoader.Load<FontVariation>("res://themes/kreon_bold_glyph_space_one.tres"));
        label.AddThemeFontSizeOverride(ThemeConstants.Label.FontSize, 18);

        return label;
    }
    
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

    private static void UpdateLocalPotionHoldersAfterCombat() {
        if (NRun.Instance is not { } run) return;
        
        var potions = run.GlobalUi.TopBar.PotionContainer._holders
            .Where(holder => holder.HasPotion)
            .Select(holder => holder.Potion!)
            .ToList();

        foreach (var potion in potions) {
            StopAlmostInertAnimation(potion);

            var combats = VolatileCombatCountdown[potion.Model];
            
            if (combats <= 0) {
                // Make inert potions grayscale
                SetShader(potion, GrayscaleShader);

                CombatCountLabel[potion].Visible = false;
            } else {
                // Make volatile potions rusty
                SetShader(potion, RustShader);

                var label = CombatCountLabel[potion];
                label.SetTextAutoSize(combats.ToString());
                if (combats == 1) {
                    label.AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.red);
                }
            }
        }
    }

    // [HarmonyPatch(typeof(NPotion), nameof(NPotion.Create))]
    // [HarmonyPostfix]
    // static void MakePotionVolatile(ref NPotion? __result) {
    //     if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
    //     
    //     if (__result is not { } potion) return;
    //
    //     SetShader(potion, RustShader);
    //     _ = CombatCountLabel[potion];
    // }

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
        
        UpdateLocalPotionHoldersAfterCombat();
    }

    [HarmonyPatch(typeof(NPotionContainer), nameof(NPotionContainer.Add))]
    [HarmonyPostfix]
    static void OnLoadPotion(NPotionContainer __instance, PotionModel potion, bool isInitialization) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        if (!isInitialization) return;

        // Check if potion was successfully added
        var potionNode = __instance._holders.Find(holder => holder.Potion?.Model == potion)?.Potion;
        if (potionNode is null) return;

        var combats = VolatileCombatCountdown[potion];

        if (CombatManager.Instance.IsInProgress && combats == 1) {
            StartAlmostInertAnimation(potionNode);
        }

        var label = CombatCountLabel[potionNode];
        if (IsInert(potion)) {
            SetShader(potionNode, GrayscaleShader);
            label.Visible = false;
        } else {
            SetShader(potionNode, RustShader);
            label.SetTextAutoSize(combats.ToString());
            if (combats == 1) {
                label.AddThemeColorOverride(ThemeConstants.Label.FontColor, StsColors.red);
            }
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
        static IEnumerable<MethodBase> ExtraHoverTips() => AccessTools.DeclaredPropertyGetter(typeof(PotionModel), nameof(PotionModel.ExtraHoverTips)).FindOverrides(searchAllTypes: true);
    
        [HarmonyPostfix]
        static void AddVolatileToExtraHoverTips(PotionModel __instance, ref IEnumerable<IHoverTip> __result) {
            if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
    
            __result = __result.Concat(CreateVolatilePotionHoverTip(__instance));
        }
    }

    // [HarmonyPatch(typeof(PotionModel), nameof(PotionModel.HoverTips), MethodType.Getter)]
    // [HarmonyPostfix]
    // static void AppendHoverTips(PotionModel __instance, ref IEnumerable<IHoverTip> __result) {
    //     if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
    //
    //     __result = __result.Concat(CreateVolatilePotionHoverTip(__instance));
    // }

    private static readonly SpireField<NPotion, Tween> AlmostInertTween = new(() => null);

    private static void StopAlmostInertAnimation(NPotion potion) {
        AlmostInertTween[potion]?.Kill();
    }

    private static void StartAlmostInertAnimation(NPotion potion) {
        StopAlmostInertAnimation(potion);
        
        var tween = potion.GetTree().CreateTween().SetLoops();
        tween.TweenCallback(Callable.From(potion.DoBounce)).SetDelay(5.0);
        AlmostInertTween[potion] = tween;
    }

    [HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder.AddPotion))]
    [HarmonyPostfix]
    static void AddPotionPostfix(NPotionHolder __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (__instance.Potion is not { } potion) return;
        
        SetShader(potion, RustShader);
        _ = CombatCountLabel[potion];

        if (CombatManager.Instance.IsInProgress && VolatileCombatCountdown[potion.Model] == 1) {
            StartAlmostInertAnimation(potion);
        }
    }

    [HarmonyPatch(typeof(NPotionHolder), "OnFocus")]
    [HarmonyPostfix]
    static void OnFocusPostfix(NPotionHolder __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (__instance.Potion is not { } potion) return;
        
        StopAlmostInertAnimation(potion);
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
        
        StopAlmostInertAnimation(potion);
        
        CombatCountLabel.Remove(potion);
    }

    [HarmonyPatch(typeof(NPotionHolder), nameof(NPotionHolder.RemoveUsedPotion))]
    [HarmonyPrefix]
    static void RemoveUsedPotionPrefix(NPotionHolder __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (__instance.Potion is not { } potion) return;
        
        StopAlmostInertAnimation(potion);
        
        CombatCountLabel.Remove(potion);
    }
    
    [HarmonyPatch(typeof(NMerchantPotion), nameof(NMerchantPotion.FillSlot))]
    [HarmonyPostfix]
    static void MakeShopPotionVolatile(NMerchantPotion __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;
        
        if (__instance._potionNode is not { } potion) return;

        _ = CombatCountLabel[potion];
        SetShader(potion, RustShader);
    }

    [HarmonyPatch(typeof(NRewardButton), nameof(NRewardButton.Reload))]
    [HarmonyPostfix]
    static void MakePotionRewardVolatile(NRewardButton __instance) {
        if (!GlutonsAscensionLevel.VolatileVials.HasAscension()) return;

        foreach (var potion in __instance._iconContainer.GetChildren().OfType<NPotion>()) {
            _ = CombatCountLabel[potion];
            SetShader(potion, RustShader);
        }
    }
}