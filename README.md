# SVS-CCCAPStateGui

Unity UI version of Clothes, Accessory and Pose state control GUI for SamabakeScramble Character Creation

# Prerequests

 * BepInEx v6
 * CCPoseLoader 2.0.0 or later
 
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
