using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TLKS
{
    /// <summary>Pip, the nervous toy AI: a glowing orb that floats beside the camera.</summary>
    public class PipOrb : MonoBehaviour
    {
        public Vector3 offset = new Vector3(-0.62f, -0.30f, 1.3f); public float scale = 1f;
        Transform eyeL, eyeR; Material glowMat; float nerv;
        public static PipOrb Create(Transform parent)
        {
            var go = new GameObject("Pip"); go.transform.SetParent(parent, false);
            var pip = go.AddComponent<PipOrb>();
            var body = World.Prim(PrimitiveType.Sphere, go.transform, Vector3.zero, Vector3.zero, Vector3.one * 0.13f, Mats.Lit(new Color(0.85f, 0.95f, 1f), 0.9f, new Color(0.1f, 0.45f, 0.4f)));
            World.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0, 0.0f, 0.045f), Vector3.zero, new Vector3(0.108f, 0.075f, 0.05f), Mats.Lit(Color.black, 0.9f));
            pip.eyeL = World.Prim(PrimitiveType.Sphere, go.transform, new Vector3(-0.026f, 0.003f, 0.066f), Vector3.zero, new Vector3(0.022f, 0.03f, 0.01f), Mats.Lit(Color.cyan, 0.2f, new Color(0.3f, 3f, 2.8f))).transform;
            pip.eyeR = World.Prim(PrimitiveType.Sphere, go.transform, new Vector3(0.026f, 0.003f, 0.066f), Vector3.zero, new Vector3(0.022f, 0.03f, 0.01f), Mats.Lit(Color.cyan, 0.2f, new Color(0.3f, 3f, 2.8f))).transform;
            var aura = World.Prim(PrimitiveType.Quad, go.transform, Vector3.zero, Vector3.zero, Vector3.one * 0.3f, Mats.Additive(new Color(0.2f, 1f, 0.85f, 0.22f)));
            aura.AddComponent<Billboard>();
            var l = new GameObject("L"); l.transform.SetParent(go.transform, false); var lt = l.AddComponent<Light>(); lt.color = new Color(0.4f, 1f, 0.9f); lt.range = 3f; lt.intensity = 1.2f;
            return pip;
        }
        void LateUpdate()
        {
            var c = Camera.main; if (c == null) return;
            float t = Time.time;
            nerv = Mathf.Lerp(nerv, Game.I != null && Game.I.ui.dialogueBusy ? 1f : 0f, Time.deltaTime * 4f);
            Vector3 o = offset * scale + new Vector3(Mathf.Sin(t * 1.3f) * 0.02f, Mathf.Sin(t * 1.9f) * 0.03f + Mathf.Sin(t * 14f) * 0.004f * nerv, 0);
            transform.position = c.transform.TransformPoint(o);
            transform.rotation = Quaternion.LookRotation(c.transform.position - transform.position, Vector3.up) * Quaternion.Euler(0, 180, 0) * Quaternion.Euler(0, 180, 0);
            float blink = Mathf.Repeat(t, 3.6f) < 0.12f ? 0.1f : 1f;
            eyeL.localScale = new Vector3(0.022f, 0.03f * blink, 0.01f); eyeR.localScale = eyeL.localScale;
        }
    }

    public partial class Game
    {
        // ====================================================== PROLOGUE
        IEnumerator Prologue()
        {
            NewSpaceStage();
            var sun = World.MakeSun(stageRoot, new Vector3(15, -50, 0), 2.7f);
            var earth = World.Earth(stageRoot); earth.root.transform.rotation = Quaternion.Euler(8, 200, 12);
            var moon = World.Moon(stageRoot); moon.transform.position = new Vector3(60, 12, 80);
            World.Starfield(stageRoot, 200, 100f);
            var ship = World.Ship(0, stageRoot); ship.transform.position = new Vector3(2.2f, 1.2f, -17f); ship.transform.rotation = Quaternion.Euler(0, 40, 0);
            ship.transform.localScale = Vector3.one * 1.2f;
            var fl = Fx.Flame(ship.transform.Find("Nozzle"), Vector3.zero, 0.5f, new Color(1f, 0.7f, 0.4f, 1f));
            CamAt(new Vector3(4, 3, -34), new Vector3(0, 0, 0));
            StartCoroutine(ui.Letterbox(true, 1.2f));
            yield return ui.Fade(0f, 2f);
            StartCoroutine(Glide(new Vector3(1.2f, 1.8f, -22f), Quaternion.LookRotation(new Vector3(2f, 1f, -17f) - new Vector3(1.2f, 1.8f, -22f)), 22f));
            yield return Say("NARRATOR", "The year is 2590. Survey pilots leave for deep space aboard ships that are built faster than they can be tested.", 4.5f);
            yield return Say("NARRATOR", "One pilot leaves on a ship with untested equipment.", 3.2f);
            yield return Say("SON", "Dad, I'll call you when I get there.", 4f);
            sfx.Whoosh();
            yield return Say("NARRATOR", "Contact broke. Only a fragment of the launch vehicle ever came home.", 4f);
            yield return Say("NARRATOR", "His father took a job on the build team of the next ship.", 4f);
            yield return ui.Fade(1f, 1f);

            // ---- the son's launch panel: a risk gauge that reads ??%
            yield return ui.Letterbox(false, 0.2f);
            Destroy(fl.gameObject);
            ship.transform.position = new Vector3(-0.35f, 0, 0); ship.transform.rotation = Quaternion.Euler(10, -35, 0); ship.transform.localScale = Vector3.one * 1.0f;
            earth.root.SetActive(false); moon.SetActive(false);
            CamAt(new Vector3(0.0f, 0.2f, -2.6f), new Vector3(0f, 0, 0));
            var spin = ship.AddComponent<Spin>(); spin.dps = 14f; spin.axis = Vector3.up;
            sun.transform.rotation = Quaternion.Euler(25, 160, 0);
            var root = ui.Stretch(ui.stage, "LaunchPanel");
            var p = ui.Panel(root, A.R, -70, 70, 760, 720, Theme.Bg2);
            ui.Label(p, "SURVEY CRAFT  -  PRE-LAUNCH PANEL", 22, Theme.Cyan, A.TL, 36, -28, 700, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(p, "PILOT STATION  -  LAUNCH CHECKLIST", 16, Theme.Dim, A.TL, 36, -60, 700, 24);
            for (int i = 0; i < Plan.N; i++)
            {
                float y = -110 - i * 38;
                ui.Label(p, Plan.Names[i].ToUpper(), 20, Theme.Text, A.TL, 36, y, 380, 30);
                ui.Label(p, "SINGLE-STRING", 16, Theme.Dim, A.TL, 380, y + 2, 200, 30);
                ui.Label(p, "NOMINAL", 20, Theme.Green, A.TR, -36, y, 160, 30, TextAnchor.UpperRight, FontStyle.Bold);
            }
            var g = ui.Gauge(p, A.T, 0, -330, 440, "MISSION SUCCESS PROBABILITY"); g.unknown = true;
            ui.Label(p, "<color=#FFB547>NOT DISPLAYED TO CREW</color>", 20, Theme.Amber, A.B, 0, 96, 700, 30, TextAnchor.UpperCenter, FontStyle.Bold);
            yield return ui.Fade(0f, 1.2f);
            yield return Say("NARRATOR", "This is what the pilot saw. A checklist, all green. And one number that was never on his screen.", 5f);
            yield return Say("NARRATOR", "The pilot never saw that number.", 3.5f);
            var w = new Wait();
            ui.Btn(p, "FIND OUT WHAT IT WAS", A.B, 0, 18, 440, 52, () => w.Done(), Theme.Amber, 20);
            yield return w;
            yield return ui.Fade(1f, 0.8f);
            ui.ClearStage();
            ui.Label(ui.stage, "THE LAST KNOWN SIGNAL", 84, Theme.Text, A.C, 0, 20, 1500, 130, TextAnchor.MiddleCenter, FontStyle.Bold);
            ui.Label(ui.stage, "In this game, you design the mission - and you always see its true odds.", 26, Theme.Cyan, A.C, 0, -60, 1500, 40, TextAnchor.MiddleCenter);
            yield return ui.Fade(0f, 1f); yield return new WaitForSeconds(2.8f); yield return ui.Fade(1f, 0.8f);
        }

        // ====================================================== MISSION 1
        GameObject BuildFrame(Transform parent, Dictionary<string, int> cnt, out Transform tip)
        {
            var root = new GameObject("Frame"); root.transform.SetParent(parent, false);
            var steel = Mats.Lit(new Color(0.55f, 0.58f, 0.62f), 0.7f); var dk = Mats.Lit(new Color(0.12f, 0.13f, 0.15f), 0.6f);
            var orange = Mats.Lit(new Color(0.9f, 0.4f, 0.1f), 0.5f, new Color(0.3f, 0.1f, 0.02f));
            var white = Mats.Lit(new Color(0.9f, 0.92f, 0.95f), 0.6f);
            World.Prim(PrimitiveType.Cube, root.transform, new Vector3(0, 0, 0), Vector3.zero, new Vector3(4.4f, 0.3f, 4.4f), steel);
            for (int i = 0; i < 4; i++) { float sx = (i % 2 == 0 ? -1 : 1) * 1.8f, sz = (i < 2 ? -1 : 1) * 1.8f; World.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(sx, 1.6f, sz), Vector3.zero, new Vector3(0.14f, 1.6f, 0.14f), steel); }
            World.Prim(PrimitiveType.Cube, root.transform, new Vector3(0, 3.2f, 0), Vector3.zero, new Vector3(4.0f, 0.2f, 4.0f), steel);
            int engines = 0; foreach (var kv in cnt) if (kv.Value > 0 && (kv.Key == "rl10" || kv.Key == "j2" || kv.Key == "rs25" || kv.Key == "f1")) engines += kv.Value;
            engines = Mathf.Min(engines, 12);
            float er = engines <= 1 ? 0 : 1.4f;
            for (int i = 0; i < engines; i++)
            {
                float a = i * Mathf.PI * 2f / Mathf.Max(1, engines); Vector3 pos = new Vector3(Mathf.Cos(a) * er, -0.3f, Mathf.Sin(a) * er);
                World.Prim(PrimitiveType.Cylinder, root.transform, pos + new Vector3(0, 0.5f, 0), Vector3.zero, new Vector3(0.45f, 0.35f, 0.45f), dk);
                World.Prim(PrimitiveType.Cylinder, root.transform, pos + new Vector3(0, -0.15f, 0), Vector3.zero, new Vector3(0.6f, 0.3f, 0.6f), Mats.Lit(new Color(0.3f, 0.3f, 0.34f), 0.9f));
            }
            float top = 3.4f;
            int n;
            if (cnt.TryGetValue("esm", out n) && n > 0) { World.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, top + 0.9f, 0), Vector3.zero, new Vector3(2.4f, 0.9f, 2.4f), orange); top += 1.8f; }
            if (cnt.TryGetValue("capsule", out n) && n > 0)
            {
                World.Prim(PrimitiveType.Cylinder, root.transform, new Vector3(0, top + 0.5f, 0), Vector3.zero, new Vector3(2.2f, 0.5f, 2.2f), white);
                World.Prim(PrimitiveType.Sphere, root.transform, new Vector3(0, top + 1.2f, 0), Vector3.zero, new Vector3(1.9f, 1.4f, 1.9f), white); top += 2.5f;
            }
            if (cnt.TryGetValue("scanner", out n) && n > 0) World.Prim(PrimitiveType.Cube, root.transform, new Vector3(1.7f, 3.55f, 0), Vector3.zero, new Vector3(0.6f, 0.6f, 1.2f), Mats.Lit(new Color(0.15f, 0.2f, 0.3f), 0.8f, new Color(0.0f, 0.3f, 0.6f)));
            if (cnt.TryGetValue("dish", out n) && n > 0) World.Prim(PrimitiveType.Sphere, root.transform, new Vector3(-1.7f, 3.8f, 0), new Vector3(0, 0, 35), new Vector3(1.0f, 0.1f, 1.0f), white);
            tip = new GameObject("Nozzle").transform; tip.SetParent(root.transform, false); tip.localPosition = new Vector3(0, -0.6f, 0);
            return root;
        }

        IEnumerator Mission1()
        {
            prog.m1Attempts = 0;
            // ---- night spaceport
            NewStage(new Color(0.12f, 0.17f, 0.3f), new Color(0.08f, 0.11f, 0.18f), new Color(0.04f, 0.04f, 0.06f), true, new Color(0.03f, 0.05f, 0.09f), 0.0022f);
            World.Sky(stageRoot);
            var moonL = World.MakeSun(stageRoot, new Vector3(38, -35, 0), 1.7f, true); moonL.color = new Color(0.65f, 0.8f, 1f);
            var ground = World.Prim(PrimitiveType.Cube, stageRoot, new Vector3(0, -0.15f, 0), Vector3.zero, new Vector3(1200, 0.3f, 1200), Mats.Ground());
            var pad = World.Prim(PrimitiveType.Cylinder, stageRoot, new Vector3(0, 0.05f, 0), Vector3.zero, new Vector3(18f, 0.1f, 18f), Mats.Lit(new Color(0.28f, 0.29f, 0.31f), 0.35f));
            var ring = World.Prim(PrimitiveType.Cylinder, stageRoot, new Vector3(0, 0.11f, 0), Vector3.zero, new Vector3(13f, 0.02f, 13f), Mats.Lit(new Color(0.9f, 0.5f, 0.1f), 0.2f, new Color(0.9f, 0.4f, 0.05f) * 0.6f));
            var hangar = World.Building("warehouse_01", stageRoot); hangar.transform.position = new Vector3(-48, 0, 28); hangar.transform.rotation = Quaternion.Euler(0, 20, 0); hangar.transform.localScale = Vector3.one * 1.5f;
            var tower = World.Building("tower_antenna", stageRoot); tower.transform.position = new Vector3(38, 0, 22); tower.transform.localScale = Vector3.one * 1.4f;
            var office = World.Building("office_building_01", stageRoot); office.transform.position = new Vector3(70, 0, 70); office.transform.rotation = Quaternion.Euler(0, -30, 0);
            var dist = World.Building("city_district", stageRoot); dist.transform.position = new Vector3(-80, 0, 360); dist.transform.localScale = Vector3.one * 1.6f;
            for (int i = 0; i < 8; i++)
            {
                float a = i * Mathf.PI / 4f;
                var lamp = new GameObject("Flood"); lamp.transform.SetParent(stageRoot, false); lamp.transform.position = new Vector3(Mathf.Cos(a) * 15f, 7f, Mathf.Sin(a) * 15f);
                var lt = lamp.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(1f, 0.85f, 0.65f); lt.range = 34f; lt.intensity = 45f;
                var mast = World.Prim(PrimitiveType.Cylinder, stageRoot, new Vector3(Mathf.Cos(a) * 15f, 3.5f, Mathf.Sin(a) * 15f), Vector3.zero, new Vector3(0.18f, 3.5f, 0.18f), Mats.Lit(new Color(0.25f, 0.25f, 0.28f), 0.5f));
                World.Prim(PrimitiveType.Sphere, stageRoot, new Vector3(Mathf.Cos(a) * 15f, 7f, Mathf.Sin(a) * 15f), Vector3.zero, Vector3.one * 0.5f, Mats.Lit(Color.white, 0.2f, new Color(5, 4, 3)));
                var gl = World.Prim(PrimitiveType.Quad, stageRoot, new Vector3(Mathf.Cos(a) * 15f, 7f, Mathf.Sin(a) * 15f), Vector3.zero, Vector3.one * 5f, Mats.Additive(new Color(1f, 0.8f, 0.55f, 0.45f))); gl.AddComponent<Billboard>();
            }
            var cnt = new Dictionary<string, int>(); foreach (var pt in Nasa.parts) cnt[pt.id] = 0;
            cnt["capsule"] = 0;
            Transform tip; var frameHolder = new GameObject("FrameHolder").transform; frameHolder.SetParent(stageRoot, false); frameHolder.position = new Vector3(0, 0.3f, 0);
            var frame = BuildFrame(frameHolder, cnt, out tip);
            CamAt(new Vector3(-7, 5, -12), new Vector3(0, 4.5f, 0));
            var pip = PipOrb.Create(stageRoot);
            yield return ui.Fade(0f, 1.5f);
            StartCoroutine(Glide(new Vector3(-6, 5.2f, -11), Quaternion.LookRotation(new Vector3(0, 4.3f, 0) - new Vector3(-6, 5.2f, -11)), 6f));

            yield return Say("PIP", "H-hello! I'm Pip. I'm a very small AI, and I'm supposed to help. Please don't be cross if I get nervous.");
            yield return Say("FATHER", "The fragment that came home. The serial number is stamped inside the housing. Pip, pull it up.");
            yield return Say("PIP", "Matching... it's an external section. It separated on ascent. Not the whole ship - just a piece of it. That's all that was found.");
            yield return Say("PIP", "The recovery report is... empty. I checked twice. There is, however, a test archive with the capsule's last recorded heading. It's locked until a test frame like his passes a fire test.");
            yield return Say("FATHER", "Then we build the frame. And this time we do the arithmetic first.");
            yield return Say("PIP", "Rule one: acceleration is thrust divided by mass. Rule two: everything you bolt on makes rule one worse.");

            // ---- parts builder
            float budget = 25000f, frameMass = 2500f; const float g = 9.81f, minTwr = 1.2f, maxTwr = 3.5f;
            bool passed = false;
            while (!passed)
            {
                var root = ui.Stretch(ui.stage, "M1");
                var lp = ui.Panel(root, A.TL, 40, -40, 860, 1000);
                ui.Label(lp, "TEST-FRAME BUILDER", 38, Theme.Cyan, A.TL, 30, -22, 700, 50, TextAnchor.UpperLeft, FontStyle.Bold);
                ui.Label(lp, "Bolt engines and modules onto the frame. Values come from NASA fact sheets where marked; the budget is a game value.", 17, Theme.Dim, A.TL, 30, -72, 800, 50);
                var cTx = new Dictionary<string, Text>();
                int idx = 0;
                Text mTx = null, tTx = null, aTx = null, cTx2 = null; Image mBar = null, tBar = null; Wait fire = new Wait();
                Action refresh = null;
                foreach (var pt in Nasa.parts)
                {
                    var part = pt; float y = -132 - idx * 108; idx++;
                    ui.Img(lp, "Row", A.TL, 20, y + 4, 820, 100, new Color(1, 1, 1, 0.045f), ui.round);
                    ui.Label(lp, part.name, 22, Theme.Text, A.TL, 34, y - 6, 560, 30, TextAnchor.UpperLeft, FontStyle.Bold);
                    ui.Label(lp, part.mass.ToString("N0") + " kg" + (part.thrust > 0 ? "   -   " + part.thrust.ToString("N0") + " kN" : ""), 20, part.kind == "engine" ? Theme.Amber : Theme.Cyan, A.TL, 34, y - 36, 560, 28);
                    ui.Label(lp, "Source: " + part.source, 13, Theme.Dim, A.TL, 34, y - 66, 640, 24);
                    ui.Btn(lp, "-", A.TR, -150, y - 14, 48, 46, () => { if (cnt[part.id] > 0) { cnt[part.id]--; refresh(); } }, Theme.Cyan, 26);
                    cTx[part.id] = ui.Label(lp, "0", 30, Theme.Text, A.TR, -98, y - 18, 44, 40, TextAnchor.UpperCenter, FontStyle.Bold);
                    ui.Btn(lp, "+", A.TR, -42, y - 14, 48, 46, () => { if (cnt[part.id] < (part.kind == "engine" ? 6 : 1)) { cnt[part.id]++; refresh(); } }, Theme.Cyan, 26);
                }
                var rp = ui.Panel(root, A.TR, -40, -40, 900, 1000);
                ui.Label(rp, "LIVE READOUT", 28, Theme.Cyan, A.TL, 34, -24, 600, 40, TextAnchor.UpperLeft, FontStyle.Bold);
                mTx = ui.Label(rp, "", 24, Theme.Text, A.TL, 34, -80, 830, 36);
                ui.Img(rp, "mbg", A.TL, 34, -122, 830, 18, new Color(1, 1, 1, 0.1f), ui.round); mBar = ui.Img(rp, "mb", A.TL, 34, -122, 830, 18, Theme.Cyan, ui.round); mBar.type = Image.Type.Filled; mBar.sprite = ui.whiteSpr; mBar.fillMethod = Image.FillMethod.Horizontal;
                tTx = ui.Label(rp, "", 24, Theme.Text, A.TL, 34, -168, 830, 36);
                ui.Img(rp, "tbg", A.TL, 34, -210, 830, 18, new Color(1, 1, 1, 0.1f), ui.round); tBar = ui.Img(rp, "tb", A.TL, 34, -210, 830, 18, Theme.Amber, ui.round); tBar.type = Image.Type.Filled; tBar.sprite = ui.whiteSpr; tBar.fillMethod = Image.FillMethod.Horizontal;
                aTx = ui.Label(rp, "", 28, Theme.Text, A.TL, 34, -262, 830, 120, TextAnchor.UpperLeft, FontStyle.Bold);
                cTx2 = ui.Label(rp, "", 22, Theme.Text, A.TL, 34, -400, 830, 300);
                ui.Label(rp, "acceleration = thrust / mass        net climb = thrust / mass - g (9.81 m/s^2)\nLiftoff needs thrust-to-weight of at least " + minTwr.ToString("0.0") + ". The test stand is rated to " + maxTwr.ToString("0.0") + ".", 17, Theme.Dim, A.BL, 34, 110, 830, 60, TextAnchor.LowerLeft);
                ui.Btn(rp, "TEST FIRE", A.B, 0, 32, 560, 66, () => fire.Done(), Theme.Green, 30);
                bool havePrev = false;
                refresh = () =>
                {
                    float mass = frameMass, thr = 0; foreach (var pt in Nasa.parts) { mass += pt.mass * cnt[pt.id]; thr += pt.thrust * cnt[pt.id]; cTx[pt.id].text = cnt[pt.id].ToString(); }
                    float twr = thr * 1000f / (mass * g), a = thr * 1000f / mass;
                    mTx.text = "Mass " + mass.ToString("N0") + " / " + budget.ToString("N0") + " kg   (frame structure " + frameMass.ToString("N0") + " kg)";
                    mTx.color = mass > budget ? Theme.Red : Theme.Text; mBar.fillAmount = Mathf.Clamp01(mass / budget); mBar.color = mass > budget ? Theme.Red : Theme.Cyan;
                    tTx.text = "Thrust " + thr.ToString("N0") + " kN   -   thrust-to-weight " + twr.ToString("0.00");
                    tBar.fillAmount = Mathf.Clamp01(twr / 5f); tBar.color = (twr >= minTwr && twr <= maxTwr) ? Theme.Green : Theme.Red;
                    aTx.text = "a = " + a.ToString("0.0") + " m/s^2     net climb " + (a - g).ToString("0.0") + " m/s^2";
                    aTx.color = twr >= minTwr ? Theme.Text : Theme.Amber;
                    cTx2.text = Chk(cnt["capsule"] > 0, "Crew capsule fitted") + Chk(mass <= budget, "Mass within budget (margin " + (budget - mass).ToString("N0") + " kg)") + Chk(twr >= minTwr, "Thrust-to-weight >= " + minTwr.ToString("0.0")) + Chk(twr <= maxTwr, "Under the frame's rated limit (<= " + maxTwr.ToString("0.0") + ")");
                    if (havePrev) Destroy(frame);
                    frame = BuildFrame(frameHolder, cnt, out tip); havePrev = true; sfx.Click();
                };
                refresh();
                if (devAuto) { yield return new WaitForSeconds(1.5f); cnt["capsule"] = 1; cnt["rl10"] = 2; refresh(); yield return new WaitForSeconds(2.5f); fire.Done(); }
                yield return fire;
                // ---- test fire
                prog.m1Attempts++;
                float M = frameMass, T = 0; foreach (var pt in Nasa.parts) { M += pt.mass * cnt[pt.id]; T += pt.thrust * cnt[pt.id]; }
                float TWR = T * 1000f / (M * g); float acc = T * 1000f / M;
                Destroy(root.gameObject);
                string failHint = null;
                if (cnt["capsule"] == 0) failHint = "There is nothing to test - the crew capsule is missing. A frame with no payload proves nothing.";
                else if (M > budget) failHint = "The frame is overweight (" + M.ToString("N0") + " kg against " + budget.ToString("N0") + "). It never leaves the bench. Remove a module or choose lighter hardware - and remember, mass is the enemy.";
                else if (TWR < minTwr) failHint = "Thrust-to-weight is only " + TWR.ToString("0.00") + ". Acceleration is thrust divided by mass: add engines or take mass off.";
                else if (TWR > maxTwr) failHint = "Thrust-to-weight " + TWR.ToString("0.0") + " is beyond what the frame is secured for. Too much thrust tears it apart. Use smaller engines.";
                // countdown
                var hud = ui.Stretch(ui.stage, "Fire"); var cd = ui.Label(hud, "", 120, Theme.Amber, A.C, 0, 150, 600, 160, TextAnchor.MiddleCenter, FontStyle.Bold);
                var ro = ui.Label(hud, "", 30, Theme.Cyan, A.TL, 60, -60, 800, 200, TextAnchor.UpperLeft, FontStyle.Bold);
                for (int c = 3; c >= 1; c--) { cd.text = c.ToString(); sfx.Beep(); yield return new WaitForSeconds(0.8f); }
                cd.text = "";
                var fl = new List<ParticleSystem>();
                int ne = 0; foreach (var pt in Nasa.parts) if (pt.kind == "engine") ne += cnt[pt.id]; ne = Mathf.Min(ne, 12);
                float er2 = ne <= 1 ? 0 : 1.4f;
                for (int i = 0; i < ne; i++)
                { float a2 = i * Mathf.PI * 2f / Mathf.Max(1, ne); var f = Fx.Flame(frameHolder, new Vector3(Mathf.Cos(a2) * er2, -0.4f, Mathf.Sin(a2) * er2), 1.4f, new Color(1f, 0.75f, 0.45f)); f.transform.localRotation = Quaternion.Euler(90, 0, 0); fl.Add(f); }
                var dust = Fx.Make(stageRoot, "Dust", new Color(0.8f, 0.75f, 0.7f, 0.5f), 3f, 3f, 6f, 70, 85f, true, 0.6f); dust.transform.position = new Vector3(0, 0.3f, 0); dust.transform.rotation = Quaternion.Euler(-90, 0, 0);
                float t = 0, h = 0, vv = 0; bool failed = failHint != null;
                var camBase = cam.transform.position;
                while (t < 6f)
                {
                    t += Time.deltaTime; float ramp = Mathf.Clamp01(t / 1.2f);
                    sfx.Rumble(0.2f + 0.6f * ramp);
                    bool overLimit = failed && cnt["capsule"] > 0 && M <= budget && TWR > maxTwr;
                    bool sluggish = failed && cnt["capsule"] > 0 && M <= budget && TWR >= 1f && TWR < minTwr;
                    bool noLift = failed && !overLimit && !sluggish;
                    float aNet = acc * ramp - g;
                    if (noLift) aNet = 0f; else if (sluggish) aNet *= 0.4f;
                    vv += Mathf.Max(aNet, 0f) * Time.deltaTime; h += vv * Time.deltaTime;
                    h = Mathf.Min(h, 40f);
                    frameHolder.position = new Vector3(0, 0.3f + h, 0);
                    frameHolder.position += UnityEngine.Random.insideUnitSphere * 0.03f * ramp;
                    ro.text = "THRUST " + (T * ramp).ToString("N0") + " kN\nACCEL  " + (ramp * acc).ToString("0.0") + " m/s^2\nHEIGHT " + h.ToString("0.0") + " m";
                    cam.transform.position = Vector3.Lerp(cam.transform.position, camBase + new Vector3(0, Mathf.Min(h * 0.5f, 14f), 0) + UnityEngine.Random.insideUnitSphere * 0.05f * ramp, Time.deltaTime * 3f);
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(new Vector3(0, 4.3f + h * 0.8f, 0) - cam.transform.position), Time.deltaTime * 3f);
                    if (overLimit && t > 2.2f) break;
                    if (!failed && h >= 30f) break;
                    yield return null;
                }
                sfx.StopRumble();
                foreach (var f in fl) { var em = f.emission; em.enabled = false; }
                if (!failed)
                {
                    passed = true; sfx.Ok();
                    ro.text = "TEST FIRE COMPLETE\nPeak a = " + acc.ToString("0.0") + " m/s^2   -   TWR " + TWR.ToString("0.00") + "\nMass margin " + (budget - M).ToString("N0") + " kg";
                    yield return new WaitForSeconds(1.5f);
                    Destroy(hud.gameObject);
                }
                else
                {
                    if (TWR > maxTwr && M <= budget && cnt["capsule"] > 0) { Fx.Burst(frameHolder.position, new Color(1, 0.6f, 0.2f), 150, 1.5f, 8f, 2f); sfx.Boom(); frame.SetActive(false); }
                    else sfx.Bad();
                    ro.text = "<color=#FF5C5C>TEST FAILED</color>";
                    yield return new WaitForSeconds(1.2f);
                    Destroy(hud.gameObject);
                    frameHolder.position = new Vector3(0, 0.3f, 0); frame.SetActive(true);
                    cam.transform.position = camBase; cam.transform.rotation = Quaternion.LookRotation(new Vector3(0, 4.3f, 0) - camBase);
                    yield return Say("PIP", failHint);
                }
            }
            // ---- win
            yield return Say("PIP", "It held! The archive is opening... The capsule's last recorded heading: zero-four-seven point three degrees. Out toward Kepler-442.");
            var hroot = ui.Stretch(ui.stage, "Heading");
            var hp = ui.Panel(hroot, A.C, 0, 0, 800, 300, Theme.Bg2);
            ui.Label(hp, "TEST ARCHIVE UNLOCKED", 20, Theme.Amber, A.TL, 36, -28, 600, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(hp, "LAST RECORDED HEADING", 18, Theme.Dim, A.TL, 36, -70, 600, 30);
            ui.Label(hp, "047.3°", 90, Theme.Cyan, A.TL, 36, -100, 600, 110, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(hp, "Recovery report: <color=#FF5C5C>EMPTY</color>", 22, Theme.Text, A.BL, 36, 30, 700, 34, TextAnchor.LowerLeft);
            sfx.Ok();
            yield return new WaitForSeconds(3.2f);
            Destroy(hroot.gameObject);
            yield return Say("FATHER", "Somebody knew something. A recovery report doesn't come back empty by accident.");
            yield return ui.Card("DEBRIEF CARD  -  MISSION 1", "Why mass is the enemy of every mission",
                "Acceleration is thrust divided by mass. Every kilogram you add to a vehicle lowers its acceleration - and to carry it, you need more propellant, which is more mass again. Your frame needed enough thrust to lift off, but not so much that the structure was overstressed.",
                "Launch vehicle teams track a \"mass margin\" through design: how many kilograms remain before the vehicle can no longer meet its performance. Engine and module masses and thrusts in this mission come from published NASA fact sheets.",
                "NASA vehicle & engine fact sheets (RL10, RS-25, J-2, F-1, Orion) - see each part's source line; NASA Technical Reports Server for detail",
                new[] { "Your result: " + prog.m1Attempts + " test-fire" + (prog.m1Attempts == 1 ? "" : "s") + " to pass." });
            prog.Card("Why mass is the enemy of every mission");
            yield return ui.Fade(1f, 0.6f);
        }

        static string Chk(bool ok, string s) { return (ok ? "<color=#5CFF9D>[OK]</color>  " : "<color=#FF5C5C>[  ]</color>  ") + s + "\n"; }
    }
}
