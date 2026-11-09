# LaneLog

LaneLog keeps your bowling scores and notes on this device.

## Use LaneLog

1. Tap **Add game** and enter the date and final score (a whole number from 0 to 300).
2. Choose a saved bowling alley, select **Add a new alley...** and enter its name, or choose **No alley**.
3. Add any notes. Optionally, choose a photo or take one with the camera, then tap **Save game**.

The alley list comes from saved games. A new name appears after you save it with a game and may disappear if no saved game uses it.

Tap a game on Home to edit it. Home also shows your game count, average score, and best score.

You can attach one score photo to each game. Replacing or removing a photo takes effect when you save the game; Cancel or Back discards the change. LaneLog stores its own copy and leaves the original in your gallery. Device backups may include LaneLog's data and photos.

## Run on Android

Open the LaneLog solution in VS Code, select the MAUI app as the startup project, and choose an Android emulator or device. You can also deploy from the repository folder with:

```powershell
dotnet build src\LaneLog.App\LaneLog.App.csproj -t:Run -f net11.0-android -c Debug -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk"
```

The project currently targets .NET 11 preview. The Android SDK and MAUI workload must be installed.

## Current limits

Photo selection and camera capture are implemented in code. I confirmed that Android opens the photo picker, but I have not tested selecting an image, saving it, or reopening it after restarting the app. Camera capture is also untested. If Android closes LaneLog while the picker is open, the app does not recover the draft or picker result. Settings and game deletion are not implemented.
