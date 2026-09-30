using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>
    /// Expansion 2 (Berry Blast) props: berry bushes and farms, the advanced berry press, cake mixers with conveyors,
    /// ovens with cooling racks, the pastry-case counter, delivery desks with shipping boxes, the new trucks, the fox,
    /// cottages, fences, blossom trees, beehives and rabbits.
    /// </summary>
    public static partial class JuiceKingBuilder
    {
        static Material _bMeadow, _bCobble, _bSoil, _bMulch, _bFence, _bPink, _bMint, _bLilac, _bCream, _bSky, _bButter,
            _bRoofRed, _bRoofBlue, _bRoofPlum, _bBrick, _bCopper, _bGlowOff, _bGlowOn, _bBelt, _bWilted, _bFoxOrange, _bFoxWhite, _bFoxDark,
            _bCardboard, _bTape, _bGingham, _bBakeryFloor, _bTwig, _bPaw, _bFlowerWhite, _bFlowerPink, _bFlowerYellow, _bRingOn, _bRingOff,
            _bBrakeOn, _bBrakeOff, _bGrille, _bBlossom, _bBlossomWhite, _bHive;
        static readonly Material[] _bLeaf = new Material[FruitN];

        static void BuildBerryMaterials()
        {
            _bMeadow = MatLib.Lit("B_Meadow", Color.white, 0.05f, MatLib.Tex("ground_meadow.png"), new Vector2(20f, 20f));
            _bCobble = MatLib.Lit("B_Cobble", Color.white, 0.18f, MatLib.Tex("ground_cobble.png"), new Vector2(8f, 4f));
            _bSoil = MatLib.Lit("B_Soil", Color.white, 0.03f, MatLib.Tex("soil_berry.png"), new Vector2(2f, 2f));
            _bMulch = MatLib.Lit("B_Mulch", new Color(0.62f, 0.46f, 0.28f), 0.05f);
            _bFence = MatLib.Lit("B_Fence", new Color(1f, 0.97f, 0.9f), 0.25f);
            _bPink = MatLib.Lit("B_WallPink", new Color(1f, 0.8f, 0.84f), 0.15f);
            _bMint = MatLib.Lit("B_WallMint", new Color(0.76f, 0.95f, 0.86f), 0.15f);
            _bLilac = MatLib.Lit("B_WallLilac", new Color(0.86f, 0.8f, 1f), 0.15f);
            _bCream = MatLib.Lit("B_WallCream", new Color(1f, 0.96f, 0.86f), 0.15f);
            _bSky = MatLib.Lit("B_WallSky", new Color(0.74f, 0.88f, 1f), 0.15f);
            _bButter = MatLib.Lit("B_Butter", new Color(1f, 0.92f, 0.6f), 0.2f);
            _bRoofRed = MatLib.Lit("B_RoofRed", new Color(0.86f, 0.33f, 0.36f), 0.2f);
            _bRoofBlue = MatLib.Lit("B_RoofBlue", new Color(0.36f, 0.52f, 0.86f), 0.2f);
            _bRoofPlum = MatLib.Lit("B_RoofPlum", new Color(0.62f, 0.3f, 0.52f), 0.2f);
            _bBrick = MatLib.Lit("B_Brick", new Color(0.86f, 0.42f, 0.34f), 0.15f);
            _bCopper = MatLib.Lit("B_Copper", new Color(0.95f, 0.62f, 0.36f), 0.7f, null, null, 0.6f);
            _bGlowOff = MatLib.Lit("B_OvenGlass", new Color(0.32f, 0.22f, 0.2f), 0.8f);
            _bGlowOn = Emissive("B_OvenGlow", new Color(1f, 0.55f, 0.15f), 2.4f);
            _bBelt = MatLib.Lit("B_Belt", Color.white, 0.3f, MatLib.Tex("belt.png"), new Vector2(1f, 4f));
            _bWilted = MatLib.Lit("B_Wilted", new Color(0.6f, 0.52f, 0.3f), 0.1f);
            _bFoxOrange = MatLib.Lit("B_FoxOrange", new Color(0.97f, 0.5f, 0.16f), 0.25f);
            _bFoxWhite = MatLib.Lit("B_FoxWhite", new Color(1f, 0.97f, 0.92f), 0.25f);
            _bFoxDark = MatLib.Lit("B_FoxDark", new Color(0.18f, 0.11f, 0.09f), 0.4f);
            _bCardboard = MatLib.Lit("B_Cardboard", new Color(0.84f, 0.64f, 0.42f), 0.1f);
            _bTape = MatLib.Lit("B_Tape", new Color(0.96f, 0.86f, 0.55f), 0.5f);
            _bGingham = MatLib.Lit("B_Gingham", Color.white, 0.2f, MatLib.Tex("gingham.png"), new Vector2(3f, 1f));
            _bBakeryFloor = MatLib.Lit("B_BakeryFloor", new Color(1f, 0.93f, 0.9f), 0.15f, MatLib.Tex("ground_tiles.png"), new Vector2(7f, 3f));
            _bTwig = MatLib.Lit("B_Twig", new Color(0.42f, 0.27f, 0.15f), 0.1f);
            _bPaw = MatLib.Sprite("B_Paw", MatLib.Tex("paw.png"), Color.white);
            _bFlowerWhite = MatLib.Lit("B_FlowerWhite", new Color(1f, 1f, 0.96f), 0.3f);
            _bFlowerPink = MatLib.Lit("B_FlowerPink", new Color(1f, 0.6f, 0.76f), 0.3f);
            _bFlowerYellow = MatLib.Lit("B_FlowerYellow", new Color(1f, 0.85f, 0.3f), 0.3f);
            _bRingOn = Emissive("B_RingOn", new Color(1f, 0.45f, 0.62f), 2f);
            _bRingOff = MatLib.Lit("B_RingOff", new Color(0.36f, 0.3f, 0.36f), 0.5f);
            _bBrakeOn = Emissive("B_BrakeOn", new Color(1f, 0.15f, 0.15f), 2.2f);
            _bBrakeOff = MatLib.Lit("B_BrakeOff", new Color(0.55f, 0.12f, 0.14f), 0.7f);
            _bGrille = MatLib.Lit("B_Grille", new Color(0.24f, 0.25f, 0.3f), 0.7f, null, null, 0.5f);
            _bBlossom = MatLib.Lit("B_Blossom", new Color(1f, 0.7f, 0.82f), 0.2f);
            _bBlossomWhite = MatLib.Lit("B_BlossomWhite", new Color(1f, 0.95f, 0.96f), 0.2f);
            _bHive = MatLib.Lit("B_Hive", new Color(1f, 0.86f, 0.45f), 0.2f);
            _bLeaf[7] = MatLib.Lit("B_LeafStrawberry", new Color(0.33f, 0.74f, 0.3f), 0.25f);
            _bLeaf[8] = MatLib.Lit("B_LeafRaspberry", new Color(0.36f, 0.64f, 0.28f), 0.25f);
            _bLeaf[9] = MatLib.Lit("B_LeafBlueberry", new Color(0.3f, 0.56f, 0.44f), 0.25f);
            _bLeaf[10] = MatLib.Lit("B_LeafCranberry", new Color(0.24f, 0.52f, 0.26f), 0.25f);
        }

        /// <summary>Cylinder between two points (pipes, rods).</summary>
        static GameObject TubeBetween(string name, Transform parent, Vector3 a, Vector3 b, float diameter, Material mat, bool shadows = false)
        {
            var mid = (a + b) * 0.5f;
            var d = b - a;
            var go = B.MeshObj(name, parent, B.Cylinder, mat, mid, new Vector3(diameter, d.magnitude * 0.5f, diameter), null, shadows);
            go.transform.localRotation = Quaternion.FromToRotation(Vector3.up, d.normalized);
            return go;
        }

        // ================================================================== bushes & farms

        /// <summary>A berry bush: leafy foliage (shakes, wilts) and a cluster of ripe berries on the camera side.</summary>
        static BerryBushNode BuildBerryBush(int f, FruitField field, Transform parent, Vector3 pos)
        {
            var go = B.Node(FruitKey[f] + "Bush", parent, pos);
            var t = go.transform;
            B.MeshObj("Mound", t, _disc, new[] { _bMulch, _bMulch }, Vector3.zero, new Vector3(1.35f, 0.06f, 1.35f), null, false);
            var bush = B.Node("Bush", t, Vector3.zero, new Vector3(0f, (float)_rnd.NextDouble() * 360f, 0f)).transform;
            var leaves = new List<Renderer>();
            var leafMat = _bLeaf[f];
            void Ball(Vector3 p, Vector3 s) => leaves.Add(B.MeshObj("LeafBall", bush, _shLeafBall[_rnd.Next(_shLeafBall.Length)], leafMat, p, s).GetComponent<Renderer>());
            void Flower(Vector3 p, Material petal)
            {
                B.MeshObj("Petals", bush, _disc, new[] { petal, petal }, p, new Vector3(0.14f, 0.02f, 0.14f), null, false);
                B.MeshObj("Heart", bush, _disc, new[] { _bFlowerYellow, _bFlowerYellow }, p + Vector3.up * 0.012f, new Vector3(0.05f, 0.02f, 0.05f), null, false);
            }
            float fruitY, fruitZ, size;
            int count;
            switch ((FruitKind)f)
            {
                case FruitKind.Strawberry:
                    for (int i = 0; i < 4; i++)
                    {
                        float a = (i * 90f + 45f) * Mathf.Deg2Rad;
                        Ball(new Vector3(Mathf.Cos(a) * 0.26f, 0.2f, Mathf.Sin(a) * 0.26f), new Vector3(0.62f, 0.36f, 0.62f));
                    }
                    Ball(new Vector3(0f, 0.3f, 0f), new Vector3(0.7f, 0.42f, 0.7f));
                    Flower(new Vector3(0.18f, 0.5f, 0.12f), _bFlowerWhite);
                    Flower(new Vector3(-0.22f, 0.46f, 0.18f), _bFlowerWhite);
                    Flower(new Vector3(0.05f, 0.52f, -0.2f), _bFlowerWhite);
                    fruitY = 0.26f; fruitZ = -0.46f; size = 0.24f; count = 6;
                    break;
                case FruitKind.Raspberry:
                    foreach (var p in new[] { new Vector3(-0.15f, 0f, 0.05f), new Vector3(0.18f, 0f, -0.05f), new Vector3(0f, 0f, 0.2f) })
                        B.MeshObj("Cane", bush, _shThinTrunk, _tTrunk, p, new Vector3(0.45f, 0.42f, 0.45f), new Vector3(0f, (float)_rnd.NextDouble() * 360f, 0f), false);
                    Ball(new Vector3(0f, 0.6f, 0f), new Vector3(0.78f, 1.0f, 0.78f));
                    Ball(new Vector3(0.22f, 0.98f, 0.1f), new Vector3(0.56f, 0.6f, 0.56f));
                    Ball(new Vector3(-0.24f, 0.84f, -0.05f), new Vector3(0.52f, 0.56f, 0.52f));
                    fruitY = 0.72f; fruitZ = -0.4f; size = 0.17f; count = 8;
                    break;
                case FruitKind.Blueberry:
                    Ball(new Vector3(0f, 0.5f, 0f), new Vector3(1.08f, 0.9f, 1.08f));
                    Ball(new Vector3(0.26f, 0.78f, 0.15f), new Vector3(0.6f, 0.55f, 0.6f));
                    Ball(new Vector3(-0.3f, 0.64f, 0.1f), new Vector3(0.56f, 0.5f, 0.56f));
                    Flower(new Vector3(0.3f, 0.95f, -0.1f), _bFlowerPink);
                    fruitY = 0.56f; fruitZ = -0.5f; size = 0.14f; count = 10;
                    break;
                default:
                    Ball(new Vector3(0f, 0.18f, 0f), new Vector3(1.25f, 0.34f, 1.15f));
                    Ball(new Vector3(0.3f, 0.26f, 0.2f), new Vector3(0.62f, 0.32f, 0.62f));
                    Ball(new Vector3(-0.35f, 0.22f, -0.1f), new Vector3(0.56f, 0.3f, 0.56f));
                    fruitY = 0.3f; fruitZ = -0.52f; size = 0.15f; count = 9;
                    break;
            }

            // Ripe berries on the side facing the camera so the harvest reads from above.
            var vis = B.Node("Visual", t, new Vector3(0f, fruitY, fruitZ)).transform;
            for (int i = 0; i < count; i++)
            {
                float a = (i / (float)count) * Mathf.PI * 1.4f - Mathf.PI * 0.7f;
                float r = 0.12f + (i % 3) * 0.07f;
                var p = new Vector3(Mathf.Sin(a) * r * 1.6f, ((i * 37) % 5 - 2) * 0.05f + (f == 7 ? -0.02f : 0f), -Mathf.Cos(a) * 0.05f);
                Berry(vis, f, p, size * (0.9f + (i % 3) * 0.08f), i * 53f);
            }

            // Shown after a fox raid: snapped twigs and paw prints.
            var dmg = B.Node("Damage", t, Vector3.zero);
            for (int i = 0; i < 3; i++)
            {
                float a = (float)_rnd.NextDouble() * Mathf.PI * 2f;
                B.Box("Twig", dmg.transform, _bTwig, new Vector3(Mathf.Cos(a) * 0.45f, 0.08f, Mathf.Sin(a) * 0.45f - 0.2f), new Vector3(0.04f, 0.04f, 0.32f),
                    new Vector3(0f, a * Mathf.Rad2Deg, 10f), false);
            }
            for (int i = 0; i < 2; i++)
                B.Decal("Paw", dmg.transform, _bPaw, new Vector3(-0.2f + i * 0.42f, 0.075f, -0.55f + i * 0.12f), new Vector2(0.24f, 0.24f), 20f + i * 30f)
                    .GetComponent<MeshRenderer>().sortingOrder = -3;

            var node = go.AddComponent<BerryBushNode>();
            node.kind = (FruitKind)f;
            node.visual = vis;
            node.radius = 0.6f;
            node.field = field;
            node.bush = bush;
            node.leaves = leaves.ToArray();
            node.wiltedLeaves = _bWilted;
            node.damageDecor = dmg;

            var hp = B.Node("HP", t, new Vector3(0f, Mathf.Max(1.4f, fruitY + 0.95f), -0.6f), null, new Vector3(1.2f, 0.16f, 1f));
            hp.AddComponent<Billboard>();
            B.Sprite("Bg", hp.transform, _sWhite, Vector3.zero, 1f, false, 20, new Color(0.1f, 0.1f, 0.1f, 0.7f));
            var fill = B.Sprite("Fill", hp.transform, _sWhite, new Vector3(0f, 0f, -0.001f), 1f, false, 21, new Color(0.45f, 1f, 0.35f));
            node.hpBar = hp.transform;
            node.hpFill = fill.transform;

            var col = go.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.45f, 0f);
            col.radius = 0.5f;
            node.blocker = col;
            return node;
        }

        /// <summary>A compact berry patch: mulched bed in a timber border, two rows of three bushes, a berry sign.</summary>
        static FruitField BuildBerryFarm(int f, Vector3 center, Vector2 size, string name, string display)
        {
            var root = B.Node(name, _stations, Vector3.zero);
            // The bed (soil, border, gate posts, sign) is always there, so locked patches read as prepared plots;
            // unlocking plants the bushes.
            var bed = B.Node(name + "Bed", _decor, Vector3.zero);
            var rt = bed.transform;
            var soil = MatLib.Lit("B_Soil_" + f, Color.white, 0.03f, MatLib.Tex("soil_berry.png"), new Vector2(size.x / 2f, size.y / 2f));
            RB("Soil", rt, soil, center + new Vector3(0f, 0.022f, 0f), new Vector3(size.x, 0.045f, size.y), 0.35f, false);
            // Timber border with gates in the middle of the south (press) and north (cake mixer) sides.
            var log = MatLib.Lit("B_Log", new Color(0.72f, 0.52f, 0.32f), 0.1f);
            float hx = size.x * 0.5f, hz = size.y * 0.5f, gate = 0.9f;
            foreach (var sx in new[] { -1f, 1f })
                RB("FrameE", rt, log, center + new Vector3(sx * hx, 0.09f, 0f), new Vector3(0.18f, 0.18f, size.y), 0.08f, false);
            foreach (var sz in new[] { -1f, 1f })
            foreach (var sx in new[] { -1f, 1f })
            {
                float len = hx - gate;
                RB("FrameN", rt, log, center + new Vector3(sx * (gate + len * 0.5f), 0.09f, sz * hz), new Vector3(len, 0.18f, 0.18f), 0.08f, false);
            }
            // Little lantern posts at the gates.
            foreach (var sz in new[] { -1f, 1f })
            foreach (var sx in new[] { -1f, 1f })
            {
                var p = center + new Vector3(sx * gate, 0f, sz * hz);
                B.Cyl("GatePost", rt, log, p + Vector3.up * 0.3f, 0.14f, 0.6f);
                B.MeshObj("GateCap", rt, LowSphere(), _bLeaf[f], p + Vector3.up * 0.64f, Vector3.one * 0.2f, null, false);
            }

            var fieldGo = B.Node("Field", root.transform, center);
            var field = fieldGo.AddComponent<FruitField>();
            field.kind = (FruitKind)f;
            field.displayName = display;
            field.idlePoint = B.Node("Idle", fieldGo.transform, new Vector3(-1.9f, 0f, -hz + 0.5f)).transform;
            foreach (var z in new[] { -1.45f, 0f, 1.45f })
            foreach (var x in new[] { -1.2f, 1.2f })
                BuildBerryBush(f, field, root.transform, center + new Vector3(x, 0f, z));

            // Round berry sign on a post at the south-west corner.
            var sign = B.Node("FarmSign", rt, center + new Vector3(-hx + 0.25f, 0f, -hz - 0.25f)).transform;
            B.Cyl("Post", sign, log, new Vector3(0f, 0.75f, 0.06f), 0.1f, 1.5f);
            B.MeshObj("Disc", sign, _disc, new[] { _mWhite, _bLeaf[f] }, new Vector3(0f, 1.55f, 0f), new Vector3(0.72f, 0.07f, 0.72f), new Vector3(-90f, 0f, 0f));
            B.Sprite("Icon", sign, _sFruit[f], new Vector3(0f, 1.55f, -0.08f), SpriteScale(_sFruit[f], 0.52f), false, 3);
            Busy(center.x, center.z, Mathf.Max(size.x, size.y) * 0.6f);
            return field;
        }

        // ================================================================== berry press

        /// <summary>Advanced juicer: tumbling glass drum, piston press, pulsing juice pipe, gauge and light ring.</summary>
        static BerryPress BuildBerryPress(int f, Vector3 pos)
        {
            var root = B.Node("Press_" + FruitKey[f], _stations, pos);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero);
            var bt = body.transform;
            var shell = _mFruitBody[f];
            var accent = MatLib.Lit("PressAccent_" + FruitKey[f], Color.Lerp(Balance.FruitColors[f], Color.white, 0.55f), 0.45f);

            RB("Plinth", bt, _mPanelDark, new Vector3(0f, 0.12f, 0f), new Vector3(2f, 0.24f, 1.8f), 0.22f);
            RB("Trim", bt, _mChrome, new Vector3(0f, 0.26f, 0f), new Vector3(2.04f, 0.05f, 1.84f), 0.24f, false);
            RB("Cabinet", bt, shell, new Vector3(0f, 0.78f, 0f), new Vector3(1.7f, 1f, 1.45f), 0.34f);
            RB("Band", bt, accent, new Vector3(0f, 1.2f, 0f), new Vector3(1.76f, 0.13f, 1.5f), 0.36f, false);
            RB("Lip", bt, _mChrome, new Vector3(0f, 1.3f, 0f), new Vector3(1.5f, 0.06f, 1.25f), 0.3f, false);

            // Front panel: gauge, light ring, berry badge and a status LED.
            var panel = B.Node("Panel", bt, new Vector3(0f, 0.72f, -0.74f)).transform;
            RB("Face", panel, _mPanelDark, Vector3.zero, new Vector3(1.24f, 0.62f, 0.07f), 0.05f, false);
            B.MeshObj("Gauge", panel, _disc, new[] { _mWhite, _mChrome }, new Vector3(-0.32f, 0.04f, -0.04f), new Vector3(0.38f, 0.03f, 0.38f), new Vector3(-90f, 0f, 0f), false);
            var needle = B.Node("Needle", panel, new Vector3(-0.32f, 0.04f, -0.075f)).transform;
            B.Box("Hand", needle, _mRed, new Vector3(0f, 0.07f, 0f), new Vector3(0.025f, 0.14f, 0.01f), null, false);
            var ring = new List<Renderer>();
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f + Mathf.PI * 0.5f;
                ring.Add(B.MeshObj("RingLight", panel, LowSphere(), _bRingOff, new Vector3(0.28f + Mathf.Cos(a) * 0.19f, 0.04f + Mathf.Sin(a) * 0.19f, -0.05f), Vector3.one * 0.07f, null, false).GetComponent<Renderer>());
            }
            B.Sprite("Badge", panel, _sFruit[f], new Vector3(0.28f, 0.04f, -0.06f), SpriteScale(_sFruit[f], 0.26f), false, 2).transform.localRotation = Quaternion.identity;
            var led = B.MeshObj("Led", panel, LowSphere(), _mLedOff, new Vector3(0.52f, 0.22f, -0.05f), new Vector3(0.09f, 0.09f, 0.05f), null, false);

            // Glass drum on chrome arms: berries tumble inside while pressing.
            foreach (var sx in new[] { -0.55f, 0.55f })
                RB("Arm", bt, _mChrome, new Vector3(sx, 1.5f, 0.05f), new Vector3(0.1f, 0.42f, 0.14f), 0.04f, false);
            B.MeshObj("DrumGlass", bt, B.Cylinder, _mGlass, new Vector3(0f, 1.78f, 0.05f), new Vector3(0.95f, 0.6f, 0.95f), new Vector3(0f, 0f, 90f), false);
            foreach (var sx in new[] { -0.62f, 0.62f })
                B.MeshObj("DrumCap", bt, _disc, new[] { _mChrome, _mChrome }, new Vector3(sx, 1.78f, 0.05f), new Vector3(1f, 0.05f, 1f), new Vector3(0f, 0f, 90f), false);
            var drum = B.Node("Drum", bt, new Vector3(0f, 1.78f, 0.05f)).transform;
            var drumBerries = new List<Transform>();
            for (int i = 0; i < 8; i++)
            {
                float a = i / 8f * Mathf.PI * 2f;
                var holder = B.Node("B", drum, new Vector3(-0.42f + (i % 4) * 0.28f, Mathf.Cos(a) * 0.22f, Mathf.Sin(a) * 0.22f)).transform;
                Berry(holder, f, Vector3.zero, f == 7 ? 0.2f : 0.16f, i * 45f);
                drumBerries.Add(holder);
            }
            B.MeshObj("Funnel", bt, _funnel, shell, new Vector3(0f, 2.12f, 0.05f), new Vector3(0.6f, 0.4f, 0.6f));
            var intake = B.Node("Intake", bt, new Vector3(0f, 2.45f, 0.05f));

            // Press chamber and piston (front right), a glass pipe down to the spout.
            RB("Chamber", bt, _mChrome, new Vector3(0.52f, 1.52f, -0.5f), new Vector3(0.42f, 0.36f, 0.42f), 0.08f);
            var piston = B.Node("Piston", bt, new Vector3(0.52f, 2.02f, -0.5f)).transform;
            B.Cyl("Rod", piston, _mChrome, new Vector3(0f, 0.05f, 0f), 0.1f, 0.5f);
            B.MeshObj("Head", piston, _disc, new[] { accent, _mChrome }, new Vector3(0f, -0.22f, 0f), new Vector3(0.34f, 0.08f, 0.34f), null, false);
            var pStart = B.Node("PipeStart", bt, new Vector3(0.52f, 1.32f, -0.72f)).transform;
            var pEnd = B.Node("PipeEnd", bt, new Vector3(0.08f, 0.98f, -0.94f)).transform;
            TubeBetween("Pipe", bt, pStart.localPosition, pEnd.localPosition, 0.14f, _mGlass);
            var blobs = new List<Transform>();
            for (int i = 0; i < 4; i++)
            {
                var b = B.MeshObj("Blob", bt, LowSphere(), _mJuice[f], pStart.localPosition, Vector3.one * 0.09f, null, false);
                blobs.Add(b.transform);
            }
            B.MeshObj("SpoutTip", bt, B.Cylinder, _mChrome, new Vector3(0f, 0.95f, -0.96f), new Vector3(0.13f, 0.07f, 0.13f));
            var spout = B.Node("SpoutPoint", bt, new Vector3(0f, 0.85f, -0.96f));

            // Stainless tray (output) and slatted crate (input), as on the other juicers.
            var tray = B.Node("Tray", t, new Vector3(0f, 0f, -1.62f));
            var trt = tray.transform;
            RB("Top", trt, _mSteel, new Vector3(0f, 0.5f, 0f), new Vector3(1.55f, 0.07f, 1f), 0.1f);
            B.Box("RailF", trt, _mChrome, new Vector3(0f, 0.56f, -0.48f), new Vector3(1.5f, 0.05f, 0.04f), null, false);
            B.Box("RailB", trt, _mChrome, new Vector3(0f, 0.56f, 0.48f), new Vector3(1.5f, 0.05f, 0.04f), null, false);
            foreach (var lx in new[] { -0.66f, 0.66f })
            foreach (var lz in new[] { -0.4f, 0.4f })
                B.Cyl("Leg", trt, _mPanelDark, new Vector3(lx, 0.235f, lz), 0.07f, 0.47f);
            var outPile = B.Node("OutputPile", trt, new Vector3(0f, 0.54f, 0f)).AddComponent<ItemPile>();
            outPile.columns = 4;
            outPile.rows = 2;
            outPile.layers = 3;
            outPile.spacing = new Vector2(0.34f, 0.4f);
            outPile.layerHeight = 0.37f;

            var crate = B.Node("Crate", t, new Vector3(0f, 0f, 1.45f));
            var ct = crate.transform;
            B.Box("Floor", ct, _mWood, new Vector3(0f, 0.08f, 0f), new Vector3(1.12f, 0.16f, 1.02f));
            var slat = MatLib.Lit("CrateSlat", new Color(0.86f, 0.62f, 0.38f), 0.1f);
            for (int i = 0; i < 2; i++)
            {
                float y = 0.24f + i * 0.17f;
                B.Box("SlatL", ct, slat, new Vector3(-0.54f, y, 0f), new Vector3(0.05f, 0.11f, 1.02f));
                B.Box("SlatR", ct, slat, new Vector3(0.54f, y, 0f), new Vector3(0.05f, 0.11f, 1.02f));
                B.Box("SlatB", ct, slat, new Vector3(0f, y, 0.49f), new Vector3(1.12f, 0.11f, 0.05f));
                B.Box("SlatF", ct, slat, new Vector3(0f, y, -0.49f), new Vector3(1.12f, 0.11f, 0.05f));
            }
            var inPile = B.Node("InputPile", t, new Vector3(0f, 0.36f, 1.45f)).AddComponent<ItemPile>();
            inPile.columns = 2;
            inPile.rows = 2;
            inPile.layers = 6;
            inPile.spacing = new Vector2(0.42f, 0.42f);
            inPile.layerHeight = 0.15f;

            // Round berry sign on a post, with a golden crown that bounces on every cup.
            var sign = B.Node("Sign", t, new Vector3(0.78f, 0f, 0.55f)).transform;
            B.Cyl("Post", sign, _mChrome, new Vector3(0f, 1.9f, 0.07f), 0.07f, 1.1f);
            B.MeshObj("Disc", sign, _disc, new[] { _mWhite, shell }, new Vector3(0f, 2.72f, 0f), new Vector3(0.78f, 0.07f, 0.78f), new Vector3(-90f, 0f, 0f));
            B.Sprite("Icon", sign, _sJuiceIcons[f], new Vector3(0f, 2.72f, -0.085f), SpriteScale(_sJuiceIcons[f], 0.56f), false, 2).transform.localRotation = Quaternion.identity;
            var crown = B.Node("Crown", sign, new Vector3(0f, 3.16f, 0f)).transform;
            B.MeshObj("Crown", crown, _crown, _mGold, Vector3.zero, Vector3.one * 0.3f, null, false);

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1.2f, -0.1f);
            col.size = new Vector3(2f, 2.5f, 4.1f);
            var hum = root.AddComponent<AudioSource>();
            hum.playOnAwake = true;
            hum.loop = true;

            var j = root.AddComponent<BerryPress>();
            j.kind = (FruitKind)f;
            j.inputPile = inPile;
            j.outputPile = outPile;
            j.intakePoint = intake.transform;
            j.spoutPoint = spout.transform;
            j.body = bt;
            j.hum = hum;
            j.hopper = ct;
            j.statusLight = led.GetComponent<Renderer>();
            j.ledOn = _mLedOn;
            j.ledOff = _mLedOff;
            j.sign = sign;
            j.drum = drum;
            j.drumBerries = drumBerries.ToArray();
            j.piston = piston;
            j.pipeBlobs = blobs.ToArray();
            j.pipeStart = pStart;
            j.pipeEnd = pEnd;
            j.gaugeNeedle = needle;
            j.ringLights = ring.ToArray();
            j.ringOn = _bRingOn;
            j.ringOff = _bRingOff;
            j.crown = crown;

            var dz = MakeZone<DropZone>("InputZone", t, pos + new Vector3(0f, 0f, 3.0f), new Vector2(2.3f, 1.5f), _mPadIn, _sFruit[f]);
            dz.receiverBehaviour = j;
            var pz = MakeZone<PickupZone>("OutputZone", t, pos + new Vector3(0f, 0f, -3.0f), new Vector2(2.3f, 1.4f), _mPadOut, _sJuiceIcons[f]);
            pz.sourceBehaviour = j;
            j.inputZone = dz;
            j.outputZone = pz;
            return j;
        }

        // ================================================================== cake mixer + conveyor, oven

        /// <summary>Stand mixer on a prep table (input pad south), with a conveyor carrying batter tins north to the oven.</summary>
        static CakeMixer BuildCakeMixer(int f, Vector3 pos)
        {
            var root = B.Node("CakeMixer_" + FruitKey[f], _stations, pos);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero).transform;
            var paint = MatLib.Lit("MixerPaint_" + FruitKey[f], Color.Lerp(Balance.FruitColors[f], Color.white, 0.45f), 0.55f);
            var batterMat = MatLib.Lit("Batter_" + FruitKey[f], Color.Lerp(Balance.JuiceColors[f], new Color(1f, 0.95f, 0.88f), 0.55f), 0.35f);

            RB("Table", body, _mSteel, new Vector3(0f, 0.85f, 0f), new Vector3(1.9f, 0.1f, 1.3f), 0.08f);
            RB("Shelf", body, _mSteel, new Vector3(0f, 0.3f, 0f), new Vector3(1.8f, 0.05f, 1.2f), 0.05f, false);
            foreach (var lx in new[] { -0.85f, 0.85f })
            foreach (var lz in new[] { -0.55f, 0.55f })
                B.Cyl("Leg", body, _mPanelDark, new Vector3(lx, 0.42f, lz), 0.08f, 0.84f);

            RB("Base", body, paint, new Vector3(0f, 1.0f, 0.1f), new Vector3(0.82f, 0.22f, 0.92f), 0.1f);
            RB("Column", body, paint, new Vector3(0f, 1.45f, 0.44f), new Vector3(0.34f, 0.9f, 0.3f), 0.12f);
            var head = B.Node("Head", body, new Vector3(0f, 1.95f, 0.12f)).transform;
            RB("HeadShell", head, paint, Vector3.zero, new Vector3(0.48f, 0.38f, 0.98f), 0.17f);
            RB("HeadBand", head, _mChrome, new Vector3(0f, 0f, 0f), new Vector3(0.5f, 0.07f, 1f), 0.03f, false);
            var whisk = B.Node("Whisk", head, new Vector3(0f, -0.22f, -0.14f)).transform;
            B.Cyl("Shaft", whisk, _mChrome, new Vector3(0f, -0.1f, 0f), 0.05f, 0.2f);
            for (int i = 0; i < 3; i++)
                B.MeshObj("Wire", whisk, LowSphere(), _mChrome, new Vector3(0f, -0.3f, 0f), new Vector3(0.2f, 0.34f, 0.03f), new Vector3(0f, i * 60f, 0f), false);
            var bowl = B.Node("Bowl", body, new Vector3(0f, 1.1f, -0.02f)).transform;
            B.MeshObj("BowlShell", bowl, _cup, _mChrome, Vector3.zero, new Vector3(0.72f, 0.42f, 0.72f));
            var batter = B.MeshObj("Batter", bowl, _disc, new[] { batterMat, batterMat }, new Vector3(0f, 0.05f, 0f), new Vector3(0.62f, 0.3f, 0.62f), null, false).transform;

            var sack = RB("Sack", body, MatLib.Lit("FlourSack", new Color(0.97f, 0.95f, 0.88f), 0.1f), new Vector3(-0.66f, 1.14f, 0.28f), new Vector3(0.36f, 0.48f, 0.32f), 0.13f).transform;
            var flour = B.Text("Flour", sack, "FLOUR", 1.4f, new Color(0.5f, 0.35f, 0.25f), new Vector3(0f, 0.02f, -0.17f), false);
            flour.rectTransform.sizeDelta = new Vector2(0.34f, 0.12f);
            var basket = B.Node("Basket", body, new Vector3(0.66f, 0.9f, 0.3f)).transform;
            B.MeshObj("Punnet", basket, _cup, _mPunnet, Vector3.zero, new Vector3(0.36f, 0.14f, 0.36f));
            for (int i = 0; i < 4; i++) Berry(basket, f, new Vector3(-0.07f + (i % 2) * 0.14f, 0.15f, -0.07f + (i / 2) * 0.14f), 0.12f, i * 70f);
            var led = B.MeshObj("Led", body, LowSphere(), _mLedOff, new Vector3(0f, 1.02f, -0.36f), new Vector3(0.09f, 0.09f, 0.05f), null, false);
            var intake = B.Node("Intake", body, new Vector3(0f, 1.6f, -0.02f));
            var outPoint = B.Node("OutPoint", body, new Vector3(0f, 1.05f, 0.62f));

            // Input crate on the south side, facing the farm and the camera.
            var crate = B.Node("Crate", t, new Vector3(0f, 0f, -1.12f)).transform;
            B.Box("Floor", crate, _mWood, new Vector3(0f, 0.06f, 0f), new Vector3(1f, 0.12f, 0.8f));
            var slat = MatLib.Lit("CrateSlat", new Color(0.86f, 0.62f, 0.38f), 0.1f);
            foreach (var sx in new[] { -0.48f, 0.48f }) B.Box("SlatSide", crate, slat, new Vector3(sx, 0.2f, 0f), new Vector3(0.05f, 0.16f, 0.8f));
            foreach (var sz in new[] { -0.38f, 0.38f }) B.Box("SlatEnd", crate, slat, new Vector3(0f, 0.2f, sz), new Vector3(1f, 0.16f, 0.05f));
            var inPile = B.Node("InputPile", t, new Vector3(0f, 0.3f, -1.12f)).AddComponent<ItemPile>();
            inPile.columns = 2;
            inPile.rows = 2;
            inPile.layers = 6;
            inPile.spacing = new Vector2(0.42f, 0.36f);
            inPile.layerHeight = 0.15f;

            // Conveyor to the oven: frame, scrolling belt, rails and rollers.
            var conv = B.Node("Conveyor", t, new Vector3(0f, 0f, 1.85f)).transform;
            RB("Frame", conv, _mPanelDark, new Vector3(0f, 0.74f, 0f), new Vector3(0.86f, 0.12f, 2.3f), 0.05f);
            var belt = B.MeshObj("Belt", conv, B.Quad, _bBelt, new Vector3(0f, 0.805f, 0f), new Vector3(0.7f, 2.24f, 1f), new Vector3(90f, 0f, 0f), false);
            foreach (var sx in new[] { -0.42f, 0.42f })
                B.Box("Rail", conv, _mChrome, new Vector3(sx, 0.86f, 0f), new Vector3(0.04f, 0.07f, 2.3f), null, false);
            foreach (var lz in new[] { -0.9f, 0.9f })
            foreach (var sx in new[] { -0.36f, 0.36f })
                B.Cyl("Leg", conv, _mPanelDark, new Vector3(sx, 0.34f, lz), 0.06f, 0.68f);
            var rollers = new List<Transform>();
            foreach (var z in new[] { -1.1f, 1.1f })
            {
                var r = B.Node("Roller", conv, new Vector3(0f, 0.76f, z)).transform;
                B.MeshObj("Drum", r, B.Cylinder, _mChrome, Vector3.zero, new Vector3(0.14f, 0.38f, 0.14f), new Vector3(0f, 0f, 90f), false);
                rollers.Add(r);
            }
            var tins = B.Node("Tins", conv, new Vector3(0f, 0.83f, 0f)).AddComponent<ItemPile>();
            tins.columns = 1;
            tins.rows = 3;
            tins.layers = 1;
            tins.spacing = new Vector2(0.4f, 0.62f);
            tins.layerHeight = 0.2f;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1f, 0.2f);
            col.size = new Vector3(2f, 2f, 1.5f);
            var cc = B.Node("ConveyorCol", t, new Vector3(0f, 0.5f, 1.85f)).AddComponent<BoxCollider>();
            cc.size = new Vector3(0.9f, 1f, 2.3f);
            var hum = root.AddComponent<AudioSource>();
            hum.playOnAwake = true;
            hum.loop = true;

            var m = root.AddComponent<CakeMixer>();
            m.kind = (FruitKind)f;
            m.inputPile = inPile;
            m.outputPile = tins;
            m.intakePoint = intake.transform;
            m.outPoint = outPoint.transform;
            m.hum = hum;
            m.bowl = bowl;
            m.whisk = whisk;
            m.head = head;
            m.batter = batter;
            m.flourSack = sack;
            m.rollers = rollers.ToArray();
            m.belt = belt.GetComponent<Renderer>();
            m.statusLight = led.GetComponent<Renderer>();
            m.ledOn = _mLedOn;
            m.ledOff = _mLedOff;
            var dz = MakeZone<DropZone>("InputZone", t, pos + new Vector3(0f, 0f, -2.25f), new Vector2(2.3f, 1.5f), _mPadIn, _sFruit[f]);
            dz.receiverBehaviour = m;
            m.inputZone = dz;
            return m;
        }

        /// <summary>Brick oven at the end of the conveyor: glowing window, timer bar, dial, chimney; cakes slide out east onto a rack.</summary>
        static Oven BuildOven(int f, Vector3 pos, CakeMixer feeder)
        {
            var root = B.Node("Oven_" + FruitKey[f], _stations, pos);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero).transform;
            RB("Base", body, _mPanelDark, new Vector3(0f, 0.15f, 0f), new Vector3(2.1f, 0.3f, 1.7f), 0.2f);
            RB("Shell", body, _bBrick, new Vector3(0f, 1.05f, 0f), new Vector3(2f, 1.5f, 1.6f), 0.36f);
            RB("Top", body, _bCopper, new Vector3(0f, 1.84f, 0f), new Vector3(2.05f, 0.1f, 1.65f), 0.3f, false);
            RB("Mouth", body, _mPanelDark, new Vector3(0f, 0.95f, -0.79f), new Vector3(1.24f, 0.84f, 0.05f), 0.22f, false);
            RB("Frame", body, _bCopper, new Vector3(0f, 1.02f, -0.8f), new Vector3(1.16f, 0.72f, 0.04f), 0.18f, false);
            var glow = RB("Window", body, _bGlowOff, new Vector3(0f, 1.02f, -0.83f), new Vector3(0.96f, 0.54f, 0.03f), 0.14f, false);
            var fruitSign = B.Sprite("Badge", body, _sCakeIcons[f], new Vector3(0f, 1.62f, -0.83f), SpriteScale(_sCakeIcons[f], 0.36f), false, 2);
            fruitSign.transform.localRotation = Quaternion.identity;
            B.Box("TimerBg", body, _mPanelDark, new Vector3(0f, 1.4f, -0.815f), new Vector3(1f, 0.09f, 0.02f), null, false);
            var timer = B.Node("TimerFill", body, new Vector3(-0.48f, 1.4f, -0.83f)).transform;
            B.Box("Fill", timer, _bRingOn, new Vector3(0.48f, 0f, 0f), new Vector3(0.96f, 0.06f, 0.02f), null, false);
            var dialFace = B.MeshObj("DialFace", body, _disc, new[] { _mWhite, _bCopper }, new Vector3(0.78f, 1.3f, -0.8f), new Vector3(0.24f, 0.03f, 0.24f), new Vector3(-90f, 0f, 0f), false);
            _ = dialFace;
            var dial = B.Node("Dial", body, new Vector3(0.78f, 1.3f, -0.83f)).transform;
            B.Box("Hand", dial, _mDark, new Vector3(0f, 0.05f, 0f), new Vector3(0.02f, 0.1f, 0.01f), null, false);
            B.Cyl("Chimney", body, _bBrick, new Vector3(-0.6f, 2.2f, 0.45f), 0.34f, 0.8f);
            B.MeshObj("ChimneyCap", body, _disc, new[] { _bCopper, _bCopper }, new Vector3(-0.6f, 2.6f, 0.45f), new Vector3(0.46f, 0.06f, 0.46f), null, false);
            var chimney = B.Node("Smoke", body, new Vector3(-0.6f, 2.7f, 0.45f)).transform;

            // Side door (east), hinged at its bottom edge.
            var hinge = B.Node("DoorHinge", body, new Vector3(1.02f, 0.62f, 0f), new Vector3(0f, 90f, 0f)).transform;
            RB("Door", hinge, _bCopper, new Vector3(0f, 0.36f, 0.02f), new Vector3(0.9f, 0.7f, 0.06f), 0.1f);
            B.Box("Handle", hinge, _mChrome, new Vector3(0f, 0.62f, 0.07f), new Vector3(0.5f, 0.05f, 0.05f), null, false);

            // Cooling rack east of the oven.
            var rack = B.Node("RackTable", t, new Vector3(2.1f, 0f, 0f)).transform;
            RB("Top", rack, _mChrome, new Vector3(0f, 0.84f, 0f), new Vector3(1.1f, 0.05f, 1.1f), 0.05f);
            for (int i = 0; i < 4; i++)
                B.Box("Wire", rack, _mSteel, new Vector3(-0.42f + i * 0.28f, 0.87f, 0f), new Vector3(0.02f, 0.02f, 1.05f), null, false);
            foreach (var lx in new[] { -0.48f, 0.48f })
            foreach (var lz in new[] { -0.48f, 0.48f })
                B.Cyl("Leg", rack, _mPanelDark, new Vector3(lx, 0.42f, lz), 0.06f, 0.84f);
            var pile = B.Node("Rack", t, new Vector3(2.1f, 0.88f, 0f)).AddComponent<ItemPile>();
            pile.columns = 2;
            pile.rows = 2;
            pile.layers = 2;
            pile.spacing = new Vector2(0.44f, 0.44f);
            pile.layerHeight = 0.34f;

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 1f, 0f);
            col.size = new Vector3(2.1f, 2f, 1.7f);
            var rc = B.Node("RackCol", t, new Vector3(2.1f, 0.5f, 0f)).AddComponent<BoxCollider>();
            rc.size = new Vector3(1.1f, 1f, 1.1f);
            var hum = root.AddComponent<AudioSource>();
            hum.playOnAwake = true;
            hum.loop = true;

            var o = root.AddComponent<Oven>();
            o.kind = (FruitKind)f;
            o.feeder = feeder;
            o.outputPile = pile;
            o.intakePoint = B.Node("Inside", body, new Vector3(0f, 0.9f, 0f)).transform;
            o.outPoint = B.Node("Out", body, new Vector3(1.2f, 0.95f, 0f)).transform;
            o.hum = hum;
            o.body = body;
            o.door = hinge;
            o.glow = glow.GetComponent<Renderer>();
            o.glowOff = _bGlowOff;
            o.glowOn = _bGlowOn;
            o.chimney = chimney;
            o.dial = dial;
            o.timerFill = timer;
            var pz = MakeZone<PickupZone>("OutputZone", t, pos + new Vector3(2.1f, 0f, -2.25f), new Vector2(2.2f, 1.5f), _mPadOut, _sCakeIcons[f]);
            pz.sourceBehaviour = o;
            o.outputZone = pz;
            return o;
        }

        // ================================================================== pastry case

        /// <summary>The cake counter: customers come to its north side; the player stocks it from the south.</summary>
        static Counter BuildPastryCase(Vector3 pos)
        {
            var root = B.Node("CakeStand", _stations, pos, new Vector3(0f, 180f, 0f));
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero).transform;
            RB("Base", body, _bCream, new Vector3(0f, 0.5f, 0f), new Vector3(4.4f, 0.95f, 1.15f), 0.16f);
            RB("Kick", body, _bRoofPlum, new Vector3(0f, 0.06f, 0f), new Vector3(4.48f, 0.12f, 1.22f), 0.2f);
            RB("Top", body, _mMarble, new Vector3(0f, 1.03f, 0f), new Vector3(4.72f, 0.1f, 1.42f), 0.22f);
            B.Box("Skirt", body, _bGingham, new Vector3(0f, 0.6f, -0.6f), new Vector3(4.1f, 0.6f, 0.04f));
            var brand = B.Text("Brand", body, "BERRY CAKES", 4.2f, new Color(1f, 0.9f, 0.95f), new Vector3(0f, 0.62f, -0.63f));
            brand.rectTransform.sizeDelta = new Vector2(3.6f, 0.8f);
            var back = B.Text("Back", body, "CAKE SHOP", 3.4f, new Color(0.62f, 0.3f, 0.52f), new Vector3(0f, 0.62f, 0.6f));
            back.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            back.rectTransform.sizeDelta = new Vector2(3.4f, 0.7f);
            foreach (var sx in new[] { -1f, 1f })
            {
                RB("Post", body, _bPink, new Vector3(sx * 2.26f, 0.62f, 0f), new Vector3(0.24f, 1.24f, 1.3f), 0.1f);
                // Glass cake dome on each end.
                B.MeshObj("Plate", body, _disc, new[] { _mWhite, _mWhite }, new Vector3(sx * 1.9f, 1.08f, 0.2f), new Vector3(0.5f, 0.03f, 0.5f), null, false);
                B.MeshObj("DisplayCake", body, _disc, new[] { _mSponge, _mCreamWhite }, new Vector3(sx * 1.9f, 1.11f, 0.2f), new Vector3(0.34f, 0.18f, 0.34f));
                B.MeshObj("Dome", body, LowSphere(), _mGlass, new Vector3(sx * 1.9f, 1.11f, 0.2f), new Vector3(0.46f, 0.56f, 0.46f), null, false);
            }
            // Striped awning on two poles over the case.
            foreach (var sx in new[] { -2.2f, 2.2f })
                B.Cyl("AwningPole", t, _mWhite, new Vector3(sx, 1.4f, 0.5f), 0.08f, 2.8f);
            var aw = B.Node("Awning", t, new Vector3(0f, 2.75f, 0.1f), new Vector3(-18f, 0f, 0f)).transform;
            B.Box("Cloth", aw, _bGingham, Vector3.zero, new Vector3(4.7f, 0.06f, 1.3f));
            for (int i = 0; i < 9; i++)
                B.MeshObj("Scallop", aw, LowSphere(), i % 2 == 0 ? _bFlowerPink : _mWhite, new Vector3(-2.2f + i * 0.55f, -0.04f, -0.66f), new Vector3(0.5f, 0.12f, 0.2f), null, false);

            var display = B.Node("Display", t, new Vector3(0f, 1.08f, 0f)).AddComponent<ItemPile>();
            display.columns = 5;
            display.rows = 2;
            display.layers = 2;
            display.spacing = new Vector2(0.44f, 0.46f);
            display.layerHeight = 0.34f;

            var label = B.Node("Capacity", t, new Vector3(0f, 2.3f, 0.6f));
            label.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", label.transform, _sRound, new Vector3(0f, 0f, 0.02f), 1f, false, 4, new Color(0.1f, 0.08f, 0.12f, 0.65f));
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(1.2f, 0.52f);
            var cap = B.Text("Text", label.transform, "0/20", 4f, Color.white, new Vector3(0f, 0.01f, 0f));

            var col = root.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.size = new Vector3(4.8f, 1.2f, 1.4f);

            var c = root.AddComponent<Counter>();
            c.line = ProductLine.Cake;
            c.display = display;
            c.body = body;
            c.capacityText = cap;
            c.servePoint = B.Node("ServePoint", t, new Vector3(0f, 0f, -1.35f)).transform;
            var dz = MakeZone<DropZone>("StockZone", t, pos + new Vector3(0f, 0f, -1.75f), new Vector2(3.4f, 1.5f), _mPadCounter, _sCakeIcons[7] ? _sCakeIcons[7] : _sJuice);
            dz.receiverBehaviour = c;
            c.dropZone = dz;
            return c;
        }

        // ================================================================== delivery desk + box

        static DeliveryBox BuildDeliveryBox(Transform parent, Vector3 worldPos)
        {
            var root = B.Node("Box", parent, Vector3.zero);
            root.transform.position = worldPos;
            var t = root.transform;
            var vis = B.Node("Visual", t, Vector3.zero).transform;
            RB("Bottom", vis, _bCardboard, new Vector3(0f, 0.03f, 0f), new Vector3(1f, 0.06f, 0.9f), 0.03f);
            RB("WallS", vis, _bCardboard, new Vector3(0f, 0.36f, -0.43f), new Vector3(1f, 0.68f, 0.04f), 0.02f);
            RB("WallN", vis, _bCardboard, new Vector3(0f, 0.36f, 0.43f), new Vector3(1f, 0.68f, 0.04f), 0.02f);
            RB("WallW", vis, _bCardboard, new Vector3(-0.48f, 0.36f, 0f), new Vector3(0.04f, 0.68f, 0.9f), 0.02f);
            RB("WallE", vis, _bCardboard, new Vector3(0.48f, 0.36f, 0f), new Vector3(0.04f, 0.68f, 0.9f), 0.02f);
            var stamp = B.Sprite("Stamp", vis, _sParcel ? _sParcel : _sTruck, new Vector3(0f, 0.38f, -0.46f), SpriteScale(_sParcel ? _sParcel : _sTruck, 0.4f), false, 2);
            _ = stamp;
            var contents = B.Node("Contents", vis, new Vector3(0f, 0.06f, 0f)).transform;
            var fill = B.Box("Goods", contents, MatLib.Lit("B_BoxGoods", Color.white, 0.4f), new Vector3(0f, 0.3f, 0f), new Vector3(0.9f, 0.6f, 0.8f), null, false);
            // Flaps: east/west fold first, then north/south on top.
            Transform Flap(string n, Vector3 hingePos, Vector3 meshPos, Vector3 size)
            {
                var h = B.Node(n, vis, hingePos).transform;
                RB("Flap", h, _bCardboard, meshPos, size, 0.02f, false);
                return h;
            }
            var fe = Flap("FlapE", new Vector3(0.49f, 0.7f, 0f), new Vector3(-0.23f, 0f, 0f), new Vector3(0.46f, 0.025f, 0.88f));
            var fw = Flap("FlapW", new Vector3(-0.49f, 0.7f, 0f), new Vector3(0.23f, 0f, 0f), new Vector3(0.46f, 0.025f, 0.88f));
            var fn = Flap("FlapN", new Vector3(0f, 0.725f, 0.44f), new Vector3(0f, 0f, -0.22f), new Vector3(0.98f, 0.025f, 0.44f));
            var fs = Flap("FlapS", new Vector3(0f, 0.725f, -0.44f), new Vector3(0f, 0f, 0.22f), new Vector3(0.98f, 0.025f, 0.44f));
            var tape = B.Box("Tape", vis, _bTape, new Vector3(0f, 0.745f, 0f), new Vector3(0.16f, 0.012f, 0.96f), null, false);
            var intake = B.Node("Intake", t, new Vector3(0f, 0.8f, 0f)).transform;

            var label = B.Node("Label", t, new Vector3(0f, 1.55f, 0f));
            label.AddComponent<Billboard>();
            var bg = B.Sprite("Bg", label.transform, _sRound, new Vector3(0f, 0f, 0.02f), 1f, false, 6, new Color(0.1f, 0.08f, 0.12f, 0.72f));
            bg.drawMode = SpriteDrawMode.Sliced;
            bg.size = new Vector2(1.7f, 0.6f);
            var icon = B.Sprite("Icon", label.transform, _sJuiceIcons[7], new Vector3(-0.5f, 0.01f, 0f), SpriteScale(_sJuiceIcons[7], 0.46f), false, 7);
            var count = B.Text("Count", label.transform, "0/10", 4.2f, Color.white, new Vector3(0.22f, 0.01f, 0f));
            count.rectTransform.sizeDelta = new Vector2(1.1f, 0.5f);
            count.enableAutoSizing = true;
            count.fontSizeMin = 2f;
            count.fontSizeMax = 4.2f;
            count.GetComponent<MeshRenderer>().sortingOrder = 8;

            var bc = root.AddComponent<BoxCollider>();
            bc.center = new Vector3(0f, 0.4f, 0f);
            bc.size = new Vector3(1f, 0.8f, 0.9f);

            var box = root.AddComponent<DeliveryBox>();
            box.visual = vis;
            box.flaps = new[] { fe, fw, fn, fs };
            box.flapOpenEuler = new[] { new Vector3(0f, 0f, -115f), new Vector3(0f, 0f, 115f), new Vector3(115f, 0f, 0f), new Vector3(-115f, 0f, 0f) };
            box.contents = contents;
            box.contentsRenderer = fill.GetComponent<Renderer>();
            box.tape = tape;
            box.intake = intake;
            box.countText = count;
            box.icon = icon;
            box.label = label.transform;
            return box;
        }

        /// <summary>
        /// One delivery desk: a lay-by where the truck stops, the loading pad with the shipping box, a little order desk
        /// with an awning and the order board. Returns the bay (not yet wired to its manager).
        /// </summary>
        static DeliveryBay BuildBerryDesk(Transform root, string name, Vector3 park, Transform parkT, Transform[] arrive, Transform[] depart, Material padMat,
            DeliveryTruck[] trucks, out GameObject revealRoot, out DeliveryZone loadZone)
        {
            var go = B.Node(name, root, Vector3.zero);
            revealRoot = go;
            var t = go.transform;
            // The pad sits clear of the truck's swinging cargo door, so the door never hides the box or its label.
            var padPos = park + new Vector3(-4f, 0f, -0.8f);
            loadZone = MakeZone<DeliveryZone>("LoadZone", t, padPos, new Vector2(2.8f, 2.6f), padMat, null);
            var box = BuildDeliveryBox(t, padPos + new Vector3(0.35f, 0.07f, 0.35f));

            // Order desk with an awning south-west of the pad (below the board, so its awning never covers it).
            var desk = B.Node("OrderDesk", t, park + new Vector3(-6.6f, 0f, -2.2f)).transform;
            RB("Desk", desk, _mWood, new Vector3(0f, 0.48f, 0f), new Vector3(1.8f, 0.96f, 0.8f), 0.1f);
            RB("DeskTop", desk, _bCream, new Vector3(0f, 0.99f, 0f), new Vector3(1.9f, 0.06f, 0.9f), 0.05f);
            B.MeshObj("Bell", desk, LowSphere(), _mGold, new Vector3(0.55f, 1.06f, -0.1f), new Vector3(0.16f, 0.12f, 0.16f));
            B.Box("Clipboard", desk, _bButter, new Vector3(-0.3f, 1.03f, 0f), new Vector3(0.36f, 0.02f, 0.48f), new Vector3(0f, 12f, 0f), false);
            foreach (var sx in new[] { -0.85f, 0.85f })
                B.Cyl("Pole", desk, _mWhite, new Vector3(sx, 1.25f, 0.35f), 0.07f, 2.5f);
            var aw = B.Node("Awning", desk, new Vector3(0f, 2.45f, 0.05f), new Vector3(-15f, 0f, 0f)).transform;
            B.Box("Cloth", aw, _bGingham, Vector3.zero, new Vector3(2.1f, 0.05f, 1.1f));
            var board = BuildOrderBoard(t, park + new Vector3(-5.2f, 0f, 2.6f));
            // Bay markings on the lay-by.
            var lineMat = MatLib.Lit("BayLine", new Color(1f, 0.95f, 0.85f), 0.1f);
            foreach (var sx in new[] { -1.5f, 1.5f })
                B.Box("BayLine", t, lineMat, park + new Vector3(sx, 0.05f, 0f), new Vector3(0.14f, 0.02f, 6f), null, false);
            B.Prop("Survival/barrel", t, park + new Vector3(-7.6f, 0f, 0.4f), 2f, 20f);
            B.Prop("Survival/box", t, park + new Vector3(-7.9f, 0f, 1.5f), 2.4f, 10f);

            var bay = go.AddComponent<DeliveryBay>();
            bay.arrivePath = arrive;
            bay.park = parkT;
            bay.departPath = depart;
            bay.loadZone = loadZone;
            bay.trucks = trucks;
            bay.board = board;
            bay.box = box;
            return bay;
        }

        // ================================================================== trucks (Berry Blast)

        /// <summary>
        /// Chunkier, friendlier trucks: rounded cab with a snub nose, headlight "eyes" in chrome rims and a smiling grille,
        /// fenders, mirrors, tail lights that glow when braking, a painted berry on the cargo box and a roof mascot per type.
        /// </summary>
        static DeliveryTruck BuildBerryTruck(TruckKind kind, Transform parent, string suffix)
        {
            var root = B.Node("BerryTruck_" + kind + suffix, parent, Vector3.zero);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero).transform;
            float len, cabLen, cargoH;
            Material paint, cab, trim;
            string brand;
            Sprite mural;
            switch (kind)
            {
                case TruckKind.Van:
                    len = 4.1f; cabLen = 1.55f; cargoH = 1.7f; brand = "BERRY VAN"; mural = _sFruit[7];
                    paint = MatLib.Lit("BTruck_Van", new Color(1f, 0.55f, 0.66f), 0.55f); cab = MatLib.Lit("BTruck_VanCab", new Color(1f, 0.93f, 0.95f), 0.45f); trim = _mWhite; break;
                case TruckKind.JuiceTruck:
                    len = 5f; cabLen = 1.65f; cargoH = 2.1f; brand = "SMOOTHIE CO."; mural = _sJuiceIcons[9];
                    paint = MatLib.Lit("BTruck_Smoothie", new Color(0.62f, 0.52f, 0.95f), 0.55f); cab = MatLib.Lit("BTruck_SmoothieCab", new Color(0.95f, 0.93f, 1f), 0.45f); trim = _mWhite; break;
                case TruckKind.Resort:
                    len = 5.4f; cabLen = 1.65f; cargoH = 2.2f; brand = "PARTY TIME"; mural = _sCakeIcons[8];
                    paint = MatLib.Lit("BTruck_Party", new Color(0.45f, 0.86f, 0.7f), 0.55f); cab = MatLib.Lit("BTruck_PartyCab", new Color(1f, 0.95f, 0.72f), 0.45f); trim = _bFlowerPink; break;
                default:
                    len = 5.6f; cabLen = 1.75f; cargoH = 2.3f; brand = "ROYAL BERRY"; mural = _sCakeIcons[10];
                    paint = MatLib.Lit("BTruck_Royal", new Color(0.18f, 0.2f, 0.42f), 0.8f, null, null, 0.3f); cab = paint; trim = _mGold; break;
            }
            const float w = 2.3f;
            float cargoLen = len - cabLen - 0.1f;
            float zRear = -len * 0.5f;
            float zFront = len * 0.5f;
            float zCab = zFront - cabLen * 0.5f - 0.2f;
            float zCargo = zRear + cargoLen * 0.5f;

            RB("Chassis", body, _mDark, new Vector3(0f, 0.52f, 0f), new Vector3(w - 0.2f, 0.26f, len), 0.12f);
            // Cab: rounded box with a snub nose (hood) in front.
            RB("Cab", body, cab, new Vector3(0f, 1.35f, zCab), new Vector3(w, 1.45f, cabLen), 0.34f);
            RB("Hood", body, cab, new Vector3(0f, 0.95f, zFront - 0.28f), new Vector3(w - 0.12f, 0.62f, 0.62f), 0.26f);
            RB("Windshield", body, _tTruckGlass, new Vector3(0f, 1.62f, zCab + cabLen * 0.5f + 0.005f), new Vector3(w - 0.36f, 0.58f, 0.06f), 0.12f, false);
            RB("WsFrame", body, trim, new Vector3(0f, 1.62f, zCab + cabLen * 0.5f - 0.01f), new Vector3(w - 0.22f, 0.7f, 0.04f), 0.14f, false);
            foreach (var sx in new[] { -1f, 1f })
            {
                RB("SideWin", body, _tTruckGlass, new Vector3(sx * (w * 0.5f + 0.005f), 1.62f, zCab + 0.12f), new Vector3(0.04f, 0.5f, cabLen * 0.5f), 0.1f, false);
                // Mirror on a little arm.
                B.Box("MirrorArm", body, _mDark, new Vector3(sx * (w * 0.5f + 0.12f), 1.55f, zCab + cabLen * 0.42f), new Vector3(0.22f, 0.04f, 0.04f), null, false);
                RB("Mirror", body, trim, new Vector3(sx * (w * 0.5f + 0.24f), 1.62f, zCab + cabLen * 0.42f), new Vector3(0.08f, 0.26f, 0.16f), 0.03f, false);
            }
            // Face: headlight eyes in chrome rims, grille and a smiling chrome bar.
            var lights = new List<Renderer>();
            foreach (var sx in new[] { -0.62f, 0.62f })
            {
                B.MeshObj("LightRim", body, _disc, new[] { _mChrome, _mChrome }, new Vector3(sx, 1.02f, zFront + 0.02f), new Vector3(0.44f, 0.05f, 0.44f), new Vector3(90f, 0f, 0f), false);
                lights.Add(B.MeshObj("Headlight", body, LowSphere(), _tHeadOff, new Vector3(sx, 1.02f, zFront + 0.05f), new Vector3(0.34f, 0.34f, 0.12f), null, false).GetComponent<Renderer>());
            }
            RB("Grille", body, _bGrille, new Vector3(0f, 0.8f, zFront + 0.02f), new Vector3(0.8f, 0.22f, 0.05f), 0.08f, false);
            for (int i = 0; i < 5; i++)
            {
                float a = (i - 2) * 0.28f;
                B.Box("Smile", body, _mChrome, new Vector3(Mathf.Sin(a) * 0.5f, 0.66f + (1f - Mathf.Cos(a)) * 0.22f, zFront + 0.05f), new Vector3(0.2f, 0.04f, 0.03f), new Vector3(0f, 0f, a * Mathf.Rad2Deg), false);
            }
            RB("Bumper", body, _mChrome, new Vector3(0f, 0.5f, zFront + 0.06f), new Vector3(w, 0.2f, 0.16f), 0.08f);
            RB("RearBumper", body, _mChrome, new Vector3(0f, 0.5f, zRear - 0.06f), new Vector3(w, 0.18f, 0.14f), 0.08f);

            // Cargo box with panel lines, brand and a painted mural on the camera-facing side.
            RB("Cargo", body, paint, new Vector3(0f, 0.7f + cargoH * 0.5f, zCargo), new Vector3(w + 0.05f, cargoH, cargoLen), 0.2f);
            RB("Stripe", body, trim, new Vector3(0f, 0.92f, zCargo), new Vector3(w + 0.1f, 0.16f, cargoLen - 0.1f), 0.2f, false);
            RB("RoofTrim", body, trim, new Vector3(0f, 0.7f + cargoH + 0.04f, zCargo), new Vector3(w - 0.1f, 0.08f, cargoLen - 0.2f), 0.2f, false);
            // Cargo door (left side): brand and mural on that side are painted on the door itself, so they swing with it
            // instead of sitting inside the door's thickness (which z-fought).
            float doorLen = Mathf.Min(1.8f, cargoLen - 0.5f);
            float doorZ = zCargo + doorLen * 0.5f;
            var hinge = B.Node("DoorHinge", body, new Vector3(-w * 0.5f - 0.04f, 0.7f + cargoH * 0.5f, doorZ)).transform;
            foreach (var sx in new[] { -1f, 1f })
            {
                bool onDoor = sx < 0f;
                var parentT = onDoor ? hinge : body;
                float faceX = onDoor ? -0.02f - 0.03f - 0.012f : w * 0.5f + 0.025f + 0.03f;
                Vector3 Local(float y, float z) => onDoor ? new Vector3(faceX, y - (0.7f + cargoH * 0.5f), z - doorZ) : new Vector3(faceX, y, z);
                float panelLen = onDoor ? doorLen - 0.25f : cargoLen - 0.3f;
                var logo = B.Text("Brand", parentT, brand, 3.4f, Color.white, Local(0.7f + cargoH * 0.3f, zCargo));
                logo.transform.localRotation = Quaternion.Euler(0f, sx > 0 ? -90f : 90f, 0f);
                logo.rectTransform.sizeDelta = new Vector2(panelLen, 0.5f);
                logo.enableAutoSizing = true;
                logo.fontSizeMin = 1.5f;
                logo.fontSizeMax = 3.4f;
                if (mural != null)
                {
                    float size = Mathf.Min(1.2f, cargoH * 0.5f, panelLen * 0.8f);
                    var m = B.Sprite("Mural", parentT, mural, Local(0.7f + cargoH * 0.64f, zCargo), SpriteScale(mural, size), false, 2);
                    m.transform.localRotation = Quaternion.Euler(0f, sx > 0 ? -90f : 90f, 0f);
                }
            }
            // Tail lights (glow when braking) and exhaust.
            var brakes = new List<Renderer>();
            foreach (var sx in new[] { -0.85f, 0.85f })
                brakes.Add(RB("TailLight", body, _bBrakeOff, new Vector3(sx, 0.9f, zRear - 0.02f), new Vector3(0.28f, 0.2f, 0.06f), 0.05f, false).GetComponent<Renderer>());
            var exhaust = B.Node("Exhaust", body, new Vector3(0.8f, 0.42f, zRear - 0.12f)).transform;
            B.MeshObj("Pipe", body, B.Cylinder, _mSteel, new Vector3(0.8f, 0.42f, zRear), new Vector3(0.12f, 0.12f, 0.12f), new Vector3(90f, 0f, 0f), false);

            // Wheels with fenders, hubcaps and a coloured centre cap.
            var wheels = new List<Transform>();
            var capMat = MatLib.Lit("BTruck_Cap_" + kind, Color.Lerp(paint.GetColor("_BaseColor"), Color.white, 0.2f), 0.5f);
            foreach (var wz in new[] { zFront - 0.75f, zRear + 0.95f })
            foreach (var sx in new[] { -1f, 1f })
            {
                RB("Fender", body, _mDark, new Vector3(sx * (w * 0.5f - 0.05f), 0.9f, wz), new Vector3(0.22f, 0.16f, 1.02f), 0.06f, false);
                var wheel = B.Node("Wheel", t, new Vector3(sx * (w * 0.5f - 0.12f), 0.42f, wz)).transform;
                B.MeshObj("Tyre", wheel, B.Cylinder, _tTyre, Vector3.zero, new Vector3(0.84f, 0.16f, 0.84f), new Vector3(0f, 0f, 90f));
                // Tyre faces are at +-0.16: the hub stands 2.5 cm proud of it and the cap 2 cm proud of the hub (no coplanar faces).
                B.MeshObj("Hub", wheel, B.Cylinder, _mChrome, new Vector3(sx * 0.155f, 0f, 0f), new Vector3(0.5f, 0.03f, 0.5f), new Vector3(0f, 0f, 90f), false);
                B.MeshObj("Cap", wheel, B.Cylinder, capMat, new Vector3(sx * 0.19f, 0f, 0f), new Vector3(0.2f, 0.02f, 0.2f), new Vector3(0f, 0f, 90f), false);
                wheels.Add(wheel);
            }

            // The door itself, the load inside and the drop point.
            RB("Door", hinge, trim == _mGold ? _mGold : _mWhite, new Vector3(-0.02f, 0f, -doorLen * 0.5f), new Vector3(0.06f, cargoH - 0.25f, doorLen), 0.05f);
            B.Box("Hold", body, _mDark, new Vector3(-w * 0.5f + 0.01f, 0.7f + cargoH * 0.5f, doorZ - doorLen * 0.5f), new Vector3(0.02f, cargoH - 0.3f, doorLen - 0.1f), null, false);
            var fill = B.Node("CargoFill", body, new Vector3(-w * 0.5f + 0.06f, 0.78f, doorZ - doorLen * 0.5f)).transform;
            B.Box("Parcels", fill, _bCardboard, new Vector3(0.02f, (cargoH - 0.3f) * 0.5f, 0f), new Vector3(0.04f, cargoH - 0.3f, doorLen - 0.2f), null, false);
            var drop = B.Node("DropPoint", body, new Vector3(-w * 0.5f + 0.5f, 0.7f + cargoH * 0.45f, doorZ - doorLen * 0.5f)).transform;

            // Roof mascot.
            float roofY = 0.7f + cargoH + 0.08f;
            switch (kind)
            {
                case TruckKind.Van:
                {
                    var mascot = B.Node("Mascot", body, new Vector3(0f, roofY + 0.4f, zCargo)).transform;
                    Berry(mascot, 7, Vector3.zero, 0.9f, 180f);
                    break;
                }
                case TruckKind.JuiceTruck:
                    B.MeshObj("RoofCup", body, _cup, _mJuice[9], new Vector3(0f, roofY, zCargo), new Vector3(1f, 1.1f, 1f));
                    B.MeshObj("RoofCream", body, LowSphere(), _mCreamWhite, new Vector3(0f, roofY + 1.12f, zCargo), new Vector3(1f, 0.4f, 1f));
                    B.MeshObj("RoofStraw", body, B.Cylinder, _mPink, new Vector3(0.22f, roofY + 1.55f, zCargo), new Vector3(0.14f, 0.5f, 0.14f), new Vector3(0f, 0f, -14f));
                    break;
                case TruckKind.Resort:
                    Material[] balloon = { _bFlowerPink, _bButter, _bSky };
                    for (int i = 0; i < 3; i++)
                    {
                        var bp = new Vector3(-0.5f + i * 0.5f, roofY + 1.1f + (i % 2) * 0.25f, zCargo - 0.4f + i * 0.4f);
                        B.MeshObj("Balloon", body, LowSphere(), balloon[i], bp, new Vector3(0.5f, 0.6f, 0.5f), null, false);
                        TubeBetween("String", body, bp + Vector3.down * 0.3f, new Vector3(bp.x * 0.5f, roofY, bp.z), 0.02f, _mWhite);
                    }
                    break;
                default:
                    B.MeshObj("Crown", body, _crown, _mGold, new Vector3(0f, roofY, zCargo), Vector3.one * 0.9f);
                    RB("GoldBand", body, _mGold, new Vector3(0f, 0.7f + cargoH * 0.82f, zCargo), new Vector3(w + 0.08f, 0.1f, cargoLen - 0.1f), 0.2f, false);
                    break;
            }

            var src = root.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = true;
            src.spatialBlend = 0f;
            var truck = root.AddComponent<DeliveryTruck>();
            truck.kind = kind;
            truck.body = body;
            truck.wheels = wheels.ToArray();
            truck.wheelRadius = 0.42f;
            truck.cargoFill = fill;
            truck.door = hinge;
            truck.doorOpenEuler = new Vector3(0f, 105f, 0f);
            truck.dropPoint = drop;
            truck.exhaust = exhaust;
            truck.lights = lights.ToArray();
            truck.lightOn = _tHeadOn;
            truck.lightOff = _tHeadOff;
            truck.brakeLights = brakes.ToArray();
            truck.brakeOn = _bBrakeOn;
            truck.brakeOff = _bBrakeOff;
            truck.engine = src;
            truck.maxSpeed = kind == TruckKind.Premium ? 6.5f : 7.5f;
            B.Decal("Blob", t, _mBlob, new Vector3(0f, 0.06f, 0f), new Vector2(w + 0.8f, len + 0.8f)).GetComponent<MeshRenderer>().sortingOrder = -4;
            root.SetActive(false);
            return truck;
        }

        // ================================================================== the fox

        static FoxActor BuildFox(Transform parent)
        {
            var root = B.Node("Fox", parent, Vector3.zero, null, Vector3.one * 1.6f);
            var t = root.transform;
            var sphere = LowSphere();
            var cone = _crownCone();
            // Body: full chest, slimmer hips, cream belly and a fluffy chest ruff.
            var body = B.Node("Body", t, new Vector3(0f, 0.44f, 0f)).transform;
            B.MeshObj("Torso", body, sphere, _bFoxOrange, Vector3.zero, new Vector3(0.35f, 0.32f, 0.76f));
            B.MeshObj("Chest", body, sphere, _bFoxOrange, new Vector3(0f, 0.03f, 0.22f), new Vector3(0.37f, 0.36f, 0.34f));
            B.MeshObj("Belly", body, sphere, _bFoxWhite, new Vector3(0f, -0.1f, 0.04f), new Vector3(0.24f, 0.16f, 0.5f), null, false);
            B.MeshObj("Ruff", body, sphere, _bFoxWhite, new Vector3(0f, -0.02f, 0.33f), new Vector3(0.28f, 0.3f, 0.2f), null, false);
            B.MeshObj("RuffTuft", body, cone, _bFoxWhite, new Vector3(0f, -0.12f, 0.37f), new Vector3(0.11f, 0.09f, 0.07f), new Vector3(180f, 0f, 0f), false);

            // Head: round skull, cheek tufts, a tapered two-tone snout, big ears with dark tips and inner ears, sly brows.
            var head = B.Node("Head", body, new Vector3(0f, 0.2f, 0.44f)).transform;
            B.MeshObj("Skull", head, sphere, _bFoxOrange, Vector3.zero, new Vector3(0.34f, 0.28f, 0.3f));
            B.MeshObj("Cheeks", head, sphere, _bFoxWhite, new Vector3(0f, -0.07f, 0.07f), new Vector3(0.3f, 0.15f, 0.22f), null, false);
            B.MeshObj("Snout", head, cone, _bFoxOrange, new Vector3(0f, -0.02f, 0.08f), new Vector3(0.17f, 0.28f, 0.13f), new Vector3(90f, 0f, 0f), false);
            B.MeshObj("Jaw", head, cone, _bFoxWhite, new Vector3(0f, -0.06f, 0.08f), new Vector3(0.13f, 0.24f, 0.08f), new Vector3(90f, 0f, 0f), false);
            B.MeshObj("Nose", head, sphere, _bFoxDark, new Vector3(0f, -0.018f, 0.355f), new Vector3(0.065f, 0.05f, 0.05f), null, false);
            foreach (var sx in new[] { -1f, 1f })
            {
                B.MeshObj("Tuft", head, cone, _bFoxWhite, new Vector3(sx * 0.13f, -0.07f, 0.02f), new Vector3(0.08f, 0.1f, 0.07f), new Vector3(0f, 0f, -sx * 115f), false);
                B.MeshObj("Eye", head, sphere, _bFoxDark, new Vector3(sx * 0.085f, 0.04f, 0.135f), new Vector3(0.06f, 0.068f, 0.04f), null, false);
                B.MeshObj("Glint", head, sphere, _mWhite, new Vector3(sx * 0.075f, 0.058f, 0.153f), Vector3.one * 0.02f, null, false);
                B.Box("Brow", head, _bFoxDark, new Vector3(sx * 0.085f, 0.088f, 0.14f), new Vector3(0.085f, 0.018f, 0.02f), new Vector3(0f, 0f, sx * 20f), false);
                var ear = B.Node("Ear", head, new Vector3(sx * 0.1f, 0.1f, -0.03f), new Vector3(-10f, 0f, -sx * 16f)).transform;
                B.MeshObj("Outer", ear, cone, _bFoxOrange, Vector3.zero, new Vector3(0.15f, 0.25f, 0.07f), null, false);
                B.MeshObj("Inner", ear, cone, _bFoxDark, new Vector3(0f, 0.02f, 0.028f), new Vector3(0.085f, 0.16f, 0.02f), null, false);
                B.MeshObj("Tip", ear, cone, _bFoxDark, new Vector3(0f, 0.16f, 0f), new Vector3(0.078f, 0.09f, 0.045f), null, false);
            }

            // Legs: orange thighs with black socks and paws (pivot at the hip, swung by FoxActor).
            var legs = new List<Transform>();
            foreach (var lz in new[] { 0.22f, -0.22f })
            foreach (var sx in new[] { -0.11f, 0.11f })
            {
                var leg = B.Node("Leg", body, new Vector3(sx, -0.08f, lz)).transform;
                B.MeshObj("Thigh", leg, sphere, _bFoxOrange, new Vector3(0f, -0.07f, 0f), new Vector3(0.13f, 0.22f, 0.15f), null, false);
                B.MeshObj("Sock", leg, B.Cylinder, _bFoxDark, new Vector3(0f, -0.21f, 0f), new Vector3(0.058f, 0.12f, 0.058f), null, false);
                B.MeshObj("Paw", leg, sphere, _bFoxDark, new Vector3(0f, -0.32f, 0.02f), new Vector3(0.09f, 0.06f, 0.12f), null, false);
                legs.Add(leg);
            }

            // Big bushy tail raised in a J, with a white tip.
            var tail = B.Node("Tail", body, new Vector3(0f, 0.08f, -0.36f), new Vector3(-32f, 0f, 0f)).transform;
            B.MeshObj("T1", tail, sphere, _bFoxOrange, new Vector3(0f, 0f, -0.12f), new Vector3(0.16f, 0.16f, 0.3f), null, false);
            B.MeshObj("T2", tail, sphere, _bFoxOrange, new Vector3(0f, 0.05f, -0.4f), new Vector3(0.26f, 0.26f, 0.42f), null, false);
            B.MeshObj("Tip", tail, sphere, _bFoxWhite, new Vector3(0f, 0.11f, -0.64f), new Vector3(0.2f, 0.2f, 0.22f), null, false);
            var loot = B.Node("Loot", head, new Vector3(0f, -0.07f, 0.36f));
            for (int i = 0; i < 3; i++) Berry(loot.transform, 7, new Vector3(-0.06f + i * 0.06f, 0f, 0.02f * i), 0.1f, i * 60f);
            B.Decal("Blob", t, _mBlob, new Vector3(0f, 0.06f, 0f), new Vector2(0.7f, 1f)).GetComponent<MeshRenderer>().sortingOrder = -2;

            var fox = root.AddComponent<FoxActor>();
            fox.body = body;
            fox.head = head;
            fox.tail = tail;
            fox.legs = legs.ToArray();
            fox.loot = loot;
            SetLayerRecursive(root, 2);
            root.SetActive(false);
            return fox;
        }

        /// <summary>The fox's den at the forest edge: a hollow log in a thicket.</summary>
        static void FoxDen(Transform parent, Vector3 pos)
        {
            var t = B.Node("FoxDen", parent, pos, new Vector3(0f, 80f, 0f)).transform;
            B.Prop("Survival/tree-log", t, Vector3.zero, 2.6f);
            B.Prop("Nature/plant_bushLarge", t, new Vector3(-1f, 0f, 0.9f), 1.6f, 40f);
            B.Prop("Nature/plant_bush", t, new Vector3(1.1f, 0f, 0.7f), 1.5f, -30f);
            B.Prop("Nature/mushroom_red", t, new Vector3(0.6f, 0f, -0.8f), 1.6f);
            B.Decal("Paw", t, _bPaw, new Vector3(0.9f, 0.06f, -1.2f), new Vector2(0.3f, 0.3f), 30f);
            B.Decal("Paw", t, _bPaw, new Vector3(1.4f, 0.06f, -1.6f), new Vector2(0.3f, 0.3f), 40f);
        }

        // ================================================================== village

        /// <summary>Pastel cottage: walls, a gabled roof, door, windows with flower boxes and a chimney.</summary>
        static GameObject Cottage(Transform parent, Vector3 pos, float yaw, Material wall, Material roof, float w = 3.6f, float d = 3f, float h = 2.4f)
        {
            var go = B.Node("Cottage", parent, pos, new Vector3(0f, yaw, 0f));
            var t = go.transform;
            RB("Walls", t, wall, new Vector3(0f, h * 0.5f, 0f), new Vector3(w, h, d), 0.14f);
            RB("Plinth", t, _mStone, new Vector3(0f, 0.12f, 0f), new Vector3(w + 0.12f, 0.24f, d + 0.12f), 0.08f);
            float pitch = 36f;
            float half = d * 0.5f + 0.25f;
            float slab = half / Mathf.Cos(pitch * Mathf.Deg2Rad);
            foreach (var s in new[] { -1f, 1f })
            {
                var r = B.Node("Roof", t, new Vector3(0f, h + Mathf.Tan(pitch * Mathf.Deg2Rad) * half * 0.5f, s * half * 0.5f), new Vector3(s * pitch, 0f, 0f)).transform;
                RB("Slab", r, roof, Vector3.zero, new Vector3(w + 0.5f, 0.16f, slab), 0.07f);
            }
            float ridgeY = h + Mathf.Tan(pitch * Mathf.Deg2Rad) * half;
            B.MeshObj("Ridge", t, B.Cylinder, roof, new Vector3(0f, ridgeY - 0.02f, 0f), new Vector3(0.18f, (w + 0.5f) * 0.5f, 0.18f), new Vector3(0f, 0f, 90f), false);
            RB("Door", t, _mWood, new Vector3(0f, 0.62f, -d * 0.5f - 0.02f), new Vector3(0.78f, 1.2f, 0.07f), 0.12f);
            B.MeshObj("Knob", t, LowSphere(), _mGold, new Vector3(0.26f, 0.62f, -d * 0.5f - 0.07f), Vector3.one * 0.07f, null, false);
            foreach (var sx in new[] { -1f, 1f })
            {
                var wp = new Vector3(sx * w * 0.28f, h * 0.55f, -d * 0.5f - 0.02f);
                RB("WinFrame", t, _mWhite, wp, new Vector3(0.66f, 0.6f, 0.05f), 0.05f, false);
                RB("Window", t, _tTruckGlass, wp + new Vector3(0f, 0f, -0.02f), new Vector3(0.52f, 0.46f, 0.03f), 0.04f, false);
                RB("FlowerBox", t, _mWood, wp + new Vector3(0f, -0.38f, -0.08f), new Vector3(0.7f, 0.14f, 0.16f), 0.03f, false);
                for (int i = 0; i < 4; i++)
                    B.MeshObj("Bloom", t, LowSphere(), i % 2 == 0 ? _bFlowerPink : _bFlowerYellow, wp + new Vector3(-0.24f + i * 0.16f, -0.28f, -0.1f), Vector3.one * 0.13f, null, false);
            }
            B.Cyl("Chimney", t, _bBrick, new Vector3(w * 0.3f, ridgeY - 0.1f, d * 0.2f), 0.36f, 1.1f);
            var col = go.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, h * 0.5f, 0f);
            col.size = new Vector3(w, h, d);
            return go;
        }

        /// <summary>White picket fence from a to b (decor, no collider).</summary>
        static void Picket(Transform parent, Vector3 a, Vector3 b)
        {
            var root = B.Node("Fence", parent, Vector3.zero).transform;
            var d = b - a;
            float len = d.magnitude;
            int n = Mathf.Max(2, Mathf.RoundToInt(len / 0.42f));
            var rot = Quaternion.LookRotation(new Vector3(d.x, 0f, d.z));
            for (int i = 0; i <= n; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)n);
                RB("Picket", root, _bFence, p + Vector3.up * 0.34f, new Vector3(0.1f, 0.68f, 0.05f), 0.03f, false);
            }
            foreach (var y in new[] { 0.22f, 0.5f })
            {
                var rail = RB("Rail", root, _bFence, (a + b) * 0.5f + Vector3.up * y, new Vector3(0.05f, 0.07f, len), 0.02f, false);
                rail.transform.localRotation = rot;
            }
        }

        /// <summary>Blossom tree: short trunk, fluffy pink or white canopy.</summary>
        static void BlossomTree(Transform parent, Vector3 pos, float scale, bool white)
        {
            var t = B.Node("BlossomTree", parent, pos, new Vector3(0f, (float)_rnd.NextDouble() * 360f, 0f), Vector3.one * scale).transform;
            B.MeshObj("Trunk", t, _shShortTrunk, _tTrunk, Vector3.zero, new Vector3(1.1f, 1.15f, 1.1f));
            var mat = white ? _bBlossomWhite : _bBlossom;
            B.MeshObj("Canopy", t, _shLeafBall[0], mat, new Vector3(0.15f, 2.05f, 0f), new Vector3(1.9f, 1.5f, 1.9f));
            B.MeshObj("Canopy", t, _shLeafBall[1], mat, new Vector3(-0.45f, 1.8f, 0.3f), new Vector3(1.2f, 1f, 1.2f));
            B.MeshObj("Canopy", t, _shLeafBall[2], mat, new Vector3(0.55f, 1.75f, -0.4f), new Vector3(1.1f, 0.95f, 1.1f));
            var col = B.Node("Col", t, new Vector3(0f, 0.8f, 0f)).AddComponent<CapsuleCollider>();
            col.radius = 0.25f;
            col.height = 1.6f;
        }

        /// <summary>Beehive with a little cloud of bees.</summary>
        static void Beehive(Transform parent, Vector3 pos)
        {
            var t = B.Node("Beehive", parent, pos).transform;
            B.Box("Stand", t, _mWood, new Vector3(0f, 0.15f, 0f), new Vector3(0.7f, 0.3f, 0.6f));
            for (int i = 0; i < 3; i++)
                RB("Box", t, i % 2 == 0 ? _bHive : _mWhite, new Vector3(0f, 0.45f + i * 0.28f, 0f), new Vector3(0.62f, 0.26f, 0.52f), 0.04f);
            RB("Lid", t, _bRoofRed, new Vector3(0f, 1.3f, 0f), new Vector3(0.74f, 0.08f, 0.64f), 0.03f);
            B.Box("Door", t, _mDark, new Vector3(0f, 0.36f, -0.27f), new Vector3(0.2f, 0.05f, 0.02f), null, false);
            var bees = NewParticles("Bees", t, new Vector3(0f, 0.9f, 0f), MatLib.Sprite("Fx_Bee", MatLib.Tex("fx_circle.png"), Color.white), 12);
            var m = bees.main;
            m.startLifetime = new ParticleSystem.MinMaxCurve(2f, 3f);
            m.startSpeed = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
            m.startSize = new ParticleSystem.MinMaxCurve(0.07f, 0.09f);
            m.startColor = new Color(1f, 0.8f, 0.15f);
            m.simulationSpace = ParticleSystemSimulationSpace.Local;
            var e = bees.emission;
            e.rateOverTime = 4f;
            var sh = bees.shape;
            sh.shapeType = ParticleSystemShapeType.Sphere;
            sh.radius = 0.4f;
            var n = bees.noise;
            n.enabled = true;
            n.strength = 1.2f;
            n.frequency = 1.2f;
            FadeInOut(bees);
            var col = t.gameObject.AddComponent<BoxCollider>();
            col.center = new Vector3(0f, 0.6f, 0f);
            col.size = new Vector3(0.7f, 1.3f, 0.6f);
        }

        /// <summary>A rabbit that hops about a meadow patch (runs off when you come close).</summary>
        static void Rabbit(Transform parent, Vector3 pos, Vector2 area, bool brown)
        {
            var root = B.Node("Rabbit", parent, pos);
            var bodyT = B.Node("Body", root.transform, Vector3.zero).transform;
            var fur = brown ? MatLib.Lit("RabbitBrown", new Color(0.72f, 0.55f, 0.4f), 0.2f) : MatLib.Lit("RabbitWhite", new Color(0.98f, 0.96f, 0.94f), 0.2f);
            var sphere = LowSphere();
            B.MeshObj("Torso", bodyT, sphere, fur, new Vector3(0f, 0.2f, 0f), new Vector3(0.3f, 0.3f, 0.42f));
            B.MeshObj("Head", bodyT, sphere, fur, new Vector3(0f, 0.36f, 0.2f), new Vector3(0.22f, 0.21f, 0.22f));
            foreach (var sx in new[] { -0.05f, 0.05f })
            {
                B.MeshObj("Ear", bodyT, sphere, fur, new Vector3(sx, 0.56f, 0.16f), new Vector3(0.06f, 0.26f, 0.05f), new Vector3(-10f, 0f, sx * 150f), false);
                B.MeshObj("Eye", bodyT, sphere, _mDark, new Vector3(sx * 1.6f, 0.39f, 0.3f), Vector3.one * 0.035f, null, false);
            }
            B.MeshObj("Nose", bodyT, sphere, _bFlowerPink, new Vector3(0f, 0.35f, 0.31f), Vector3.one * 0.03f, null, false);
            B.MeshObj("Tail", bodyT, sphere, _mWhite, new Vector3(0f, 0.22f, -0.22f), Vector3.one * 0.11f, null, false);
            B.Decal("Blob", root.transform, _mBlob, new Vector3(0f, 0.06f, 0f), new Vector2(0.45f, 0.55f)).GetComponent<MeshRenderer>().sortingOrder = -2;
            var w = root.AddComponent<Wanderer>();
            w.areaCenter = pos;
            w.areaSize = area;
            w.speed = 1.6f;
            w.pause = new Vector2(1f, 3.5f);
            w.skittish = 2.4f;
            w.floats = true;
            w.bobTarget = bodyT;
        }
    }
}
