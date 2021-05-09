# NeosUtilities #
A Plugin for [NeosVR](https://neos.com/) that provides several useful utility features

**This component is to be used for testing only.**

**Please do not include it in any public distributions.**

**Message Epsilion for more details.**


## Simple Utilities ##

### Tools ###
* **Logix Cleanup Tool**
  * Removes all unused `LogixReference`, `LogixInterfaceProxy` and `Relay` nodes from a given slot and its children.
* **MonoPack Tool**
  * Packs all child LogiX nodes under a single slot.
* **Motion Blur Override**
  * Enables or disables Motion Blur on the currently used HeadOutputDevice

### Installation ###
* Download the latest NeosSimpleUtilities.dll from [Releases](https://github.com/Aerizeon/NeosUtilities/releases)
* Copy the dll to the NeosVR **Libraries** directory
* Add `-LoadAssembly "Libraries\NeosSimpleUtilities.dll"` to your NeosVR launch options.
* Open your **Local** Home world in Neos
* Create a slot named `Plugins` on root, if it does not exist.
* Attach the components you wish to load from the **Epsilion/Utilities** folder.
