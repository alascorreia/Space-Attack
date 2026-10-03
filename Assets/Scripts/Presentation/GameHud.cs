using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

namespace OrbitGuard
{
    public sealed class GameHud
    {
        readonly Font font;
        readonly bool touchMode;
        readonly Transform root;
        readonly GameObject menu, hud, touch;
        readonly Text score, wave, lives, shield, banner;
        readonly Transform panel;
        int lastScore = -1, lastWave = -1, lastLives = -1, lastCooldown = -1;
        public bool leftHeld, rightHeld, fireHeld;
        public Action onPlay, onResume, onMenu, onShield, onPause;
        public GameHud(GameObject owner)
        {
            font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            var canvas = new GameObject("Interface", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas.transform.SetParent(owner.transform);
            canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(960, 720);
            scaler.matchWidthOrHeight = .5f;
            root = Group("Safe area", canvas.transform).transform;
            root.gameObject.AddComponent<SafeAreaLayout>();
            if (UnityEngine.Object.FindAnyObjectByType<EventSystem>() == null)
                new GameObject("Event System", typeof(EventSystem), typeof(InputSystemUIInputModule));
            hud = Group("HUD", root);
            score = Label(hud.transform, "", 24, new Vector2(.02f, .89f), new Vector2(.36f, .98f), TextAnchor.MiddleLeft);
            wave = Label(hud.transform, "", 18, new Vector2(.36f, .89f), new Vector2(.64f, .98f));
            lives = Label(hud.transform, "", 21, new Vector2(.64f, .89f), new Vector2(.84f, .98f));
            Button(hud.transform, "II", new Vector2(.87f, .91f), new Vector2(.97f, .97f), () => onPause?.Invoke());
            touchMode = Application.isMobilePlatform;
            bool mobile = touchMode;
            shield = Label(hud.transform, "", 16, new Vector2(.2f, mobile ? .16f : .025f), new Vector2(.8f, mobile ? .21f : .09f));
            if (!touchMode)
                Label(hud.transform, "MOVE  A / D or arrows     FIRE  Space / Auto     SHIELD  Shift     PAUSE  Esc", 14, new Vector2(.08f, .09f), new Vector2(.92f, .13f));
            banner = Label(root, "", 25, new Vector2(.08f, .74f), new Vector2(.92f, .85f)); banner.color = PixelArt.Gold;
            touch = Group("Touch controls", root);
            HoldButton(touch.transform, "<", new Vector2(.02f, .04f), new Vector2(.15f, .15f), v => leftHeld = v);
            HoldButton(touch.transform, ">", new Vector2(.18f, .04f), new Vector2(.31f, .15f), v => rightHeld = v);
            Button(touch.transform, "SHIELD", new Vector2(.65f, .04f), new Vector2(.8f, .15f), () => onShield?.Invoke());
            HoldButton(touch.transform, "FIRE", new Vector2(.82f, .04f), new Vector2(.98f, .15f), v => fireHeld = v);
            menu = Group("Menu overlay", root);
            var backdrop = menu.AddComponent<Image>(); backdrop.color = new Color(.015f, .027f, .065f, .96f);
            panel = Group("Menu contents", menu.transform).transform;
            Playing(false);
        }
        static GameObject Group(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform)); obj.transform.SetParent(parent, false);
            Rect(obj.GetComponent<RectTransform>(), Vector2.zero, Vector2.one); return obj;
        }
        static void Rect(RectTransform rect, Vector2 min, Vector2 max)
        { rect.anchorMin = min; rect.anchorMax = max; rect.offsetMin = Vector2.zero; rect.offsetMax = Vector2.zero; }
        Text Label(Transform parent, string value, int size, Vector2 min, Vector2 max, TextAnchor align = TextAnchor.MiddleCenter)
        {
            var obj = new GameObject("Label", typeof(RectTransform), typeof(Text)); obj.transform.SetParent(parent, false);
            Rect(obj.GetComponent<RectTransform>(), min, max);
            var text = obj.GetComponent<Text>(); text.font = font; text.text = value; text.fontSize = size;
            text.alignment = align; text.color = new Color(.86f, .94f, 1); text.raycastTarget = false;
            text.resizeTextForBestFit = true; text.resizeTextMinSize = 10; text.resizeTextMaxSize = size;
            return text;
        }
        Button Button(Transform parent, string title, Vector2 min, Vector2 max, Action action)
        {
            var obj = new GameObject(title, typeof(RectTransform), typeof(Image), typeof(Button)); obj.transform.SetParent(parent, false);
            Rect(obj.GetComponent<RectTransform>(), min, max);
            obj.GetComponent<Image>().color = new Color(.09f, .2f, .27f);
            var b = obj.GetComponent<Button>(); var colors = b.colors;
            colors.highlightedColor = PixelArt.Mint; colors.pressedColor = new Color(.3f, .65f, .65f); b.colors = colors;
            b.onClick.AddListener(() => action?.Invoke());
            Label(obj.transform, title, 20, new Vector2(.05f, .1f), new Vector2(.95f, .9f)); return b;
        }
        void HoldButton(Transform parent, string title, Vector2 min, Vector2 max, Action<bool> set)
        {
            var button = Button(parent, title, min, max, null);
            var trigger = button.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown }; down.callback.AddListener(_ => set(true)); trigger.triggers.Add(down);
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp }; up.callback.AddListener(_ => set(false)); trigger.triggers.Add(up);
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit }; exit.callback.AddListener(_ => set(false)); trigger.triggers.Add(exit);
        }
        void Clear()
        {
            for (int i = panel.childCount - 1; i >= 0; i--) { var child = panel.GetChild(i).gameObject; child.SetActive(false); UnityEngine.Object.Destroy(child); }
            menu.SetActive(true); leftHeld = rightHeld = fireHeld = false;
        }
        void Title(string title, string subtitle)
        {
            Label(panel, "O R B I T   /   G U A R D", 15, new Vector2(.1f, .86f), new Vector2(.9f, .92f)).color = PixelArt.Mint;
            Label(panel, title, 56, new Vector2(.07f, .69f), new Vector2(.93f, .85f));
            Label(panel, subtitle, 18, new Vector2(.08f, .58f), new Vector2(.92f, .68f));
        }
        void MenuButton(string title, float y, Action action) => Button(panel, title, new Vector2(.28f, y), new Vector2(.72f, y + .078f), action);
        public void MainMenu(SaveData save, Action settings, Action progress)
        {
            Clear(); Playing(false); Title("DEFEND THE ORBIT", "Small ship. Endless sky. One more wave.");
            MenuButton("LAUNCH", .46f, () => onPlay?.Invoke());
            MenuButton("FLIGHT RECORD", .36f, progress); MenuButton("SETTINGS", .26f, settings);
            Label(panel, "BEST  " + save.highScore.ToString("N0") + "    /    WAVE  " + save.bestWave, 18, new Vector2(.1f, .17f), new Vector2(.9f, .23f)).color = PixelArt.Gold;
            Label(panel, touchMode ? "MOVE / FIRE / SHIELD  Use the on-screen controls\nAutomatic firing is enabled by default." : "MOVE  A / D or arrows     FIRE  Space     SHIELD  Shift     PAUSE  Esc\nAutomatic firing is enabled by default. Change it in Settings.", 16, new Vector2(.06f, .035f), new Vector2(.94f, .13f));
        }
        public void Pause(Action settings)
        { Clear(); Title("FLIGHT PAUSED", "Take a breath. Your orbit is safe."); MenuButton("RESUME", .44f, () => onResume?.Invoke()); MenuButton("SETTINGS", .34f, settings); MenuButton("MAIN MENU", .24f, () => onMenu?.Invoke()); }
        public void Results(int points, int reached, bool victory, SaveData save)
        {
            Clear(); Playing(false); Title(victory ? "ORBIT SECURED" : "SIGNAL LOST", "SCORE  " + points.ToString("N0") + "    /    WAVE  " + reached);
            Label(panel, "PERSONAL BEST  " + save.highScore.ToString("N0"), 22, new Vector2(.1f, .49f), new Vector2(.9f, .56f)).color = PixelArt.Gold;
            MenuButton("FLY AGAIN", .36f, () => onPlay?.Invoke()); MenuButton("MAIN MENU", .25f, () => onMenu?.Invoke());
        }
        public void Progress(SaveData save, int count, Action back)
        {
            Clear(); Title("FLIGHT RECORD", "Saved on this device");
            Label(panel, "HIGH SCORE\n" + save.highScore.ToString("N0") + "\n\nFURTHEST WAVE\n" + save.bestWave + "\n\nCAMPAIGN CLEARED  " + Mathf.Min(save.bestClearedWave, count) + " / " + count + "\nBEST CLEARED WAVE  " + save.bestClearedWave, 26, new Vector2(.12f, .25f), new Vector2(.88f, .58f));
            MenuButton("BACK", .12f, back);
        }
        public void Settings(SaveData save, Action back)
        {
            Clear(); Title("SETTINGS", "Make the cockpit yours.");
            MenuButton("SOUND  " + Mathf.RoundToInt(save.sound * 100) + "%", .45f, () => { save.sound = save.sound < .1f ? .35f : save.sound < .5f ? .7f : save.sound < .9f ? 1 : 0; save.Flush(); Settings(save, back); });
            MenuButton("AUTO FIRE  " + (save.autoFire ? "ON" : "OFF"), .35f, () => { save.autoFire = !save.autoFire; save.Flush(); Settings(save, back); });
            MenuButton("REDUCED MOTION  " + (save.reducedMotion ? "ON" : "OFF"), .25f, () => { save.reducedMotion = !save.reducedMotion; save.Flush(); Settings(save, back); });
            MenuButton("BACK", .12f, back);
        }
        public void Playing(bool value)
        { hud.SetActive(value); touch.SetActive(value && touchMode); if (value) menu.SetActive(false); banner.text = ""; }
        public void Update(int points, int number, int health, float cooldown)
        {
            if (points != lastScore) { score.text = "SCORE  " + points.ToString("D6"); lastScore = points; }
            if (number != lastWave) { wave.text = "WAVE  " + number.ToString("D2"); lastWave = number; }
            if (health != lastLives) { lives.text = "LIVES  " + Mathf.Max(0, health).ToString(); lastLives = health; }
            int seconds = Mathf.CeilToInt(cooldown);
            if (seconds != lastCooldown)
            {
                shield.text = seconds <= 0 ? "SHIELD READY" : "SHIELD RECHARGING  " + seconds + "s";
                shield.color = seconds <= 0 ? PixelArt.Mint : new Color(.5f, .62f, .72f); lastCooldown = seconds;
            }
        }
        public void Banner(string message) { banner.text = message; }
    }
}
