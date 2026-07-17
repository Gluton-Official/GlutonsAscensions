using System.Reflection;
using System.Text;
using System.Reflection.Emit;
using BaseLib.Utils;
using GlutonsAscensions.Helpers;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Events;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Enchantments;
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

    private static readonly Dictionary<Type, List<(string, EventRequirements)>> _disablableEventOptions = new() {
        [typeof(AromaOfChaos)] = [("LET_GO", new EventRequirements().RemovableCard())],
        [typeof(DoorsOfLightAndDark)] = [("DARK", new EventRequirements().RemovableCard())],
        [typeof(FieldOfManSizedHoles)] = [("RESIST", new EventRequirements().RemovableCards(2))],
        [typeof(LuminousChoir)] = [("REACH_INTO_THE_FLESH", new EventRequirements().RemovableCards(2))],
        [typeof(Symbiote)] = [("KILL_WITH_FIRE", new EventRequirements().RemovableCard())],
        [typeof(Trial)] = [("NONDESCRIPT.options.INNOCENT", new EventRequirements().RemovableCards(2))],
        [typeof(Wellspring)] = [("BATHE", new EventRequirements().RemovableCard())],
        [typeof(WhisperingHollow)] = [("HUG", new EventRequirements().RemovableCard())],
        [typeof(ZenWeaver)] = [
            ("EMOTIONAL_AWARENESS", new EventRequirements().RemovableCard()),
            ("ARACHNID_ACUPUNCTURE", new EventRequirements().RemovableCards(2))
        ],
    };

    [HarmonyPatch(typeof(EventModel), "SetEventState")]
    [HarmonyPrefix]
    static void DisableEventOptions(EventModel __instance, ref IEnumerable<EventOption> eventOptions) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;
        
        var eventModelType = __instance.GetType()!;
        
        if (__instance.Owner is not { } player) return;
        if (!_disablableEventOptions.TryGetValue(eventModelType, out var disabledEventOptions)) return;

        var eventOptionsList = eventOptions.ToList();
        foreach (var (optionKey, requirements) in disabledEventOptions) {
            var eventOption = eventOptionsList.FirstOrDefault(option => option.TextKey.EndsWith(optionKey));
            if (eventOption is not null && !eventOption.IsLocked && !requirements.IsMetBy(player, __instance)) {
                GlutonsAscensionsMod.Logger.Info($"Disabling {eventModelType.Name} option '{optionKey}' for Player {player.Name} because its requirements are not met:{requirements.FormatUnmetRequirements(player, __instance).Indent("  ")}");
                _isLockedProperty.SetBackingField(eventOption, true);
            }
        }
        eventOptions = eventOptionsList;
    }
}

[HarmonyPatch]
public class EventPatches {
    private static readonly Dictionary<Type, EventRequirements> _disallowedEvents = new() {
        [typeof(LuminousChoir)] = new EventRequirements().RemovableCards(2),
        [typeof(Symbiote)] = new EventRequirements().RemovableCard().EnchantableWith<Corrupted>().MeetAny(),
    };

    [HarmonyTargetMethods]
    static IEnumerable<MethodBase> Events() =>
        AccessTools.Method(typeof(EventModel), nameof(EventModel.IsAllowed)).FindOverrides();

    [HarmonyPostfix]
    static void IsAllowedPostfix(EventModel __instance, IRunState runState, ref bool __result) {
        if (!GlutonsAscensionLevel.LockedIn.HasAscension()) return;
        
        var eventModelType = __instance.GetType()!;

        if (!__result) return; // Skip if already not allowed
        if (!_disallowedEvents.TryGetValue(eventModelType, out var requirements)) return;
        
        __result = requirements.IsMetBy(runState.Players, __instance);
        
        GlutonsAscensionsMod.Logger.Info($"Checked {eventModelType.Name}, IsAllowed: {__result}");

        if (!__result) {
            var sb = new StringBuilder($"Not allowing {eventModelType.Name}:");
            foreach (var player in runState.Players.Where(player => !requirements.IsMetBy(player, __instance))) {
                sb.Append($"\n  Player {player.Name} does not meet requirements:{requirements.FormatUnmetRequirements(player, __instance).Indent("    ")}");
            }
            GlutonsAscensionsMod.Logger.Info(sb.ToString());
        }
    } 
}

internal class EventRequirements {
    private bool hasRemovableCardsRequirement;
    private bool hasEnchantableRequirement;
    private bool hasGoldRequirement;
    private bool basedOnGoldVar;

    private int removableCards;
    private int enchantableCards;
    private EnchantmentModel? enchantment;
    private int? gold;
    private bool meetAny;

    public EventRequirements RemovableCard() => RemovableCards(1);
    public EventRequirements RemovableCards(int requiredAmount) {
        removableCards = requiredAmount;
        hasRemovableCardsRequirement = true;
        return this;
    }
    
    public EventRequirements EnchantableCard() => EnchantableCards(1);
    public EventRequirements EnchantableCards(int requiredAmount) {
        enchantableCards = requiredAmount;
        hasEnchantableRequirement = true;
        return this;
    }
    public EventRequirements EnchantableWith<T>(int requiredAmount = 1) where T : EnchantmentModel {
        enchantableCards = requiredAmount;
        hasEnchantableRequirement = true;
        enchantment = ModelDb.Enchantment<T>();
        return this;
    }

    public EventRequirements Gold(int requiredAmount) {
        gold = requiredAmount;
        hasGoldRequirement = true;
        basedOnGoldVar = false;
        return this;
    }
    
    public EventRequirements GoldVar() {
        gold = null;
        hasGoldRequirement = true;
        basedOnGoldVar = true;
        return this;
    }
    
    public EventRequirements MeetAny() {
        meetAny = true;
        return this;
    }

    public bool IsMetBy(IEnumerable<Player> players, EventModel eventModel) => players.All(player => IsMetBy(player, eventModel));
    public bool IsMetBy(Player player, EventModel eventModel) {
        var meetsRequirements = true;
        
        if (hasRemovableCardsRequirement) {
            meetsRequirements &= player.Deck.RemovableCardCount() >= removableCards;
        }
        if (meetAny && meetsRequirements) return true;

        if (hasEnchantableRequirement) {
            meetsRequirements &= player.Deck.Cards.Count(card => card.Enchantment is null && enchantment?.CanEnchant(card) != false) >= enchantableCards;
        }
        if (meetAny && meetsRequirements) return true;

        if (hasGoldRequirement) {
            meetsRequirements &= basedOnGoldVar switch {
                true when !eventModel.DynamicVars.ContainsKey("Gold") => throw new Exception($"[GlutonsAscensions] Event {eventModel.GetType()} does not have a Gold dynamic variable"),
                true => player.Gold >= eventModel.DynamicVars.Gold.IntValue,
                _ => player.Gold >= gold
            };
        }
        
        return meetsRequirements;
    }

    public string FormatUnmetRequirements(Player player, EventModel eventModel) {
        var sb = new StringBuilder();
        if (hasRemovableCardsRequirement && player.Deck.RemovableCardCount() < removableCards) {
            sb.Append($"\nRemovable Cards: actual {player.Deck.RemovableCardCount()}, expected >={removableCards}");
        }
        if (hasEnchantableRequirement) {
            var enchantableCardCount = player.Deck.Cards.Count(card => card.Enchantment is null && enchantment?.CanEnchant(card) != false);
            if (enchantableCardCount < enchantableCards) {
                if (enchantment is null) {
                    sb.Append($"\nEnchantable Cards: actual {enchantableCardCount}, expected >={enchantableCards}");
                } else {
                    sb.Append($"\nCards Enchantable with {enchantment.GetType()!.Name}: actual {enchantableCardCount}, expected >={enchantableCards}");
                }
            }
        }
        if (hasGoldRequirement) {
            switch (basedOnGoldVar) {
                case true when player.Gold < eventModel.DynamicVars.Gold.IntValue:
                    sb.Append($"\nGold Var: actual {player.Gold}, expected >={eventModel.DynamicVars.Gold.IntValue}");
                    break;
                case false when player.Gold < gold:
                    sb.Append($"\nGold: actual {player.Gold}, expected >={gold}");
                    break;
            }
        }
        return sb.ToString();
    }
}
