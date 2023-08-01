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
![Screenshot 1](https://github.com/Suslanium/ES5Unity/assets/84632927/0b1ce4a7-0b9c-4b22-a32d-07d636c01414)
![Screenshot 2](https://github.com/Suslanium/ES5Unity/assets/84632927/bfd2bfbc-a217-4cb2-a374-23c837eb7b28)
![Screenshot 3](https://github.com/Suslanium/ES5Unity/assets/84632927/11a898f1-956c-4f40-a3f4-489bc27a07b2)
![Screenshot 4](https://github.com/Suslanium/ES5Unity/assets/84632927/e4a85442-9ec9-4973-b810-a529729eedf0)
![Screenshot 5](https://github.com/Suslanium/ES5Unity/assets/84632927/aa940035-fb04-4cf4-83a5-456547295e64)

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
