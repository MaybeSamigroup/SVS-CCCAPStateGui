# SVS-CCCAPStateGui

Unity UI version of Clothes, Accessory and Pose state control GUI for SamabakeScramble Character Creation

# Prerequests

 * [BepInEx](https://github.com/BepInEx/BepInEx)
   * v6.0.0 be 725 or later
 * [ByteFiddler]
   * v1.0 or later and suitable configuration
 * [BepInEx.ConfigurationManager](https://github.com/BepInEx/BepInEx.ConfigurationManager)
   * v18.3 or later
 * [CCPoseLoader](https://github.com/MaybeSamigroup/SVS-Fishbone)
   * 2.0.1 or later

Confirmed working under SVS 1.1.4 + [SVS-HF Patch](https://github.com/ManlyMarco/SVS-HF_Patch) 1.6 environment.

# Installation

Extract the release to game root.

# How to use

Start character creation then you'll see addtional menu.

# Configuration

 * InitialVisibility

   Whether GUI is visible or not when Character Creation starts.

 * TranslationDir

   Where to load from .json format Translation Dictionary.

   Relative path from:

   ``(game root)/BepInEx/config/CCCAPStateGui``

# Translation dictionary format

## gui.json

 * Key
   Internal enum name
 * Value
   display name

## names.json

 * Key
   (Asset bundle path relative from ```(game root)```/abdata:AnimatorController asset name):(AnimationClip Name)
 * Value
   display name

# Preview

![previwe](https://github.com/user-attachments/assets/1e13ca62-2758-48f5-8148-b7a173427c55)
