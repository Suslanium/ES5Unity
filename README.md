# ES5Unity
A WIP project whose goal is to run TES V: Skyrim on the Unity engine.

##### Credits:
+ [UESP documentation for ESP/ESM file format](https://en.uesp.net/wiki/Skyrim_Mod:Mod_File_Format)
+ [UESP documentation for BSA file format](https://en.uesp.net/wiki/Skyrim_Mod:Archive_File_Format)
+ [Niftools NifXml (NIF file format specification)](https://github.com/niftools/nifxml)
+ [TESUnity (a lot of code was copied from this project)](https://github.com/ColeDeanShepherd/TESUnity)
+ [BSAManager (code for hash calculation was taken from here)](https://github.com/philjord/BSAManager)

# Current state
This project is in a very early stage of development. Currently it can load interior cells from Skyrim at a very basic level (no lighting, no doors, no collisions, not all objects are imported, materials are not quite right).
Some screenshots:
<p>
  <img src="https://github.com/Suslanium/ES5Unity/assets/84632927/a8e1e268-59d1-4d51-be10-d59798d347f2" width="960" height="540">
  <img src="https://github.com/Suslanium/ES5Unity/assets/84632927/9a994c69-359d-40b1-ad1f-76e595a06229" width="960" height="540">
  <img src="https://github.com/Suslanium/ES5Unity/assets/84632927/57979a2c-e025-49be-b763-882b7a50bd14" width="960" height="540">
  <img src="https://github.com/Suslanium/ES5Unity/assets/84632927/993d433b-9cf3-4d33-8268-b739d92e4e8a" width="960" height="540">
  <img src="https://github.com/Suslanium/ES5Unity/assets/84632927/c0b93bbd-63a3-4595-abf9-5d08a08a71da" width="960" height="540">
</p>

##### TODO list:
+ Add support for cell lighting import
+ Add support for transparent materials
+ Figure out glossiness and specular map tint(currently some objects are too glossy, and some objects have an exagerrated specular highlight tint)
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
