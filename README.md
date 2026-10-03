# Orbit Guard

A compact Space Invaders inspired game for Unity 6000.6.3f1, desktop Web browsers, with mobile-ready controls. Open `Assets/Scenes/SampleScene.unity` and press Play. The runtime bootstrap creates the game and UI in the existing scene, so no manual prefab wiring is required.

## Play

Move with A/D or arrow keys. Automatic firing is enabled by default; disable it in Settings and hold Space for manual fire. Shift activates a 1.6 second shield with a seven second recharge. Escape pauses. Touch devices show movement, shield and fire buttons. Losing focus pauses the run. Cover blocks both friendly and enemy shots: move into gaps to fire. Armored gold invaders take two hits; later waves introduce aimed shots and diving enemies. Clearing waves awards a survival bonus; every third clear restores one hull point. Fast consecutive kills build a score multiplier up to 4x.

Five authored wave archetypes lead into bounded endless escalation. Disable `endlessAfterCampaign` for a finite campaign and victory screen. Progress and settings are saved locally using PlayerPrefs; records show the furthest wave reached, best cleared wave, and campaign completion. Each launch starts a fresh run. Browser storage can be cleared by the player.

## Tuning and ownership

Use **Orbit Guard > Select Campaign**, or select `Assets/Resources/Campaign.asset`. The editor creates this asset on first import. Add any number of wave entries and tune their names, formation size, march speed, shot cadence, projectile speed, armored rows, and diving. Formation limits are deliberately capped at 50 enemies; extend the capacity and validation together if larger formations are needed. Player speed, lives, firing cadence and shield recharge are campaign-wide settings.

- `GameManager`: composition root, explicit menu/play/intermission/pause/results states, formation movement, collision and scoring. One centralized frame loop.
- `CampaignDefinition`: ScriptableObject authoring; wave data is independent of presentation.
- `VisualPool`: fixed-capacity reusable projectile and particle views. A full pool skips a shot/effect; it never grows during combat.
- `GameHud`: responsive Canvas, menu navigation and touch input.
- `PixelArt`: tiny shared point-filtered textures generated once at startup.
- `GameAudio`: four small synthesized sound effects created once.
- `SaveData`: namespaced persistent scores, records and settings.

Enemies (50), cover (36), bullets (128), effects (192) and stars (75) are preallocated. Collision uses swept segment/rectangle tests instead of physics components. No per-enemy Update, LINQ, runtime instantiation or coroutine during combat. HUD text only updates when displayed values change; profile on target browsers before adding content. Settings/menu navigation creates UI only outside combat. Sprites use shared Unity sprite materials; no lighting, bloom, postprocessing or external assets are required.

## Desktop Web first; mobile later

Use **Web - Desktop - Release** as the primary build profile with the enabled SampleScene. Keyboard controls and landscape browser play are the primary experience; desktop touchscreen hardware does not automatically enable the mobile UI. The mobile Web profile remains available for a future port. Input uses the installed Input System package. The camera fits the arena across aspect ratios; Canvas scales to screen size. Mobile touch buttons support simultaneous movement and shield/fire. Landscape is recommended for a larger arena. The UI respects Screen.safeArea. Native mobile builds need platform signing/build modules and device verification. Change touch layout and orientation without changing world coordinates or wave data.

## Verification

Unity 6000.6.3f1 batch compilation and an isolated Play Mode smoke run passed: bootstrap, first-wave spawning, intermission, bounded pool exhaustion/reuse, swept collision, shield absorption, pause/resume, invulnerability, retry, life rewards, clear progress, and finite victory. Visual rendering, Web builds and device performance have not been verified by this headless run.

Play the scene and check: launch; manual/automatic fire; cover damage; shield absorption/recharge; three-wave life reward; pause and focus loss; failure/retry; score persistence after restart; sound/motion settings; and touch controls on a real device. To test victory quickly, use one small wave and turn endless off. Do browser profiling and a real Web build before publishing.
