// ForgeSimulator.cs
// Place this file in any Editor/ folder in your Unity project.
// Open via: Window > FORGE > RTP Simulator

using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ForgeSimulator : EditorWindow
{
    // ── Symbol definitions ────────────────────────────────────────────────
    private class SymbolDef
    {
        public string name;
        public int    normalCount;   // stops in normal state
        public int    surgeCount;    // stops in surge state
        public float  value;         // 3-of-a-kind base multiplier (0 = wild)
        public bool   isWild;
    }

    private List<SymbolDef> symbols = new List<SymbolDef>
    {
        new SymbolDef { name = "Scrap",    normalCount = 7, surgeCount = 9, value =   2f, isWild = false },
        new SymbolDef { name = "Shatter",  normalCount = 7, surgeCount = 7, value =   2f, isWild = false },
        new SymbolDef { name = "Ingots",   normalCount = 3, surgeCount = 1, value =   4f, isWild = false },
        new SymbolDef { name = "Plate",    normalCount = 2, surgeCount = 1, value =   7f, isWild = false },
        new SymbolDef { name = "Molten",   normalCount = 1, surgeCount = 1, value =  15f, isWild = false },
        new SymbolDef { name = "Wild",     normalCount = 2, surgeCount = 3, value =   0f, isWild = true  },
    };

    // ── Wild multiplier stacks ────────────────────────────────────────────
    private float wildMult1Normal  = 2f;
    private float wildMult2Normal  = 4f;
    private float wildMult3Normal  = 8f;
    private float wildMult1Surge   = 3f;
    private float wildMult2Surge   = 6f;
    private float wildMult3Surge   = 12f;

    // ── Surge settings ────────────────────────────────────────────────────
    private float surgeChance      = 0.01f;   // probability per spin
    private int   surgeDuration    = 3;        // spins per surge event

    // ── Simulation settings ───────────────────────────────────────────────
    private int   totalStops       = 22;
    private int   simSpins         = 10000;
    private int   simSeed          = 42;
    private bool  useFixedSeed     = true;

    // ── Results ───────────────────────────────────────────────────────────
    private bool   hasResults      = false;
    private float  analyticalRTP;
    private float  simulatedRTP;
    private float  hitFrequency;
    private float  surgeContribution;
    private float  normalContribution;
    private int    longestDrySpell;
    private float  avgDrySpell;
    private Dictionary<string, int>   winBuckets     = new Dictionary<string, int>();
    private Dictionary<string, float> symbolRTPBreak = new Dictionary<string, float>();
    private float[] bankrollHistory;

    // ── UI state ──────────────────────────────────────────────────────────
    private Vector2 scroll;
    private bool    showSymbolConfig   = true;
    private bool    showWildConfig     = true;
    private bool    showSurgeConfig    = true;
    private bool    showSimConfig      = true;
    private bool    showResults        = true;
    private bool    showWinDist        = true;
    private bool    showRTPBreak       = true;
    private bool    showBankrollGraph  = true;
    private int     addSymbolCount     = 1;
    private string  newSymbolName      = "New Symbol";

    // ── Styles ────────────────────────────────────────────────────────────
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle valueStyle;
    private GUIStyle warningStyle;
    private bool     stylesInit = false;

    [MenuItem("Window/FORGE/RTP Simulator")]
    public static void Open() => GetWindow<ForgeSimulator>("FORGE Simulator");

    private void InitStyles()
    {
        if (stylesInit) return;
        headerStyle = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 13, normal = { textColor = new Color(0.9f, 0.75f, 0.3f) } };
        sectionStyle = new GUIStyle(EditorStyles.boldLabel)
            { fontSize = 11, normal = { textColor = new Color(0.7f, 0.85f, 1f) } };
        valueStyle = new GUIStyle(EditorStyles.label)
            { fontSize = 12, fontStyle = FontStyle.Bold };
        warningStyle = new GUIStyle(EditorStyles.label)
            { normal = { textColor = new Color(1f, 0.5f, 0.3f) }, fontStyle = FontStyle.Bold };
        stylesInit = true;
    }

    private void OnGUI()
    {
        InitStyles();
        scroll = EditorGUILayout.BeginScrollView(scroll);

        GUILayout.Space(8);
        GUILayout.Label("FORGE  —  Slot RTP Simulator", headerStyle);
        DrawDivider();

        DrawSymbolConfig();
        DrawWildConfig();
        DrawSurgeConfig();
        DrawSimConfig();
        DrawRunButtons();

        if (hasResults)
        {
            DrawResults();
            DrawWinDistribution();
            DrawRTPBreakdown();
            DrawBankrollGraph();
        }

        GUILayout.Space(16);
        EditorGUILayout.EndScrollView();
    }

    // ── Config sections ───────────────────────────────────────────────────

    private void DrawSymbolConfig()
    {
        showSymbolConfig = DrawFoldout(showSymbolConfig, "Symbol Configuration");
        if (!showSymbolConfig) return;

        EditorGUI.indentLevel++;

        totalStops = EditorGUILayout.IntField("Total stops per reel", totalStops);

        GUILayout.Space(4);

        // Header row
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Name",          GUILayout.Width(70));
        GUILayout.Label("Normal stops",  GUILayout.Width(90));
        GUILayout.Label("Surge stops",   GUILayout.Width(80));
        GUILayout.Label("Payout",         GUILayout.Width(70));
        GUILayout.Label("Is Wild",       GUILayout.Width(50));
        GUILayout.Label("",              GUILayout.Width(22));
        EditorGUILayout.EndHorizontal();

        DrawThinDivider();

        int removeIdx = -1;
        for (int i = 0; i < symbols.Count; i++)
        {
            var s = symbols[i];
            EditorGUILayout.BeginHorizontal();
            s.name        = EditorGUILayout.TextField(s.name,        GUILayout.Width(70));
            s.normalCount = EditorGUILayout.IntField(s.normalCount,  GUILayout.Width(90));
            s.surgeCount  = EditorGUILayout.IntField(s.surgeCount,   GUILayout.Width(80));

            GUI.enabled = !s.isWild;
            s.value = EditorGUILayout.FloatField(s.isWild ? 0f : s.value, GUILayout.Width(70));
            GUI.enabled = true;

            s.isWild = EditorGUILayout.Toggle(s.isWild, GUILayout.Width(50));
            if (GUILayout.Button("✕", GUILayout.Width(22))) removeIdx = i;
            EditorGUILayout.EndHorizontal();
        }

        if (removeIdx >= 0 && symbols.Count > 1) symbols.RemoveAt(removeIdx);

        GUILayout.Space(4);
        EditorGUILayout.BeginHorizontal();
        newSymbolName = EditorGUILayout.TextField("New symbol name", newSymbolName);
        if (GUILayout.Button("Add symbol", GUILayout.Width(100)))
            symbols.Add(new SymbolDef { name = newSymbolName, normalCount = 1, surgeCount = 1, value = 2f });
        EditorGUILayout.EndHorizontal();

        // Stop count validation
        int normalTotal = 0, surgeTotal = 0;
        foreach (var s in symbols) { normalTotal += s.normalCount; surgeTotal += s.surgeCount; }
        if (normalTotal != totalStops)
            EditorGUILayout.HelpBox($"Normal stops sum to {normalTotal}, expected {totalStops}.", MessageType.Warning);
        if (surgeTotal != totalStops)
            EditorGUILayout.HelpBox($"Surge stops sum to {surgeTotal}, expected {totalStops}.", MessageType.Warning);

        EditorGUI.indentLevel--;
        GUILayout.Space(6);
    }

    private void DrawWildConfig()
    {
        showWildConfig = DrawFoldout(showWildConfig, "Wild Multipliers");
        if (!showWildConfig) return;

        EditorGUI.indentLevel++;
        GUILayout.Label("Normal state", EditorStyles.miniLabel);
        wildMult1Normal = EditorGUILayout.FloatField("1 Wild multiplier", wildMult1Normal);
        wildMult2Normal = EditorGUILayout.FloatField("2 Wilds multiplier", wildMult2Normal);
        wildMult3Normal = EditorGUILayout.FloatField("3 Wilds multiplier", wildMult3Normal);

        GUILayout.Space(4);
        GUILayout.Label("Surge state", EditorStyles.miniLabel);
        wildMult1Surge = EditorGUILayout.FloatField("1 Wild multiplier", wildMult1Surge);
        wildMult2Surge = EditorGUILayout.FloatField("2 Wilds multiplier", wildMult2Surge);
        wildMult3Surge = EditorGUILayout.FloatField("3 Wilds multiplier", wildMult3Surge);
        EditorGUI.indentLevel--;
        GUILayout.Space(6);
    }

    private void DrawSurgeConfig()
    {
        showSurgeConfig = DrawFoldout(showSurgeConfig, "Wild Surge Event");
        if (!showSurgeConfig) return;

        EditorGUI.indentLevel++;
        surgeChance   = EditorGUILayout.Slider("Trigger chance per spin", surgeChance, 0f, 0.1f);
        surgeDuration = EditorGUILayout.IntSlider("Surge duration (spins)", surgeDuration, 1, 10);
        EditorGUI.indentLevel--;
        GUILayout.Space(6);
    }

    private void DrawSimConfig()
    {
        showSimConfig = DrawFoldout(showSimConfig, "Simulation Settings");
        if (!showSimConfig) return;

        EditorGUI.indentLevel++;
        simSpins    = EditorGUILayout.IntField("Spins to simulate", simSpins);
        useFixedSeed = EditorGUILayout.Toggle("Use fixed seed", useFixedSeed);
        if (useFixedSeed)
            simSeed = EditorGUILayout.IntField("Seed", simSeed);
        EditorGUI.indentLevel--;
        GUILayout.Space(6);
    }

    private void DrawRunButtons()
    {
        DrawDivider();
        EditorGUILayout.BeginHorizontal();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.4f);
        if (GUILayout.Button("▶  Run Analytical RTP", GUILayout.Height(28)))
            RunAnalytical();

        GUI.backgroundColor = new Color(0.3f, 0.6f, 1f);
        if (GUILayout.Button($"⚡  Simulate {simSpins:N0} Spins", GUILayout.Height(28)))
            RunSimulation();

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        DrawDivider();
    }

    // ── Analytical calculation ────────────────────────────────────────────

    private void RunAnalytical()
    {
        float normalRTP = CalculateStateRTP(isSurge: false);
        float surgeRTP  = CalculateStateRTP(isSurge: true);

        // Expected surge contribution: each surge trigger gives 3 surge spins
        // P(any given spin is a surge spin) = surgeChance * surgeDuration
        float surgeSpinWeight  = surgeChance * surgeDuration;
        float normalSpinWeight = 1f - surgeSpinWeight;

        normalContribution = normalSpinWeight * normalRTP;
        surgeContribution  = surgeSpinWeight  * surgeRTP;
        analyticalRTP      = normalContribution + surgeContribution;

        hasResults = true;
        Repaint();
    }

    private float CalculateStateRTP(bool isSurge)
    {
        // Build reel strip for this state
        float[] probs = new float[symbols.Count];
        float   total = 0;
        for (int i = 0; i < symbols.Count; i++)
        {
            int cnt = isSurge ? symbols[i].surgeCount : symbols[i].normalCount;
            probs[i] = cnt / (float)totalStops;
            total   += cnt;
        }

        float wm1 = isSurge ? wildMult1Surge : wildMult1Normal;
        float wm2 = isSurge ? wildMult2Surge : wildMult2Normal;
        float wm3 = isSurge ? wildMult3Surge : wildMult3Normal;

        // Find wild probability
        float pWild = 0f;
        foreach (var s in symbols)
            if (s.isWild)
                pWild += (isSurge ? s.surgeCount : s.normalCount) / (float)totalStops;

        float rtp = 0f;

        foreach (var s in symbols)
        {
            if (s.isWild) continue;

            float p = probs[symbols.IndexOf(s)];

            // 3 matching (no wilds)
            rtp += p * p * p * s.value;

            // 1 wild: wild can be in any of the 3 positions, other 2 are matching symbol
            // 3 arrangements × P(sym)² × P(wild) × value × wildMult1
            rtp += 3f * p * p * pWild * s.value * wm1;

            // 2 wilds: symbol fills 1 position, wilds fill other 2
            // 3 arrangements × P(sym) × P(wild)² × value × wildMult2
            rtp += 3f * p * pWild * pWild * s.value * wm2;
        }

        // 3 wilds
        rtp += pWild * pWild * pWild * wm3 * BestSymbolValue(); // 3 wilds = 3x best symbol × wild mult

        // Store per-symbol breakdown for display (normal state only, used in results)
        if (!isSurge)
        {
            symbolRTPBreak.Clear();
            foreach (var s in symbols)
            {
                if (s.isWild) continue;
                float p = probs[symbols.IndexOf(s)];
                float contrib = p * p * p * s.value
                              + 3f * p * p * pWild * s.value * wm1
                              + 3f * p * pWild * pWild * s.value * wm2;
                symbolRTPBreak[s.name] = contrib;
            }
            symbolRTPBreak["3× Wild"] = pWild * pWild * pWild * wm3 * BestSymbolValue();
        }

        return rtp;
    }

    // ── Monte Carlo simulation ────────────────────────────────────────────

    private void RunSimulation()
    {
        // Also compute analytical at the same time
        RunAnalytical();

        System.Random rng = useFixedSeed ? new System.Random(simSeed) : new System.Random();

        float bankroll     = 1000f;
        float totalWagered = 0f;
        float totalPaid    = 0f;
        int   hits         = 0;
        int   drySpell     = 0;
        int   maxDrySpell  = 0;
        float totalDry     = 0f;
        int   dryCount     = 0;
        int   surgeSpinsLeft = 0;

        bankrollHistory = new float[simSpins];
        winBuckets.Clear();
        string[] bucketKeys = { "0 (no win)", "1–5×", "5–15×", "15–50×", "50×+" };
        foreach (var k in bucketKeys) winBuckets[k] = 0;

        for (int spin = 0; spin < simSpins; spin++)
        {
            // Check for surge trigger (only when not already in surge)
            if (surgeSpinsLeft <= 0 && rng.NextDouble() < surgeChance)
                surgeSpinsLeft = surgeDuration;

            bool isSurge = surgeSpinsLeft > 0;
            if (surgeSpinsLeft > 0) surgeSpinsLeft--;

            float wm1 = isSurge ? wildMult1Surge : wildMult1Normal;
            float wm2 = isSurge ? wildMult2Surge : wildMult2Normal;
            float wm3 = isSurge ? wildMult3Surge : wildMult3Normal;

            // Spin 3 reels
            var s1 = RollSymbol(rng, isSurge);
            var s2 = RollSymbol(rng, isSurge);
            var s3 = RollSymbol(rng, isSurge);

            float payout = EvaluateSpin(s1, s2, s3, wm1, wm2, wm3);

            bankroll    -= 1f;
            bankroll    += payout;
            totalWagered += 1f;
            totalPaid   += payout;

            bankrollHistory[spin] = bankroll;

            bool isWin = payout >= 1f;
            if (isWin) hits++;

            // Dry spell: consecutive spins returning less than bet
            if (payout < 1f)
            {
                drySpell++;
                if (drySpell > maxDrySpell) maxDrySpell = drySpell;
            }
            else
            {
                if (drySpell > 0) { totalDry += drySpell; dryCount++; }
                drySpell = 0;
            }

            // Win bucket — no <1x bucket since all payouts are whole multipliers
            string bucket;
            if (payout == 0f)       bucket = "0 (no win)";
            else if (payout < 5f)   bucket = "1–5×";
            else if (payout < 15f)  bucket = "5–15×";
            else if (payout < 50f)  bucket = "15–50×";
            else                    bucket = "50×+";
            winBuckets[bucket]++;
        }

        simulatedRTP  = totalPaid / totalWagered;
        hitFrequency  = hits / (float)simSpins;
        longestDrySpell = maxDrySpell;
        avgDrySpell   = dryCount > 0 ? totalDry / dryCount : 0f;

        hasResults = true;
        Repaint();
    }

    private SymbolDef RollSymbol(System.Random rng, bool isSurge)
    {
        int roll = rng.Next(0, totalStops);
        int cursor = 0;
        foreach (var s in symbols)
        {
            cursor += isSurge ? s.surgeCount : s.normalCount;
            if (roll < cursor) return s;
        }
        return symbols[symbols.Count - 1];
    }

    // Returns the highest payout value among non-wild symbols
    private float BestSymbolValue()
    {
        float best = 0f;
        foreach (var s in symbols)
            if (!s.isWild && s.value > best)
                best = s.value;
        return best;
    }

    private float EvaluateSpin(SymbolDef s1, SymbolDef s2, SymbolDef s3, float wm1, float wm2, float wm3)
    {
        int wilds = (s1.isWild ? 1 : 0) + (s2.isWild ? 1 : 0) + (s3.isWild ? 1 : 0);

        if (wilds == 3) return wm3 * BestSymbolValue(); // 3 wilds = 3x best symbol × wild mult

        // Collect non-wild symbols
        var nonWilds = new List<SymbolDef>();
        if (!s1.isWild) nonWilds.Add(s1);
        if (!s2.isWild) nonWilds.Add(s2);
        if (!s3.isWild) nonWilds.Add(s3);

        // All non-wilds must match for a win
        SymbolDef first = nonWilds[0];
        foreach (var s in nonWilds)
            if (s.name != first.name) return 0f;

        float wildMult = wilds == 0 ? 1f : wilds == 1 ? wm1 : wm2;
        return first.value * wildMult;
    }

    // ── Result drawing ────────────────────────────────────────────────────

    private void DrawResults()
    {
        showResults = DrawFoldout(showResults, "Results");
        if (!showResults) return;

        EditorGUI.indentLevel++;

        // Analytical
        GUILayout.Label("Analytical RTP", sectionStyle);
        DrawStatRow("Overall RTP",          $"{analyticalRTP:P2}",       TargetColor(analyticalRTP, 0.88f, 0.92f));
        DrawStatRow("Normal state RTP",     $"{normalContribution / (1f - surgeChance * surgeDuration):P2}");
        DrawStatRow("Surge state RTP",      $"{(surgeChance * surgeDuration > 0 ? surgeContribution / (surgeChance * surgeDuration) : 0):P2}");
        DrawStatRow("Surge contribution",   $"{surgeContribution:P2} of total RTP");

        if (simulatedRTP > 0)
        {
            GUILayout.Space(6);
            GUILayout.Label($"Simulation  ({simSpins:N0} spins)", sectionStyle);
            DrawStatRow("Simulated RTP",     $"{simulatedRTP:P2}",       TargetColor(simulatedRTP, 0.88f, 0.92f));
            DrawStatRow("RTP drift",         $"{Mathf.Abs(simulatedRTP - analyticalRTP):P2} from analytical");
            DrawStatRow("Hit frequency",     $"{hitFrequency:P1}");
            DrawStatRow("Longest dry spell", $"{longestDrySpell} spins");
            DrawStatRow("Avg dry spell",     $"{avgDrySpell:F1} spins");
        }

        EditorGUI.indentLevel--;
        GUILayout.Space(6);
    }

    private void DrawWinDistribution()
    {
        if (winBuckets.Count == 0) return;
        showWinDist = DrawFoldout(showWinDist, "Win Distribution  (simulated)");
        if (!showWinDist) return;

        EditorGUI.indentLevel++;

        // Layout: topPad (pct labels) | bars | xAxis
        // All three zones are fixed — nothing escapes the allocated rect
        const float topPad  = 20f;
        const float xAxisH  = 18f;
        const float totalH  = topPad + 140f + xAxisH;
        float maxBarH = totalH - topPad - xAxisH;

        Rect areaRect = GUILayoutUtility.GetRect(0, totalH, GUILayout.ExpandWidth(true));
        areaRect = EditorGUI.IndentedRect(areaRect);

        // No <1x bucket — all payouts are whole multipliers
        string[] keys = { "No win", "1–5×", "5–15×", "15–50×", "50×+" };
        Color[]  cols = {
            new Color(0.35f, 0.35f, 0.35f),
            new Color(0.3f,  0.8f,  0.5f),
            new Color(0.9f,  0.8f,  0.2f),
            new Color(1.0f,  0.6f,  0.2f),
            new Color(1.0f,  0.3f,  0.3f),
        };

        string[] winKeys = { "1–5×", "5–15×", "15–50×", "50×+" };
        int totalWins = 0, maxWinVal = 1;
        foreach (var k in winKeys)
        {
            int c = winBuckets.ContainsKey(k) ? winBuckets[k] : 0;
            totalWins += c;
            maxWinVal  = Mathf.Max(maxWinVal, c);
        }
        int noWinCount = winBuckets.ContainsKey("0 (no win)") ? winBuckets["0 (no win)"] : 0;

        int[] counts = {
            noWinCount,
            winBuckets.ContainsKey("1–5×")  ? winBuckets["1–5×"]  : 0,
            winBuckets.ContainsKey("5–15×")  ? winBuckets["5–15×"]  : 0,
            winBuckets.ContainsKey("15–50×") ? winBuckets["15–50×"] : 0,
            winBuckets.ContainsKey("50×+")        ? winBuckets["50×+"]        : 0,
        };

        // No-win bar capped to tallest win bar so it can’t dominate the scale
        int scaleRef = maxWinVal;

        float barW  = areaRect.width / keys.Length;
        float baseY = areaRect.y + topPad + maxBarH; // bottom edge of bar zone

        // Dark background behind bar area only
        EditorGUI.DrawRect(new Rect(areaRect.x, areaRect.y + topPad, areaRect.width, maxBarH),
            new Color(0.12f, 0.12f, 0.12f));

        // Label style with dark background pill for readability
        var labelStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
        {
            normal = { textColor = Color.white },
            fontSize = 10,
        };

        for (int i = 0; i < keys.Length; i++)
        {
            int   count = counts[i];
            float h     = Mathf.Min(count, scaleRef) / (float)scaleRef * maxBarH;
            float barX  = areaRect.x + i * barW + 2;
            float barY  = baseY - h;

            // Bar
            EditorGUI.DrawRect(new Rect(barX, barY, barW - 4, h), cols[i]);

            // Pct label always in topPad zone — never inside a bar, never below bars
            // All buckets % of total spins so bars are directly comparable
            float pct = simSpins > 0 ? count / (float)simSpins * 100f : 0f;

            if (pct > 0.05f)
            {
                Rect labelRect = new Rect(barX, areaRect.y + 2, barW - 4, topPad - 4);
                // Dark pill behind text
                EditorGUI.DrawRect(labelRect, new Color(0f, 0f, 0f, 0.55f));
                GUI.Label(labelRect, $"{pct:F1}%", labelStyle);
            }

            // X-axis label — fixed row below bars, inside totalH
            GUI.Label(new Rect(areaRect.x + i * barW, baseY + 2, barW, xAxisH),
                keys[i], EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUI.indentLevel--;
        GUILayout.Space(6);
    }

    private void DrawRTPBreakdown()
    {
        if (symbolRTPBreak.Count == 0) return;
        showRTPBreak = DrawFoldout(showRTPBreak, "RTP Breakdown by Symbol  (analytical, normal state)");
        if (!showRTPBreak) return;

        EditorGUI.indentLevel++;
        foreach (var kv in symbolRTPBreak)
            DrawStatRow(kv.Key, $"{kv.Value:P3}");
        EditorGUI.indentLevel--;
        GUILayout.Space(6);
    }

    private void DrawBankrollGraph()
    {
        if (bankrollHistory == null || bankrollHistory.Length == 0) return;
        showBankrollGraph = DrawFoldout(showBankrollGraph, "Bankroll Over Time  (simulated)");
        if (!showBankrollGraph) return;

        EditorGUI.indentLevel++;

        Rect area = GUILayoutUtility.GetRect(0, 120, GUILayout.ExpandWidth(true));
        area = EditorGUI.IndentedRect(area);
        EditorGUI.DrawRect(area, new Color(0.15f, 0.15f, 0.18f));

        float minB = float.MaxValue, maxB = float.MinValue;
        foreach (var b in bankrollHistory) { minB = Mathf.Min(minB, b); maxB = Mathf.Max(maxB, b); }
        float range = Mathf.Max(maxB - minB, 1f);

        int step = Mathf.Max(1, bankrollHistory.Length / (int)area.width);
        Vector3 prev = Vector3.zero;
        bool first = true;

        Handles.color = new Color(0.4f, 0.8f, 1f, 0.9f);
        for (int i = 0; i < bankrollHistory.Length; i += step)
        {
            float x = area.x + (i / (float)bankrollHistory.Length) * area.width;
            float y = area.yMax - ((bankrollHistory[i] - minB) / range) * area.height;
            Vector3 pt = new Vector3(x, y, 0);
            if (!first) Handles.DrawLine(prev, pt);
            prev  = pt;
            first = false;
        }

        // Starting bankroll line
        float startY = area.yMax - ((1000f - minB) / range) * area.height;
        if (startY >= area.y && startY <= area.yMax)
        {
            Handles.color = new Color(1f, 1f, 0.4f, 0.4f);
            Handles.DrawLine(new Vector3(area.x, startY), new Vector3(area.xMax, startY));
        }

        GUI.Label(new Rect(area.x + 4, area.y + 2,  100, 16), $"Max: {maxB:F0}", EditorStyles.whiteMiniLabel);
        GUI.Label(new Rect(area.x + 4, area.yMax - 16, 100, 16), $"Min: {minB:F0}", EditorStyles.whiteMiniLabel);

        EditorGUI.indentLevel--;
        GUILayout.Space(6);
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private bool DrawFoldout(bool state, string label)
    {
        GUILayout.Space(2);
        state = EditorGUILayout.Foldout(state, label, true, EditorStyles.foldoutHeader);
        return state;
    }

    private void DrawStatRow(string label, string value, Color? color = null)
    {
        EditorGUILayout.BeginHorizontal();
        GUILayout.Label(label, GUILayout.Width(220));
        var s = new GUIStyle(EditorStyles.boldLabel);
        if (color.HasValue) s.normal.textColor = color.Value;
        GUILayout.Label(value, s);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawDivider()
    {
        GUILayout.Space(4);
        var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.3f, 0.3f, 0.3f));
        GUILayout.Space(4);
    }

    private void DrawThinDivider()
    {
        var rect = GUILayoutUtility.GetRect(0, 1, GUILayout.ExpandWidth(true));
        EditorGUI.DrawRect(rect, new Color(0.25f, 0.25f, 0.25f, 0.5f));
    }

    private Color TargetColor(float rtp, float low, float high)
    {
        if (rtp >= low && rtp <= high) return new Color(0.3f, 0.9f, 0.4f);
        if (rtp < low - 0.05f || rtp > high + 0.05f) return new Color(1f, 0.4f, 0.3f);
        return new Color(1f, 0.85f, 0.3f);
    }
}
