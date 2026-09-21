# Second Dimension Windows branding

The update165 Windows executable contains the SD dimensional-portal emblem. SECOND_DIMENSION.ico includes 16, 24, 32, 48, 64, 128 and 256 pixel images. SECOND_DIMENSION.png is the full-size editable-source reference, generated with the built-in image-generation tool. Scaling and ICO conversion preserve the design.

The restored Unity project still contains its historical player-icon settings. After making a new unsigned Windows build, apply the supplied icon to a new output executable with Windows PowerShell:

```powershell
.\Set-GameIcon.ps1 -Executable 'D:\Build\SECOND_DIMENSION_GUILD_OF_WORLDS.exe' -OutputExecutable 'D:\Build\SECOND_DIMENSION_BRANDED.exe'
```

After verification, use the branded file as SECOND_DIMENSION_GUILD_OF_WORLDS.exe beside the matching data folder. Keep the input until the new build has passed startup checks. The script refuses an existing output and a valid signed input; apply branding before code signing. It verifies every embedded icon, preserves all other resources, and compares every non-resource PE section. Python and Unity are not required for this branding step.

Alternatively, assign SECOND_DIMENSION.png as the application's default icon in Unity Player Settings before a future full build. Update165 uses the verified Windows resource step and has not claimed a new full Editor build.

Resource API reference: [Microsoft UpdateResourceW](https://learn.microsoft.com/en-us/windows/win32/api/winbase/nf-winbase-updateresourcew).

Exact built-in generation prompt:

> Use case: logo-brand. Production Windows app icon for the fantasy card RPG Second Dimension: Guild of Worlds. A single polished rounded-square game emblem, filling a square canvas: a bold interlocking SD monogram in warm metallic gold, centered over one luminous turquoise dimensional portal ring with a subtle layered playing-card silhouette behind it. Deep midnight indigo enamel background inside the rounded square, refined gold rim, restrained magical light. Strong simple shapes and high contrast so SD and the portal remain recognizable at 32 pixels. Professional premium fantasy game branding, clean finished icon, balanced symmetric composition. Exactly the letters SD, no additional text, no Unity logo, no mockup, no scenery, no small decorative details outside the emblem. Transparent outside the rounded square. Square image.
