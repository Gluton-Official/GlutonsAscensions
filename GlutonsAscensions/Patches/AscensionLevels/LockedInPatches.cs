using System.Reflection;
using System.Reflection.Emit;
using BaseLib.Extensions;
using BaseLib.Utils;
using GlutonsAscensions.Helpers;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Events;
using MegaCrit.Sts2.Core.Models.Relics;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Runs;

namespace GlutonsAscensions.Patches.AscensionLevels;

[HarmonyPatch]
public class LockedInPatches {
    private static readonly SavedSpireField<CardModel, bool> IsEternal = new (() => false, GlutonsAscensionsMod.ModNamespace(nameof(IsEternal)));
    
    [HarmonyPatch(typeof(AncientEventModel), nameof(AncientEventModel.Done))]
    [HarmonyPrefix]
    static void FinalizeStartingDeckAsEternal(AncientEventModel __instance) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;

        // Must be first Act
        if (__instance.Owner?.RunState.CurrentActIndex == 0) {
            foreach (var card in __instance.Owner?.Deck.Cards ?? []) {
                card.AddKeyword(CardKeyword.Eternal);
                IsEternal[card] = true;
            }
        }
    }
    
    [HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.GetTranscendenceTransformedCard))]
    [HarmonyPostfix]
    static void AddEternalAfterTranscending(CardModel starterCard, ref CardModel __result) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;

        if (IsEternal[starterCard]) {
            __result.AddKeyword(CardKeyword.Eternal);
            IsEternal[__result] = true;
        }
    }

    [HarmonyPatch(typeof(ArchaicTooth), nameof(ArchaicTooth.AfterObtained), MethodType.Async)]
    [HarmonyTranspiler]
    static IEnumerable<CodeInstruction> AfterObtainedTranspiler(IEnumerable<CodeInstruction> instructions) {
        var codeMatcher = new CodeMatcher(instructions);

        // CardModel transcendenceStarterCard = archaicTooth.GetTranscendenceStarterCard(archaicTooth.Owner);
        // 
        // call         instance class MegaCrit.Sts2.Core.Models.CardModel MegaCrit.Sts2.Core.Models.Relics.ArchaicTooth::GetTranscendenceStarterCard(class MegaCrit.Sts2.Core.Entities.Players.Player)
        // stloc.2      // transcendenceStarterCard
        codeMatcher
            .MatchEndForward(
                CodeMatch.Calls(AccessTools.Method(typeof(ArchaicTooth), nameof(ArchaicTooth.GetTranscendenceStarterCard))),
                CodeMatch.StoresLocal()
            )
            .ThrowIfInvalid("Could not find call to ArchaicTooth.GetTranscendenceStarterCard followed by a stloc");
            
        var transcendenceStarterCardLocalIndex = codeMatcher.Instruction.LocalIndex();
        
        // transcendenceStarterCard = LockedInPatches.RemoveEternalBeforeTransform(transcendenceStarterCard);
        //
        // ldloc.2      // transcendenceStarterCard
        // call         RemoveEternalBeforeTransform
        // stloc.2      // transcendenceStarterCard
        codeMatcher
            .InsertAfter(
                CodeInstruction.LoadLocal(transcendenceStarterCardLocalIndex),
                CodeInstruction.Call(() => RemoveEternalBeforeTransform(null!)),
                CodeInstruction.StoreLocal(transcendenceStarterCardLocalIndex)
            );
        
        // CardPileAddResult? nullable = await CardCmd.Transform(transcendenceStarterCard, archaicTooth.GetTranscendenceTransformedCard(transcendenceStarterCard));
        // 
        // call         class [System.Runtime]System.Threading.Tasks.Task`1<valuetype [System.Runtime]System.Nullable`1<valuetype MegaCrit.Sts2.Core.Entities.Cards.CardPileAddResult>> MegaCrit.Sts2.Core.Commands.CardCmd::Transform(class MegaCrit.Sts2.Core.Models.CardModel, class MegaCrit.Sts2.Core.Models.CardModel, valuetype MegaCrit.Sts2.Core.Nodes.CommonUi.CardPreviewStyle)
        codeMatcher
            .MatchEndForward(
                CodeMatch.Calls(AccessTools.Method(typeof(CardCmd), nameof(CardCmd.Transform), [typeof(CardModel), typeof(CardModel), typeof(CardPreviewStyle)])),
                new CodeMatch(OpCodes.Callvirt),
                CodeMatch.StoresLocal()
            )
            .ThrowIfInvalid("Could not find call to CardCmd.Transform followed by a callvirt opcode followed by a stloc");
        
        var cardPileAddResultLocalIndex = codeMatcher.Instruction.LocalIndex();
        
        codeMatcher
            .InsertAfterAndAdvance(
                CodeInstruction.LoadLocal(cardPileAddResultLocalIndex),
                CodeInstruction.LoadLocal(transcendenceStarterCardLocalIndex),
                CodeInstruction.Call(() => RemoveStarterCardFromEternalListIfSuccessful(null!, null!))
            );

        return codeMatcher.Instructions();
    }
    
    private static CardModel? RemoveEternalBeforeTransform(CardModel? card) {
        if (GlutonsAscensionLevel.LockedIn.HasAscension() && card is not null) {
            card.RemoveKeyword(CardKeyword.Eternal);
        }
        return card;
    }

    private static void RemoveStarterCardFromEternalListIfSuccessful(CardPileAddResult? result, CardModel card) {
        if (result is { success: true }) {
            IsEternal.Remove(card);
        }
    }

    [HarmonyPatch(typeof(RunState), nameof(RunState.CloneCard))]
    [HarmonyPostfix]
    static void PreserveEternalForClonedCard(CardModel mutableCard, ref CardModel __result) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;

        if (mutableCard.Keywords.Contains(CardKeyword.Eternal)) {
            __result.AddKeyword(CardKeyword.Eternal);
            IsEternal[__result] = true;
        }
    }
    
    [HarmonyPatch(typeof(CardModel), nameof(CardModel.FromSerializable))]
    [HarmonyPostfix]
    static void AddEternalWhenLoadingCard(ref CardModel __result) {
        if (IsEternal[__result]) {
            __result.AddKeyword(CardKeyword.Eternal);
        }
    }
    
    private static readonly PropertyInfo _isLockedProperty = AccessTools.Property(typeof(EventOption), nameof(EventOption.IsLocked)) ?? throw new Exception("[GlutonsAscensions] Unable to get NumOfShops property");

    private static readonly Dictionary<Type, (string, EventRequirements)> _disablableEvents = new() {
        [typeof(AromaOfChaos)] = ("LET_GO", new EventRequirements { RemovableCards = 1 }),
        // [typeof(DenseVegetation)] = ("TRUDGE_ON", new EventRequirements { RemovableCards = 1 }),
        [typeof(DoorsOfLightAndDark)] = ("DARK", new EventRequirements { RemovableCards = 1 }),
        [typeof(LuminousChoir)] = ("REACH_INTO_THE_FLESH", new EventRequirements { RemovableCards = 2 }),
        [typeof(Wellspring)] = ("BATHE", new EventRequirements { RemovableCards = 1 }),
        [typeof(WhisperingHollow)] = ("HUG", new EventRequirements { RemovableCards = 1 }),
        [typeof(FieldOfManSizedHoles)] = ("RESIST", new EventRequirements { RemovableCards = 2 }),
        // [typeof(SpiritGrafter)] = ("REJECTION", new EventRequirements { RemovableCards = 1 }),
        [typeof(Symbiote)] = ("KILL_WITH_FIRE", new EventRequirements { RemovableCards = 1 }),
        [typeof(ZenWeaver)] = ("EMOTIONAL_AWARENESS", new EventRequirements { RemovableCards = 1 }),
        [typeof(ZenWeaver)] = ("ARACHNID_ACUPUNCTURE", new EventRequirements { RemovableCards = 2 }),
    };

    [HarmonyPatch(typeof(EventModel), nameof(EventModel.BeginEvent))]
    [HarmonyPostfix]
    static void DisableEventOptions(EventModel __instance) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;
        if (!_disablableEvents.ContainsKey(__instance.GetType())) return;
        
        if (__instance.Owner is not { } player) return;
        
        var (optionKey, requirements) = _disablableEvents.GetValueSafe(__instance.GetType());

        var option = __instance.CurrentOptions.FirstOrDefault(option => option.TextKey.EndsWith(optionKey));
        if (option is null) return;
        
        if (requirements.MetBy(player, __instance)) return;
        
        _isLockedProperty.SetBackingField(option, true);
    }

    [HarmonyPatch(typeof(EventModel), "SetEventState")]
    [HarmonyPostfix]
    static void DisableTrialNondescriptInnocentOption(EventModel __instance) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;
        if (__instance is not Trial) return;
        
        if (__instance.Owner is not { } player) return;
        
        var option = __instance.CurrentOptions.FirstOrDefault(option => option.TextKey.EndsWith("NONDESCRIPT.options.INNOCENT"));
        if (option is null) return;
            
        var requirements = new EventRequirements { RemovableCards = 2 };
        if (requirements.MetBy(player, __instance)) return;
            
        _isLockedProperty.SetBackingField(option, true);
    }
}

[HarmonyPatch]
public class EventPatches {
    private static readonly Dictionary<Type, EventRequirements> _disallowedEvents = new() {
        [typeof(LuminousChoir)] = new EventRequirements { RemovableCards = 2, Gold = null, MeetAny = true },
        [typeof(Symbiote)] = new EventRequirements { RemovableCards = 1 },
    };

    [HarmonyTargetMethods]
    static IEnumerable<MethodBase> Events() =>
        AccessTools.Method(typeof(EventModel), nameof(EventModel.IsAllowed)).FindOverrides();

    [HarmonyPostfix]
    static void IsAllowedPostfix(EventModel __instance, ref bool __result) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;
        if (!_disallowedEvents.ContainsKey(__instance.GetType())) return;

        var requirements = _disallowedEvents.GetValueSafe(__instance.GetType());

        if (__instance is Symbiote) {
            var symbioteRequirementsMet = __instance.Owner?.RunState.Players.All(player =>
                player.Deck.Cards.Any(Symbiote.CanEnchant) || requirements.MetBy(player, __instance));
            __result = symbioteRequirementsMet ?? true;
            return;
        }
        
        __result = requirements.MetBy(__instance);       
    } 
}

internal class EventRequirements {
    private bool _hasRemovableCardsRequirement { get; init; }
    private bool _hasGoldRequirement { get; init; }
    private bool _basedOnGoldVar { get; init; }

    public int RemovableCards {
        get;
        init {
            field = value;
            _hasRemovableCardsRequirement = true;
        }
    }

    public int? Gold {
        get;
        init {
            field = value;
            _hasGoldRequirement = true;
            if (value is null) {
                _basedOnGoldVar = true;
            }
        }
    }
    
    public bool MeetAny { get; init; }

    public bool MetBy(EventModel eventModel) => eventModel.Owner?.RunState.Players.All(player => MetBy(player, eventModel)) ?? true;
    public bool MetBy(Player player, EventModel eventModel) {
        if (_hasRemovableCardsRequirement) {
            var meetsRemovableCardRequirement = player.Deck.RemovableCardCount() >= RemovableCards;
            if (meetsRemovableCardRequirement == MeetAny) {
                return meetsRemovableCardRequirement;
            }
        }

        if (!_hasGoldRequirement) return true;
        if (!_basedOnGoldVar) return player.Gold >= Gold;
        
        if (!eventModel.DynamicVars.ContainsKey("Gold")) throw new Exception($"Event {eventModel.GetType()} does not have a Gold dynamic variable");
        
        return player.Gold >= eventModel.DynamicVars.Gold.BaseValue;
    }
}
