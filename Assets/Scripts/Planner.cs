using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TLKS
{
    public class PlannerCfg
    {
        public string title = "MISSION PLANNER", sub = "", runLabel = "RUN PREFLIGHT";
        public bool allowSon, fixedSupplies, lockDest;
        public List<Dest> dests = new List<Dest>();
        public float supplyMax = 300f;
        public Wait sonLoaded = new Wait();
        public Func<string> gate;        // returns a message to block the run button, or null
        public Action<string> say;       // set by the planner: show a line from Pip inside the planner
        public Action onChange;          // called after every refresh
        public string footer = "Reliabilities, masses and the launch limit are GAME-BALANCE values, not NASA figures. The formulas are real.";
    }

    public partial class Game
    {
        // ====================================================== PLANNER
        public IEnumerator RunPlanner(PlannerCfg cfg, Plan plan, Gauge4 outGauge = null)
        {
            if (plan.dest == null && cfg.dests.Count > 0) plan.dest = cfg.dests[0];
            float origSupply = plan.supplyDays;
            var root = ui.Stretch(ui.stage, "Planner");
            var main = ui.Panel(root, A.TL, 40, -40, 1150, 1000);
            ui.Label(main, cfg.title, 40, Theme.Cyan, A.TL, 32, -22, 900, 56, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(main, cfg.sub, 20, Theme.Dim, A.TL, 32, -74, 1080, 56);

            // equipment row
            Button eqBtn = null;
            ui.Label(main, "EQUIPMENT", 16, Theme.Dim, A.TL, 32, -136, 200, 24, TextAnchor.UpperLeft, FontStyle.Bold);
            var eqTx = new Text[1];
            Text[] rTx = new Text[Plan.N], uTx = new Text[Plan.N], pTx = new Text[Plan.N], mTx = new Text[Plan.N]; Image[] pBar = new Image[Plan.N];
            Text massTx = null, briefTx = null, statusTx = null, supTx = null, destTx = null; Image massBar = null; RiskGauge gauge = null;
            Button runBtn = null; List<Button> destBtns = new List<Button>(); Slider supSl = null;
            Action refresh = null;

            eqBtn = ui.Btn(main, "", A.TL, 32, -160, 430, 46, () => { if (!cfg.allowSon && !plan.tested) return; plan.tested = !plan.tested; refresh(); }, Theme.Cyan, 18);
            if (cfg.allowSon)
                ui.Btn(main, "LOAD THE SON'S CONFIGURATION", A.TL, 480, -160, 450, 46, () =>
                {
                    plan.SetSon(); cfg.sonLoaded.Done(); refresh(); sfx.Alarm();
                }, Theme.Amber, 18);

            // header
            float hy = -222;
            string[] hd = { "SUBSYSTEM", "r (each)", "UNITS", "P(subsystem)", "MASS / UNIT" };
            float[] hx = { 32, 300, 440, 620, 950 };
            for (int i = 0; i < hd.Length; i++) ui.Label(main, hd[i], 15, Theme.Dim, A.TL, hx[i], hy, 220, 24, TextAnchor.UpperLeft, FontStyle.Bold);
            for (int i = 0; i < Plan.N; i++)
            {
                int k = i; float y = -250 - i * 66;
                var row = ui.Img(main, "Row", A.TL, 20, y + 6, 1110, 58, new Color(1, 1, 1, 0.04f), ui.round);
                ui.Label(main, Plan.Names[i], 26, Theme.Text, A.TL, 32, y - 6, 260, 40, TextAnchor.UpperLeft, FontStyle.Bold);
                rTx[i] = ui.Label(main, "0.90", 24, Theme.Dim, A.TL, 300, y - 6, 120, 40);
                ui.Btn(main, "-", A.TL, 430, y, 48, 46, () => { if (plan.n[k] > 1) { plan.n[k]--; refresh(); } }, Theme.Cyan, 26);
                uTx[i] = ui.Label(main, "1", 28, Theme.Text, A.TL, 486, y - 6, 50, 40, TextAnchor.UpperCenter, FontStyle.Bold);
                ui.Btn(main, "+", A.TL, 540, y, 48, 46, () => { if (plan.n[k] < 4) { plan.n[k]++; refresh(); } }, Theme.Cyan, 26);
                ui.Img(main, "BarBG", A.TL, 620, y - 12, 220, 14, new Color(1, 1, 1, 0.1f), ui.round);
                pBar[i] = ui.Img(main, "Bar", A.TL, 620, y - 12, 220, 14, Theme.Green, ui.round);
                pBar[i].type = Image.Type.Filled; pBar[i].sprite = ui.whiteSpr; pBar[i].fillMethod = Image.FillMethod.Horizontal;
                pTx[i] = ui.Label(main, "90.0%", 20, Theme.Text, A.TL, 620, y - 30, 220, 26);
                mTx[i] = ui.Label(main, Plan.UnitMass[i].ToString("N0") + " kg", 22, Theme.Dim, A.TL, 950, y - 6, 180, 40);
            }
            // supplies
            float sy = -250 - 5 * 66 - 6;
            ui.Label(main, "SUPPLIES (days of consumables)", 18, Theme.Text, A.TL, 32, sy, 380, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            supSl = ui.Sld(main, A.TL, 410, sy + 6, 480, 36, 20, cfg.supplyMax, Mathf.Clamp(plan.supplyDays, 20, cfg.supplyMax), true, v => { plan.supplyDays = v; refresh(); });
            supSl.interactable = !cfg.fixedSupplies;
            supSl.SetValueWithoutNotify(Mathf.Clamp(origSupply, 20, cfg.supplyMax)); plan.supplyDays = origSupply;
            supTx = ui.Label(main, "", 22, Theme.Cyan, A.TL, 910, sy - 2, 230, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            // destination
            float dy = sy - 56;
            ui.Label(main, "ROUTE", 18, Theme.Text, A.TL, 32, dy, 100, 30, TextAnchor.UpperLeft, FontStyle.Bold);
            for (int i = 0; i < cfg.dests.Count; i++)
            {
                var d = cfg.dests[i];
                var b = ui.Btn(main, d.name, A.TL, 130 + i * 250, dy + 6, 240, 44, () => { if (!cfg.lockDest) { plan.dest = d; refresh(); } }, Theme.Cyan, 16);
                destBtns.Add(b);
            }
            destTx = ui.Label(main, "", 18, Theme.Dim, A.TL, 32, dy - 52, 1090, 52);
            // mass bar
            float my = dy - 112;
            ui.Label(main, "MASS BUDGET (rocket equation: m0 = mf * e^(dv / ve))", 16, Theme.Dim, A.TL, 32, my, 800, 24, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Img(main, "MBG", A.TL, 32, my - 30, 1086, 22, new Color(1, 1, 1, 0.1f), ui.round);
            massBar = ui.Img(main, "MBar", A.TL, 32, my - 30, 1086, 22, Theme.Green, ui.round);
            massBar.type = Image.Type.Filled; massBar.sprite = ui.whiteSpr; massBar.fillMethod = Image.FillMethod.Horizontal;
            massTx = ui.Label(main, "", 18, Theme.Text, A.TL, 32, my - 58, 1090, 54);
            ui.Label(main, "P(mission) = PRODUCT over subsystems of [ 1 - (1 - r)^n ]        " + cfg.footer, 15, Theme.Dim, A.BL, 32, 14, 1090, 50, TextAnchor.LowerLeft);

            // right column
            var rp = ui.Panel(root, A.TR, -40, -40, 640, 1000);
            gauge = ui.Gauge(rp, A.T, 0, -50, 520, "MISSION SUCCESS PROBABILITY");
            ui.Label(rp, "RISK BRIEFING", 16, Theme.Amber, A.TL, 40, -420, 560, 26, TextAnchor.UpperLeft, FontStyle.Bold);
            briefTx = ui.Label(rp, "", 20, Theme.Text, A.TL, 40, -448, 560, 200);
            var pipTx = ui.Label(rp, "", 20, new Color(0.4f, 1f, 0.85f), A.TL, 40, -650, 560, 150);
            cfg.say = s => { pipTx.text = "<b>PIP:</b> " + s; sfx.Beep(); };
            statusTx = ui.Label(rp, "", 20, Theme.Red, A.BL, 40, 130, 560, 60, TextAnchor.LowerLeft, FontStyle.Bold);
            var done = new Wait();
            runBtn = ui.Btn(rp, cfg.runLabel, A.B, 0, 36, 540, 70, () =>
            {
                if (plan.dest == null) return;
                if (cfg.gate != null) { var gm = cfg.gate(); if (gm != null) { ui.Toast(gm, Theme.Amber); return; } }
                if (!plan.MassOk()) { ui.Toast("Launch mass is over the limit. Remove redundancy or supplies.", Theme.Red); sfx.Bad(); return; }
                if (!plan.SuppliesOk()) { ui.Toast("Not enough supplies for this route.", Theme.Red); sfx.Bad(); return; }
                done.Done();
            }, Theme.Green, 28);

            refresh = () =>
            {
                UIKit.SetBtn(eqBtn, plan.tested ? "FLIGHT-TESTED  (r = 0.90)" : "UNTESTED PROTOTYPE  (r = 0.63)");
                for (int i = 0; i < Plan.N; i++)
                {
                    float p = plan.SubProb(i);
                    rTx[i].text = plan.R(i).ToString("0.00"); uTx[i].text = plan.n[i].ToString();
                    pTx[i].text = (p * 100f).ToString("0.0") + "%"; pBar[i].fillAmount = p; pBar[i].color = Theme.Heat((p - 0.5f) * 2f);
                }
                supTx.text = plan.supplyDays.ToString("0") + " days  (" + (plan.supplyDays * Plan.SupplyPerDay).ToString("N0") + " kg)";
                for (int i = 0; i < destBtns.Count; i++)
                {
                    var im = destBtns[i].GetComponent<Image>(); bool sel = cfg.dests[i] == plan.dest;
                    im.color = sel ? new Color(0.2f, 0.5f, 0.6f, 1f) : new Color(0.07f, 0.18f, 0.22f, 0.95f);
                    UIKit.SetBtn(destBtns[i], (sel ? "> " : "") + cfg.dests[i].name);
                }
                if (plan.dest != null) destTx.text = "dv " + plan.dest.dv.ToString("0.0") + " km/s  -  needs at least " + plan.dest.daysNeeded.ToString("0") + " days of supplies  -  " + plan.dest.note;
                float m0 = plan.Launch(), dry = plan.Dry(), prop = plan.Prop();
                massBar.fillAmount = Mathf.Clamp01(m0 / Plan.LaunchLimit);
                massBar.color = m0 > Plan.LaunchLimit ? Theme.Red : (m0 > Plan.LaunchLimit * 0.9f ? Theme.Amber : Theme.Cyan);
                massTx.text = "Dry mass mf = " + dry.ToString("N0") + " kg  -  propellant = " + prop.ToString("N0") + " kg  -  launch mass m0 = " + m0.ToString("N0") + " / " + Plan.LaunchLimit.ToString("N0") + " kg   (ve = " + (Plan.Ve() / 1000f).ToString("0.0") + " km/s)";
                massTx.color = m0 > Plan.LaunchLimit ? Theme.Red : Theme.Text;
                float tot = plan.Total(), eff = plan.Effective();
                gauge.Set(eff);
                int w = plan.Weakest();
                string why = !plan.MassOk() ? "<color=#FF5C5C>Launch mass exceeds the limit - this ship cannot fly.</color>\n"
                           : !plan.SuppliesOk() ? "<color=#FF5C5C>Supplies run out before arrival - this route is unreachable.</color>\n" : "";
                briefTx.text = why + "Weakest link: <b><color=#FFB547>" + Plan.Names[w] + "</color></b> at " + (plan.SubProb(w) * 100f).ToString("0.0") + "%.\n" +
                    "Every subsystem must work, so the plan is only as strong as that product: " + (tot * 100f).ToString("0.0") + "%.\n" +
                    "Each extra unit costs mass, and every kg of dry mass costs " + (Mathf.Exp((plan.dest != null ? plan.dest.dv * 1000f : 0) / Plan.Ve())).ToString("0.0") + " kg at launch.";
                bool ok = plan.dest != null && plan.MassOk() && plan.SuppliesOk();
                runBtn.interactable = true;
                statusTx.text = ok ? "READY - plan can fly. Probability shown is the true probability." : "NOT FLYABLE - adjust the plan.";
                statusTx.color = ok ? Theme.Green : Theme.Red;
                if (cfg.onChange != null) cfg.onChange();
            };
            refresh();
            gauge.Snap(plan.Effective());
            if (devAuto)
            {
                yield return new WaitForSeconds(2f);
                cfg.sonLoaded.done = true;
                plan.tested = true; plan.n = new[] { 2, 2, 2, 2, 1 };
                Dest best = plan.dest; float bp = -1;
                foreach (var d in cfg.dests) { plan.dest = d; if (plan.Effective() > bp) { bp = plan.Effective(); best = d; } }
                plan.dest = best; refresh(); yield return new WaitForSeconds(2f); done.Done();
            }
            yield return done;
            Destroy(root.gameObject);
        }

        // ====================================================== RUN SEQUENCE
        public class Gauge4 { }

        public IEnumerator RunMission(Plan plan, bool forceRadioCut, string title, Action<RunResult> result, bool showDsn = false)
        {
            var rr = new RunResult { planned = plan.Effective() };
            var root = ui.Stretch(ui.stage, "Run");
            var p = ui.Panel(root, A.TL, 40, -40, showDsn ? 1180 : 1840, 760);
            float W = showDsn ? 1180 : 1840;
            ui.Label(p, title, 36, Theme.Cyan, A.TL, 32, -22, W - 64, 50, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(p, "Each checkpoint rolls its subsystem at the reliability in your plan. Planned success: " + (rr.planned * 100f).ToString("0.0") + "%", 20, Theme.Dim, A.TL, 32, -72, W - 64, 30);
            string[] cp = { "T-30 s  Life-support loop", forceRadioCut ? "T-10 s  EMERGENCY TEST: long-range radio cut" : "T-10 s  Comms link to Earth", "T+60 s  Navigation lock", "T+2 min  Reactor power-up", "T+5 min  Main drive burn" };
            var node = new Image[5]; var nLab = new Text[5];
            float span = W - 320;
            for (int i = 0; i < 5; i++)
            {
                float x = 160 + span * i / 4f;
                node[i] = ui.Img(p, "Node", A.TL, x - 22, -190, 44, 44, new Color(1, 1, 1, 0.15f), ui.circle);
                nLab[i] = ui.Label(p, Plan.Short[i] + "\n" + cp[i], 16, Theme.Dim, A.TL, x - (span / 8f), -246, span / 4f, 90, TextAnchor.UpperCenter);
            }
            var logTx = ui.Label(p, "", 22, Theme.Text, A.TL, 32, -350, W - 64, 330);
            if (showDsn) DsnPanel(root, A.TR, -40, -40);
            string log = "";
            for (int i = 0; i < 5; i++)
            {
                node[i].color = Theme.Amber; nLab[i].color = Theme.Amber; sfx.Beep();
                yield return new WaitForSeconds(1.0f);
                float pr = plan.SubProb(i); bool ok;
                if (i == 1 && forceRadioCut)
                {
                    pr = plan.n[1] >= 2 ? 1f - Mathf.Pow(1f - plan.R(1), plan.n[1] - 1) : 0f;
                    log += "<color=#FFB547>Primary long-range radio CUT. Backup units remaining: " + (plan.n[1] - 1) + ".</color>\n";
                }
                float roll = UnityEngine.Random.value; ok = roll < pr;
                log += (ok ? "<color=#5CFF9D>PASS</color>" : "<color=#FF5C5C>FAIL</color>") + "  " + Plan.Names[i] + ": rolled " + roll.ToString("0.00") + " against " + pr.ToString("0.00") + "\n";
                logTx.text = log;
                node[i].color = ok ? Theme.Green : Theme.Red; nLab[i].color = ok ? Theme.Green : Theme.Red;
                if (ok) sfx.Ok(); else { sfx.Bad(); rr.failedAt = i; break; }
            }
            rr.success = rr.failedAt < 0; rr.log.Add(log);
            log += rr.success ? "\n<b><color=#5CFF9D>ALL CHECKPOINTS CLEARED.</color></b>" : "\n<b><color=#FF5C5C>MISSION FAILED at " + Plan.Names[rr.failedAt] + ".</color></b> Return to the planner and strengthen that system.";
            logTx.text = log;
            var w = new Wait();
            ui.Btn(p, rr.success ? "CONTINUE" : "BACK TO PLANNER", A.BR, -32, 24, 300, 56, () => w.Done(), rr.success ? Theme.Green : Theme.Amber, 22);
            if (devAuto) { yield return new WaitForSeconds(2.5f); w.Done(); }
            yield return w;
            Destroy(root.gameObject);
            result(rr);
        }

        // ====================================================== DSN PANEL
        public RectTransform DsnPanel(Transform parent, A a, float x, float y)
        {
            var p = ui.Panel(parent, a, x, y, 600, 760);
            ui.Label(p, "DEEP SPACE NETWORK", 26, Theme.Cyan, A.TL, 24, -20, 560, 36, TextAnchor.UpperLeft, FontStyle.Bold);
            ui.Label(p, "DSN Now (eyes.nasa.gov/dsn) - " + Nasa.dsnStamp, 15, Nasa.dsnLive ? Theme.Green : Theme.Amber, A.TL, 24, -56, 560, 24);
            ui.Label(p, "Real antennas, real spacecraft. A backup radio matters because one dish talks to one craft at a time.", 15, Theme.Dim, A.TL, 24, -80, 556, 44);
            int n = 0;
            foreach (var l in Nasa.dsn)
            {
                if (n >= 12) break;
                float yy = -134 - n * 50;
                ui.Label(p, l.dish + "  " + l.station, 17, Theme.Text, A.TL, 24, yy, 280, 24, TextAnchor.UpperLeft, FontStyle.Bold);
                ui.Label(p, (l.down ? "downlink" : "tracking") + "  ->  " + l.craft, 16, l.down ? Theme.Green : Theme.Dim, A.TL, 24, yy - 22, 300, 24);
                ui.Label(p, l.rtlt > 0 ? "round-trip " + Nasa.Dur(l.rtlt) : "", 16, Theme.Cyan, A.TR, -24, yy - 4, 240, 24, TextAnchor.UpperRight);
                n++;
            }
            if (n == 0) ui.Label(p, "No active links in snapshot.", 18, Theme.Dim, A.TL, 24, -140, 500, 30);
            return p;
        }
    }
}
