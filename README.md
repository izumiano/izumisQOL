# izumisQOL
izumi's quality of life

Currently available quality of life tools:

Swapping between multiple customizable keybind configurations

A whitelisting tool for mods

The ability to save what your journal stats are at one point and compare them with what they are currently

Showing an individual mod's time and death totals in the journal

Automatic pausing when your controller disconnects/freezes

An optional restart button in the main menu

(EXPERIMENTAL) Swapping modded keybind settings

Usage Instructions:

Keybind Swapper:

To be able to swap between keybindings you first need to save your settings. To do so go into 'Mod Settings' and find 'izumi's quality of life'. Once there open 'Binding Settings' and press 'Copy Current Keybinds Here'. This will save the keybinds you currently have configured in your celeste settings. 

To add another configuration press 'Add', select the new keybind slot, reconfigure your keybinds in the celeste settings and again press 'Copy Current Keybinds Here'.

Now you have two different keybind configurations saved!
These can now be swapped between by either binding a key to load them or by selecting the configuration you want in your 'Mod Settings'. 
:)

Whitelister:

The whitelister lets you set up multiple lists of mods you want enabled. To use the whitelister you first need some whitelists saved. Saving a whitelist can be done by going into 'Mod Settings', then find 'izumi's quality of life'. Once there open 'Whitelist Settings' and press 'Save Current Whitelist'. This will save your currently enabled mod to the selected whitelist.

To add another whitelist configuration press 'Add', then in your mod settings enable the mods you want in this whitelist. After that make sure you have the correct whitelist selected and press 'Save Current Whitelist'.

If you now press 'Apply Current Whitelist', the next time you open celeste only the mods in that whitelist will be enabled. If you would rather want the whitelist to keep your current mods enabled and only add the ones in the whitelist set 'Is Exclusive' to 'Off'.

Save Journal Snapshot:

The journal snapshot tool lets you save the information in the (vanilla and collabutils2) journal to later compare with. If you have something saved it will then let you swap between viewing the regular journal, showing a-, b- and c-side info seperately (if you have the option enabled), showing the info you saved and showing the difference between what you saved and what it is currently.

To use it first bind a button to 'Save journal snapshot' in the mod settings. Then open the journal(must be either the vanilla journal or a collabutils2 journal like the one in strawberry jam) and press the button you bound. Once you've done that you should be able to press up or down while in the journal to scroll through the different journal modes.

Automatic controller-freeze pausing:
If you enable the gamepad-pauser in the settings menu the game will pause if your controller disconnects or if the controller freezes for the amount of frames specified in settings (defualt: 10).

OBS Recording/Streaming Indicator:

If enabled this will let you connect celeste to obs and show a warning icon in the corner if you're not recording/streaming. Even if you have enabled it you can also temporily suppress the warning for times when you're not intending to record.
To use it, first you need to enable websockets in OBS which you can do in from |Tools->WebSocket Server Settings->Enable WebSocket server|. You can also find the websockets server port and password here.
After you have enabled websockets, in celeste go to |Mod Settings->izumi's quality of life->OBS Integration Settings| and set OBS Integration Enabled to On. 
Then import your host:port(localhost:4455 by default) and password from your clipboard and then click Connect. If it managed to connect properly you will get a message in the bottom left saying "Connected!". 
Final step now is to turn on Check Recording Status, Check Streaming Status and/or Check Replay Buffer Status.

NoClip:

NoClip is a mode where if enabled, Madeline can fly and has no collision letting her for example fly through walls. You can bind a button to enable/disable it or do so in NoClip Settings. Holding grab will let you move faster, holding dash lets you move slower. The speed can also be changed in the settings.

(Experimental) Mod Keybind Swapper:

If you want to try swapping modded keybinds go to your celeste folder then 'Saves/izumisQOL/keybinds' and open whitelist.txt. In here write the names of all the mods you want to be able to swap the keybinds of. Once you've done this everything should function exactly like the vanilla keybind swapper.

:3
