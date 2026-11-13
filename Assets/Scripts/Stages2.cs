using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TLKS
{
    public partial class Game
    {
        // ---------------------------------------------------------------- shared night spaceport set
        void BuildSpaceport(bool shipOnPad, out GameObject ship)
        {
            NewStage(new Color(0.12f, 0.17f, 0.3f), new Color(0.08f, 0.11f, 0.18f), new Color(0.04f, 0.04f, 0.06f), true, new Color(0.03f, 0.05f, 0.09f), 0.002f);
            World.Sky(stageRoot);
            var l = World.MakeSun(stageRoot, new Vector3(38, -35, 0), 1.7f, true); l.color = new Color(0.65f, 0.8f, 1f);
            World.Prim(PrimitiveType.Cube, stageRoot, new Vector3(0, -0.15f, 0), Vector3.zero, new Vector3(1200, 0.3f, 1200), Mats.Ground());
            World.Prim(PrimitiveType.Cylinder, stageRoot, new Vector3(0, 0.05f, 0), Vector3.zero, new Vector3(30f, 0.1f, 30f), Mats.Lit(new Color(0.28f, 0.29f, 0.31f), 0.35f));
            World.Prim(PrimitiveType.Cylinder, stageRoot, new Vector3(0, 0.11f, 0), Vector3.zero, new Vector3(22f, 0.02f, 22f), Mats.Lit(new Color(0.2f, 0.5f, 0.9f), 0.2f, new Color(0.1f, 0.4f, 0.9f) * 0.7f));
            var hangar = World.Building("warehouse_01", stageRoot); hangar.transform.position = new Vector3(-62, 0, 38); hangar.transform.rotation = Quaternion.Euler(0, 20, 0); hangar.transform.localScale = Vector3.one * 1.8f;
            var tower = World.Building("tower_antenna", stageRoot); tower.transform.position = new Vector3(50, 0, 26); tower.transform.localScale = Vector3.one * 1.5f;
            var dist = World.Building("city_district", stageRoot); dist.transform.position = new Vector3(-80, 0, 380); dist.transform.localScale = Vector3.one * 1.6f;
            for (int i = 0; i < 10; i++)
            {
                float a = i * Mathf.PI * 2f / 10f; Vector3 p = new Vector3(Mathf.Cos(a) * 24f, 8f, Mathf.Sin(a) * 24f);
                var lamp = new GameObject("Flood"); lamp.transform.SetParent(stageRoot, false); lamp.transform.position = p;
                var lt = lamp.AddComponent<Light>(); lt.type = LightType.Point; lt.color = new Color(0.8f, 0.9f, 1f); lt.range = 50f; lt.intensity = 70f;
                World.Prim(PrimitiveType.Cylinder, stageRoot, new Vector3(p.x, 4f, p.z), Vector3.zero, new Vector3(0.2f, 4f, 0.2f), Mats.Lit(new Color(0.25f, 0.25f, 0.28f), 0.5f));
                World.Prim(PrimitiveType.Sphere, stageRoot, p, Vector3.zero, Vector3.one * 0.6f, Mats.Lit(Color.white, 0.2f, new Color(4, 4.5f, 5)));
                var gl = World.Prim(PrimitiveType.Quad, stageRoot, p, Vector3.zero, Vector3.one * 6f, Mats.Additive(new Color(0.7f, 0.85f, 1f, 0.45f))); gl.AddComponent<Billboard>();
            }
            ship = null;
            if (shipOnPad)
            {
                ship = World.Ship(1, stageRoot); ship.transform.position = new Vector3(0, 9, 0); ship.transform.rotation = Quaternion.Euler(-90, 0, 0); ship.transform.localScale = Vector3.one * 14f;
            }
        }

        // ====================================================== MISSION 2
        IEnumerator Mission2()
        {
            prog.m2Attempts = 0;
            GameObject ship; BuildSpaceport(true, out ship);
            CamAt(new Vector3(-36, 8, -44), new Vector3(0, 11, 0));
            var pip = PipOrb.Create(stageRoot);
            yield return ui.Fade(0f, 1.2f);
            StartCoroutine(Glide(new Vector3(-30, 12, -38), Quaternion.LookRotation(new Vector3(0, 12, 0) - new Vector3(-30, 12, -38)), 14f));
            yield return Say("FATHER", "The new ship is nearly ready. But I keep thinking about his. Pip - his flight records. All of them.");
            yield return Say("PIP", "There's one file that didn't make it into the briefing archive. An engineering assessment. I... I think you should read it yourself.");

            // ---- the document
            var root = ui.Stretch(ui.stage, "Doc");
            var d = ui.Panel(root, A.C, 0, 0, 1240, 800, new Color(0.93f, 0.93f, 0.9f, 0.97f), false);
            var ink = new Color(0.1f, 0.12f, 0.16f);
            ui.Label(d, "FLIGHT ASSESSMENT  -  ENGINEERING REVIEW", 30, ink, A.TL, 50, -40, 1100, 44, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(d, "Survey craft: untested equipment, five single-string subsystems.\nLife support r = 0.63  -  Comms r = 0.63  -  Navigation r = 0.63  -  Power r = 0.63  -  Propulsion r = 0.63\nNo redundant units fitted.", 22, ink, A.TL, 50, -100, 1140, 120);
            ui.Label(d, "MISSION SUCCESS PROBABILITY:", 24, ink, A.TL, 50, -250, 800, 36, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(d, "10%", 130, new Color(0.75f, 0.1f, 0.1f), A.TL, 50, -285, 600, 160, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(d, "Recommendation: do not fly without redundant comms and life support.", 22, ink, A.TL, 50, -450, 1140, 36);
            ui.Label(d, "ROUTING:   Engineering assessment   --->   [ reporting gap ]   --->   Pilot briefing", 22, new Color(0.5f, 0.1f, 0.1f), A.TL, 50, -520, 1140, 36, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(d, "Records show the number was lost between the engineering assessment and the briefing. A reporting gap - not a cover-up. The pilot still deserved to see it.", 20, ink, A.TL, 50, -575, 1140, 80);
            sfx.Alarm();
            var w0 = new Wait(); ui.Btn(d, "SAVE BOTH VERSIONS  -  LOAD HIS ROUTE", A.BR, -40, 30, 560, 60, () => w0.Done(), Theme.Amber, 22);
            if (devAuto) { yield return new WaitForSeconds(2.5f); w0.Done(); }
            yield return w0;
            Destroy(root.gameObject);
            yield return Say("FATHER", "Ten percent. And nobody put it in front of him. I want that to be impossible on this ship. Pip, the planner - I want to see every number.");
            yield return Say("PIP", "On it. The planner always shows the true odds - never a hidden number. Start by loading his configuration, so you can see exactly what he flew.");

            // ---- the planner
            var cfg = new PlannerCfg { title = "MISSION PLANNER", sub = "Survey route to Kepler-442. Every subsystem must work. Add redundant units, but each costs mass the ship cannot spare.", allowSon = true, supplyMax = 300f, runLabel = "RUN PREFLIGHT" };
            cfg.dests.Add(new Dest("Kepler-442 survey route", 9.0f, 100f, "Heading 047.3 from the test archive (route is fiction)"));
            var plan = new Plan { dest = cfg.dests[0], tested = true, supplyDays = 120f };
            bool praised = false, loaded = false;
            cfg.gate = () => cfg.sonLoaded.done ? null : "Load the son's configuration first - see what he flew.";
            cfg.onChange = () =>
            {
                if (!loaded && cfg.sonLoaded.done && plan.n[0] == 1 && !plan.tested) { loaded = true; cfg.say("0.63 to the fifth power is 0.099. Ten percent. Now - switch to flight-tested equipment and add backups. Watch what each unit costs in mass."); }
                else if (loaded && !praised && plan.Effective() >= 0.7f) { praised = true; cfg.say("That's about " + Mathf.RoundToInt(plan.Effective() * 100f) + "%. A tested ship with backup radio and backup life support is near 71%. Ready when you are."); }
            };
            bool cleared = false;
            while (!cleared)
            {
                prog.m2Attempts++;
                cfg.say = null;
                var pl = RunPlanner(cfg, plan);
                // first pass: set Pip's opening line once the planner is built
                bool first = true;
                while (pl.MoveNext()) { if (first && cfg.say != null) { first = false; cfg.say("Press LOAD THE SON'S CONFIGURATION, then fix the plan yourself."); } yield return pl.Current; }
                float planned = plan.Effective();
                RunResult res = null;
                yield return RunMission(plan, true, "PREFLIGHT  -  NUCLEAR SHIP  -  EMERGENCY RADIO-CUT TEST", r => res = r, true);
                if (res.success) { cleared = true; prog.plannedFather = planned; prog.fatherPlan = plan.Clone(); }
                else
                {
                    string sys = Plan.Names[res.failedAt];
                    string hint = res.failedAt == 1 ? (plan.n[1] < 2 ? "The radio test cuts the long-range radio. With a single comms unit there is nothing to fall back on. Add a backup." : "The backup radio failed its own roll. More units, or better equipment, improves the odds.")
                                : "The " + sys.ToLower() + " check failed. A single unit fails " + Mathf.RoundToInt((1f - plan.R(res.failedAt)) * 100f) + "% of the time - consider a backup.";
                    yield return Say("PIP", hint);
                    cfg.sonLoaded.done = true;
                }
            }
            yield return Say("PIP", "Preflight cleared. The reactor is nominal, and the backup radio took the test without a hiccup.");
            yield return Say("FATHER", "Launch authorization granted. Whatever happens out there, the crew will always see the real number.");
            ship.AddComponent<Spin>().dps = 0f;
            yield return ui.Card("DEBRIEF CARD  -  MISSION 2", "Redundancy, and why risk numbers must reach the crew",
                "A mission succeeds only if every critical system works. With single-string systems, reliabilities multiply: 0.63 to the fifth power is about 10%. A backup unit turns a 63% subsystem into 86%, and 0.90 into 99% - at the price of mass, which the rocket equation multiplies again at launch.",
                "NASA missions carry redundant systems and publish risk assessments (probability of loss-of-mission and loss-of-crew) so decision-makers and crews can see them. The Deep Space Network's many dishes and spacecraft links are real examples of shared, redundant communications.",
                "Redundancy mathematics (standard reliability engineering); NASA Deep Space Network, DSN Now (eyes.nasa.gov/dsn); reliabilities in this game are GAME-BALANCE values",
                new[] { "You planned " + Mathf.RoundToInt(prog.plannedFather * 100f) + "% against the son's 10%.", "Preflight cleared after " + prog.m2Attempts + " attempt" + (prog.m2Attempts == 1 ? "" : "s") + "." });
            prog.Card("Redundancy, and why risk numbers must reach the crew");
            yield return ui.Fade(1f, 0.6f);
        }

        // ====================================================== MISSION 3
        class SigHud
        {
            public Text delay, info, status; public Image link1, link2, relayNode; public RectTransform panel;
        }
        SigHud BuildSigHud(Transform parent, A a, float x, float y)
        {
            var h = new SigHud();
            var p = ui.Panel(parent, a, x, y, 760, 250); h.panel = p;
            ui.Label(p, "SIGNAL PATH", 20, Theme.Cyan, A.TL, 22, -14, 300, 28, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(p, "JPL Horizons, 13 Nov 2026", 14, Theme.Dim, A.TR, -22, -18, 300, 24, TextAnchor.UpperRight);
            h.link1 = ui.Img(p, "L1", A.TL, 115, -88, 245, 5, Theme.Green); h.link2 = ui.Img(p, "L2", A.TL, 410, -88, 245, 5, Theme.Green);
            var ship = ui.Img(p, "Ship", A.TL, 60, -68, 46, 46, Theme.Cyan, ui.circle); h.relayNode = ui.Img(p, "Relay", A.TL, 360, -68, 46, 46, Theme.Green, ui.circle); var earth = ui.Img(p, "Earth", A.TL, 660, -68, 46, 46, new Color(0.3f, 0.6f, 1f), ui.circle);
            ui.Label(p, "SHIP", 14, Theme.Text, A.TL, 55, -122, 80, 20, TextAnchor.UpperCenter); ui.Label(p, "RELAY", 14, Theme.Text, A.TL, 352, -122, 70, 20, TextAnchor.UpperCenter); ui.Label(p, "EARTH", 14, Theme.Text, A.TL, 655, -122, 70, 20, TextAnchor.UpperCenter);
            h.delay = ui.Label(p, "", 26, Theme.Cyan, A.TL, 22, -150, 720, 36, TextAnchor.UpperLeft, FontStyle.Bold);
            h.info = ui.Label(p, "", 15, Theme.Dim, A.TL, 22, -188, 720, 56);
            return h;
        }
        void SigRefresh(SigHud h, bool relayUp)
        {
            var mars = Nasa.Get("Mars"); var jup = Nasa.Get("Jupiter");
            double shipRelay = (jup.pos - mars.pos).magnitude, relayEarth = mars.rangeKm, direct = jup.rangeKm;
            if (relayUp)
            {
                h.link1.color = Theme.Green; h.link2.color = Theme.Green; h.relayNode.color = Theme.Green;
                h.delay.text = "ONE-WAY LIGHT DELAY  " + Nasa.Dur(Nasa.LightDelay(shipRelay + relayEarth)); h.delay.color = Theme.Cyan;
                h.info.text = "ship -> relay (Mars-orbit relay) " + (shipRelay / 1e6).ToString("0") + " million km  +  relay -> Earth " + (relayEarth / 1e6).ToString("0") + " million km.  delay = distance / speed of light (299,792 km/s). Direct line to Earth: " + Nasa.Dur(Nasa.LightDelay(direct)) + ".";
            }
            else
            {
                h.link1.color = Theme.Red; h.link2.color = Theme.Red; h.relayNode.color = Theme.Red;
                h.delay.text = "NO ROUTE TO EARTH  -  RELAY LOST"; h.delay.color = Theme.Red;
                h.info.text = "A single relay was the only path. With it gone, even a direct line to Earth would take " + Nasa.Dur(Nasa.LightDelay(direct)) + " one way - and nothing is listening.";
            }
        }

        GameObject BuildRelay(Transform parent)
        {
            var r = new GameObject("Relay"); r.transform.SetParent(parent, false);
            var hull = Mats.Lit(new Color(0.75f, 0.77f, 0.8f), 0.8f); var panel = Mats.Lit(new Color(0.07f, 0.12f, 0.28f), 0.85f, new Color(0.02f, 0.05f, 0.12f)); var gold = Mats.Lit(new Color(0.9f, 0.7f, 0.25f), 0.8f);
            World.Prim(PrimitiveType.Cube, r.transform, Vector3.zero, Vector3.zero, new Vector3(2.4f, 2.4f, 3.4f), gold);
            World.Prim(PrimitiveType.Cylinder, r.transform, new Vector3(0, 0, 0), new Vector3(0, 0, 90), new Vector3(0.25f, 7f, 0.25f), hull);
            World.Prim(PrimitiveType.Cube, r.transform, new Vector3(-7, 0, 0), Vector3.zero, new Vector3(6f, 0.1f, 3f), panel);
            World.Prim(PrimitiveType.Cube, r.transform, new Vector3(7, 0, 0), Vector3.zero, new Vector3(6f, 0.1f, 3f), panel);
            World.Prim(PrimitiveType.Sphere, r.transform, new Vector3(0, 2.2f, 0), new Vector3(-90, 0, 0), new Vector3(3.6f, 0.25f, 3.6f), hull);
            World.Prim(PrimitiveType.Cylinder, r.transform, new Vector3(0, 1.4f, 0), Vector3.zero, new Vector3(0.12f, 0.7f, 0.12f), hull);
            var beacon = World.Prim(PrimitiveType.Sphere, r.transform, new Vector3(0, -1.5f, 0), Vector3.zero, Vector3.one * 0.4f, Mats.Lit(Color.green, 0.3f, new Color(0.2f, 5f, 0.5f)));
            beacon.name = "Beacon";
            var ring = World.Prim(PrimitiveType.Quad, r.transform, new Vector3(0, -1.5f, 0), Vector3.zero, Vector3.one * 5f, Mats.Additive(new Color(0.3f, 2f, 0.6f, 0.5f))); ring.AddComponent<Billboard>();
            r.AddComponent<Spin>().dps = 3f;
            return r;
        }

        LineRenderer MakeGate(Transform parent, Vector3 pos, float radius, Color col)
        {
            var go = new GameObject("Gate"); go.transform.SetParent(parent, false); go.transform.position = pos;
            var lr = go.AddComponent<LineRenderer>(); lr.useWorldSpace = false; lr.loop = true; lr.positionCount = 48; lr.widthMultiplier = 0.35f;
            for (int i = 0; i < 48; i++) { float a = i * Mathf.PI * 2f / 48f; lr.SetPosition(i, new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, 0)); }
            var m = Mats.Additive(col); lr.material = m; return lr;
        }

        IEnumerator Mission3()
        {
            prog.m3Attempts = 0;
            NewSpaceStage();
            var sunL = World.MakeSun(stageRoot, new Vector3(20, 40, 0), 2.6f);
            var earth = World.Earth(stageRoot); earth.root.transform.position = new Vector3(-30, -16, -90); earth.root.transform.localScale = Vector3.one * 1.3f; earth.root.transform.rotation = Quaternion.Euler(0, 40, 0);
            var moon = World.Moon(stageRoot); moon.transform.position = new Vector3(60, 18, -170);
            var sunObj = World.Sun(stageRoot); sunObj.transform.position = new Vector3(-420, 140, -100); sunObj.transform.localScale = Vector3.one * 0.9f;
            sunL.transform.rotation = Quaternion.LookRotation((earth.root.transform.position - sunObj.transform.position).normalized);
            World.Starfield(stageRoot, 700, 160f);
            var ship = World.Ship(1, stageRoot); ship.transform.position = new Vector3(0, 0, 0); ship.transform.localScale = Vector3.one;
            var flame = Fx.Flame(ship.transform.Find("Nozzle"), Vector3.zero, 0.55f, new Color(0.4f, 0.8f, 1f));
            var relay = BuildRelay(stageRoot); relay.transform.position = new Vector3(8, 4, 360);
            var gates = new List<LineRenderer>();
            for (int i = 0; i < 6; i++) gates.Add(MakeGate(stageRoot, new Vector3(Mathf.Sin(i * 1.3f) * 14f, Mathf.Cos(i * 0.9f) * 9f + 2f, 55f + i * 52f), 6.5f, new Color(0.3f, 1.5f, 2f, 0.9f)));
            var pip = PipOrb.Create(stageRoot);
            var hud = ui.Stretch(ui.stage, "M3Hud");
            var sig = BuildSigHud(hud, A.TL, 40, -40); SigRefresh(sig, true);
            var objTx = ui.Label(hud, "", 24, Theme.Cyan, A.T, 80, -34, 900, 40, TextAnchor.UpperCenter, FontStyle.Bold);
            var hullBar = ui.Img(hud, "HullBG", A.TR, -40, -40, 300, 16, new Color(1, 1, 1, 0.12f), ui.round); var hullFill = ui.Img(hud, "Hull", A.TR, -40, -40, 300, 16, Theme.Green, ui.round);
            hullFill.type = Image.Type.Filled; hullFill.sprite = ui.whiteSpr; hullFill.fillMethod = Image.FillMethod.Horizontal; hullFill.fillOrigin = 1;
            ui.Label(hud, "HULL", 14, Theme.Dim, A.TR, -40, -60, 300, 20, TextAnchor.UpperRight);
            var supBar = ui.Img(hud, "SupBG", A.TR, -40, -96, 300, 16, new Color(1, 1, 1, 0.12f), ui.round); var supFill = ui.Img(hud, "Sup", A.TR, -40, -96, 300, 16, Theme.Cyan, ui.round);
            supFill.type = Image.Type.Filled; supFill.sprite = ui.whiteSpr; supFill.fillMethod = Image.FillMethod.Horizontal; supFill.fillOrigin = 1;
            ui.Label(hud, "EMERGENCY SUPPLIES", 14, Theme.Dim, A.TR, -40, -116, 300, 20, TextAnchor.UpperRight);
            ui.Label(hud, "WASD / arrows steer   -   SHIFT boost", 15, Theme.Dim, A.BR, -40, 40, 500, 24, TextAnchor.LowerRight);
            hullFill.fillAmount = 1; supFill.fillAmount = 1;
            cam.transform.position = ship.transform.position + new Vector3(0, 2, -6); cam.transform.rotation = Quaternion.LookRotation(Vector3.forward);
            yield return ui.Fade(0f, 1.2f);
            yield return Say("PIP", "We're clear of the pad and on heading zero-four-seven. Look at the Signal Path in the corner - ship, relay, Earth. That chain is how we stay in touch.");
            yield return Say("PIP", "The light delay is real: distance divided by the speed of light, using JPL Horizons positions for today. Every message waits that long, each way.");
            yield return Say("FATHER", "Take us through the gates to the relay. I want to open a channel to Earth.");

            // ---------------- flight
            float speed = 24f; Quaternion rot = ship.transform.rotation; int gi = 0; float hull = 1f;
            Vector3 relayPos = relay.transform.position;
            objTx.text = "FLY THROUGH THE GATES  -  0 / " + gates.Count;
            while (gi < gates.Count)
            {
                float yaw = Input.GetAxis("Horizontal"), pitch = -Input.GetAxis("Vertical");
                bool boost = Input.GetKey(KeyCode.LeftShift);
                speed = Mathf.Lerp(speed, boost ? 42f : 24f, Time.deltaTime * 2f);
                ship.transform.Rotate(pitch * 55f * Time.deltaTime, yaw * 60f * Time.deltaTime, 0, Space.Self);
                // keep level: damp roll
                var e = ship.transform.eulerAngles; ship.transform.rotation = Quaternion.Euler(e.x, e.y, Mathf.LerpAngle(e.z, -yaw * 25f, Time.deltaTime * 4f));
                ship.transform.position += ship.transform.forward * speed * Time.deltaTime;
                var gp = gates[gi].transform.position;
                float dz = Vector3.Dot(ship.transform.position - gp, Vector3.forward);
                if (dz > 0f)
                {
                    float miss = Vector2.Distance(new Vector2(ship.transform.position.x, ship.transform.position.y), new Vector2(gp.x, gp.y));
                    if (miss < 8.5f) { gates[gi].material.SetColor("_Tint", new Color(0.3f, 3f, 0.8f, 0.9f)); sfx.Ok(); } else { gates[gi].material.SetColor("_Tint", new Color(2.5f, 0.6f, 0.3f, 0.5f)); sfx.Beep(); }
                    gi++; objTx.text = "FLY THROUGH THE GATES  -  " + gi + " / " + gates.Count;
                }
                // steer assist to keep the tunnel the right way
                var camTarget = ship.transform.position - ship.transform.forward * 4.2f + ship.transform.up * 1.4f;
                cam.transform.position = Vector3.Lerp(cam.transform.position, camTarget, Time.deltaTime * 5f);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(ship.transform.position + ship.transform.forward * 8f - cam.transform.position, Vector3.up), Time.deltaTime * 5f);
                sfx.Rumble(0.12f);
                yield return null;
            }
            // autopilot approach to relay
            objTx.text = "APPROACHING RELAY";
            Vector3 hover = relayPos + new Vector3(-6, 3, -18);
            Vector3 p0 = ship.transform.position; Quaternion r0 = ship.transform.rotation; Quaternion r1 = Quaternion.LookRotation((relayPos - hover).normalized);
            Vector3 c0 = cam.transform.position; Quaternion cr0 = cam.transform.rotation;
            Vector3 camHold = relayPos + new Vector3(14, 6, -30);
            yield return Over(7f, k =>
            {
                ship.transform.position = Vector3.Lerp(p0, hover, k); ship.transform.rotation = Quaternion.Slerp(r0, r1, k);
                cam.transform.position = Vector3.Lerp(c0, camHold, k);
                cam.transform.rotation = Quaternion.Slerp(cr0, Quaternion.LookRotation((Vector3.Lerp(ship.transform.position, relayPos, 0.5f)) - cam.transform.position), k);
            });
            sfx.StopRumble();
            var fe = flame.emission; fe.rate = 30f;
            yield return Say("FATHER", "There it is. The relay. Pip, open the channel to Earth.");
            objTx.text = "PRESS SPACE TO OPEN CHANNEL TO EARTH";
            float waitT = 0; while (!Input.GetKeyDown(KeyCode.Space) && !(devAuto && (waitT += Time.deltaTime) > 2f)) yield return null;
            objTx.text = "OPENING CHANNEL...";
            sfx.Beep(); yield return new WaitForSeconds(1.2f); sfx.Beep(); yield return new WaitForSeconds(0.8f);
            // relay strike
            var beacon = relay.transform.Find("Beacon");
            var bolt = new GameObject("Strike"); var lr = bolt.AddComponent<LineRenderer>(); lr.widthMultiplier = 0.4f; lr.material = Mats.Additive(new Color(3f, 1.2f, 0.4f, 1f)); lr.positionCount = 2;
            Vector3 strikeFrom = relayPos + new Vector3(80, 40, 160);
            for (int i = 0; i < 6; i++) { lr.SetPosition(0, strikeFrom); lr.SetPosition(1, Vector3.Lerp(strikeFrom, relayPos, (i + 1) / 6f)); yield return null; }
            Destroy(bolt);
            sfx.Boom(); Fx.Burst(relayPos, new Color(1f, 0.7f, 0.3f), 400, 2.2f, 14f, 2.6f); Fx.Burst(relayPos, new Color(0.6f, 0.8f, 1f), 150, 1.2f, 22f, 2f);
            relay.SetActive(false);
            StartCoroutine(Shake(0.8f, 0.4f));
            SigRefresh(sig, false); sfx.Alarm();
            objTx.text = "<color=#FF5C5C>RELAY DESTROYED</color>"; objTx.supportRichText = true;
            yield return new WaitForSeconds(2f);
            yield return Say("PIP", "The relay... it's gone. Something hit it - it was destroyed when we opened the channel. The path to Earth just broke. Look at the Signal Path: no route.");
            yield return Say("STRANGER", "UNKNOWN CRAFT TO SURVEY SHIP: Stay away from the relay network. Turn back. Do not open that channel again.");
            yield return Say("FATHER", "Whoever you are, you just blew up our only link home. I'm not turning around.");

            // ---------------- the stranger
            var stranger = World.Ship(2, stageRoot); stranger.transform.position = ship.transform.position + new Vector3(30, 10, 120); stranger.transform.localScale = Vector3.one * 1.1f;
            Fx.Flame(stranger.transform.Find("Nozzle"), Vector3.zero, 0.5f, new Color(1f, 0.4f, 0.2f));
            objTx.text = "<color=#FF5C5C>EVADE THE UNKNOWN CRAFT</color>  -  steer with WASD";
            sfx.Alarm();
            float t = 0, evasion = 0, sSpeed = 22f; bool hit = false; speed = 14f; Vector3 vel = ship.transform.forward * 3f;
            Vector3 camTarget2 = Vector3.zero;
            while (!hit)
            {
                t += Time.deltaTime;
                float ax = Input.GetAxis("Horizontal"), ay = Input.GetAxis("Vertical");
                bool boost = Input.GetKey(KeyCode.LeftShift);
                var lateral = (ax * Vector3.right + ay * Vector3.up) * (boost ? 22f : 14f);
                vel = Vector3.Lerp(vel, lateral + Vector3.forward * 4f, Time.deltaTime * 2.5f);
                ship.transform.position += vel * Time.deltaTime;
                ship.transform.rotation = Quaternion.Slerp(ship.transform.rotation, Quaternion.LookRotation(Vector3.forward + vel * 0.05f) * Quaternion.Euler(0, 0, -vel.x * 1.5f), Time.deltaTime * 4f);
                sSpeed = Mathf.Min(sSpeed + Time.deltaTime * 2.2f, 44f);
                Vector3 toP = ship.transform.position - stranger.transform.position; float dist = toP.magnitude;
                float turn = Mathf.Min(1f, (0.9f + t * 0.12f) * Time.deltaTime);
                stranger.transform.rotation = Quaternion.Slerp(stranger.transform.rotation, Quaternion.LookRotation(toP.normalized), turn);
                stranger.transform.position += stranger.transform.forward * sSpeed * Time.deltaTime;
                if (dist > 6f) evasion += Time.deltaTime;
                if (dist < 1.3f || t > 16f) hit = true;
                camTarget2 = ship.transform.position + new Vector3(0, 2.5f, -9f);
                cam.transform.position = Vector3.Lerp(cam.transform.position, camTarget2, Time.deltaTime * 4f);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation((Vector3.Lerp(ship.transform.position, stranger.transform.position, 0.35f)) - cam.transform.position), Time.deltaTime * 3f);
                yield return null;
            }
            // collision
            stranger.transform.position = ship.transform.position + new Vector3(0, 0, 0.9f);
            sfx.Boom(); Fx.Burst(ship.transform.position, new Color(1f, 0.6f, 0.2f), 300, 1.2f, 10f, 2f); StartCoroutine(Shake(1.5f, 0.6f));
            float damage = Mathf.Clamp(0.92f - evasion * 0.035f, 0.35f, 0.9f); prog.damage = damage; hull = 1f - damage;
            hullFill.fillAmount = hull; hullFill.color = Theme.Heat(hull);
            objTx.text = "<color=#FF5C5C>COLLISION  -  HULL " + Mathf.RoundToInt(hull * 100) + "%</color>";
            yield return new WaitForSeconds(1.5f);
            yield return ui.Fade(1f, 0.5f);
            // docking
            stranger.transform.position = ship.transform.position + ship.transform.forward * 1.4f; stranger.transform.rotation = Quaternion.LookRotation(-ship.transform.forward);
            cam.transform.position = ship.transform.position + new Vector3(4, 2, -5); cam.transform.rotation = Quaternion.LookRotation(ship.transform.position + ship.transform.forward * 0.6f - cam.transform.position);
            ship.transform.rotation = Quaternion.Euler(0, 0, 8f);
            yield return ui.Fade(0f, 0.6f);
            yield return Say("PIP", "Main systems... disabled. Someone just docked with us. They're coming aboard.", 3.5f);
            objTx.text = "<color=#FFB547>UNKNOWN CRAFT DOCKED  -  EMERGENCY SUPPLIES BEING REMOVED</color>";
            float sup0 = 1f;
            yield return Over(4f, k => { supFill.fillAmount = Mathf.Lerp(1f, 0.5f, k); supFill.color = Theme.Heat(supFill.fillAmount); sfx.Rumble(0.1f * (1 - k)); });
            sfx.StopRumble();
            yield return Say("FATHER", "He took half our emergency supplies. Half! And he's not saying a word.", 4f);
            stranger.transform.position += Vector3.zero;
            yield return Over(3.5f, k => { stranger.transform.position = ship.transform.position + ship.transform.forward * (1.4f + k * 80f); });
            Destroy(stranger);
            sfx.Whoosh();
            yield return new WaitForSeconds(1.0f);
            yield return Say("PIP", "He's gone. The ship is crippled - hull at " + Mathf.RoundToInt(hull * 100) + "%, scanner offline, control degraded. We need to triage the power before life support fails.");
            Destroy(hud.gameObject);
            hud = ui.Stretch(ui.stage, "Triage");

            // ---------------- triage minigame
            bool stable = false;
            while (!stable)
            {
                prog.triageTries++;
                var tg = ui.Stretch(ui.stage, "TriageUI");
                var tp = ui.Panel(tg, A.TL, 40, -40, 1130, 1000);
                ui.Label(tp, "TRIAGE  -  LIMITED POWER", 38, Theme.Amber, A.TL, 32, -22, 900, 56, TextAnchor.UpperLeft, FontStyle.Bold);
                ui.Label(tp, "The collision cut reactor output. You cannot power everything at once. Restore the scanner and the controls one at a time - a restored system stays restored - but watch life support and thermal: starving them drains them.", 19, Theme.Dim, A.TL, 32, -80, 1070, 56);
                float budgetP = 100f - damage * 36f; float used = 0f;
                string[] sn = { "LIFE SUPPORT", "THERMAL", "SCANNER", "CONTROL / ENGINES" };
                float[] need = { 25, 15, 35, 20 }; float[] val = new float[4]; float[] integ = { 100, 100, 0, 0 };
                var bars = new Image[4]; var vTx = new Text[4]; var sl = new Slider[4];
                bool updating = false;
                for (int i = 0; i < 4; i++)
                {
                    int k = i; float y = -160 - i * 170;
                    ui.Img(tp, "row", A.TL, 20, y + 10, 1090, 150, new Color(1, 1, 1, 0.04f), ui.round);
                    ui.Label(tp, sn[i], 26, Theme.Text, A.TL, 40, y, 500, 34, TextAnchor.UpperLeft, FontStyle.Bold);
                    ui.Label(tp, i < 2 ? "needs " + need[i] + " units to stay stable" : "needs " + need[i] + " units to restore", 17, Theme.Dim, A.TL, 40, y - 34, 500, 24);
                    vTx[i] = ui.Label(tp, "0", 28, Theme.Cyan, A.TL, 900, y, 160, 36, TextAnchor.UpperRight, FontStyle.Bold);
                    sl[i] = ui.Sld(tp, A.TL, 40, y - 62, 600, 36, 0, 50, 0, true, v =>
                    {
                        if (updating) return; val[k] = v;
                        float tot = 0; for (int j = 0; j < 4; j++) tot += val[j];
                        if (tot > budgetP) { val[k] = Mathf.Max(0, v - (tot - budgetP)); updating = true; sl[k].value = val[k]; updating = false; }
                        vTx[k].text = val[k].ToString("0");
                    });
                    ui.Img(tp, "bbg", A.TL, 40, y - 110, 1020, 16, new Color(1, 1, 1, 0.1f), ui.round);
                    bars[i] = ui.Img(tp, "b", A.TL, 40, y - 110, 1020, 16, i < 2 ? Theme.Green : Theme.Cyan, ui.round); bars[i].type = Image.Type.Filled; bars[i].sprite = ui.whiteSpr; bars[i].fillMethod = Image.FillMethod.Horizontal;
                }
                var budTx = ui.Label(tp, "", 24, Theme.Amber, A.BL, 32, 30, 1000, 36, TextAnchor.LowerLeft, FontStyle.Bold);
                bool failedT = false, wonT = false; float tt = 0;
                ui.Toast("Budget: " + budgetP.ToString("0") + " power units. You cannot run everything at once.", Theme.Amber, 4f);
                while (!failedT && !wonT)
                {
                    tt += Time.deltaTime;
                    if (devAuto)
                    {
                        float[] pv = tt < 9f ? new float[] { 25, 0, 35, 0 } : new float[] { 25, 15, 0, 20 };
                        updating = true; for (int j = 0; j < 4; j++) { val[j] = pv[j]; sl[j].SetValueWithoutNotify(pv[j]); vTx[j].text = pv[j].ToString("0"); } updating = false;
                    }
                    float tot = 0; for (int j = 0; j < 4; j++) tot += val[j];
                    budTx.text = "POWER  " + tot.ToString("0") + " / " + budgetP.ToString("0") + " units";
                    // life support & thermal decay when underpowered
                    for (int j = 0; j < 2; j++) { integ[j] += (val[j] >= need[j] ? 12f : -6.5f * (1f - val[j] / need[j])) * Time.deltaTime; integ[j] = Mathf.Clamp(integ[j], 0, 100); }
                    for (int j = 2; j < 4; j++) { if (integ[j] >= 100f) continue; if (val[j] >= need[j]) integ[j] = Mathf.Min(100, integ[j] + 12f * Time.deltaTime); else integ[j] = Mathf.Max(0, integ[j] - 2f * Time.deltaTime); }
                    for (int j = 0; j < 4; j++) { bars[j].fillAmount = integ[j] / 100f; bars[j].color = j < 2 ? Theme.Heat(integ[j] / 100f) : Theme.Cyan; }
                    if (integ[0] <= 0f || integ[1] <= 0f) failedT = true;
                    if (integ[2] >= 100f && integ[3] >= 100f && integ[0] > 25f && integ[1] > 25f) wonT = true;
                    yield return null;
                }
                Destroy(tg.gameObject);
                if (failedT)
                {
                    sfx.Bad(); sfx.Alarm();
                    yield return Say("PIP", "Loss of ship control! Life support and thermal can't be starved - they drop fast. Restarting from the impact. Try feeding the scanner and the engines one at a time.");
                }
                else { stable = true; sfx.Ok(); }
            }
            yield return Say("PIP", "Scanner online, control restored! ... Oh. There's a beacon on the scanner, still transmitting from a station nearby. And a planet - the spectrum shows signs of liquid water.");
            yield return Say("FATHER", "He took half our supplies. We can't go home now. Pip - re-run the plan.");

            // ---------------- re-plan with half the supplies
            var basePlan = (prog.fatherPlan != null ? prog.fatherPlan.Clone() : new Plan { tested = true });
            basePlan.supplyDays = Mathf.Max(20f, Mathf.Round(basePlan.supplyDays * 0.5f));
            var cfg = new PlannerCfg { title = "RE-PLAN UNDER PRESSURE", sub = "Half the supplies are gone: " + basePlan.supplyDays.ToString("0") + " days remain. The gauge shows only what you can still reach.", fixedSupplies = true, runLabel = "COMMIT COURSE", supplyMax = 300f };
            cfg.dests.Add(new Dest("Return to Earth", 9.0f, 420f, "far beyond the remaining supplies"));
            cfg.dests.Add(new Dest("Relay station (Mars orbit)", 7.0f, 190f, "the lost relay's sister station"));
            cfg.dests.Add(new Dest("Outer outpost", 8.5f, 240f, "long haul through the dark"));
            cfg.dests.Add(new Dest("Pip's water planet", 3.6f, 55f, "scanner: liquid water signature, near the beacon"));
            basePlan.dest = cfg.dests[0];
            bool toldPip = false;
            cfg.onChange = () => { if (!toldPip && cfg.say != null) { toldPip = true; cfg.say("Select each route and read the gauge. Only one destination is still reachable on these supplies."); } };
            bool ok = false;
            while (!ok)
            {
                yield return RunPlanner(cfg, basePlan);
                if (basePlan.dest.name == "Pip's water planet") ok = true;
                else { ui.Toast("That route is unreachable on the supplies left.", Theme.Red); }
            }
            prog.fatherPlan = basePlan.Clone();
            yield return Say("PIP", "Committed. Course to the planet, by the beacon. It's the only way anyone gets home from here.");
            yield return ui.Card("DEBRIEF CARD  -  MISSION 3", "Single points of failure in deep-space communication",
                "Your ship talked to Earth through one relay. When it failed, there was no route at all - and even a direct line would take " + Nasa.Dur(Nasa.LightDelay(Nasa.Get("Jupiter").rangeKm)) + " one way from Jupiter's distance. Light delay is simply distance divided by the speed of light.",
                "NASA does not rely on one link. The Deep Space Network spreads antennas across California, Spain and Australia so a spacecraft is always in view of one, and relay orbiters at Mars pass data from rovers to Earth.",
                "JPL Horizons System (ssd.jpl.nasa.gov/horizons), positions for 2026-11-13; NASA Deep Space Network",
                new[] { "Hull after impact: " + Mathf.RoundToInt(hull * 100) + "%.", "Triage attempts: " + prog.triageTries + "." });
            prog.Card("Single points of failure in deep-space communication");
            yield return ui.Fade(1f, 0.6f);
        }

        IEnumerator AutoTriage(Slider[] sl)
        {
            sl[0].value = 25; sl[2].value = 34; yield return new WaitForSeconds(9.5f);
            sl[2].value = 0; sl[0].value = 25; sl[1].value = 15; sl[3].value = 19; yield return new WaitForSeconds(11f);
        }

        public IEnumerator Shake(float dur, float amp)
        {
            float t = 0; Vector3 last = Vector3.zero;
            while (t < dur)
            {
                t += Time.deltaTime; var off = UnityEngine.Random.insideUnitSphere * amp * (1f - t / dur);
                cam.transform.position += off - last; last = off; yield return null;
            }
            cam.transform.position -= last;
        }
    }
}
