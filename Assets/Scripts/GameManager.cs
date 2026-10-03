using UnityEngine;
using UnityEngine.InputSystem;
using OrbitGuard;

/// <summary>Composition root and explicit game-state loop. No physics or per-enemy Update calls.</summary>
public sealed class GameManager : MonoBehaviour
{
    [SerializeField] CampaignDefinition campaign;
    enum Phase { Menu, Playing, Intermission, Paused, Results }
    sealed class Enemy
    {
        public SpriteRenderer view;
        public Vector2 slot;
        public int hp, points;
        public bool alive, diving;
        public float diveTime;
    }
    sealed class Cover { public SpriteRenderer view; public int hp; }
    const float HalfWidth = 6.6f, PlayerY = -4.3f;
    readonly Enemy[] enemies = new Enemy[50];
    readonly Cover[] covers = new Cover[36];
    readonly Transform[] stars = new Transform[75];
    readonly float[] starSpeeds = new float[75];
    readonly Sprite[] alienSprites = new Sprite[3];
    Camera gameCamera;
    SpriteRenderer ship, shieldView;
    VisualPool bullets, particles;
    GameAudio audioFx;
    GameHud ui;
    SaveData save;
    Phase phase, resumePhase;
    WaveDefinition definition;
    int score, waveIndex, lives, remaining, direction = 1, combo;
    float fireTimer, enemyFireTimer, phaseTimer, shieldTimer, shieldCooldown, invulnerability, diveTimer, comboTimer, shake;
    Vector2 formation;
    float formationSpeed, shotInterval;
    bool initialized;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindAnyObjectByType<GameManager>() == null) new GameObject("Orbit Guard").AddComponent<GameManager>();
    }
    void Start()
    {
        if (initialized) return;
        initialized = true;
        Application.targetFrameRate = 60;
        campaign = campaign != null ? campaign : Resources.Load<CampaignDefinition>("Campaign");
        if (campaign == null) campaign = ScriptableObject.CreateInstance<CampaignDefinition>();
        save = new SaveData();
        gameCamera = Camera.main;
        if (gameCamera == null) gameCamera = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener)).GetComponent<Camera>();
        gameCamera.orthographic = true; gameCamera.backgroundColor = new Color(.018f, .028f, .065f);
        gameCamera.clearFlags = CameraClearFlags.SolidColor;
        gameCamera.transform.position = new Vector3(0, 0, -10);
        var square = PixelArt.Make("#");
        alienSprites[0] = PixelArt.Crab(); alienSprites[1] = PixelArt.Squid(); alienSprites[2] = PixelArt.Beetle();
        ship = View("Pilot", PixelArt.Ship(), PixelArt.Mint, new Vector2(0, PlayerY), new Vector2(.72f, .72f), 5);
        shieldView = View("Shield", PixelArt.Make("..#####..", ".#.....#.", "#.......#", "#.......#", "#.......#", ".#.....#.", "..#####.."), PixelArt.Mint, ship.transform.position, Vector2.one, 6);
        shieldView.enabled = false;
        bullets = new VisualPool("Projectiles", 128, square, transform, 4);
        particles = new VisualPool("Feedback particles", 192, square, transform, 8);
        for (int i = 0; i < enemies.Length; i++)
        {
            enemies[i] = new Enemy { view = View("Invader " + i, alienSprites[i % 3], PixelArt.Coral, Vector2.zero, Vector2.one * .66f, 3) };
            enemies[i].view.gameObject.SetActive(false);
        }
        for (int i = 0; i < covers.Length; i++) covers[i] = new Cover { view = View("Cover " + i, square, PixelArt.Mint, Vector2.zero, new Vector2(.27f, .22f), 2) };
        for (int i = 0; i < stars.Length; i++)
        {
            float size = Random.Range(.015f, .045f);
            stars[i] = View("Star", square, new Color(.28f, .4f, .59f, Random.Range(.3f, .8f)), new Vector2(Random.Range(-15f, 15f), Random.Range(-15f, 15f)), Vector2.one * size, -10).transform;
            starSpeeds[i] = Random.Range(.08f, .35f);
        }
        View("Defence line", square, new Color(.16f, .28f, .37f), new Vector2(0, -4.85f), new Vector2(13.7f, .018f), -2);
        View("Left boundary", square, new Color(.07f, .14f, .22f), new Vector2(-7, 0), new Vector2(.02f, 10), -2);
        View("Right boundary", square, new Color(.07f, .14f, .22f), new Vector2(7, 0), new Vector2(.02f, 10), -2);
        audioFx = new GameAudio(gameObject); ui = new GameHud(gameObject);
        ui.onPlay = StartRun; ui.onPause = Pause; ui.onResume = Resume; ui.onMenu = MainMenu; ui.onShield = ActivateShield;
        MainMenu();
    }
    SpriteRenderer View(string name, Sprite sprite, Color color, Vector2 position, Vector2 scale, int order)
    {
        var obj = new GameObject(name); obj.transform.SetParent(transform);
        obj.transform.position = position; obj.transform.localScale = scale;
        var view = obj.AddComponent<SpriteRenderer>(); view.sprite = sprite; view.color = color; view.sortingOrder = order; return view;
    }
    int WaveCount => campaign.waves == null ? 0 : campaign.waves.Length;
    void MainMenu()
    {
        SaveProgress(); phase = Phase.Menu; bullets.Clear(); particles.Clear();
        foreach (var enemy in enemies) enemy.view.gameObject.SetActive(false);
        foreach (var cover in covers) cover.view.gameObject.SetActive(false);
        ship.enabled = false; shieldView.enabled = false;
        gameCamera.transform.position = new Vector3(0, 0, -10);
        ui.MainMenu(save, () => ui.Settings(save, MainMenu), () => ui.Progress(save, WaveCount, MainMenu));
    }
    void StartRun()
    {
        score = waveIndex = combo = 0; lives = campaign.startingLives;
        shieldCooldown = shieldTimer = invulnerability = fireTimer = shake = 0;
        ship.enabled = true; ship.transform.position = new Vector2(0, PlayerY); ship.color = PixelArt.Mint;
        ui.Playing(true); BeginWave();
    }
    void BeginWave()
    {
        bullets.Clear(); particles.Clear(); definition = campaign.GetWave(waveIndex);
        int rows = Mathf.Clamp(definition.rows, 1, 5), cols = Mathf.Clamp(definition.columns, 3, 10);
        int repeat = Mathf.Max(0, waveIndex - WaveCount + 1);
        formationSpeed = Mathf.Min(2.8f, definition.marchSpeed * (1 + repeat * .12f));
        shotInterval = Mathf.Max(.28f, definition.shotInterval / (1 + repeat * .1f));
        formation = Vector2.zero; direction = 1; remaining = rows * cols;
        enemyFireTimer = 1.5f; diveTimer = 4; comboTimer = 0;
        for (int i = 0; i < enemies.Length; i++)
        {
            var e = enemies[i]; e.alive = i < remaining; e.diving = false; e.diveTime = 0;
            e.view.gameObject.SetActive(e.alive);
            if (!e.alive) continue;
            int row = i / cols, col = i % cols;
            e.slot = new Vector2((col - (cols - 1) * .5f) * 1.05f, 3.55f - row * .83f);
            e.hp = row < definition.armoredRows ? 2 : 1; e.points = (rows - row) * 10 + (e.hp == 2 ? 20 : 0);
            e.view.sprite = alienSprites[row % 3]; e.view.color = e.hp == 2 ? PixelArt.Gold : row % 2 == 0 ? PixelArt.Coral : PixelArt.Violet;
            e.view.transform.position = e.slot; e.view.transform.localScale = Vector2.one * .66f;
        }
        // Renew damaged bunkers every wave, rewarding survival without permanent deadlocks.
        for (int i = 0; i < covers.Length; i++)
        {
            int bunker = i / 12, cell = i % 12, row = cell / 4, col = cell % 4;
            var cover = covers[i]; cover.hp = 3; cover.view.gameObject.SetActive(true); cover.view.color = new Color(.24f, .65f, .62f);
            cover.view.transform.position = new Vector2((bunker - 1) * 3.8f + (col - 1.5f) * .3f, -2.8f + row * .24f);
        }
        phase = Phase.Intermission; phaseTimer = 2;
        ui.Banner("WAVE " + (waveIndex + 1).ToString("D2") + "  /  " + definition.name);
        save.bestWave = Mathf.Max(save.bestWave, waveIndex + 1); SaveProgress();
    }
    void Update()
    {
        if (!initialized) return;
        float dt = Mathf.Min(Time.deltaTime, .04f);
        // Fit the whole arena on portrait screens; world coordinates and balancing stay identical.
        gameCamera.orthographicSize = Mathf.Max(6.3f, 7.5f / Mathf.Max(.3f, gameCamera.aspect));
        if (!save.reducedMotion && phase != Phase.Paused)
            for (int i = 0; i < stars.Length; i++) { var p = stars[i].position; p.y -= starSpeeds[i] * dt; if (p.y < -15) p.y = 15; stars[i].position = p; }
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame) { if (phase == Phase.Paused) Resume(); else Pause(); }
        if (phase != Phase.Playing && phase != Phase.Intermission) return;
        TickEffects(dt);
        shieldCooldown = Mathf.Max(0, shieldCooldown - dt); shieldTimer = Mathf.Max(0, shieldTimer - dt); invulnerability = Mathf.Max(0, invulnerability - dt);
        shieldView.enabled = shieldTimer > 0; shieldView.transform.position = ship.transform.position;
        ship.color = invulnerability > 0 && Mathf.Sin(Time.time * 35) > 0 ? new Color(.35f, 1, .83f, .25f) : PixelArt.Mint;
        MovePlayer(dt, keyboard);
        ui.Update(score, waveIndex + 1, lives, shieldCooldown);
        if (phase == Phase.Intermission)
        {
            phaseTimer -= dt;
            if (phaseTimer <= 0) { phase = Phase.Playing; ui.Banner(""); }
            return;
        }
        fireTimer -= dt;
        if (fireTimer <= 0 && (save.autoFire || ui.fireHeld || (keyboard != null && keyboard.spaceKey.isPressed)))
        {
            if (bullets.Rent((Vector2)ship.transform.position + Vector2.up * .5f, new Vector2(.07f, .32f), PixelArt.Mint, 2, Vector2.up * 12, 0) != null) audioFx.Play(0, save.sound * .35f);
            fireTimer = campaign.fireInterval;
        }
        if (keyboard != null && (keyboard.leftShiftKey.wasPressedThisFrame || keyboard.rightShiftKey.wasPressedThisFrame)) ActivateShield();
        TickEnemies(dt); if (phase != Phase.Playing) return;
        TickBullets(dt); if (phase != Phase.Playing) return;
        comboTimer -= dt; if (comboTimer <= 0 && combo > 0) { combo = 0; ui.Banner(""); }
        if (remaining == 0)
        {
            score += 100 + lives * 25; audioFx.Play(3, save.sound); waveIndex++;
            save.bestClearedWave = Mathf.Max(save.bestClearedWave, waveIndex);
            if (!campaign.endlessAfterCampaign && waveIndex >= WaveCount) Finish(true);
            else { if (waveIndex % 3 == 0) lives = Mathf.Min(5, lives + 1); BeginWave(); }
        }
    }
    void MovePlayer(float dt, Keyboard keyboard)
    {
        float movement = (ui.rightHeld ? 1 : 0) - (ui.leftHeld ? 1 : 0);
        if (keyboard != null) movement += (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1 : 0) - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1 : 0);
        var p = ship.transform.position; p.x = Mathf.Clamp(p.x + Mathf.Clamp(movement, -1, 1) * campaign.playerSpeed * dt, -HalfWidth, HalfWidth);
        ship.transform.position = p;
        if (!save.reducedMotion) ship.transform.rotation = Quaternion.Euler(0, 0, -movement * 7);
        else ship.transform.rotation = Quaternion.identity;
    }
    void ActivateShield()
    {
        if (phase != Phase.Playing || shieldCooldown > 0) return;
        shieldTimer = 1.6f; shieldCooldown = campaign.shieldCooldown; Burst(ship.transform.position, PixelArt.Mint, 12); audioFx.Play(3, save.sound * .4f);
    }
    void TickEnemies(float dt)
    {
        float left = 100, right = -100;
        foreach (var e in enemies) if (e.alive && !e.diving) { left = Mathf.Min(left, e.slot.x + formation.x); right = Mathf.Max(right, e.slot.x + formation.x); }
        if ((right >= HalfWidth && direction > 0) || (left <= -HalfWidth && direction < 0)) { direction *= -1; formation.y -= .25f; }
        formation.x += direction * formationSpeed * (1 + (1 - remaining / (float)(Mathf.Clamp(definition.rows, 1, 5) * Mathf.Clamp(definition.columns, 3, 10))) * 1.6f) * dt;
        foreach (var e in enemies)
        {
            if (!e.alive) continue;
            if (e.diving)
            {
                e.diveTime += dt; var p = e.view.transform.position; p.y -= 2.4f * dt; p.x += Mathf.Sin(e.diveTime * 4) * dt * 1.5f;
                e.view.transform.position = p;
                if (p.y < -5.3f) { e.diving = false; e.view.transform.position = e.slot + formation; }
                else if (Mathf.Abs(p.x - ship.transform.position.x) < .55f && Mathf.Abs(p.y - PlayerY) < .5f) { DamagePlayer(); Kill(e, false); }
            }
            else
            {
                e.view.transform.position = e.slot + formation;
                if (e.view.transform.position.y < -3.85f) { Finish(false); return; }
            }
            e.view.transform.localScale = new Vector2(.66f, .66f + (save.reducedMotion ? 0 : Mathf.Sin(Time.time * 7 + e.slot.x) * .025f));
            // Invaders crush cover instead of overlapping intact bunkers.
            if (e.view.transform.position.y < -1.8f)
                foreach (var c in covers) if (c.hp > 0 && ((Vector2)c.view.transform.position - (Vector2)e.view.transform.position).sqrMagnitude < .3f) { c.hp = 0; c.view.gameObject.SetActive(false); }
        }
        enemyFireTimer -= dt;
        if (enemyFireTimer <= 0)
        {
            int start = Random.Range(0, enemies.Length);
            for (int n = 0; n < enemies.Length; n++)
            {
                var e = enemies[(start + n) % enemies.Length]; if (!e.alive || e.diving) continue;
                bool front = true;
                foreach (var other in enemies) if (other.alive && !other.diving && Mathf.Abs(other.slot.x - e.slot.x) < .1f && other.slot.y < e.slot.y) { front = false; break; }
                if (!front) continue;
                Vector2 p = e.view.transform.position;
                float aim = waveIndex >= 2 ? Mathf.Clamp((ship.transform.position.x - p.x) * .25f, -1.1f, 1.1f) : 0;
                bullets.Rent(p + Vector2.down * .4f, new Vector2(.1f, .28f), PixelArt.Coral, 5, new Vector2(aim, -Mathf.Clamp(definition.bulletSpeed + Mathf.Max(0, waveIndex - WaveCount) * .15f, 2, 8)), 1);
                break;
            }
            enemyFireTimer = shotInterval * Random.Range(.8f, 1.2f);
        }
        diveTimer -= dt;
        if (definition.divingEnemies && diveTimer <= 0)
        {
            foreach (var e in enemies) if (e.alive && !e.diving) { e.diving = true; e.diveTime = 0; Burst(e.view.transform.position, PixelArt.Gold, 5); break; }
            diveTimer = 5;
        }
    }
    void TickBullets(float dt)
    {
        foreach (var b in bullets.items)
        {
            if (!b.active) continue;
            Vector2 before = b.transform.position, after = before + b.velocity * dt;
            b.transform.position = after; b.lifetime -= dt;
            if (b.lifetime <= 0 || Mathf.Abs(after.y) > 5.3f) { bullets.Return(b); continue; }
            if (b.team == 0)
            {
                foreach (var e in enemies)
                {
                    if (!e.alive || !Crosses(before, after, e.view.transform.position, .38f, .32f)) continue;
                    bullets.Return(b); e.hp--; Burst(after, e.hp > 0 ? PixelArt.Gold : e.view.color, e.hp > 0 ? 4 : 14);
                    audioFx.Play(1, save.sound * .65f);
                    if (e.hp <= 0) Kill(e, true); else e.view.color = PixelArt.Coral;
                    break;
                }
            }
            else if (Crosses(before, after, ship.transform.position, shieldTimer > 0 ? .65f : .33f, shieldTimer > 0 ? .6f : .3f))
            { bullets.Return(b); if (shieldTimer > 0) Burst(after, PixelArt.Mint, 5); else DamagePlayer(); }
            if (!b.active) continue;
            foreach (var c in covers)
            {
                if (c.hp <= 0 || !Crosses(before, after, c.view.transform.position, .16f, .13f)) continue;
                c.hp--; c.view.color = Color.Lerp(new Color(.08f, .22f, .27f), PixelArt.Mint, c.hp / 3f);
                if (c.hp == 0) c.view.gameObject.SetActive(false);
                bullets.Return(b); Burst(after, PixelArt.Mint, 3); break;
            }
        }
    }
    static bool Crosses(Vector2 from, Vector2 to, Vector2 target, float halfX, float halfY)
    {
        // Swept segment vs expanded rectangle; shots cannot tunnel on low frame rates.
        Vector2 delta = to - from; float near = 0, far = 1;
        for (int axis = 0; axis < 2; axis++)
        {
            float extent = axis == 0 ? halfX : halfY;
            if (Mathf.Abs(delta[axis]) < .00001f) { if (Mathf.Abs(from[axis] - target[axis]) > extent) return false; }
            else
            {
                float a = (target[axis] - extent - from[axis]) / delta[axis], b = (target[axis] + extent - from[axis]) / delta[axis];
                near = Mathf.Max(near, Mathf.Min(a, b)); far = Mathf.Min(far, Mathf.Max(a, b)); if (near > far) return false;
            }
        }
        return true;
    }
    void Kill(Enemy enemy, bool reward)
    {
        if (!enemy.alive) return;
        enemy.alive = false; enemy.view.gameObject.SetActive(false); remaining--;
        if (!reward) return;
        combo++; comboTimer = 1.3f;
        score += enemy.points * Mathf.Min(4, 1 + combo / 5);
        if (combo >= 5) ui.Banner("CHAIN  x" + Mathf.Min(4, 1 + combo / 5));
    }
    void DamagePlayer()
    {
        if (invulnerability > 0 || shieldTimer > 0) return;
        lives--; combo = 0; invulnerability = 1.8f; shake = .22f;
        Burst(ship.transform.position, PixelArt.Coral, 24); audioFx.Play(2, save.sound);
        if (lives <= 0) Finish(false);
    }
    void Burst(Vector2 position, Color color, int count)
    {
        if (save.reducedMotion) count = Mathf.Min(3, count);
        for (int i = 0; i < count; i++) particles.Rent(position, Vector2.one * Random.Range(.035f, .09f), color, Random.Range(.2f, .6f), Random.insideUnitCircle * 3);
    }
    void TickEffects(float dt)
    {
        foreach (var p in particles.items)
        {
            if (!p.active) continue;
            p.lifetime -= dt;
            if (p.lifetime <= 0) { particles.Return(p); continue; }
            p.transform.position += (Vector3)(p.velocity * dt); p.velocity *= 1 - dt * 3;
            var color = p.renderer.color; color.a = Mathf.Min(1, p.lifetime * 3); p.renderer.color = color;
        }
        shake = Mathf.Max(0, shake - dt);
        var offset = shake > 0 && !save.reducedMotion ? Random.insideUnitCircle * shake * .3f : Vector2.zero;
        gameCamera.transform.position = new Vector3(offset.x, offset.y, -10);
    }
    void Pause()
    {
        if (phase != Phase.Playing && phase != Phase.Intermission) return;
        resumePhase = phase; phase = Phase.Paused;
        ShowPauseMenu();
    }
    void ShowPauseMenu() { ui.Pause(() => ui.Settings(save, ShowPauseMenu)); }
    void Resume() { if (phase != Phase.Paused) return; phase = resumePhase; ui.Playing(true); if (phase == Phase.Intermission) ui.Banner("WAVE " + (waveIndex + 1) + "  /  " + definition.name); }
    void Finish(bool victory)
    {
        phase = Phase.Results; SaveProgress(); bullets.Clear(); shieldView.enabled = false;
        gameCamera.transform.position = new Vector3(0, 0, -10);
        ui.Results(score, victory ? waveIndex : waveIndex + 1, victory, save);
    }
    void SaveProgress() { if (save == null) return; save.highScore = Mathf.Max(save.highScore, score); save.Flush(); }
    void OnApplicationPause(bool paused) { if (paused && initialized) { Pause(); SaveProgress(); } }
    void OnApplicationFocus(bool focused) { if (!focused && initialized) Pause(); }
    void OnApplicationQuit() { SaveProgress(); }
}
