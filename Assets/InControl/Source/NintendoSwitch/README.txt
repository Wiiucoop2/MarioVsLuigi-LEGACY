USAGE:

To add this module to InControl, unzip and add to your InControl folder and then edit InputManager.cs to see if the following code is already there (it should be in more recent versions):

In SetupInternal(), approximately around line 150:

#if UNITY_SWITCH
if (NintendoSwitchInputDeviceManager.Enable())
{
	enableUnityInput = false;
}
#endif


NOTE:

Due to the complex nature of input on the Switch (grips, modes, styles, etc.) you’ll need to familiarize yourself with it through Nintendo's documentation on input as the defaults set up by InControl are not necessarily going to be right for your game. Also, you should open up NintendoSwitchInputDeviceManager.cs and read through the top few methods. I’ve tried to comment it thoroughly because there are a lot of configuration options and they are not exposed through the Unity inspector.

It should also go without saying that due to Nintendo’s NDA, you should not share this code with anyone or post it anywhere publicly.

