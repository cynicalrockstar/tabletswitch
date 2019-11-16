Changes the screen scaling when you dock or undock your Surface, to 200% when undocked (the default for Surface devices) and to 150% when docked. Anyone with a minimal understanding of C# should be able to figure out exactly what the code is doing and modify the specifics for their own circumstances.

Things to know:

Before building, you'll also need to go into the registry and find your own monitor ID to put in the ChangeDPI method.

If you build this in Visual Studio, you'll get a Win32 binary. So if you want to use it on an X, build it at the command line with the appropriate switches to generate an ARM64 executable:

dotnet publish -c Release -r win-arm64

What you're building is a TSR-style app. So once it gets run, it will just stay running forever until it's terminated or the system shuts down.

It's monitoring whether you are DOCKED or not. It does NOT care whether you are in tablet mode or desktop mode. If you want to change scaling after switching between MODES, then you'll need to use the SM_TABLETPC system metric instead of SM_CONVERTIBLESLATEMODE.
