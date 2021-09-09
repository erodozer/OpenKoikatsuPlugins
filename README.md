# OpenKoikatsuPlugins

Monorepo of open source plugins for Koikatsu that focuses on improved gameplay functionality and immersion.

These mods attempt to utilize as much of the original game's code paths instead of circumventing whenever possible.
This makes the extensions relatively unintrusive and portable.

## Installation
Prerequisites:
* Install [BepInEx 5.4.15](https://github.com/BepInEx/BepInEx/releases) or later.
* Install [KKAPI 1.25.0](https://github.com/IllusionMods/IllusionModdingAPI) or later.
* Install [BepInEx.UnityInput](https://github.com/nhydock/BepInEx.UnityInput)

Download the DLLs from the latest release and move them into BepInEx\plugins under your game directory.

## Plugins

### TopMe

Allows the AI to take charge of things in the bedroom.

#### Pick Position

Different positions and actions are chosen automatically, allowing for a more passive experience.
This will even juggle new positions added with [AnimationLoader](https://github.com/IllusionMods/AnimationLoader)

#### Edging

AI is capable of edging on intervals, preventing the male gauge from always reaching maximum.  When in edge state the male guage will begin to decrease.  Edging by default is available in all service and female initiated insertion positions, but can be configured to be available on all positions.

#### Auto Orgasm

Allows for male orgasm to occur automatically when the gauge is maxed out, similar to female characters.

### Immersion

Adds new gameplay balancing mechanics to improve immersion.

#### Full Stomach
  limit how many times you can accept invitations for lunch, which can be increased based on player's strength level.

#### Sore Member
  limit how many times you can accept H invites per day, as well as impacts the rate of male excitement based on number of achieved orgasms.  
  Increasing the player's HLevel will decrease the rate at which they become sore, as well as increase how much they recover between periods.
  H ends automatically when limit is reached.

#### Empty Stomach
  Skipping lunch will decrease the player's strength and Hlevel.  If the player attempts to H, their orgasm limit will be severely impacted.

#### Cherry Boy
  Sensitivity is amplified based on inverse of the player's experience.  By having more H, sensitivity decreases.  If you go long periods without having H, you may become sensitive again.
