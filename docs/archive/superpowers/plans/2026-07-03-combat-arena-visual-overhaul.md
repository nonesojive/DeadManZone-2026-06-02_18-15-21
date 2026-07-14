> **SUPERSEDED - DO NOT DESIGN FROM THIS FILE.**
> This document is archived history. Systems described here have been renamed,
> replaced or deleted (Morale as a run resource, Gold, 8x2 reserves, 6 shop slots, ...).
> **The authoritative design is [`docs/GDD.md`](../../../GDD.md).** See `docs/archive/README.md`.

---

# CombatArena2D Visual Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make CombatArena2D fights readable, weighty, and emotionally clear for the IronMarch Union slice while preserving deterministic combat replay.

**Architecture:** Keep `TickCombatRun` and `CombatEventLog` authoritative. Extend the existing presentation replay layer: `CombatArenaPresenter` maps combat events to `CombatUnitActor`, `CombatUnitVisual2D`, `CombatArena2DVfx`, `CombatArenaAudioPresenter`, `TacticPausePanel`, and `BattleReportPresenter`. Add focused test hooks only where serialized UI fields are otherwise inaccessible.

**Tech Stack:** Unity 6.0.3.8f1 project, C#, NUnit EditMode tests, Unity PlayMode tests, existing Canvas/TMP UI and CombatArena2D sprite pipeline.

---

## Test Commands

The project version is `6000.3.8f1`. Expected editor command path:

```powershell
$Unity = "C:\Program Files\Unity\Hub\Editor\6000.3.8f1\Editor\Unity.exe"
Test-Path $Unity
```

Expected: `True`. If this returns `False`, stop and ask for the local Unity editor path before running tests.

Filtered EditMode command:

```powershell
& $Unity -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Presentation.Tests.EditMode.CombatUnit2DStripPlayerTests" `
  -testResults "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\TestResults-EditMode.xml" -quit
```

Filtered PlayMode command:

```powershell
& $Unity -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform playmode `
  -testFilter "DeadManZone.PlayMode.Tests.CombatArenaReplayPlayModeTests" `
  -testResults "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\TestResults-PlayMode.xml" -quit
```

No git commits should be created unless the user explicitly asks for a commit.

---

## File Structure

- Modify `Assets/_Project/Presentation/Combat/Arena/CombatUnit2DStripPlayer.cs`
  - Owns animation state availability, one-shot lock/release behavior, and final-frame death lock.
- Modify `Assets/_Project/Presentation/Combat/Arena/CombatUnitVisual2D.cs`
  - Chooses idle/walk/run, plays hurt/hit-react, and exposes death duration for presenter timing.
- Modify `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs`
  - Converts movement speed/distance into walk/run presentation requests while keeping replay anchors authoritative.
- Modify `Assets/_Project/Presentation/Combat/Arena/ICombatArenaVfxPresenter.cs`
  - Adds event-specific methods for graze and environmental damage.
- Modify `Assets/_Project/Presentation/Combat/Arena/CombatArena2DVfx.cs`
  - Adds readable graze/environment damage presentation and soft caps/staggers damage text.
- Modify `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs`
  - Maps `gas_damage`, `graze`, and `destroyed` to clearer presentation paths.
- Modify `Assets/_Project/Presentation/Combat/TacticPausePanel.cs`
  - Improves opening and mid-fight pause copy and test accessibility.
- Modify `Assets/_Project/Presentation/Combat/BattleReportPresenter.cs`
  - Improves aftermath teaching copy and test accessibility.
- Modify `Assets/_Project/Presentation.Tests/EditMode/CombatUnit2DStripPlayerTests.cs`
  - Adds animation contract tests.
- Modify `Assets/_Project/Presentation.Tests/EditMode/CombatArena2DHelpersTests.cs`
  - Strengthens Bulwark animation asset validation.
- Modify `Assets/_Project/Tests.PlayMode/CombatArenaReplayPlayModeTests.cs`
  - Adds replay presentation tests for death wait and gas damage mapping.
- Modify `Assets/_Project/Tests.PlayMode/TacticPausePanelPlayModeTests.cs`
  - Adds tactical pause copy/readability tests.
- Create `Assets/_Project/Presentation.Tests/EditMode/BattleReportPresenterTests.cs`
  - Adds aftermath formatting tests.

---

### Task 1: Lock Animation State Contract

**Files:**
- Modify: `Assets/_Project/Presentation.Tests/EditMode/CombatUnit2DStripPlayerTests.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatUnit2DStripPlayer.cs`

- [ ] **Step 1: Write failing EditMode tests**

Add these methods and helpers inside `CombatUnit2DStripPlayerTests`:

```csharp
[Test]
public void Shoot_OneShot_ReturnsToIdleUnlocked()
{
    var set = CreateAnimationSet();
    var player = new CombatUnit2DStripPlayer();
    player.Bind(set);
    player.Play(CombatUnit2DAnimState.Shoot);

    player.Tick(1.1f);

    Assert.AreEqual(CombatUnit2DAnimState.Idle, player.State);
    Assert.IsFalse(player.IsLocked);
    Object.DestroyImmediate(set);
}

[Test]
public void Die_OneShot_HoldsFinalFrameLocked()
{
    var set = CreateAnimationSet();
    var player = new CombatUnit2DStripPlayer();
    player.Bind(set);
    player.Play(CombatUnit2DAnimState.Die);

    player.Tick(1.1f);

    Assert.AreEqual(CombatUnit2DAnimState.Die, player.State);
    Assert.IsTrue(player.IsLocked);
    Object.DestroyImmediate(set);
}

[Test]
public void ResolvePlayableState_MissingRun_FallsBackToWalk()
{
    var set = CreateAnimationSet(includeRun: false);
    var player = new CombatUnit2DStripPlayer();
    player.Bind(set);

    Assert.AreEqual(
        CombatUnit2DAnimState.Walk,
        player.ResolvePlayableState(CombatUnit2DAnimState.Run, CombatUnit2DAnimState.Walk));

    Object.DestroyImmediate(set);
}

[Test]
public void ResolvePlayableState_MissingHitReact_FallsBackToHurt()
{
    var set = CreateAnimationSet(includeHitReact: false);
    var player = new CombatUnit2DStripPlayer();
    player.Bind(set);

    Assert.AreEqual(
        CombatUnit2DAnimState.Hurt,
        player.ResolvePlayableState(CombatUnit2DAnimState.HitReact, CombatUnit2DAnimState.Hurt));

    Object.DestroyImmediate(set);
}

private static CombatUnit2DAnimationSetSO CreateAnimationSet(
    bool includeRun = true,
    bool includeHurt = true,
    bool includeHitReact = true)
{
    var set = ScriptableObject.CreateInstance<CombatUnit2DAnimationSetSO>();
    set.idle = MakeStrip(loop: true);
    set.walk = MakeStrip(loop: true);
    set.run = includeRun ? MakeStrip(loop: true) : default;
    set.hurt = includeHurt ? MakeStrip(loop: false) : default;
    set.hitReact = includeHitReact ? MakeStrip(loop: false) : default;
    set.shoot = MakeStrip(loop: false);
    set.die = MakeStrip(loop: false);
    return set;
}

private static CombatUnit2DStrip MakeStrip(bool loop)
{
    var texture = new Texture2D(32, 16, TextureFormat.RGBA32, false);
    texture.SetPixels(new Color[32 * 16]);
    texture.Apply();
    var sprite = Sprite.Create(texture, new Rect(0, 0, 32, 16), new Vector2(0.5f, 0.05f), 16f);
    return new CombatUnit2DStrip
    {
        sheet = sprite,
        frameCount = 2,
        columns = 2,
        framesPerSecond = 2f,
        loop = loop
    };
}
```

- [ ] **Step 2: Run test to verify failure**

Run the filtered EditMode command above.

Expected: `ResolvePlayableState` tests fail because `CombatUnit2DStripPlayer` does not expose that method yet.

- [ ] **Step 3: Implement minimal animation state query**

Add these methods to `CombatUnit2DStripPlayer`:

```csharp
public bool CanPlay(CombatUnit2DAnimState state) =>
    _set != null && ResolveStrip(state).IsValid;

public CombatUnit2DAnimState ResolvePlayableState(
    CombatUnit2DAnimState preferred,
    CombatUnit2DAnimState fallback)
{
    if (CanPlay(preferred))
        return preferred;

    if (CanPlay(fallback))
        return fallback;

    return CombatUnit2DAnimState.Idle;
}
```

- [ ] **Step 4: Run test to verify pass**

Run the filtered EditMode command above.

Expected: `CombatUnit2DStripPlayerTests` pass.

---

### Task 2: Add Hurt And Run Presentation

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatUnitVisual2D.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs`
- Modify: `Assets/_Project/Presentation.Tests/EditMode/CombatArena2DHelpersTests.cs`

- [ ] **Step 1: Write failing Bulwark animation validation**

In `CombatArena2DHelpersTests.SpriteResolver_BulwarkSquad_UsesDedicatedCombatSpriteAndAnimations`, add these assertions after the existing walk assertions:

```csharp
Assert.IsTrue(piece.combatArena2DAnimations.idle.IsValid, "bulwark_squad needs idle frames.");
Assert.IsTrue(piece.combatArena2DAnimations.shoot.IsValid, "bulwark_squad needs shoot frames.");
Assert.IsTrue(piece.combatArena2DAnimations.die.IsValid, "bulwark_squad needs die frames.");
Assert.IsTrue(
    piece.combatArena2DAnimations.hurt.IsValid || piece.combatArena2DAnimations.hitReact.IsValid,
    "bulwark_squad should have hurt or hit-react frames, or the visual fallback must be verified separately.");
```

- [ ] **Step 2: Run test to verify asset validation**

Run:

```powershell
& $Unity -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Presentation.Tests.EditMode.CombatArena2DHelpersTests.SpriteResolver_BulwarkSquad_UsesDedicatedCombatSpriteAndAnimations" `
  -testResults "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\TestResults-EditMode.xml" -quit
```

Expected: either pass if assets are wired, or fail with a precise missing-strip message.

- [ ] **Step 3: Add locomotion and hurt API to `CombatUnitVisual2D`**

Add a private field beside `_walking`:

```csharp
private bool _running;
```

Replace `SetWalking` with a wrapper plus the new method:

```csharp
public void SetWalking(bool walking) => SetLocomotion(walking, running: false);

public void SetLocomotion(bool moving, bool running)
{
    _walking = moving;
    _running = moving && running;
    bool canRun = _animated && running && _animPlayer.CanPlay(CombatUnit2DAnimState.Run);
    if (_animated && !_animPlayer.IsLocked)
    {
        var state = moving
            ? (canRun ? CombatUnit2DAnimState.Run : CombatUnit2DAnimState.Walk)
            : CombatUnit2DAnimState.Idle;
        _animPlayer.Play(state, restart: false);
    }
}
```

Replace `PlayHurt()` with:

```csharp
public void PlayHurt(bool strongHit = false)
{
    if (!_animated || _dying || _animPlayer.IsLocked && _animPlayer.State == CombatUnit2DAnimState.Die)
        return;

    var preferred = strongHit ? CombatUnit2DAnimState.HitReact : CombatUnit2DAnimState.Hurt;
    var fallback = strongHit ? CombatUnit2DAnimState.Hurt : CombatUnit2DAnimState.HitReact;
    var state = _animPlayer.ResolvePlayableState(preferred, fallback);
    if (state != CombatUnit2DAnimState.Idle)
        _animPlayer.Play(state);
}
```

Update `TickAnimation()` locomotion resume logic to:

```csharp
if (!_animPlayer.IsLocked && _walking && _animPlayer.State == CombatUnit2DAnimState.Idle)
{
    var state = _running && _animPlayer.CanPlay(CombatUnit2DAnimState.Run)
        ? CombatUnit2DAnimState.Run
        : CombatUnit2DAnimState.Walk;
    _animPlayer.Play(state, restart: false);
}
```

In `Clear()`, reset `_running = false;` next to `_walking`.

- [ ] **Step 4: Add run selection in `CombatUnitActor`**

In the free-chase movement branch, replace `_visual2D.SetWalking(true);` with:

```csharp
_visual2D.SetLocomotion(moving: true, running: ShouldUseRunPresentation(moveDelta));
```

In the anchored movement branch, replace `_visual2D.SetWalking(true);` with:

```csharp
_visual2D.SetLocomotion(moving: true, running: ShouldUseRunPresentation(delta));
```

Add this private method to `CombatUnitActor`:

```csharp
private bool ShouldUseRunPresentation(Vector3 movementDelta)
{
    movementDelta.y = 0f;
    return movementDelta.sqrMagnitude > 0.35f * 0.35f && _moveWorldSpeed > 3.2f;
}
```

Change `PlayHurt()` to accept a strong-hit flag:

```csharp
public void PlayHurt(bool strongHit = false)
{
    if (_frozen || !IsAlive)
        return;

    _visual2D?.PlayHurt(strongHit);
}
```

- [ ] **Step 5: Run EditMode tests**

Run the filtered EditMode command and the Bulwark helper test command.

Expected: tests pass or clearly identify missing Bulwark asset strips to wire before proceeding.

---

### Task 3: Map Damage Events To Readable VFX

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/ICombatArenaVfxPresenter.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArena2DVfx.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs`
- Modify: `Assets/_Project/Tests.PlayMode/CombatArenaReplayPlayModeTests.cs`

- [ ] **Step 1: Write failing gas damage mapping test**

Add this fake VFX presenter inside `CombatArenaReplayPlayModeTests`:

```csharp
private sealed class RecordingVfxPresenter : ICombatArenaVfxPresenter
{
    public int RifleTracers;
    public int CannonTracers;
    public int Impacts;
    public int Explosions;
    public int Deaths;
    public int GenericDamage;
    public int Grazes;
    public int EnvironmentalDamage;

    public void PlayRifleMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld) => RifleTracers++;
    public void PlayCannonMuzzleAndTracer(Vector3 muzzleWorld, Vector3 targetWorld) => CannonTracers++;
    public void PlayImpact(Vector3 targetWorld, int damageAmount) => Impacts++;
    public void PlayExplosion(Vector3 targetWorld, int damageAmount) => Explosions++;
    public void PlayDeath(Vector3 worldPosition) => Deaths++;
    public void PlayDamage(Vector3 worldPosition, int amount) => GenericDamage++;
    public void PlayGraze(Vector3 worldPosition, int amount) => Grazes++;
    public void PlayEnvironmentalDamage(Vector3 worldPosition, int amount) => EnvironmentalDamage++;
}
```

Add this test:

```csharp
[UnityTest]
public IEnumerator PlayLog_GasDamage_UsesEnvironmentalDamageWithoutWeaponTracer()
{
    var database = RequireDatabase();
    if (database == null)
        yield break;

    var harness = new ArenaHarness();
    yield return LoadArena(harness, withDirector: true);
    _root = harness.Root;

    harness.Presenter.InitializeArena(CombatArenaTestBoards.BuildFieldGunVsHq(database));
    var vfx = new RecordingVfxPresenter();
    harness.Presenter.SetVfxPresenterForTests(vfx);

    var log = new CombatEventLog();
    log.Append(0, 0, "gas", "gas_damage", "enemy_rifle_1", 3);

    harness.Director.PlayLog(log, segment: 0);
    yield return new WaitUntil(() => !harness.Director.IsPlaying);

    Assert.AreEqual(1, vfx.EnvironmentalDamage);
    Assert.AreEqual(0, vfx.RifleTracers);
    Assert.AreEqual(0, vfx.CannonTracers);
}
```

- [ ] **Step 2: Run test to verify failure**

Run the filtered PlayMode command above.

Expected: compile fails because `PlayGraze`, `PlayEnvironmentalDamage`, and `SetVfxPresenterForTests` do not exist yet.

- [ ] **Step 3: Extend VFX interface and 2D implementation**

Update `ICombatArenaVfxPresenter`:

```csharp
void PlayGraze(Vector3 worldPosition, int amount);
void PlayEnvironmentalDamage(Vector3 worldPosition, int amount);
```

Add to `CombatArena2DVfx`:

```csharp
public void PlayGraze(Vector3 worldPosition, int amount)
{
    PlayStrip(
        CombatArena2DVfxArt.RifleImpactFrames,
        worldPosition + Vector3.up * 0.12f,
        0.65f,
        0.24f);
    SpawnFloatingText(worldPosition + Vector3.up * 0.72f, amount > 0 ? $"-{amount}" : amount.ToString());
}

public void PlayEnvironmentalDamage(Vector3 worldPosition, int amount)
{
    SpawnImpactFlash(worldPosition + Vector3.up * 0.2f, 0.28f);
    SpawnFloatingText(worldPosition + Vector3.up * 0.78f, amount > 0 ? $"-{amount}" : amount.ToString());
}
```

- [ ] **Step 4: Add presenter test hook and event mapping**

Add to `CombatArenaPresenter`:

```csharp
public void SetVfxPresenterForTests(ICombatArenaVfxPresenter presenter)
{
    _activeVfx = presenter;
}
```

Change `ApplyEventVisual` so `gas_damage` uses a separate path:

```csharp
case "damage":
case "graze":
    PlayDamageEvent(combatEvent);
    break;
case "gas_damage":
    PlayEnvironmentalDamageEvent(combatEvent);
    break;
```

Add:

```csharp
private void PlayEnvironmentalDamageEvent(CombatEvent combatEvent)
{
    if (!TryGetDamageTargetPosition(combatEvent, out var targetWorld))
        return;

    _activeVfx?.PlayEnvironmentalDamage(targetWorld, combatEvent.Value);
    if (_actors.TryGetValue(combatEvent.TargetId, out var victim))
        victim.PlayHurt(strongHit: false);
}
```

Inside the attack impact callback in `PlayDamageEvent`, change the impact call to:

```csharp
if (combatEvent.ActionType == "graze")
    _activeVfx?.PlayGraze(targetWorld, combatEvent.Value);
else
    PlayAttackImpactVfx(profile, targetWorld, combatEvent.Value);

victim?.PlayHurt(strongHit: combatEvent.Value >= 10);
```

- [ ] **Step 5: Run PlayMode tests**

Run the filtered PlayMode command.

Expected: `CombatArenaReplayPlayModeTests` pass.

---

### Task 4: Tie Death VFX To Actual Death Completion

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatUnitVisual2D.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatUnitActor.cs`
- Modify: `Assets/_Project/Presentation/Combat/Arena/CombatArenaPresenter.cs`
- Modify: `Assets/_Project/Tests.PlayMode/CombatArenaReplayPlayModeTests.cs`

- [ ] **Step 1: Write failing death wait test**

Add this test to `CombatArenaReplayPlayModeTests`:

```csharp
[UnityTest]
public IEnumerator PlayLog_Destroyed_KeepsPendingDeathUntilPresentationCompletes()
{
    var database = RequireDatabase();
    if (database == null)
        yield break;

    var harness = new ArenaHarness();
    yield return LoadArena(harness, withDirector: true);
    _root = harness.Root;

    harness.Presenter.InitializeArena(CombatArenaTestBoards.BuildFieldGunVsHq(database));
    var vfx = new RecordingVfxPresenter();
    harness.Presenter.SetVfxPresenterForTests(vfx);

    var log = new CombatEventLog();
    log.Append(0, 0, "field_gun_1", "destroyed", string.Empty, 0);

    harness.Director.PlayLog(log, segment: 0);
    yield return null;

    Assert.IsTrue(
        harness.Presenter.HasPendingDeathPresentations,
        "Destroyed replay should keep a pending death presentation after the first frame.");

    yield return harness.Presenter.WaitForPendingDeathPresentations();

    Assert.IsFalse(harness.Presenter.HasPendingDeathPresentations);
    Assert.AreEqual(1, vfx.Deaths);
}
```

- [ ] **Step 2: Run test to verify failure or current behavior**

Run the filtered PlayMode command.

Expected before implementation: either death VFX count remains `0` because of the fixed delayed coroutine timing, or the pending wait behavior is not tied to the visual completion.

- [ ] **Step 3: Expose visual death duration**

Add to `CombatUnitVisual2D`:

```csharp
public float DeathDurationSeconds =>
    _animated ? Mathf.Max(1f, _animPlayer.CurrentDurationSeconds) : 0.35f;
```

Add to `CombatUnitActor`:

```csharp
public float EstimatedDeathDurationSeconds => _visual2D != null ? _visual2D.DeathDurationSeconds : 0.35f;
```

- [ ] **Step 4: Fire death VFX on death completion**

In `CombatArenaPresenter.PlayDestroyedEvent`, replace the delayed death VFX coroutine with completion-tied VFX:

```csharp
Vector3 deathWorld = dead.transform.position;
_actors.Remove(combatEvent.ActorId);
_pendingDeathPresentations++;
dead.PlayDeath(() =>
{
    audio?.PlayDeath(deathWorld);
    _activeVfx?.PlayDeath(deathWorld);
    _pendingDeathPresentations = Mathf.Max(0, _pendingDeathPresentations - 1);
    _pool.Release(dead);
});
```

Delete `PlayDeathVfxAfterDelay`.

- [ ] **Step 5: Run PlayMode tests**

Run the filtered PlayMode command.

Expected: destroyed actor tests and pending-death test pass.

---

### Task 5: Improve Pause Copy And Testability

**Files:**
- Modify: `Assets/_Project/Presentation/Combat/TacticPausePanel.cs`
- Modify: `Assets/_Project/Tests.PlayMode/TacticPausePanelPlayModeTests.cs`

- [ ] **Step 1: Write failing pause title tests**

Modify `InitializeForTests` to be called with a title text after implementation. First add these tests:

```csharp
[UnityTest]
public IEnumerator OpeningPause_UsesDoctrineTitle()
{
    _root = new GameObject("TacticPanelRoot");
    var panel = _root.AddComponent<TacticPausePanel>();
    var titleText = CreateText("Title");
    var authorityText = CreateText("Authority");
    var reasonText = CreateText("Reason");
    var continueButton = CreateButton("Continue");

    panel.InitializeForTests(authorityText, reasonText, continueButton, titleText);

    panel.ShowPause(new CombatPauseContext
    {
        CheckpointIndex = 0,
        Authority = 2,
        ActiveTactic = TacticType.DisciplinedFire,
        HasCommandPiece = false,
        AvailableAbilities = System.Array.Empty<AvailableCommand>()
    });
    yield return null;

    StringAssert.Contains("Opening Doctrine", titleText.text);
}

[UnityTest]
public IEnumerator MidFightPause_ExplainsHealthTrigger()
{
    _root = new GameObject("TacticPanelRoot");
    var panel = _root.AddComponent<TacticPausePanel>();
    var titleText = CreateText("Title");
    var authorityText = CreateText("Authority");
    var reasonText = CreateText("Reason");
    var continueButton = CreateButton("Continue");

    panel.InitializeForTests(authorityText, reasonText, continueButton, titleText);

    panel.ShowPause(new CombatPauseContext
    {
        CheckpointIndex = 1,
        Trigger = new PauseTriggerContext
        {
            CheckpointIndex = 1,
            TriggeredBy = CombatSide.Player,
            Threshold = 0.60f
        },
        Authority = 2,
        ActiveTactic = TacticType.DisciplinedFire,
        HasCommandPiece = false,
        AvailableAbilities = System.Array.Empty<AvailableCommand>()
    });
    yield return null;

    StringAssert.Contains("Command Pause", titleText.text);
    StringAssert.Contains("Your forces at 60%", titleText.text);
}

private TMP_Text CreateText(string name)
{
    var go = new GameObject(name);
    go.transform.SetParent(_root.transform, false);
    return go.AddComponent<TextMeshProUGUI>();
}

private Button CreateButton(string name)
{
    var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
    go.transform.SetParent(_root.transform, false);
    return go.GetComponent<Button>();
}
```

- [ ] **Step 2: Run pause tests to verify failure**

Run:

```powershell
& $Unity -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform playmode `
  -testFilter "DeadManZone.PlayMode.Tests.TacticPausePanelPlayModeTests" `
  -testResults "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\TestResults-PlayMode.xml" -quit
```

Expected: compile fails because `InitializeForTests` does not accept the title text yet.

- [ ] **Step 3: Implement pause copy**

Change `InitializeForTests` signature:

```csharp
public void InitializeForTests(
    TMP_Text testAuthorityText,
    TMP_Text testReasonText,
    Button testContinueButton,
    TMP_Text testTitleText = null)
{
    authorityText = testAuthorityText;
    reasonText = testReasonText;
    continueButton = testContinueButton;
    if (testTitleText != null)
        titleText = testTitleText;
    if (continueButton != null)
    {
        continueButton.onClick.RemoveAllListeners();
        continueButton.onClick.AddListener(SubmitAndContinue);
    }
}
```

Replace `GetPauseTitle`:

```csharp
private static string GetPauseTitle(CombatPauseContext context)
{
    if (context == null)
        return "Combat Pause";

    if (context.CheckpointIndex == 0 && context.Trigger == null)
        return "Opening Doctrine - Set the line before contact";

    if (context.Trigger == null)
        return "Command Pause - Issue new orders";

    string side = context.Trigger.TriggeredBy == CombatSide.Player ? "Your" : "Enemy";
    int percent = Mathf.RoundToInt(context.Trigger.Threshold * 100f);
    return $"Command Pause - {side} forces at {percent}%";
}
```

- [ ] **Step 4: Run pause tests**

Run the filtered pause PlayMode command above.

Expected: `TacticPausePanelPlayModeTests` pass.

---

### Task 6: Make Battle Report More Educational

**Files:**
- Create: `Assets/_Project/Presentation.Tests/EditMode/BattleReportPresenterTests.cs`
- Modify: `Assets/_Project/Presentation/Combat/BattleReportPresenter.cs`

- [ ] **Step 1: Write failing EditMode test**

Create `BattleReportPresenterTests.cs`:

```csharp
using DeadManZone.Core.Combat;
using DeadManZone.Presentation.Combat;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeadManZone.Presentation.Tests.EditMode
{
    public sealed class BattleReportPresenterTests
    {
        [Test]
        public void Show_IncludesOutcomeIncomeCasualtiesAndTeachingLine()
        {
            var root = new GameObject("BattleReportRoot");
            var panelRoot = new GameObject("Panel");
            panelRoot.transform.SetParent(root.transform, false);
            var presenter = root.AddComponent<BattleReportPresenter>();
            var outcome = CreateText("Outcome", root.transform);
            var summary = CreateText("Summary", root.transform);
            var dealt = CreateText("Dealt", root.transform);
            var taken = CreateText("Taken", root.transform);
            var button = new GameObject("Continue", typeof(Button)).GetComponent<Button>();

            presenter.InitializeForTests(panelRoot, outcome, summary, dealt, taken, button);
            presenter.Show(new BattleReport
            {
                PlayerWon = false,
                ManpowerCasualties = 7,
                SuppliesEarned = 12,
                MoraleDelta = -2,
                TopDamageDealt = new[]
                {
                    new BattleReportEntry { DisplayName = "Bulwark Squad", Damage = 24 }
                },
                TopDamageTaken = new[]
                {
                    new BattleReportEntry { DisplayName = "Enlisted Rifleman", Damage = 18 }
                }
            });

            StringAssert.Contains("Defeat", outcome.text);
            StringAssert.Contains("Casualties", summary.text);
            StringAssert.Contains("Supplies gained: +12", summary.text);
            StringAssert.Contains("Manpower losses were severe", summary.text);
            StringAssert.Contains("Bulwark Squad", dealt.text);
            StringAssert.Contains("Enlisted Rifleman", taken.text);

            Object.DestroyImmediate(root);
        }

        private static TMP_Text CreateText(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }
    }
}
```

- [ ] **Step 2: Run test to verify failure**

Run:

```powershell
& $Unity -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Presentation.Tests.EditMode.BattleReportPresenterTests" `
  -testResults "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\TestResults-EditMode.xml" -quit
```

Expected: compile fails because `InitializeForTests` does not exist.

- [ ] **Step 3: Implement battle report test hook and copy**

Add to `BattleReportPresenter`:

```csharp
public void InitializeForTests(
    GameObject testPanelRoot,
    TMP_Text testOutcomeText,
    TMP_Text testSummaryText,
    TMP_Text testDealtText,
    TMP_Text testTakenText,
    Button testContinueButton)
{
    panelRoot = testPanelRoot;
    outcomeText = testOutcomeText;
    summaryText = testSummaryText;
    dealtText = testDealtText;
    takenText = testTakenText;
    continueButton = testContinueButton;
}
```

Replace the summary assignment in `Show`:

```csharp
summaryText.text =
    $"Casualties: -{report.ManpowerCasualties}\n" +
    $"Supplies gained: +{report.SuppliesEarned}\n" +
    $"Morale: {report.MoraleDelta:+#;-#;0}\n" +
    GetTeachingLine(report);
```

Add:

```csharp
private static string GetTeachingLine(BattleReport report)
{
    if (report.ManpowerCasualties >= 6)
        return "Manpower losses were severe. Check front-line protection before the next fight.";

    if (report.TopDamageDealt != null && report.TopDamageDealt.Count > 0)
        return $"{report.TopDamageDealt[0].DisplayName} carried your damage this fight.";

    return "Review positioning and command timing before the next deployment.";
}
```

- [ ] **Step 4: Run battle report test**

Run the filtered battle report EditMode command above.

Expected: `BattleReportPresenterTests` pass.

---

### Task 7: Final Verification Pass

**Files:**
- Read only: all modified files
- Runtime: Unity Editor Play mode

- [ ] **Step 1: Run focused EditMode tests**

Run:

```powershell
& $Unity -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform editmode `
  -testFilter "DeadManZone.Presentation.Tests.EditMode" `
  -testResults "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\TestResults-EditMode.xml" -quit
```

Expected: presentation EditMode tests pass.

- [ ] **Step 2: Run focused PlayMode tests**

Run:

```powershell
& $Unity -batchmode -nographics `
  -projectPath "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone" `
  -runTests -testPlatform playmode `
  -testFilter "DeadManZone.PlayMode.Tests.CombatArenaReplayPlayModeTests;DeadManZone.PlayMode.Tests.TacticPausePanelPlayModeTests" `
  -testResults "C:\Users\jiveg\OneDrive\Desktop\Game Projects\DeadManZone\TestResults-PlayMode.xml" -quit
```

Expected: combat replay and pause-panel PlayMode tests pass.

- [ ] **Step 3: Check Unity console**

Open Unity Editor, wait for compilation, and confirm no new compiler errors. If using MCP for Unity, read `mcpforunity://instances`, set the active instance if needed, then use `read_console` filtered to errors.

Expected: no compile errors from modified files.

- [ ] **Step 4: Manual Play mode check**

In Unity:

1. Open `Assets/_Project/Scenes/Run.unity`.
2. Start a fresh IronMarch Union run.
3. Enter combat for fight 1.
4. Verify opening pause title reads as doctrine setup.
5. Submit a tactic and watch until first damage and first death.
6. Verify hit reaction, tracer/impact readability, damage text, and death completion before arena unload.
7. Continue to battle report.
8. Verify battle report outcome, casualties, top damage, supplies gained, and teaching line.

Expected: combat presentation is readable and no death vanishes before its animation completes.

- [ ] **Step 5: Capture evidence**

Capture screenshots to `Assets/_Project/Art/QA/CombatArenaVisualOverhaul/`:

- `build_phase_hud.png` if build phase is reachable without unrelated fixes.
- `combat_pause.png`
- `unit_death_moment.png`
- `battle_report.png`

Expected: screenshots show the improved visual presentation and can be attached to a future PR or milestone report.

---

## Self-Review Notes

- Spec coverage: animation fidelity, hit/death clarity, VFX readability, pause drama, battle report feedback, and test-first workflow are covered.
- Deterministic sim safety: no task modifies `DeadManZone.Core`, `TickCombatRun`, combat formulas, targeting, or save schema.
- Scope guardrail: reserves, Emergency Draft, full economy polish, and full 10-fight tuning remain outside this plan.
- Known dependency: Unity batchmode commands require the actual local Unity editor path if it is not installed at the expected Hub location.

