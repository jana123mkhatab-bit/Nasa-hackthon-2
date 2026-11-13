using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TLKS
{
    public class Land
    {
        public float size = 1500f, amp = 4f, bowl = 1f; public int n = 300; float ox, oz;
        public Land(int seed, float amp, float bowl) { this.amp = amp; this.bowl = bowl; var r = new System.Random(seed); ox = r.Next(0, 5000); oz = r.Next(0, 5000); }
        float P(float x, float z, float f) { return Mathf.PerlinNoise(x * f + ox, z * f + oz) - 0.5f; }
        public float H(float x, float z)
        {
            float h = P(x, z, 0.0035f) * amp * 14f + P(x, z, 0.014f) * amp * 3.5f + P(x, z, 0.06f) * amp * 0.5f;
            float r = Mathf.Sqrt(x * x + z * z);
            h += bowl * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(260f, 760f, r)) * 150f;
            float dl = Vector2.Distance(new Vector2(x, z), new Vector2(250f, -150f));
            h -= 16f * Mathf.Exp(-dl * dl / (2f * 85f * 85f));
            h = Mathf.Lerp(0f, h, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(36f, 130f, r)));
            // keep the walking route and the camp on dry land
            float dc = Vector2.Distance(new Vector2(x, z), new Vector2(168f, 98f)), ds = Vector2.Distance(new Vector2(x, z), new Vector2(200f, 140f));
            h = Mathf.Lerp(h, 0.8f, 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(30f, 100f, Mathf.Min(dc, ds))));
            float dl2 = DistToSeg(x, z, 8f, 0f, 168f, 98f);
            h = Mathf.Lerp(h, Mathf.Max(h, 0.4f), 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(8f, 45f, dl2)));
            return h;
        }
        static float DistToSeg(float px, float pz, float ax, float az, float bx, float bz)
        {
            float dx = bx - ax, dz = bz - az; float t = Mathf.Clamp01(((px - ax) * dx + (pz - az) * dz) / (dx * dx + dz * dz));
            float cx = ax + dx * t, cz = az + dz * t; return Mathf.Sqrt((px - cx) * (px - cx) + (pz - cz) * (pz - cz));
        }
        public Vector3 Norm(float x, float z) { float e = 1.5f; return new Vector3(H(x - e, z) - H(x + e, z), 2f * e, H(x, z - e) - H(x, z + e)).normalized; }

        public GameObject Build(Transform parent)
        {
            var go = new GameObject("Terrain"); go.transform.SetParent(parent, false);
            int v = n + 1; var verts = new Vector3[v * v]; var cols = new Color[v * v]; var uv = new Vector2[v * v]; var tris = new int[n * n * 6];
            Color grass = new Color(0.20f, 0.36f, 0.20f), grass2 = new Color(0.30f, 0.44f, 0.22f), rock = new Color(0.36f, 0.31f, 0.30f), sand = new Color(0.62f, 0.55f, 0.42f), dark = new Color(0.12f, 0.2f, 0.2f);
            for (int j = 0; j < v; j++)
                for (int i = 0; i < v; i++)
                {
                    float x = (i / (float)n - 0.5f) * size, z = (j / (float)n - 0.5f) * size, h = H(x, z);
                    verts[j * v + i] = new Vector3(x, h, z); uv[j * v + i] = new Vector2(i, j) * 0.1f;
                    float sl = 1f - Norm(x, z).y; float noise = Mathf.PerlinNoise(x * 0.05f + 99f, z * 0.05f + 7f);
                    Color c = Color.Lerp(grass, grass2, noise);
                    c = Color.Lerp(c, rock, Mathf.SmoothStep(0f, 1f, sl * 5f));
                    if (h < -2.5f) c = Color.Lerp(sand, dark, Mathf.InverseLerp(-3f, -8f, h)); else if (h < -0.6f) c = Color.Lerp(c, sand, Mathf.InverseLerp(-0.6f, -2.5f, h));
                    c *= 0.85f + 0.3f * Mathf.PerlinNoise(x * 0.4f + 3f, z * 0.4f + 9f);
                    if (h > 90f) c = Color.Lerp(c, rock, 0.7f);
                    cols[j * v + i] = c;
                }
            int t = 0;
            for (int j = 0; j < n; j++) for (int i = 0; i < n; i++) { int a = j * v + i; tris[t++] = a; tris[t++] = a + v; tris[t++] = a + 1; tris[t++] = a + 1; tris[t++] = a + v; tris[t++] = a + v + 1; }
            var m = new Mesh(); m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; m.vertices = verts; m.colors = cols; m.uv = uv; m.triangles = tris; m.RecalculateNormals(); m.RecalculateBounds();
            go.AddComponent<MeshFilter>().sharedMesh = m;
            var mr = go.AddComponent<MeshRenderer>(); mr.sharedMaterial = Mats.Lit(Color.white, 0.08f, null, Mats.NoiseTex(), true); mr.receiveShadows = true;
            return go;
        }
    }

    public class Walker : MonoBehaviour { }

    public partial class Game
    {
        float landDiff; // 0..1
        // ====================================================== MISSION 4
        IEnumerator Mission4()
        {
            var ex = Nasa.Planet();
            float gPl = Plan.g0 * ex.massE / (ex.radiusE * ex.radiusE);   // surface gravity from Archive mass & radius
            float flux = ex.insol * 1361f;
            // ------------- orbit approach
            NewSpaceStage();
            var star = World.MakeSun(stageRoot, new Vector3(15, -60, 0), 2.6f); star.color = new Color(1f, 0.78f, 0.52f);
            var planet = new GameObject("Planet"); planet.transform.SetParent(stageRoot, false);
            var pm = Mats.Lit(new Color(0.75f, 1f, 0.82f), 0.35f, null, Mats.Tex("earth_daymap")); pm.SetFloat("_Ambient", 0.5f);
            var pSurf = World.Model("Models/earth/earth", planet.transform, pm); pSurf.AddComponent<Spin>().dps = 1.5f;
            var cm = new Material(Mats.S("TLKS_Clouds")); cm.SetTexture("_BaseMap", Mats.Tex("earth_clouds")); cm.SetColor("_Color", Color.white);
            var pCl = World.Model("Models/earth/earth_clouds", planet.transform, cm); pCl.AddComponent<Spin>().dps = 2.2f;
            var am = new Material(Mats.S("TLKS_Atmos")); am.SetColor("_Color", new Color(0.35f, 1.2f, 0.9f)); am.SetFloat("_Power", 2.8f); am.SetFloat("_Gain", 1.6f);
            var pAt = World.Model("Models/earth/earth_atmosphere", planet.transform, am); pAt.transform.localScale = Vector3.one * 1.015f;
            planet.transform.localScale = Vector3.one * ex.radiusE * 0.9f; planet.transform.rotation = Quaternion.Euler(10, 100, 15);
            World.Starfield(stageRoot, 400, 140f);
            var ship = World.Ship(1, stageRoot); ship.transform.position = new Vector3(-14, 3, -26); ship.transform.rotation = Quaternion.Euler(0, 55, 0);
            Fx.Flame(ship.transform.Find("Nozzle"), Vector3.zero, 0.35f, new Color(0.4f, 0.8f, 1f));
            CamAt(new Vector3(-9, 4, -38), new Vector3(0, 0, 0));
            var pip = PipOrb.Create(stageRoot);
            yield return ui.Fade(0f, 1.2f);
            StartCoroutine(Glide(new Vector3(-12, 4, -33), Quaternion.LookRotation(planet.transform.position - new Vector3(-12, 4, -33)), 10f));
            yield return Say("PIP", "Orbit insertion complete. That's the planet, and that's the beacon - still transmitting from the surface. Scanning now.");

            // ------------- scanner readout (real Exoplanet Archive entry)
            var root = ui.Stretch(ui.stage, "Scan");
            var sp = ui.Panel(root, A.TL, 40, -40, 760, 1000);
            ui.Label(sp, "ORBITAL SCANNER", 34, Theme.Cyan, A.TL, 30, -22, 700, 48, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(sp, "Based on NASA Exoplanet Archive entry: <b>" + ex.name + "</b>", 19, Theme.Amber, A.TL, 30, -70, 700, 30);
            string[] ks = { "Planet radius", "Stellar flux (insolation)", "Equilibrium temperature", "Orbital period", "Mass (Archive best estimate)", "Surface gravity (derived)", "Distance from Earth", "Host star temperature" };
            string[] vs = { ex.radiusE.ToString("0.00") + " Earth radii", ex.insol.ToString("0.00") + " x Earth  (" + flux.ToString("0") + " W/m^2)", ex.teq.ToString("0") + " K", ex.period.ToString("0.0") + " days", ex.massE.ToString("0.00") + " Earth masses", gPl.ToString("0.0") + " m/s^2  (g = G M / R^2)", (ex.distPc * 3.26156f).ToString("N0") + " light-years", ex.starTeff.ToString("0") + " K" };
            for (int i = 0; i < ks.Length; i++) { ui.Label(sp, ks[i], 18, Theme.Dim, A.TL, 30, -125 - i * 74, 700, 26); ui.Label(sp, vs[i], 28, Theme.Text, A.TL, 30, -150 - i * 74, 700, 40, TextAnchor.UpperLeft, FontStyle.Bold); }
            ui.Label(sp, "Habitable-zone reading: flux below Earth's, equilibrium temperature near the freezing point of water once an atmosphere warms it. Not proof of life - a few numbers that say \"worth a closer look\".", 17, Theme.Dim, A.BL, 30, 100, 700, 110, TextAnchor.LowerLeft);
            var w0 = new Wait(); ui.Btn(sp, "SELECT LANDING SITE", A.BL, 30, 30, 700, 56, () => w0.Done(), Theme.Green, 24);
            if (devAuto) { yield return new WaitForSeconds(3f); w0.Done(); }
            yield return w0; Destroy(root.gameObject);

            // ------------- landing-site selection
            var sites = new[] {
                new { name = "A  -  Basalt Plain", water = 0.22f, slope = 4f, solar = 0.99f, beacon = 140f, amp = 1.6f, bowl = 0.0f, wind = 4f, pad = 44f },
                new { name = "B  -  Echo Valley", water = 0.81f, slope = 7f, solar = 0.71f, beacon = 12f, amp = 2.4f, bowl = 1.0f, wind = 2f, pad = 30f },
                new { name = "C  -  Ridge Crest", water = 0.09f, slope = 19f, solar = 0.98f, beacon = 60f, amp = 5.5f, bowl = 0.0f, wind = 8f, pad = 20f } };
            int chosen = -1; var w1 = new Wait();
            var rp = ui.Stretch(ui.stage, "Sites");
            var hdr = ui.Panel(rp, A.T, 0, -30, 1700, 120);
            ui.Label(hdr, "LANDING-SITE SELECTION", 36, Theme.Cyan, A.TL, 30, -14, 900, 50, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(hdr, "<color=#66FFD9>PIP:</color> \"I... I recommend Echo Valley. But please read the numbers - I'd like you to see why, not just trust me.\"", 20, Theme.Text, A.TL, 30, -66, 1640, 44);
            Button landBtn = null;
            for (int i = 0; i < 3; i++)
            {
                int k = i; var s = sites[i]; float x = 40 + i * 600;
                var card = ui.Panel(rp, A.TL, x, -180, 570, 700, Theme.Bg2);
                ui.Label(card, s.name, 28, Theme.Text, A.TL, 28, -22, 520, 40, TextAnchor.UpperLeft, FontStyle.Bold);
                string[] ln = { "WATER SIGNAL", "SLOPE", "SOLAR EXPOSURE", "DISTANCE TO NAV BEACON" };
                string[] lv = { Mathf.RoundToInt(s.water * 100) + "%", s.slope + "°", Mathf.RoundToInt(s.solar * 100) + "%  (" + Mathf.RoundToInt(s.solar * flux) + " W/m^2)", s.beacon + " km" };
                float[] bv = { s.water, 1f - s.slope / 25f, s.solar, 1f - s.beacon / 160f };
                for (int j = 0; j < 4; j++)
                {
                    float y = -90 - j * 112;
                    ui.Label(card, ln[j], 16, Theme.Dim, A.TL, 28, y, 520, 24, TextAnchor.UpperLeft, FontStyle.Bold);
                    ui.Label(card, lv[j], 30, Theme.Text, A.TL, 28, y - 24, 520, 40, TextAnchor.UpperLeft, FontStyle.Bold);
                    ui.Img(card, "bg", A.TL, 28, y - 72, 514, 12, new Color(1, 1, 1, 0.1f), ui.round);
                    var bar = ui.Img(card, "bar", A.TL, 28, y - 72, 514, 12, Theme.Heat(bv[j]), ui.round); bar.type = Image.Type.Filled; bar.sprite = ui.whiteSpr; bar.fillMethod = Image.FillMethod.Horizontal; bar.fillAmount = Mathf.Clamp01(bv[j]);
                }
                ui.Btn(card, "CHOOSE THIS SITE", A.B, 0, 24, 500, 56, () => { chosen = k; w1.Done(); }, Theme.Cyan, 20);
            }
            ui.Label(rp, "Longer bar = better. Water is life; slope decides how hard the landing is; sunlight powers the camp; the beacon is how you find help.", 18, Theme.Dim, A.BL, 40, 120, 1700, 50);
            if (devAuto) { yield return new WaitForSeconds(3f); chosen = 1; w1.Done(); }
            yield return w1; Destroy(rp.gameObject);
            var site = sites[chosen]; prog.site = chosen;
            landDiff = 1f - (site.pad - 18f) / 30f;
            if (chosen == 1) yield return Say("PIP", "Echo Valley: eighty-one percent water signal, ten times closer to the beacon, gentle slope. The walls give less sunlight - seventy-one percent - but that is plenty. Good choice.");
            else if (chosen == 0) yield return Say("PIP", "The plain is flat and bright, but the water signal is only twenty-two percent and the beacon is far. We can land - but it will be a long walk.");
            else yield return Say("PIP", "A nineteen-degree ridge with a nine percent water signal... that is going to be a hard landing, and a harder camp. I'll help however I can.");

            // ------------- landing minigame
            bool landed = false; var land = new Land(1234 + chosen, site.amp, site.bowl);
            prog.landingTries = 0;
            while (!landed)
            {
                prog.landingTries++;
                NewStage(new Color(0.42f, 0.55f, 0.62f), new Color(0.32f, 0.38f, 0.36f), new Color(0.16f, 0.14f, 0.11f), true, new Color(0.5f, 0.64f, 0.7f), 0.0006f);
                cam.backgroundColor = new Color(0.5f, 0.64f, 0.7f);
                var sl = World.MakeSun(stageRoot, new Vector3(34, -50, 0), 1.7f, true); sl.color = new Color(1f, 0.85f, 0.62f);
                BuildSurface(land, site.name, sl);
                var lander = World.Ship(1, stageRoot); lander.transform.localScale = Vector3.one * 5f;
                var flame = Fx.Flame(lander.transform.Find("Nozzle"), Vector3.zero, 1.2f, new Color(0.5f, 0.85f, 1f)); var fe = flame.emission; fe.rate = 0;
                var padRing = MakeGate(stageRoot, new Vector3(0, 0.6f, 0), site.pad, new Color(0.3f, 3f, 1f, 0.9f)); padRing.transform.rotation = Quaternion.Euler(90, 0, 0);
                var beam = new GameObject("Beam"); beam.transform.SetParent(stageRoot, false); var bl = beam.AddComponent<LineRenderer>(); bl.positionCount = 2; bl.SetPosition(0, new Vector3(0, 0, 0)); bl.SetPosition(1, new Vector3(0, 400, 0)); bl.widthMultiplier = 3f; bl.material = Mats.Additive(new Color(0.3f, 2f, 1f, 0.15f)); bl.useWorldSpace = true;
                Vector3 pos = new Vector3(UnityEngine.Random.Range(-70f, 70f), 480f, UnityEngine.Random.Range(-120f, -50f)); Vector3 vel = new Vector3(0, -22f, 0);
                float fuel = 100f; Vector3 wind = new Vector3(UnityEngine.Random.Range(-1f, 1f), 0, UnityEngine.Random.Range(-1f, 1f)).normalized * site.wind;
                var hud = ui.Stretch(ui.stage, "LandHUD");
                var hp = ui.Panel(hud, A.BL, 40, 40, 520, 250);
                var tAlt = ui.Label(hp, "", 26, Theme.Text, A.TL, 24, -20, 480, 36, TextAnchor.UpperLeft, FontStyle.Bold);
                var tVy = ui.Label(hp, "", 26, Theme.Text, A.TL, 24, -60, 480, 36, TextAnchor.UpperLeft, FontStyle.Bold);
                var tHs = ui.Label(hp, "", 26, Theme.Text, A.TL, 24, -100, 480, 36, TextAnchor.UpperLeft, FontStyle.Bold);
                var tFuel = ui.Label(hp, "", 22, Theme.Cyan, A.TL, 24, -144, 480, 30, TextAnchor.UpperLeft, FontStyle.Bold);
                ui.Img(hp, "fbg", A.TL, 24, -182, 470, 14, new Color(1, 1, 1, 0.12f), ui.round); var fBar = ui.Img(hp, "fb", A.TL, 24, -182, 470, 14, Theme.Cyan, ui.round); fBar.type = Image.Type.Filled; fBar.sprite = ui.whiteSpr; fBar.fillMethod = Image.FillMethod.Horizontal;
                ui.Label(hp, "gravity " + gPl.ToString("0.0") + " m/s^2   wind " + site.wind + " m/s", 16, Theme.Dim, A.TL, 24, -210, 480, 24);
                var obj = ui.Label(hud, "LAND ON THE GREEN PAD  -  touch down under 6 m/s vertical and 3.5 m/s sideways", 24, Theme.Cyan, A.T, 0, -40, 1500, 40, TextAnchor.UpperCenter, FontStyle.Bold);
                ui.Label(hud, "SPACE main engine   -   WASD thrusters (sideways)   -   attempt " + prog.landingTries, 16, Theme.Dim, A.BR, -40, 40, 700, 24, TextAnchor.LowerRight);
                CamAt(pos + new Vector3(0, 10, -34), pos);
                yield return ui.Fade(0f, 0.8f);
                bool done = false, crashed = false; string why = ""; float thrust = 0;
                while (!done)
                {
                    float dt = Time.deltaTime;
                    float alt0 = pos.y - land.H(pos.x, pos.z) - 2.0f;
                    bool sp2 = Input.GetKey(KeyCode.Space) && fuel > 0f;
                    float ax = fuel > 0 ? Input.GetAxis("Horizontal") : 0f, az = fuel > 0 ? Input.GetAxis("Vertical") : 0f;
                    if (devAuto)
                    {
                        float tv = Mathf.Min(Mathf.Sqrt(2f * 9f * Mathf.Max(alt0, 0f)), 40f) + 1.5f; sp2 = -vel.y > tv;
                        ax = Mathf.Clamp((Mathf.Clamp(-pos.x * 0.15f, -10f, 10f) - vel.x) * 0.6f, -1f, 1f); az = Mathf.Clamp((Mathf.Clamp(-pos.z * 0.15f, -10f, 10f) - vel.z) * 0.6f, -1f, 1f);
                    }
                    thrust = Mathf.MoveTowards(thrust, sp2 ? 1f : 0f, dt * 5f);
                    if (alt0 > 40f && -vel.y > Mathf.Sqrt(2f * 14f * alt0) * 0.9f) obj.text = "<color=#FF5C5C>BURN  -  HOLD SPACE TO BRAKE</color>"; else if (alt0 < 300f) obj.text = "LAND ON THE GREEN PAD  -  touch down under 6 m/s vertical and 3.5 m/s sideways";
                    vel.y += (thrust * 30f - gPl) * dt;
                    vel.x += (ax * 8f + wind.x * 0.18f) * dt; vel.z += (az * 8f + wind.z * 0.18f) * dt;
                    fuel -= (thrust * 3.6f + (Mathf.Abs(ax) + Mathf.Abs(az)) * 0.7f) * dt; fuel = Mathf.Max(fuel, 0f);
                    pos += vel * dt;
                    float gh = land.H(pos.x, pos.z); float alt = pos.y - gh - 2.0f;
                    lander.transform.position = pos; lander.transform.rotation = Quaternion.Euler(vel.z * 0.7f, 0, -vel.x * 0.7f) * Quaternion.Euler(-90, 0, 0);
                    fe.rate = thrust * 180f; sfx.Rumble(0.1f + thrust * 0.6f);
                    tAlt.text = "ALTITUDE  " + Mathf.Max(0, alt).ToString("0") + " m";
                    tVy.text = "VERTICAL  " + (-vel.y).ToString("0.0") + " m/s"; tVy.color = (-vel.y) > 6f && alt < 120f ? Theme.Red : Theme.Text;
                    float hs = new Vector2(vel.x, vel.z).magnitude; tHs.text = "SIDEWAYS  " + hs.ToString("0.0") + " m/s"; tHs.color = hs > 3.5f && alt < 120f ? Theme.Amber : Theme.Text;
                    tFuel.text = "FUEL  " + fuel.ToString("0") + "%"; fBar.fillAmount = fuel / 100f; fBar.color = Theme.Heat(fuel / 60f);
                    float k2 = Mathf.Clamp01(alt / 120f);
                    var ct = pos + new Vector3(0, Mathf.Lerp(7f, 16f, k2), Mathf.Lerp(-20f, -38f, k2));
                    cam.transform.position = Vector3.Lerp(cam.transform.position, ct, dt * 4f);
                    cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(pos + new Vector3(0, -Mathf.Lerp(4f, 18f, k2), 0) - cam.transform.position), dt * 3f);
                    if (alt < 0f)
                    {
                        float dxp = new Vector2(pos.x, pos.z).magnitude;
                        pos.y = gh + 2.0f; done = true;
                        if (-vel.y > 6f) { crashed = true; why = "Touchdown at " + (-vel.y).ToString("0.0") + " m/s vertical - the legs collapsed. Use short bursts and brake before the ground."; }
                        else if (hs > 3.5f) { crashed = true; why = "The lander slid at " + hs.ToString("0.0") + " m/s sideways and tipped. Cancel your drift with the thrusters before touching down."; }
                        else if (dxp > site.pad) { crashed = true; why = "You touched down " + dxp.ToString("0") + " m from the pad on unsafe ground. Aim for the green ring."; }
                    }
                    yield return null;
                }
                sfx.StopRumble(); fe.rate = 0;
                if (crashed)
                {
                    sfx.Boom(); Fx.Burst(pos, new Color(1f, 0.6f, 0.2f), 260, 3f, 12f, 2f); lander.SetActive(false); StartCoroutine(Shake(1f, 0.8f));
                    yield return new WaitForSeconds(1.4f);
                    yield return ui.Fade(1f, 0.5f);
                    yield return Say("PIP", why + " Resetting to the approach checkpoint - the lander is back at altitude with full fuel.");
                }
                else
                {
                    landed = true; sfx.Ok(); Fx.Burst(pos - new Vector3(0, 1.8f, 0), new Color(0.7f, 0.65f, 0.55f), 120, 5f, 6f, 3f);
                    obj.text = "<color=#5CFF9D>TOUCHDOWN  -  SAFE</color>"; padPosForWalk = new Vector3(0, 0, 0);
                    landerFinal = lander;
                    yield return new WaitForSeconds(2f);
                    // quick stash of the surface for walking: re-use stage
                    Destroy(hud.gameObject);
                    surfaceLand = land; surfaceSite = site.name;
                }
            }
            yield return Say("PIP", "We're down, and in one piece. The beacon is still sending. There's a cable running across the ground, away from the pad - toward the water signal.");

            // ------------- walk: follow the cable
            yield return WalkAndReveal(land);

            // ------------- transmit the survey packet
            yield return TransmitPacket(ex, site.water, site.name);
            yield return ui.Card("DEBRIEF CARD  -  MISSION 4", "How scientists judge habitability from a few numbers",
                "A planet's radius and mass say whether it is rocky; the flux it receives from its star and its equilibrium temperature say whether liquid water is plausible. Kepler-442 b receives " + ex.insol.ToString("0.00") + " times Earth's sunlight and has an equilibrium temperature of " + ex.teq.ToString("0") + " K - near the range where an atmosphere could keep water liquid.",
                "NASA's Exoplanet Archive lists these properties for thousands of confirmed planets. \"Habitable zone\" means the right distance for liquid water, not that life exists. Landing-site choice on real missions weighs science value, slope, sunlight and safety - exactly the trade you made.",
                "NASA Exoplanet Archive, planetary systems composite table - " + ex.name + " (exoplanetarchive.ipac.caltech.edu). Light-travel time of 1,000+ years and the relay network are fiction.",
                new[] { "Site chosen: " + site.name + ".", "Landing attempts: " + prog.landingTries + ".", "Derived gravity: " + gPl.ToString("0.0") + " m/s^2." });
            prog.Card("How scientists judge habitability from a few numbers");
            yield return ui.Fade(1f, 0.6f);
        }

        Vector3 padPosForWalk; GameObject landerFinal; Land surfaceLand; string surfaceSite;

        void BuildSurface(Land land, string siteName, Light sun, bool sunset = false)
        {
            Color top = sunset ? new Color(0.22f, 0.3f, 0.5f) : new Color(0.2f, 0.42f, 0.72f), hor = sunset ? new Color(0.95f, 0.55f, 0.35f) : new Color(0.72f, 0.82f, 0.85f);
            World.SkyDome(stageRoot, top, hor, -sun.transform.forward, sunset ? new Color(4f, 1.6f, 0.6f) : new Color(3.5f, 2.6f, 1.4f));
            RenderSettings.fogColor = Color.Lerp(hor, top, 0.15f); cam.clearFlags = CameraClearFlags.SolidColor;
            Fx.Motes(stageRoot, sunset ? new Color(1f, 0.8f, 0.5f, 0.8f) : new Color(1f, 1f, 0.8f, 0.7f));
            World.alienFlora = true;
            land.Build(stageRoot);
            var wmat = Mats.Lit(new Color(0.08f, 0.28f, 0.34f), 0.95f, new Color(0.01f, 0.05f, 0.07f));
            World.Prim(PrimitiveType.Cube, stageRoot, new Vector3(0, -6f, 0), Vector3.zero, new Vector3(3000, 0.1f, 3000), wmat);
            var rnd = new System.Random(77);
            for (int i = 0; i < 160; i++)
            {
                float x = (float)(rnd.NextDouble() - 0.5) * 1100f, z = (float)(rnd.NextDouble() - 0.5) * 1100f; if (Mathf.Sqrt(x * x + z * z) < 70f) continue;
                float h = land.H(x, z); if (h < -3f) continue;
                if (rnd.NextDouble() < 0.55)
                {
                    var tr = World.Building("tree_01", stageRoot); tr.transform.position = new Vector3(x, h, z); tr.transform.localScale = Vector3.one * (4f + (float)rnd.NextDouble() * 4f); tr.transform.rotation = Quaternion.Euler(0, (float)rnd.NextDouble() * 360f, 0);
                }
                else
                {
                    float s = 1.5f + (float)rnd.NextDouble() * 5f;
                    World.Prim(PrimitiveType.Sphere, stageRoot, new Vector3(x, h + s * 0.2f, z), new Vector3(0, (float)rnd.NextDouble() * 360f, 0), new Vector3(s * 1.4f, s * 0.8f, s), Mats.Lit(new Color(0.34f, 0.3f, 0.3f), 0.15f));
                }
            }
        }

        // ------------------------------------------------ the walk, the reveal
        IEnumerator WalkAndReveal(Land land)
        {
            Vector3 collector = new Vector3(168f, 0, 98f); collector.y = land.H(collector.x, collector.z);
            Vector3 shipPos = new Vector3(200f, 0, 140f); shipPos.y = land.H(shipPos.x, shipPos.z);
            var col = World.Prim(PrimitiveType.Cylinder, stageRoot, collector + new Vector3(0, 3f, 0), Vector3.zero, new Vector3(5f, 3f, 5f), Mats.Lit(new Color(0.85f, 0.88f, 0.9f), 0.7f));
            World.Prim(PrimitiveType.Cylinder, stageRoot, collector + new Vector3(0, 6.5f, 0), Vector3.zero, new Vector3(9f, 0.4f, 9f), Mats.Lit(new Color(0.2f, 0.5f, 0.7f), 0.9f, new Color(0.0f, 0.2f, 0.4f)));
            World.Prim(PrimitiveType.Cylinder, stageRoot, collector + new Vector3(0, 1f, 0), Vector3.zero, new Vector3(7f, 0.7f, 7f), Mats.Lit(new Color(0.3f, 0.3f, 0.34f), 0.5f));
            var cl = new GameObject("CL"); cl.transform.SetParent(stageRoot, false); cl.transform.position = collector + new Vector3(0, 8f, 0); var cll = cl.AddComponent<Light>(); cll.color = new Color(0.4f, 0.9f, 1f); cll.range = 40f; cll.intensity = 6f;
            var sonShip = World.Ship(2, stageRoot); sonShip.transform.localScale = Vector3.one * 5f; sonShip.transform.position = shipPos + new Vector3(0, 2.4f, 0); sonShip.transform.rotation = Quaternion.Euler(-90, 30, 0);
            for (int i = 0; i < 5; i++)
            {
                var p = shipPos + new Vector3(10f + (i % 3) * 2.6f, 0, -6f + (i / 3) * 2.8f); p.y = land.H(p.x, p.z);
                World.Prim(PrimitiveType.Cube, stageRoot, p + new Vector3(0, 0.9f, 0), new Vector3(0, i * 20f, 0), new Vector3(2f, 1.8f, 2f), Mats.Lit(new Color(0.9f, 0.5f, 0.12f), 0.4f, new Color(0.25f, 0.1f, 0.0f)));
            }
            // cable
            var pts = new List<Vector3>(); Vector3 a0 = new Vector3(8, 0, 0); Vector3 a1 = collector;
            for (int i = 0; i <= 40; i++) { float k = i / 40f; var p = Vector3.Lerp(a0, a1, k) + new Vector3(Mathf.Sin(k * 9f) * 8f * Mathf.Sin(k * Mathf.PI), 0, 0); p.y = land.H(p.x, p.z) + 0.25f; pts.Add(p); }
            var cab = new GameObject("Cable"); cab.transform.SetParent(stageRoot, false); var lr = cab.AddComponent<LineRenderer>(); lr.useWorldSpace = true; lr.positionCount = pts.Count; lr.SetPositions(pts.ToArray()); lr.widthMultiplier = 0.4f; lr.material = Mats.Additive(new Color(2.6f, 1.1f, 0.3f, 0.95f));
            var cab2 = new GameObject("CableCore"); cab2.transform.SetParent(stageRoot, false); var lr2 = cab2.AddComponent<LineRenderer>(); lr2.useWorldSpace = true; lr2.positionCount = pts.Count; lr2.SetPositions(pts.ToArray()); lr2.widthMultiplier = 0.1f; lr2.material = Mats.Lit(new Color(0.1f, 0.1f, 0.1f), 0.2f);
            // the son
            var son = new GameObject("Son"); son.transform.SetParent(stageRoot, false);
            var suit = Mats.Lit(new Color(0.85f, 0.45f, 0.15f), 0.4f); var visor = Mats.Lit(Color.black, 0.95f, new Color(0.1f, 0.3f, 0.45f));
            World.Prim(PrimitiveType.Capsule, son.transform, new Vector3(0, 1f, 0), Vector3.zero, new Vector3(0.7f, 0.9f, 0.5f), suit);
            World.Prim(PrimitiveType.Sphere, son.transform, new Vector3(0, 2.0f, 0), Vector3.zero, Vector3.one * 0.5f, Mats.Lit(new Color(0.9f, 0.9f, 0.92f), 0.7f));
            World.Prim(PrimitiveType.Sphere, son.transform, new Vector3(0, 2.0f, 0.18f), Vector3.zero, new Vector3(0.38f, 0.3f, 0.2f), visor);
            son.transform.position = shipPos + new Vector3(-14, 0, -4); son.transform.position = new Vector3(son.transform.position.x, land.H(son.transform.position.x, son.transform.position.z), son.transform.position.z);
            Destroy(landerFinal);
            var lander = World.Ship(1, stageRoot); lander.transform.localScale = Vector3.one * 5f; lander.transform.position = new Vector3(0, 2.0f, 0); lander.transform.rotation = Quaternion.Euler(-90, 0, 0);
            // player
            var hud = ui.Stretch(ui.stage, "WalkHUD");
            var obj = ui.Label(hud, "", 24, Theme.Cyan, A.T, 0, -40, 1500, 40, TextAnchor.UpperCenter, FontStyle.Bold);
            ui.Label(hud, "WASD walk   -   MOUSE look   -   SHIFT run", 16, Theme.Dim, A.BR, -40, 40, 700, 24, TextAnchor.LowerRight);
            var pip = PipOrb.Create(stageRoot);
            Vector3 pp = new Vector3(12, 0, 6); float yaw = 80f, pitch = 4f;
            cam.fieldOfView = 62; cam.nearClipPlane = 0.1f;
            if (!devAuto) { Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; }
            int stageN = 0; bool talkedC = false, talkedS = false; float sonTimer = 0;
            yield return ui.Fade(0f, 0.8f);
            while (stageN < 3)
            {
                float dt = Time.deltaTime;
                yaw += Input.GetAxis("Mouse X") * 2.2f; pitch = Mathf.Clamp(pitch - Input.GetAxis("Mouse Y") * 2.2f, -60, 60);
                var fwd = Quaternion.Euler(0, yaw, 0) * Vector3.forward; var right = Quaternion.Euler(0, yaw, 0) * Vector3.right;
                float spd = Input.GetKey(KeyCode.LeftShift) ? 11f : 6.5f;
                var mv = (fwd * Input.GetAxis("Vertical") + right * Input.GetAxis("Horizontal")) * spd * dt;
                if (devAuto && stageN < 2)
                {
                    Vector3 tgt = stageN == 0 ? collector : shipPos; Vector3 dd = new Vector3(tgt.x - pp.x, 0, tgt.z - pp.z);
                    yaw = Mathf.LerpAngle(yaw, Mathf.Atan2(dd.x, dd.z) * Mathf.Rad2Deg, dt * 4f); mv = new Vector3(dd.x, 0, dd.z).normalized * 24f * dt;
                }
                var np = pp + mv;
                if (land.H(np.x, np.z) > -3.2f) pp = np;
                float ground = land.H(pp.x, pp.z);
                pp.y = Mathf.Lerp(pp.y, ground, dt * 12f);
                cam.transform.position = new Vector3(pp.x, pp.y + 1.75f + Mathf.Sin(Time.time * 9f) * 0.03f * mv.magnitude * 60f, pp.z);
                cam.transform.rotation = Quaternion.Euler(pitch, yaw, 0);
                if (stageN == 0)
                {
                    float d = Vector2.Distance(new Vector2(pp.x, pp.z), new Vector2(collector.x, collector.z));
                    obj.text = "FOLLOW THE CABLE  -  " + d.ToString("0") + " m";
                    if (d < 22f && !talkedC)
                    {
                        talkedC = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                        yield return Say("FATHER", "A water collector. Working - and somebody maintains it. These panels are clean.");
                        yield return Say("PIP", "The cable continues to a ship, east of here. A very familiar-looking ship...");
                        Cursor.lockState = CursorLockMode.Locked; Cursor.visible = false; stageN = 1;
                    }
                }
                else if (stageN == 1)
                {
                    float d = Vector2.Distance(new Vector2(pp.x, pp.z), new Vector2(shipPos.x, shipPos.z));
                    obj.text = "INVESTIGATE THE SHIP  -  " + d.ToString("0") + " m";
                    if (d < 20f && !talkedS)
                    {
                        talkedS = true; Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
                        yield return Say("FATHER", "That's his ship. The registration - it's his. And those cases... those are my supply cases.");
                        stageN = 2;
                    }
                }
                else if (stageN == 2)
                {
                    sonTimer += dt; obj.text = "SOMEONE IS APPROACHING";
                    var toP = new Vector3(pp.x, 0, pp.z) - new Vector3(son.transform.position.x, 0, son.transform.position.z);
                    if (toP.magnitude > 5f) { son.transform.position += toP.normalized * 3.2f * dt; var sp3 = son.transform.position; son.transform.position = new Vector3(sp3.x, land.H(sp3.x, sp3.z), sp3.z); }
                    son.transform.rotation = Quaternion.LookRotation(toP.sqrMagnitude > 0.01f ? toP.normalized : Vector3.forward);
                    if (toP.magnitude <= 5.5f)
                    {
                        Cursor.lockState = CursorLockMode.None; Cursor.visible = true; stageN = 3;
                        cam.transform.rotation = Quaternion.LookRotation(son.transform.position + Vector3.up * 1.8f - cam.transform.position);
                    }
                }
                yield return null;
            }
            obj.text = "";
            yield return Say("SON", "...Dad?", 2.5f);
            yield return Say("FATHER", "It was you. You rammed my ship. You took my supplies.");
            yield return Say("SON", "I didn't know it was you. Your ship came out of the relay glare with no ID. I needed supplies - and I panicked. I'm sorry. I tried to warn you off.");
            yield return Say("FATHER", "Why didn't you ever call? Eleven years. Why did we never hear a word?");
            yield return Say("SON", "I was never unreachable, Dad. My long-range radio was damaged in the launch. The only way out was to fly up to orbit and try to push my recording through the relay.");
            // transmission log
            var lroot = ui.Stretch(ui.stage, "Log");
            var lp = ui.Panel(lroot, A.C, 0, 0, 1100, 760, Theme.Bg2);
            ui.Label(lp, "TRANSMISSION LOG  -  SURVEY CRAFT (SON)", 26, Theme.Amber, A.TL, 30, -22, 1000, 40, TextAnchor.UpperLeft, FontStyle.Bold);
            var lt = ui.Label(lp, "", 19, Theme.Text, A.TL, 30, -76, 1040, 640);
            string log = ""; int attempts = 0;
            for (int i = 0; i < 19; i++)
            {
                attempts += UnityEngine.Random.Range(6, 14);
                log += "ATTEMPT " + attempts.ToString("000") + "   uplink to RELAY via orbit   <color=#FF5C5C>" + (i < 18 ? "NO ACKNOWLEDGEMENT" : "RELAY WENT DARK WHILE TRANSMITTING") + "</color>\n";
                lt.text = log; sfx.Beep(); yield return new WaitForSeconds(0.28f);
            }
            lt.text = log + "\n<color=#FFB547>Final entry: " + attempts + " attempts. Relay lost during retry.</color>";
            yield return new WaitForSeconds(1.2f);
            yield return Say("SON", "The last time I went up, the relay went dark while I was trying again.", 3.5f);
            yield return Say("SON", "That was me.", 3f);
            Destroy(lroot.gameObject);
            yield return Say("FATHER", "...Then I destroyed it - and you were already on the other end of it. All this time. Right there.");
            yield return Say("PIP", "Both of you. The same relay. The same instant. I... I don't have a number for that.");
            Destroy(hud.gameObject);
            cam.fieldOfView = 50; cam.nearClipPlane = 0.05f;
        }

        // ------------------------------------------------ transmit
        IEnumerator TransmitPacket(Exo ex, float waterSignal, string siteName)
        {
            var root = ui.Stretch(ui.stage, "Packet");
            var p = ui.Panel(root, A.C, 0, 0, 1300, 860, Theme.Bg2);
            ui.Label(p, "SURVEY PACKET  -  TRANSMIT TO EARTH", 36, Theme.Cyan, A.TL, 36, -26, 1200, 50, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(p, "Attach everything Earth needs to see. Click each item to attach it.", 20, Theme.Dim, A.TL, 36, -80, 1200, 30);
            string[] items = { "Planet coordinates (" + ex.name + ", " + siteName.Substring(5) + ")", "Water readings (signal " + Mathf.RoundToInt(waterSignal * 100) + "%)", "Planet scan: radius, flux, temperature, period", "FLIGHT ASSESSMENT v1  -  engineering review: 10% success probability", "FLIGHT BRIEFING v2  -  the version the pilot saw (no probability)" };
            bool[] on = new bool[items.Length]; var btns = new Button[items.Length]; Button tx = null;
            Action chk = () => { bool all = true; for (int i = 0; i < items.Length; i++) if (!on[i]) all = false; tx.interactable = all; };
            for (int i = 0; i < items.Length; i++)
            {
                int k = i;
                btns[i] = ui.Btn(p, "[   ]   " + items[i], A.TL, 36, -140 - i * 84, 1220, 68, () => { on[k] = !on[k]; UIKit.SetBtn(btns[k], (on[k] ? "[ X ]   " : "[   ]   ") + items[k]); chk(); }, Theme.Cyan, 21);
            }
            ui.Label(p, "Real light-travel time to " + ex.name + " is over " + (ex.distPc * 3.26f).ToString("N0") + " years. In the story, an invented relay and station network carries the packet. (Science vs fiction.)", 17, Theme.Dim, A.BL, 36, 110, 1200, 60, TextAnchor.LowerLeft);
            var w = new Wait();
            tx = ui.Btn(p, "TRANSMIT", A.B, 0, 30, 560, 66, () => w.Done(), Theme.Green, 28); tx.interactable = false;
            if (devAuto) { yield return new WaitForSeconds(2f); w.Done(); }
            yield return w;
            Destroy(root.gameObject);
            sfx.Whoosh();
            var hud = ui.Stretch(ui.stage, "TxHUD");
            var lbl = ui.Label(hud, "TRANSMITTING...", 44, Theme.Cyan, A.C, 0, 80, 1400, 70, TextAnchor.MiddleCenter, FontStyle.Bold);
            var bar = ui.Img(hud, "tbg", A.C, 0, 0, 800, 14, new Color(1, 1, 1, 0.12f), ui.round); var fill = ui.Img(hud, "tf", A.C, 0, 0, 800, 14, Theme.Cyan, ui.round); fill.type = Image.Type.Filled; fill.sprite = ui.whiteSpr; fill.fillMethod = Image.FillMethod.Horizontal;
            // pulse into the sky
            var beam = new GameObject("TxBeam"); var b = beam.AddComponent<LineRenderer>(); b.positionCount = 2; b.widthMultiplier = 1.2f; b.material = Mats.Additive(new Color(0.5f, 2f, 3f, 0.7f)); b.useWorldSpace = true;
            Vector3 origin = cam.transform.position + cam.transform.forward * 3f + Vector3.down * 0.8f;
            yield return Over(5f, k => { fill.fillAmount = k; b.SetPosition(0, origin); b.SetPosition(1, origin + (cam.transform.forward * 0.3f + Vector3.up) * 600f * k); }, false);
            Destroy(beam);
            lbl.text = "<color=#5CFF9D>NASA ACKNOWLEDGES - PACKET RECEIVED</color>"; sfx.Ok();
            yield return new WaitForSeconds(2.4f);
            Destroy(hud.gameObject);
            yield return Say("NARRATOR", "Earth received everything: the coordinates, the water readings - and both versions of the flight documents.", 4.5f);
        }

        // ====================================================== FINALE
        IEnumerator Finale()
        {
            if (surfaceLand == null) surfaceLand = new Land(1235, 2.4f, 1f);
            NewStage(new Color(0.55f, 0.68f, 0.75f), new Color(0.45f, 0.52f, 0.5f), new Color(0.22f, 0.2f, 0.16f), true, new Color(0.62f, 0.66f, 0.62f), 0.0012f);
            cam.backgroundColor = new Color(0.62f, 0.66f, 0.62f);
            var sl = World.MakeSun(stageRoot, new Vector3(14, -70, 0), 2.4f, true); sl.color = new Color(1f, 0.7f, 0.42f);
            var land = surfaceLand; BuildSurface(land, "Echo Valley", sl, true);
            Vector3 collector = new Vector3(168f, 0, 98f); collector.y = land.H(collector.x, collector.z);
            World.Prim(PrimitiveType.Cylinder, stageRoot, collector + new Vector3(0, 3f, 0), Vector3.zero, new Vector3(5f, 3f, 5f), Mats.Lit(new Color(0.85f, 0.88f, 0.9f), 0.7f));
            World.Prim(PrimitiveType.Cylinder, stageRoot, collector + new Vector3(0, 6.5f, 0), Vector3.zero, new Vector3(9f, 0.4f, 9f), Mats.Lit(new Color(0.2f, 0.5f, 0.7f), 0.9f, new Color(0.0f, 0.2f, 0.4f)));
            var mk = new Func<Color, GameObject>(c =>
            {
                var o = new GameObject("Person");
                World.Prim(PrimitiveType.Capsule, o.transform, new Vector3(0, 1f, 0), Vector3.zero, new Vector3(0.7f, 0.9f, 0.5f), Mats.Lit(c, 0.4f));
                World.Prim(PrimitiveType.Sphere, o.transform, new Vector3(0, 2.0f, 0), Vector3.zero, Vector3.one * 0.5f, Mats.Lit(new Color(0.9f, 0.9f, 0.92f), 0.7f));
                World.Prim(PrimitiveType.Sphere, o.transform, new Vector3(0, 2.0f, 0.18f), Vector3.zero, new Vector3(0.38f, 0.3f, 0.2f), Mats.Lit(Color.black, 0.95f, new Color(0.1f, 0.3f, 0.45f)));
                o.transform.SetParent(stageRoot, false); return o;
            });
            var fa = mk(new Color(0.9f, 0.92f, 0.96f)); var so = mk(new Color(0.85f, 0.45f, 0.15f));
            Vector3 start = new Vector3(60f, 0, 30f), end = collector + new Vector3(-8, 0, -6);
            CamAt(new Vector3(70, 14, -10), start);
            yield return ui.Fade(0f, 1.2f);
            StartCoroutine(ui.Letterbox(true, 1f));
            float dur = 22f; var pip = PipOrb.Create(stageRoot); pip.offset = new Vector3(0.9f, -0.2f, 2.8f);
            var walk = StartCoroutine(Over(dur, k =>
            {
                Vector3 p = Vector3.Lerp(start, end, k); Vector3 q = p + new Vector3(2.2f, 0, -1.2f);
                p.y = land.H(p.x, p.z); q.y = land.H(q.x, q.z); fa.transform.position = p; so.transform.position = q;
                var dir = (end - start).normalized; fa.transform.rotation = so.transform.rotation = Quaternion.LookRotation(dir);
                var c = Vector3.Lerp(p, collector, 0.3f) + new Vector3(18f - 8f * k, 6f - 2.5f * k, -26f + 8f * k); cam.transform.position = Vector3.Lerp(cam.transform.position, c, Time.deltaTime * 2f);
                cam.transform.rotation = Quaternion.Slerp(cam.transform.rotation, Quaternion.LookRotation(p + new Vector3(0, 1.6f, 0) - cam.transform.position), Time.deltaTime * 3f);
            }, false));
            yield return Say("FATHER", "The coordinates are away. NASA has the water readings. And both versions of the flight documents.", 5f);
            yield return Say("SON", "The pilot should always see the real number.", 4f);
            yield return Say("FATHER", "Yes. From now on, every pilot does.", 4f);
            yield return Say("SON", "Come on, Dad. The collector needs checking - and we have a whole planet to survey.", 5f);
            yield return Say("PIP", "I'll... I'll bring the instruments!", 3.5f);
            yield return new WaitForSeconds(1.0f);
            yield return ui.Fade(1f, 1.2f);
            yield return ui.Letterbox(false, 0.2f);
            // ---------- end card
            NewSpaceStage(); var sun2 = World.MakeSun(stageRoot, new Vector3(20, -40, 0), 2.4f); var er = World.Earth(stageRoot); CamAt(new Vector3(-4, 4, -34), new Vector3(8, 0, 0));
            var root = ui.Stretch(ui.stage, "EndCard");
            var p2 = ui.Panel(root, A.C, 0, 0, 1500, 960, Theme.Bg2);
            ui.Label(p2, "MISSION RECORD", 54, Theme.Cyan, A.TL, 44, -30, 1000, 70, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(p2, "Planned probability vs. what actually happened", 22, Theme.Dim, A.TL, 44, -100, 1000, 34);
            float son = 0.0992f, fath = prog.plannedFather > 0 ? prog.plannedFather : 0.715f;
            AddBar(p2, "THE SON'S SHIP  -  planned success", son, -160, "10%  -  the number he never saw");
            AddBar(p2, "THE FATHER'S SHIP  -  planned success", fath, -240, Mathf.RoundToInt(fath * 100) + "%  -  always shown on the gauge");
            ui.Label(p2, "WHAT ACTUALLY HAPPENED", 18, Theme.Amber, A.TL, 44, -330, 700, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            string res = "Mission 1  -  test-fires to qualify the frame: " + prog.m1Attempts + "\nMission 2  -  preflight attempts before every checkpoint cleared: " + prog.m2Attempts + "\nMission 3  -  hull after collision: " + Mathf.RoundToInt((1f - prog.damage) * 100f) + "%,  triage attempts: " + prog.triageTries + "\nMission 4  -  landing attempts: " + prog.landingTries + ",  site: " + (prog.site == 1 ? "Echo Valley" : prog.site == 0 ? "Basalt Plain" : "Ridge Crest");
            ui.Label(p2, res, 22, Theme.Text, A.TL, 44, -362, 1400, 150);
            ui.Label(p2, "A plan of " + Mathf.RoundToInt(fath * 100) + "% is not a promise. Real missions are judged against their predicted risk - and the crew is always told.", 20, Theme.Dim, A.TL, 44, -520, 1400, 60);
            ui.Label(p2, "DEBRIEF CARDS UNLOCKED  (" + prog.cards.Count + " / 4)", 18, Theme.Green, A.TL, 44, -590, 900, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            string cs = ""; foreach (var c in prog.cards) cs += "  -  " + c + "\n";
            ui.Label(p2, cs, 21, Theme.Text, A.TL, 44, -622, 1400, 170);
            ui.Label(p2, "NASA data: JPL Horizons - NASA Exoplanet Archive - Deep Space Network - NASA Visible Earth imagery. A new mode is now open: MISSION DESIGN MODE.", 16, Theme.Dim, A.BL, 44, 90, 1400, 44, TextAnchor.LowerLeft);
            var w = new Wait(); int pick = 0;
            ui.Btn(p2, "MISSION DESIGN MODE", A.BL, 44, 24, 420, 56, () => { pick = 1; w.Done(); }, Theme.Green, 22);
            ui.Btn(p2, "MAIN MENU", A.BR, -44, 24, 320, 56, () => { pick = 0; w.Done(); }, Theme.Cyan, 22);
            yield return ui.Fade(0f, 1f);
            PlayerPrefs.SetInt("tlks_done", 1); PlayerPrefs.Save();
            yield return w;
            yield return ui.Fade(1f, 0.6f);
            if (pick == 1) yield return DesignMode();
        }

        void AddBar(Transform p, string label, float v, float y, string note)
        {
            ui.Label(p, label, 18, Theme.Dim, A.TL, 44, y, 900, 26, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Img(p, "bg", A.TL, 44, y - 30, 1100, 22, new Color(1, 1, 1, 0.1f), ui.round);
            var f = ui.Img(p, "f", A.TL, 44, y - 30, 1100, 22, Theme.Heat(v * 1.25f), ui.round); f.type = Image.Type.Filled; f.sprite = ui.whiteSpr; f.fillMethod = Image.FillMethod.Horizontal; f.fillAmount = Mathf.Clamp01(v);
            ui.Label(p, note, 20, Theme.Text, A.TL, 1170, y - 30, 330, 30);
        }

        // ====================================================== DESIGN MODE
        IEnumerator DesignMode()
        {
            bool again = true;
            while (again)
            {
                NewSpaceStage(); World.MakeSun(stageRoot, new Vector3(20, -40, 0), 2.4f);
                var er = World.Earth(stageRoot); var mo = World.Moon(stageRoot); mo.transform.position = new Vector3(40, 6, 50);
                var ship = World.Ship(1, stageRoot); ship.transform.position = new Vector3(-6, 2, -26); ship.transform.rotation = Quaternion.Euler(0, 55, 0);
                Fx.Flame(ship.transform.Find("Nozzle"), Vector3.zero, 0.3f, new Color(0.4f, 0.8f, 1f));
                World.Starfield(stageRoot, 300, 120f); CamAt(new Vector3(-8, 3, -34), new Vector3(2, 0, 0));
                yield return ui.Fade(0f, 0.8f);
                var mo2 = Nasa.Get("Moon"); var ma = Nasa.Get("Mars"); var ju = Nasa.Get("Jupiter");
                var cfg = new PlannerCfg { title = "MISSION DESIGN MODE", sub = "Pick a destination, set supplies and redundancy, watch the gauge, then fly it. Distances and light delay are from JPL Horizons (13 Nov 2026).", runLabel = "RUN MISSION", supplyMax = 450f };
                cfg.dests.Add(new Dest("Moon", 3.2f, 10f, "Earth distance " + (mo2.rangeKm / 1000.0).ToString("N0") + " thousand km - light delay " + Nasa.Dur(mo2.lightTimeSec) + " (JPL Horizons). dv is game balance."));
                cfg.dests.Add(new Dest("Mars", 4.5f, 250f, "Earth distance " + (ma.rangeKm / 1e6).ToString("0") + " million km - light delay " + Nasa.Dur(ma.lightTimeSec) + " (JPL Horizons). dv is game balance."));
                cfg.dests.Add(new Dest("Jupiter", 6.0f, 400f, "Earth distance " + (ju.rangeKm / 1e6).ToString("0") + " million km - light delay " + Nasa.Dur(ju.lightTimeSec) + " (JPL Horizons). dv is game balance."));
                var plan = new Plan { tested = true, supplyDays = 60f, dest = cfg.dests[0] };
                bool ok = false; RunResult res = null; float planned = 0f;
                while (!ok)
                {
                    yield return RunPlanner(cfg, plan);
                    planned = plan.Effective();
                    yield return RunMission(plan, false, "MISSION  -  " + plan.dest.name.ToUpper(), r => res = r);
                    ok = true;
                }
                yield return ui.Card("DEBRIEF  -  DESIGN MODE", res.success ? "Mission to " + plan.dest.name + ": SUCCESS" : "Mission to " + plan.dest.name + ": FAILED at " + Plan.Names[res.failedAt],
                    "You planned for " + Mathf.RoundToInt(planned * 100f) + "% success and the dice said " + (res.success ? "yes" : "no") + ". A " + Mathf.RoundToInt(planned * 100f) + "% plan fails " + Mathf.RoundToInt((1f - planned) * 100f) + " times in a hundred - that is what the gauge means. Launch mass was " + plan.Launch().ToString("N0") + " kg, of which " + plan.Prop().ToString("N0") + " kg was propellant (rocket equation).",
                    "NASA mission teams set a risk posture before launch, compare it with predicted risk, and review every anomaly in a debrief. Light-time to " + plan.dest.name + " is " + Nasa.Dur(Nasa.Get(plan.dest.name).lightTimeSec) + " one way, so crews cannot wait for help.",
                    "JPL Horizons (distances, light time); redundancy mathematics; game-balance reliabilities and masses", new[] { "Weakest subsystem in your plan: " + Plan.Names[plan.Weakest()] + " (" + Mathf.RoundToInt(plan.SubProb(plan.Weakest()) * 100f) + "%)" }, "CONTINUE");
                var w = new Wait(); int pick = 0;
                var rp = ui.Panel(ui.stage, A.C, 0, 0, 800, 220, Theme.Bg2);
                ui.Label(rp, "What next?", 30, Theme.Cyan, A.TL, 30, -24, 700, 40, TextAnchor.UpperLeft, FontStyle.Bold);
                ui.Btn(rp, "PLAN ANOTHER MISSION", A.BL, 30, 30, 360, 60, () => { pick = 1; w.Done(); }, Theme.Green, 20);
                ui.Btn(rp, "MAIN MENU", A.BR, -30, 30, 330, 60, () => { pick = 0; w.Done(); }, Theme.Cyan, 20);
                yield return w;
                again = pick == 1;
                yield return ui.Fade(1f, 0.5f);
            }
        }
    }
}
