# NeosMotionBlurOverride
A Plugin for NeosVR to remove Motion Blur in desktop mode


**This component is to be used for testing only.**

**Please do not include it in any public distributions.**

**Message Epsilion for more details.**

# Installation
* Download the latest NeosMotionBlurOverride.dll from [Releases](https://github.com/Aerizeon/NeosMotionBlurOverride/releases)
* Copy the dll to the NeosVR `Libraries` directory
* Add `-LoadAssembly "Libraries\NeosMotionBlurOverride.dll"` to your NeosVR launch options.
* Open your "Local" Home world in Neos
* Create a slot named "Plugins" on root, if it does not exist.
* Attach the "MotionBlurOverride" component from the "Epsilion" folder.
* Save the world, so that the components will load when Neos is started.
