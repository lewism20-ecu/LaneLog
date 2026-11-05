# LaneLog

LaneLog is a bowling score tracker. Your games are saved on this device.

## Use LaneLog

1. Tap **Add game** and enter the date and final score (a whole number from 0 to 300).
2. Choose a saved bowling alley, select **Add a new alley...** and enter its name, or choose **No alley**.
3. Add any notes and tap **Save game**.

New alley names become choices after you save the game. Choices come from your saved games, so a name disappears from the list if no saved game uses it.

Tap a game on Home to edit it. Home also shows your game count, average score, and best score.

## Run on Android

Open the LaneLog solution in VS Code, select the MAUI app as the startup project, and choose an Android emulator or device. You can also deploy from the repository folder with:

```powershell
dotnet build src\LaneLog.App\LaneLog.App.csproj -t:Run -f net11.0-android -c Debug -p:AndroidSdkDirectory="$env:LOCALAPPDATA\Android\Sdk"
```

The project currently targets .NET 11 preview. The Android SDK and MAUI workload must be installed.

## Current limits

Photo attachments, Settings, and deleting games are not implemented yet.
