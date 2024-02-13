# <img src="https://github.com/Suslanium/ES5Unity/assets/84632927/4e23b155-8d36-472d-90f8-40c148b9b1e4" width="24" height="24"/> ES5Unity
A WIP project whose goal is to run TES V: Skyrim on the Unity engine. Currently this **works ONLY with Skyrim LE (aka Oldrim)**. SE/AE support will be added once basic features are implemented.

**❄️Currently almost frozen due to lack of free time. The development will likely continue in the summer**

##### Credits:
+ [UESP documentation for ESP/ESM file format](https://en.uesp.net/wiki/Skyrim_Mod:Mod_File_Format)
+ [UESP documentation for BSA file format](https://en.uesp.net/wiki/Skyrim_Mod:Archive_File_Format)
+ [Niftools NifXml (NIF file format specification)](https://github.com/niftools/nifxml)
+ [TESUnity (a lot of code was copied from this project)](https://github.com/ColeDeanShepherd/TESUnity)
+ [BSAManager (code for hash calculation was taken from here)](https://github.com/philjord/BSAManager)

##### Setup:
A guide for setting up and running this project in Unity will be made after the first stable 'release' (once some basic stuff will be properly done)

# Current state
This project is in a *very* early stage of development. Currently it can load interior cells from Skyrim at a very basic level (no doors, not all objects are imported, etc).
Some screenshots:
![Screenshot1](https://github.com/Suslanium/ES5Unity/assets/84632927/e421be83-2705-43c4-acaa-31e6edb41fd8)
![Screenshot2](https://github.com/Suslanium/ES5Unity/assets/84632927/df8542ea-e79d-4df0-9a9f-5ffda1cb2812)
![Screenshot3](https://github.com/Suslanium/ES5Unity/assets/84632927/d42ca88f-82db-4c60-bd5f-c57063e441b7)
![Screenshot4](https://github.com/Suslanium/ES5Unity/assets/84632927/ac75c897-fcc7-441f-934c-87597e827620)
![Screenshot5](https://github.com/Suslanium/ES5Unity/assets/84632927/d2459143-593f-4af1-ab7c-af198e8c11af)
![Screenshot6](https://github.com/Suslanium/ES5Unity/assets/84632927/ab9491ba-46ee-4c7b-aab3-0814fffecc1c)

##### TODO list:
+ ✅**Add support for cell lighting import** *(Done at a basic level)*
+ ✅**Add support for transparent materials** *(Done)*
+ ✅**Figure out glossiness and specular map tint** *(Kind of done, though shaders are still not perfect at all)*
+ ⚠️**Add occlusion culling** *(It works, but it is still far from perfect. Most likely it will be rewriteen from scratch later)*
+ ✅**Add support for collisions** *(Almost done; convex shapes and compressed meshes are supported as of now)*
+ ✅**Add a player that can walk around and explore the world** *(Done)*
+ ✅**Optimize cell loading** *(Done, the cells now load with almost no lag)*
+ ✅**Add support for doors inside cells** *(Done, the door teleports now work correctly)*
+ 🔄*Refactor the code and add comments/documentation* *(Right now the code quality is pretty rough, there are some very long files, a lot of code that may be difficult to understand and maintain in the future, etc.)*
+ 🔲Figure out exterior cell loading *(Terrains, cell grid, LODs, etc)*
+ 🔲Add support for Skyrim SE meshes and archives
+ 🔲Add support for billboards
+ 🔲Add collectable items
+ 🔲Add support for nif-embedded animations
+ 🔲Add support for skinned meshes and skeletons
+ 🔲Add support for .hkx animations (this may be not possible)
+ 🔲Add support for effects (magic, fx, etc.) (this may be not possible)
+ 🔲Etc.
