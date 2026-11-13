using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace TLKS
{
    public partial class Game : MonoBehaviour
    {
        public static Game I;
        public UIKit ui; public Sfx sfx; public Camera cam; public Transform stageRoot;
        public Progress prog = new Progress();
        public Light sunLight;
        Coroutine flow; float lastEsc = -10f;
        public bool judge;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            var go = new GameObject("TheLastKnownSignal");
            DontDestroyOnLoad(go);
            go.AddComponent<Game>();
        }

        void Awake()
        {
            I = this;
            Application.targetFrameRate = 60; QualitySettings.vSyncCount = 1;
            Application.runInBackground = true;
            Nasa.Load();
            sfx = gameObject.AddComponent<Sfx>(); sfx.Init();
            var cgo = new GameObject("Main Camera"); cgo.tag = "MainCamera"; DontDestroyOnLoad(cgo);
            cam = cgo.AddComponent<Camera>(); cam.fieldOfView = 50; cam.nearClipPlane = 0.05f; cam.farClipPlane = 4000f;
            cam.clearFlags = CameraClearFlags.SolidColor; cam.backgroundColor = Color.black; cam.allowHDR = true;
            cgo.AddComponent<AudioListener>();
            var ud = cgo.AddComponent<UniversalAdditionalCameraData>();
            ud.renderPostProcessing = true; ud.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing; ud.antialiasingQuality = AntialiasingQuality.High;
            var ph = new GameObject("PostFX"); DontDestroyOnLoad(ph); Fx.SetupPost(ph);
            ui = new UIKit(sfx);
            judge = PlayerPrefs.GetInt("tlks_judge", 0) == 1;
        }

        public static bool devAuto;

        void Start()
        {
            StartCoroutine(Nasa.RefreshDsn());
            // developer / QA hooks:  -chapter N  (0 prologue .. 5 finale, 8 design)   -shots <dir> <interval>   -auto
            var args = Environment.GetCommandLineArgs(); int ch = -1; string shots = null; float iv = 4f;
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-chapter" && i + 1 < args.Length) int.TryParse(args[i + 1], out ch);
                if (args[i] == "-shots" && i + 1 < args.Length) { shots = args[i + 1]; if (i + 2 < args.Length) float.TryParse(args[i + 2], out iv); }
                if (args[i] == "-auto") devAuto = true;
            }
            if (shots != null) StartCoroutine(ShotLoop(shots, iv));
            if (ch >= 0) flow = StartCoroutine(DevRun(ch)); else flow = StartCoroutine(MainLoop());
        }

        IEnumerator DevRun(int ch)
        {
            yield return ui.Fade(0f, 0.01f);
            if (ch == 8) yield return DesignMode(); else yield return Campaign(ch);
            flow = StartCoroutine(MainLoop());
        }

        IEnumerator ShotLoop(string dir, float iv)
        {
            System.IO.Directory.CreateDirectory(dir); int n = 0;
            while (true) { yield return new WaitForSeconds(iv); ScreenCapture.CaptureScreenshot(System.IO.Path.Combine(dir, "shot_" + (n++).ToString("00") + ".png")); }
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape) && !inMenu)
            {
                if (Time.unscaledTime - lastEsc < 2f) { AbortToMenu(); }
                else { lastEsc = Time.unscaledTime; ui.Toast("Press ESC again to return to the main menu", Theme.Amber, 2f); }
            }
            if (Input.GetKeyDown(KeyCode.F11)) Screen.fullScreen = !Screen.fullScreen;
        }

        void AbortToMenu()
        {
            StopAllCoroutines(); sfx.StopRumble(); Time.timeScale = 1f; ui.HideDialogue();
            flow = StartCoroutine(MainLoop());
        }

        // ------------------------------------------------------------ stage helpers
        public bool inMenu;

        public void NewStage(Color ambientSky, Color ambientEq, Color ambientGround, bool fog = false, Color? fogColor = null, float fogDensity = 0.002f)
        {
            if (stageRoot != null) Destroy(stageRoot.gameObject);
            stageRoot = new GameObject("Stage").transform;
            ui.ClearStage();
            cam.transform.SetParent(null); cam.fieldOfView = 50;
            cam.backgroundColor = Color.black; cam.clearFlags = CameraClearFlags.SolidColor;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ambientSky; RenderSettings.ambientEquatorColor = ambientEq; RenderSettings.ambientGroundColor = ambientGround;
            RenderSettings.fog = fog; if (fog) { RenderSettings.fogMode = FogMode.ExponentialSquared; RenderSettings.fogColor = fogColor ?? Color.gray; RenderSettings.fogDensity = fogDensity; }
            sfx.StopRumble();
            Cursor.lockState = CursorLockMode.None; Cursor.visible = true; cam.nearClipPlane = 0.05f; World.alienFlora = false;
        }
        public void NewSpaceStage() { NewStage(new Color(0.03f, 0.05f, 0.1f), new Color(0.02f, 0.03f, 0.06f), new Color(0.01f, 0.01f, 0.02f)); World.Sky(stageRoot); }

        public IEnumerator Over(float dur, Action<float> f, bool smooth = true)
        {
            float t = 0;
            while (t < dur) { t += Time.deltaTime; float k = Mathf.Clamp01(t / dur); f(smooth ? Mathf.SmoothStep(0, 1, k) : k); yield return null; }
            f(1f);
        }
        public IEnumerator Glide(Vector3 pos, Quaternion rot, float dur)
        {
            Vector3 p0 = cam.transform.position; Quaternion r0 = cam.transform.rotation;
            yield return Over(dur, k => { cam.transform.position = Vector3.Lerp(p0, pos, k); cam.transform.rotation = Quaternion.Slerp(r0, rot, k); });
        }
        public void CamAt(Vector3 pos, Vector3 look) { cam.transform.position = pos; cam.transform.rotation = Quaternion.LookRotation(look - pos, Vector3.up); }
        public IEnumerator Pause(float s) { yield return new WaitForSeconds(s); }
        public IEnumerator Say(string who, string t, float auto = 0) { return ui.Say(who, t, auto); }

        // ------------------------------------------------------------ main loop / menu
        IEnumerator MainLoop()
        {
            while (true)
            {
                int pick = 0;
                yield return Menu(v => pick = v);
                switch (pick)
                {
                    case 1: yield return Campaign(0); break;
                    case 2: case 3: case 4: case 5: case 6: case 7: yield return Campaign(pick - 2); break;
                    case 8: yield return DesignMode(); break;
                    case 9: yield return Credits(); break;
                }
            }
        }

        // chapter 0 prologue,1..4 missions, 5 finale. campaign runs from the chosen chapter onward
        IEnumerator Campaign(int from)
        {
            if (from == 0) prog = new Progress();
            if (from <= 0) yield return Prologue();
            if (from <= 1) yield return Mission1();
            if (from <= 2) yield return Mission2();
            if (from <= 3) yield return Mission3();
            if (from <= 4) yield return Mission4();
            if (from <= 5) yield return Finale();
            PlayerPrefs.SetInt("tlks_done", 1); PlayerPrefs.Save();
        }

        IEnumerator Menu(Action<int> choose)
        {
            inMenu = true;
            NewSpaceStage();
            var sun = World.MakeSun(stageRoot, new Vector3(18, -35, 0), 2.6f);
            var earth = World.Earth(stageRoot);
            var moon = World.Moon(stageRoot); moon.transform.position = new Vector3(-52, 6, 62);
            earth.root.transform.rotation = Quaternion.Euler(10, 150, 18);
            World.Starfield(stageRoot, 260, 120f);
var sunObj = World.Sun(stageRoot); sunObj.transform.position = new Vector3(-150, 70, 460);
            sunObj.transform.localScale = Vector3.one * 1.4f;
            CamAt(new Vector3(-3, 6, -40), new Vector3(-12, 0, 0));
            yield return ui.Fade(0f, 1.2f);

            var w = new Wait(); int result = 0;
            Action<int> pick = v => { result = v; w.Done(); };
            var root = ui.Stretch(ui.stage, "Menu");
            ui.Label(root, "A NASA SPACE APPS CAIRO 2026 GAME", 18, Theme.Cyan, A.TL, 90, -110, 800, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(root, "THE LAST\nKNOWN SIGNAL", 104, Theme.Text, A.TL, 84, -150, 1000, 260, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(root, "Design the mission. See the true odds. Live with the consequences.", 24, Theme.Dim, A.TL, 90, -420, 900, 40);
            float y = -500;
            ui.Btn(root, "BEGIN CAMPAIGN", A.TL, 90, y, 380, 62, () => pick(1), Theme.Cyan, 26); y -= 76;
            var ch = ui.Btn(root, "CHAPTER SELECT", A.TL, 90, y, 380, 54, null, Theme.Cyan, 22); y -= 66;
            bool unlocked = judge || PlayerPrefs.GetInt("tlks_done", 0) == 1;
            var dm = ui.Btn(root, unlocked ? "MISSION DESIGN MODE" : "MISSION DESIGN MODE  (LOCKED)", A.TL, 90, y, 380, 54, () => { if (judge || PlayerPrefs.GetInt("tlks_done", 0) == 1) pick(8); else ui.Toast("Finish the campaign to unlock - or enable Judge Mode below", Theme.Amber); }, unlocked ? Theme.Green : Theme.Dim, 20); y -= 66;
            ui.Btn(root, "SCIENCE VS FICTION  &  NASA CREDITS", A.TL, 90, y, 380, 54, () => pick(9), Theme.Cyan, 18); y -= 66;
            var jb = ui.Btn(root, judge ? "JUDGE MODE: ON (all unlocked)" : "JUDGE MODE: OFF", A.TL, 90, y, 380, 44, null, Theme.Amber, 16); y -= 54;
            jb.onClick.AddListener(() => { judge = !judge; PlayerPrefs.SetInt("tlks_judge", judge ? 1 : 0); UIKit.SetBtn(jb, judge ? "JUDGE MODE: ON (all unlocked)" : "JUDGE MODE: OFF"); UIKit.SetBtn(dm, (judge || PlayerPrefs.GetInt("tlks_done", 0) == 1) ? "MISSION DESIGN MODE" : "MISSION DESIGN MODE  (LOCKED)"); });
            ui.Btn(root, "QUIT", A.TL, 90, y, 380, 44, () => Application.Quit(), Theme.Red, 18);
            // chapter list
            var chp = ui.Panel(root, A.TL, 500, -566, 420, 420, Theme.Bg2); chp.gameObject.SetActive(false);
            string[] names = { "Prologue: The Last Known Signal", "Mission 1: What Fell to Earth", "Mission 2: Ten Percent", "Mission 3: The Ship That Came Out of Nowhere", "Mission 4: Someone's Been Living Here", "Final scene and end card" };
            for (int i = 0; i < names.Length; i++) { int k = i; ui.Btn(chp, names[i], A.TL, 16, -16 - i * 64, 388, 54, () => pick(2 + k), Theme.Cyan, 16); }
            ch.onClick.AddListener(() => chp.gameObject.SetActive(!chp.gameObject.activeSelf));
            ui.Label(root, "Data: NASA JPL Horizons - NASA Exoplanet Archive - Deep Space Network (DSN Now) - NASA Visible Earth imagery     |     F11 fullscreen - ESC x2 returns to this menu", 15, Theme.Dim, A.BL, 90, 28, 1700, 26);

            var orbit = StartCoroutine(MenuOrbit(earth));
            yield return w;
            StopCoroutine(orbit);
            inMenu = false;
            sfx.Click();
            yield return ui.Fade(1f, 0.8f);
            choose(result);
        }

        IEnumerator MenuOrbit(World.EarthRig earth)
        {
            float a = 0;
            while (true)
            {
                a += Time.deltaTime * 1.5f;
                CamAt(new Vector3(-3 + Mathf.Sin(a * 0.05f) * 2f, 6 + Mathf.Sin(a * 0.04f), -40), new Vector3(-12, 0, 0));
                yield return null;
            }
        }

        IEnumerator Credits()
        {
            NewSpaceStage(); World.MakeSun(stageRoot, new Vector3(20, -40, 0), 2.4f);
            var e = World.Earth(stageRoot); CamAt(new Vector3(0, 4, -30), new Vector3(0, 0, 0));
            yield return ui.Fade(0f, 0.6f);
            var w = new Wait();
            var p = ui.Panel(ui.stage, A.C, 0, 0, 1500, 900, Theme.Bg2);
            ui.Label(p, "SCIENCE vs FICTION", 40, Theme.Cyan, A.TL, 44, -30, 900, 60, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(p, "<color=#5CFF9D>REAL</color>\nAcceleration = thrust / mass  -  the rocket equation (m0 = mf e^(dv/ve))  -  redundancy mathematics  -  light delay = distance / speed of light  -  planetary positions from JPL Horizons  -  the planet readout from a real NASA Exoplanet Archive entry (Kepler-442 b)  -  Deep Space Network activity from DSN Now.\n\n<color=#FF5C5C>FICTIONAL</color>\nThe year 2590  -  the nuclear-powered ship and its performance  -  the travel time to the planet  -  the characters  -  the relay and station network. (The real light-time to Kepler-442 b is over 1,100 years: the game's relay network is invented.)\n\n<color=#FFB547>GAME-BALANCE VALUES</color>\nSubsystem reliabilities (0.63 and 0.90), mass costs, supply masses, launch limit and destination day-counts are chosen for gameplay and are labelled as such in the game. Engine / module figures come from NASA fact sheets and should be re-checked against the cited pages.", 22, Theme.Text, A.TL, 44, -100, 1400, 520);
            ui.Label(p, "CREDITS   -   NASA JPL Horizons System (ssd.jpl.nasa.gov/horizons)  -  NASA Exoplanet Archive (exoplanetarchive.ipac.caltech.edu)  -  DSN Now (eyes.nasa.gov/dsn)  -  NASA Visible Earth / Blue Marble / Black Marble imagery (visibleearth.nasa.gov)  -  Image credit: NASA.  Story, code and 3D art produced with AI assistance (AI-use disclosure included in the submission).", 17, Theme.Dim, A.BL, 44, 100, 1400, 150, TextAnchor.LowerLeft);
            ui.Btn(p, "BACK", A.BR, -40, 24, 220, 54, () => w.Done(), Theme.Cyan, 22);
            yield return w;
            yield return ui.Fade(1f, 0.5f);
        }
    }
}
