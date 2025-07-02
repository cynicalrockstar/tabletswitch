# Tablet DPI Switcher

**Updated for 2025**

The project has been ugpraded to a .NET 8.0 project, with a small C++ DLL to do the grungy system work.

This app changes the screen scaling when you attach or detach your Surface keyboard, to 150% when undocked (the default for Surface devices) and to 125% when docked. You can change these scaling numbers by modifying the DesktopDpi and TabletDpi settings in appsettings.json.

This version dispenses with the registry changes from 2018 (which don't work any more) and borrows some C++ code from [here](https://github.com/lihas/windows-DPI-scaling-sample) and [here](https://github.com/imniko/SetDPI) for the undocumented Windows calls to make the switch happen. This should work just fine on both x64 and ARM64 devices, for both Windows 10 and 11. But your mileage may vary.

**Things to know:**

What you're building is a TSR-style app. So once it gets run, it will just stay running forever until it's terminated or the system shuts down.

It's monitoring whether you are DOCKED or not. It does NOT care whether you are in tablet mode or desktop mode. If you want to change scaling after switching between MODES, then you'll need to use the SM_TABLETPC system metric instead of SM_CONVERTIBLESLATEMODE.
