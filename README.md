# OpenKoikatsuPlugins

Monorepo of open source plugins for Koikatsu that focuses on improved gameplay functionality and immersion.

These mods attempt to utilize as much of the original game's code paths instead of circumventing whenever possible.
This makes the extensions relatively unintrusive and portable.

## Installation
Prerequisites:
* Install [BepInEx 5.4](https://github.com/BepInEx/BepInEx/releases) or later.
* Install [KKAPI 1.20.0](https://github.com/IllusionMods/IllusionModdingAPI) or later.

Download the DLLs from the latest release and move them into BepInEx\plugins under your game directory.

## Plugins

### OK.TopMe

Allows the AI to take charge of things in the bedroom.  Different positions and actions are chosen automatically, allowing for a more passive experience.

### OK.Immersion

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
