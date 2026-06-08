# Player Spawn Debugging Guide

## Problem
Player is not spawning despite Fusion logs showing `[Fusion] adding player [Player:1]`. The `OnPlayerJoined()` callback is never being invoked.

## Changes Made
Enhanced `GameNetworkManager.cs` with comprehensive logging at critical points:

### 1. **Callback Registration Phase**
```
[Network] ═══ CALLBACK REGISTRATION START ═══
[Network] _currentRunner is null? {false}
[Network] _currentRunner reference: {hashcode}
[Network] this reference: {hashcode}  
[Network] this is INetworkRunnerCallbacks? {true}
[Network] ═══ CALLBACK REGISTRATION SUCCESS ═══
```

**What to look for:**
- Should see both START and SUCCESS messages
- If you only see START but not SUCCESS → Exception occurred (check logs above)
- If neither appears → callback registration code was skipped

### 2. **Fusion Startup Phase**
```
[Network] ═══ FUSION STARTUP START ═══
[Network] Starting Fusion with GameMode=AutoHostOrClient, RoomName=Phong_Ragdoll_Direct
[Network] ═══ FUSION STARTUP SUCCESS ═══
[Network] LocalPlayer: Player:1
```

**What to look for:**
- Should complete successfully
- LocalPlayer should show a valid player reference (e.g., `Player:1`, not `Player:-1`)
- If STARTUP fails, check ShutdownReason in logs

### 3. **Player Joined Callback**
```
[Network] ╔════════════════════════════════════════╗
[Network] ║ OnPlayerJoined CALLBACK CALLED!        ║
[Network] ╚════════════════════════════════════════╝
[Network] Player joined: Player:1
[Network] Callback Runner HashCode: {A}
[Network] Stored _currentRunner HashCode: {B}
[Network] Are they same instance? {true/false}
[Network] LocalPlayer: Player:1
[Network] Is local player? true
[Network] ✓ Player is local player, spawning...
```

**Critical checks:**
- **If you see this box** → Callback IS being called ✓
- **If you DON'T see this box** → Callback is NOT being invoked ✗
- If hashcodes don't match → Different runner instance! (Major issue)
- If "Is local player? false" → Non-local player joined (expected for other players, but our code should handle this)

### 4. **Player Spawn Phase**
```
[Network] ═══ SpawnLocalPlayer START ═══
[Network] Player: Player:1
[Network] _localPlayerObject already exists? false
[Network] Spawning player at position: (0.00, 2.00, 0.00)
[Network] Runner: {hashcode}
[Network] PlayerPrefab: Player
[Network] ✓ Player spawned successfully: Player(Clone)
[Network] Spawned object has NetworkObject? true
[Network] CharacterInput found? true
[Network] ═══ SpawnLocalPlayer END ═══
```

**What to look for:**
- Complete sequence should appear if callback was called
- If any part is missing → callback wasn't reached
- If PlayerPrefab shows null → Prefab not assigned in BootScene

## Test Sequence

Run the game and look for these log phases IN ORDER:

1. **[UI] Step 4: Starting game via GameNetworkManager...** (from UIManager)
2. **[Network] ═══ StartGameMatchCoroutine START ═══** (Start of network setup)
3. **[Network] ═══ CALLBACK REGISTRATION START ═══** through **SUCCESS** (Callbacks added)
4. **[Network] ═══ FUSION STARTUP START ═══** through **SUCCESS** (Fusion ready)
5. **[Fusion] adding player [Player:1]** (Fusion's internal log)
6. **[Network] ║ OnPlayerJoined CALLBACK CALLED!║** ← **THIS IS THE KEY CHECK**
7. **[Network] ═══ SpawnLocalPlayer...** (Player spawn sequence)
8. **[UI] LoadingScene loaded** (UI transition)

## Diagnosis Matrix

| Symptom | Likely Cause |
|---------|-------------|
| Steps 1-5 OK, but no step 6 | Callbacks not registered OR wrong runner instance |
| Step 6 appears but "Are they same instance? false" | Multiple runners created (major bug) |
| Step 6 doesn't appear, but Fusion log shows player | Fusion issue or callback system problem |
| "Is local player? false" | Player joined but we're not the authority (need debug further) |
| Step 7 doesn't appear | Callback is reached but SpawnLocalPlayer fails |
| "PlayerPrefab: {null}" | Player prefab not assigned in BootScene inspector |

## What to Report

When the issue occurs, capture:
1. Full console log from [UI] Step 4 onwards
2. Note which step the logs stop at
3. Look for any exception stack traces (shown in red)
4. Check if you see the OnPlayerJoined callback box - YES or NO?

## Root Causes to Investigate

If callback is never called:
1. ✓ Verify prefabs assigned in BootScene
2. ? Check if GameNetworkManager exists at all
3. ? Check if AddCallbacks() is throwing exception (caught by try/catch)
4. ? Check Fusion version compatibility
5. ? Check if multiple runner instances are being created

If callback IS called but player doesn't spawn:
1. ? SpawnLocalPlayer exception (check logs)
2. ? CharacterInput not found
3. ? NetworkObject not properly configured on prefab
