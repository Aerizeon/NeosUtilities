# NeosUtilities #
A Plugin for [NeosVR](https://neos.com/) that provides several useful utility features

**Save a backup of your items, before using these tools**

**I am not responsible for any work lost because you didn't save a backup**

**Extensively test all LogiX after using this these tools, as things may break in unexpected ways**


## Simple Utilities ##

### Tools ###
* **Logix Cleanup Tool**
  * Removes all unused `LogixReference`, `LogixInterfaceProxy` and `Relay` nodes from a given slot and its children.
* **MonoPack Tool**
  * Packs all child LogiX nodes under a single slot.
* **Motion Blur Override**
  * Enables or disables Motion Blur on the currently used HeadOutputDevice
* **Avatar Stripping Tool**
  * Removes all non-IK components from an avatar, so it can be used as a prop in the world.

### Installation ###
* Download the latest NeosSimpleUtilities.dll from [Releases](https://github.com/Aerizeon/NeosUtilities/releases)
* Copy the dll to the NeosVR **Libraries** directory
* Add `-LoadAssembly "Libraries\NeosSimpleUtilities.dll"` to your NeosVR launch options.
* Open your **Local** Home world in Neos
* Create a slot named `Plugins` on root, if it does not exist.
* Attach the components you wish to load from the **Add-Ons/Optimization** folder.

## Advanced Utilities ##

*Nothing here yet!*
