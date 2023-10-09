# ❄️This project is currently frozen as I don't really have enough time to work on it (and spoiler alert, I probably won't be working on this anytime soon). Right now the code quality is pretty rough, but this is more like a draft. I just want to implement some basic stuff (loading locations and being able to walk) and then refactor the code, add other features, etc. Currently this works ONLY with Skyrim LE (aka Oldrim). SE/AE support will be added once basic features are implemented.

# ES5Unity
A WIP project whose goal is to run TES V: Skyrim on the Unity engine.

##### Credits:
+ [UESP documentation for ESP/ESM file format](https://en.uesp.net/wiki/Skyrim_Mod:Mod_File_Format)
+ [UESP documentation for BSA file format](https://en.uesp.net/wiki/Skyrim_Mod:Archive_File_Format)
+ [Niftools NifXml (NIF file format specification)](https://github.com/niftools/nifxml)
+ [TESUnity (a lot of code was copied from this project)](https://github.com/ColeDeanShepherd/TESUnity)
+ [BSAManager (code for hash calculation was taken from here)](https://github.com/philjord/BSAManager)

# Current state
This project is in a *very* early stage of development. Currently it can load interior cells from Skyrim at a very basic level (no doors, no collisions, not all objects are imported, etc).
Some screenshots:
![Screenshot1](https://github.com/Suslanium/ES5Unity/assets/84632927/e421be83-2705-43c4-acaa-31e6edb41fd8)
![Screenshot2](https://github.com/Suslanium/ES5Unity/assets/84632927/df8542ea-e79d-4df0-9a9f-5ffda1cb2812)
![Screenshot3](https://github.com/Suslanium/ES5Unity/assets/84632927/d42ca88f-82db-4c60-bd5f-c57063e441b7)
![Screenshot4](https://github.com/Suslanium/ES5Unity/assets/84632927/ac75c897-fcc7-441f-934c-87597e827620)
![Screenshot5](https://github.com/Suslanium/ES5Unity/assets/84632927/d2459143-593f-4af1-ab7c-af198e8c11af)
![Screenshot6](https://github.com/Suslanium/ES5Unity/assets/84632927/ab9491ba-46ee-4c7b-aab3-0814fffecc1c)

##### TODO list:
+ ~Add support for cell lighting import~ Done(at a basic level)
+ ~Add support for transparent materials~ Done
+ ~Figure out glossiness and specular map tint(currently some objects are too glossy, and some objects have an exagerrated specular highlight tint)~ Kind of done, though shaders are still not perfect at all
+ **Add occlusion culling** (this is the main problem right now, large locations cause extremely low fps because the GPU is trying to render stuff that the player can't see)
+ Add support for collisions
+ Add support for Skyrim SE meshes and archives
+ Add support for billboards
+ Add support for doors inside cells
+ Figure out exterior cell loading
+ Add a player that can walk around and explore the world
+ Add collectable items
+ Add support for nif-embedded animations
+ Add support for skinned meshes and skeletons
+ Add support for .hkx animations (this may be not possible)
+ Add support for effects (magic, fx, etc.) (this may be not possible)
+ Etc.
