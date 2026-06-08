# Next Steps - Diagnosing Player Spawn Issue

## Current Status
Enhanced logging has been added to `GameNetworkManager.cs` at all critical points in the player spawn flow. This will help identify where the breakdown occurs.

## What Changed
1. **StartGameMatch() and StartGameMatchCoroutine()** - Enhanced with detailed phase markers
2. **OnPlayerJoined()** - Added runner instance verification 
3. **SpawnLocalPlayer()** - Added detailed spawn diagnostics
4. **SetupLocalPlayerAfterSpawn()** - Added exception handling and component verification

## Steps to Test

### 1. Build and Run
```
1. Open the project in Unity Editor
2. Go to File > Build and Run (or run in editor)
3. Click "Quick Play"
4. Select a character and click "Lock In"
5. Watch the console
```

### 2. Check Console Output
Open Unity Console and look for the log sequence. The CRITICAL check is:

```
[Network] ║ OnPlayerJoined CALLBACK CALLED!        ║
```

**If you see this box:**
- ✓ Callback IS working
- Check if player spawns after this
- If no spawn, look at next phase logs

**If you DON'T see this box:**
- ✗ Callback is NOT being called
- Check ALL logs from [Network] ═══ CALLBACK REGISTRATION START ═══
- Look for any red exceptions above it

### 3. Capture Full Log
When testing, capture the ENTIRE console output from:
- `[UI] Step 4: Starting game via GameNetworkManager...` 
- TO the end

You can:
- Right-click in Console → Copy All
- Or screenshot the console
- Or check `Player.log` in project root

### 4. Analyze Using Diagnosis Matrix
Refer to `PLAYER_SPAWN_DIAGNOSTICS.md` "Diagnosis Matrix" section to match your symptom to the likely cause.

## Key Files Modified
- `Assets/Scripts/Networking/GameNetworkManager.cs` - Enhanced with diagnostic logging

## Key Files to Verify
- `Assets/Scenes/BootScene.unity` - Verify GameNetworkManager prefabs assigned
- `Assets/Prefabs/Player.prefab` - Verify has NetworkObject component
- `Assets/Prefabs/PhotonRunnerPrefab.prefab` - Verify has NetworkRunner component

## If Issue Persists

If you still don't see the callback firing after running with the new logs, the problem is likely:

1. **Callbacks not registered** → Check if AddCallbacks throws exception
2. **Wrong runner instance** → Check hashcodes match
3. **Fusion version issue** → May need to research compatibility

At that point, please share:
1. The FULL console log (from Start to where it stops)
2. Which log phase you last see
3. Any red exception text

This will help determine if it's a:
- Configuration issue (prefabs)
- Code issue (exception)  
- Architecture issue (multiple runners)
- Fusion compatibility issue

## Quick Verification Checklist

Before running, verify:
- [ ] BootScene.unity has GameNetworkManager component
- [ ] GameNetworkManager has runnerPrefab assigned (not empty)
- [ ] GameNetworkManager has playerPrefab assigned (not empty)
- [ ] PhotonRunnerPrefab has NetworkRunner component
- [ ] Player.prefab has NetworkObject component
- [ ] Scripts compile without errors (bottom of Unity editor should show ✓)

## When You Have Results

Once you test and see the logs:
1. Share the console output
2. Specifically note: **Do you see the "OnPlayerJoined CALLBACK CALLED" box?** (YES/NO)
3. Share the last [Network] log that appears before the game stops responding

This will pinpoint the exact cause!
