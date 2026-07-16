![Mod Banner](assets/mod_banner.png)

# Gluton's Ascensions

STS2 mod that adds 10 new ascensions.

[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3747530530)  

## Ascensions

| Level | Ascension       | Description                                                        | Notes                                                                                                                                    |
|-------|-----------------|--------------------------------------------------------------------|------------------------------------------------------------------------------------------------------------------------------------------|
| 11    | Torn Rug        | The Merchant offers 1 less attack and skill, and no on-sale cards. |                                                                                                                                          |
| 12    | Out-of-Business | Each act only has 1 marked Merchant room.                          |                                                                                                                                          |
| 13    | Barren          | Unknown rooms are less common.                                     | Mean and range changed from 12, 10–14 to 10, 8–12                                                                                        |
| 14    | Volatile Vials  | Potions become Inert after 3 combats.                              | Inert potions have no effect when used.                                                                                                  |
| 15    | Short Supply    | Ancients only offer 2 relics.                                      |                                                                                                                                          |
| 16    | Slim Pickings   | Rewards from Elites have 1 less card.                              |                                                                                                                                          |
| 17    | Plundered       | Marked Treasure rooms are empty.                                   | Spoils chest still contains bonus gold, Silver Crucible will only affect non-marked Treasure rooms                                       |
| 18    | Cold Comfort    | Ancients only heal up to 30% of your max HP between acts.          | Max heal is equivalent to base rest site healing.                                                                                        |
| 19    | Unprepared      | Draw 1 less card at the start of each combat.                      |                                                                                                                                          |
| 20    | Locked-In       | Your starting deck gains Eternal.                                  | Includes all cards gained during Neow/Floor 1, some events and their options are disabled if entire deck is eternal (may encounter bugs) |

> [!IMPORTANT]
> 
> There are a few ways to unlock Ascension 11+:
> 1. Winning a run on the previous ascension
> 2. Confirm unlocking Ascension 11 in the ascension select menu
> 3. Running `unlock ascensions [<level:int>] [<character>|multiplayer] ` in the dev console

## Disclaimers

- Balance has not been thoroughly tested

## Requirements

- [BaseLib](https://github.com/Alchyr/BaseLib-StS2/releases)

## Installation

1. Download the `GlutonsAscensions.<version>.zip` from [latest release](https://github.com/Gluton-Official/GlutonsAscensions/releases/latest)
2. Place the unzipped contents in `Slay the Spire 2/mods/GlutonsAscensions/`

## Planned Features

- Potentially make multiplayer runs at Ascension 10 and below not require disabling this mod if other players don't have the mod if other players don't have the mod if other players don't have the mod if other players don't have the mod

## Notes

- Ascension functionality is done pretty much exclusively through Harmony patches, but would be much better as an `AscensionModel` class that extends `AbstractModel`, in the same way as relics and run modifiers