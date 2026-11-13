using System;
using System.Collections.Generic;
using UnityEngine;

namespace TLKS
{
    public class Dest
    {
        public string name, note; public float dv; public float daysNeeded; public string source;
        public Dest(string n, float dv, float days, string note, string src = "game balance") { name = n; this.dv = dv; daysNeeded = days; this.note = note; source = src; }
    }

    public class Plan
    {
        public const int N = 5;
        public static readonly string[] Names = { "Life support", "Backup comms", "Navigation", "Power", "Propulsion" };
        public static readonly string[] Short = { "LIFE", "COMMS", "NAV", "POWER", "PROP" };
        public static readonly float[] UnitMass = { 1200f, 350f, 250f, 900f, 1500f };
        public float[] r = new float[N] { 0.9f, 0.9f, 0.9f, 0.9f, 0.9f };
        public int[] n = new int[N] { 1, 1, 1, 1, 1 };
        public float supplyDays = 120f;
        public Dest dest;
        public bool tested = true;

        // ---- game-balance constants (labelled on screen)
        public const float HullPayload = 6000f;     // kg
        public const float SupplyPerDay = 25f;      // kg
        public const float Isp = 900f;              // s, fictional nuclear-thermal drive
        public const float LaunchLimit = 45000f;    // kg
        public const float g0 = 9.80665f;

        public float R(int i) { return tested ? 0.90f : 0.63f; }
        public float SubProb(int i) { return 1f - Mathf.Pow(1f - R(i), n[i]); }
        public float Total() { float p = 1f; for (int i = 0; i < N; i++) p *= SubProb(i); return p; }
        public float Dry()
        {
            float m = HullPayload + supplyDays * SupplyPerDay;
            for (int i = 0; i < N; i++) m += n[i] * UnitMass[i];
            return m;
        }
        public static float Ve() { return Isp * g0; }
        public float Launch() { return Dry() * Mathf.Exp((dest != null ? dest.dv * 1000f : 0f) / Ve()); } // m0 = mf * e^(dv/ve)
        public float Prop() { return Launch() - Dry(); }
        public bool MassOk() { return Launch() <= LaunchLimit; }
        public bool SuppliesOk() { return dest == null || supplyDays >= dest.daysNeeded; }
        public int Weakest() { int w = 0; for (int i = 1; i < N; i++) if (SubProb(i) < SubProb(w)) w = i; return w; }
        public float Effective() { return (MassOk() && SuppliesOk()) ? Total() : 0f; }

        public void SetSon() { tested = false; for (int i = 0; i < N; i++) n[i] = 1; }
        public Plan Clone() { var p = (Plan)MemberwiseClone(); p.n = (int[])n.Clone(); p.r = (float[])r.Clone(); return p; }
    }

    public class RunResult { public bool success; public int failedAt = -1; public List<string> log = new List<string>(); public float planned; }

    public class Progress
    {
        public float plannedSon = 0.0992f, plannedFather, actualRolls; public bool fatherRunOk;
        public List<string> cards = new List<string>();
        public int attempts; public Plan fatherPlan; public float damage; public int triageTries, landingTries; public float landingScore; public int site;
        public int m1Attempts, m2Attempts, m3Attempts, m4Attempts;
        public void Card(string c) { if (!cards.Contains(c)) cards.Add(c); }
    }
}
