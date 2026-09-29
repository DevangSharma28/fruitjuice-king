using TMPro;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace JuiceKing.EditorTools
{
    /// <summary>Procedural tropical art for Expansion 1: plants, island, road, trucks and beach props.</summary>
    public static partial class JuiceKingBuilder
    {
        static Material _tSand, _tGrassPatch, _tOcean, _tFoam, _tRoad, _tTrunk, _tPalmLeaf, _tBananaLeaf, _tMangoLeaf, _tPapayaLeaf,
            _tMangoSkin, _tMangoBlush, _tBanana, _tPapayaSkin, _tThatch, _tBamboo, _tSoil, _tSoilRim, _tLagoon, _tPathBlob, _tFlame,
            _tHibiscus, _tDeck, _tRock, _tPadRound, _tGlowPad, _tGlowBase, _tTruckGlass, _tTyre, _tHeadOn, _tHeadOff, _tBerry;
        static Material _tShallows;
        static Mesh _lowSphere, _ballSphere;
        static Mesh[] _shLeafBall = new Mesh[3];
        static Mesh _shFrond, _shBroadLeaf, _shPapayaLeaf, _shBanana, _shPalmTrunk, _shShortTrunk, _shThinTrunk, _shStem, _shThatch;

        // ================================================================== materials

        static Material LitTransparent(string name, Color c, Texture tex, Vector2 tiling, int queue)
        {
            var m = MatLib.Lit(name, c, 0.05f, tex, tiling);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = queue;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
            return m;
        }

        static void BuildTropicalMaterials()
        {
            _tSand = MatLib.Lit("T_Sand", Color.white, 0.05f, MatLib.Tex("ground_sand.png"), new Vector2(1f, 1f));
            _tGrassPatch = LitTransparent("T_GrassPatch", new Color(0.8f, 0.86f, 0.78f), MatLib.Tex("grass_patch.png"), Vector2.one, 2600);
            _tOcean = MatLib.Lit("T_Ocean", Color.white, 0.85f, MatLib.Tex("ground_ocean.png"), new Vector2(36f, 36f));
            _tLagoon = MatLib.Lit("T_Lagoon", new Color(0.85f, 1f, 1f), 0.9f, MatLib.Tex("ground_ocean.png"), new Vector2(2f, 2f));
            _tFoam = MatLib.Sprite("T_Foam", MatLib.Tex("foam_strip.png"), new Color(1f, 1f, 1f, 0.9f));
            _tFoam.renderQueue = 2990;
            _tRoad = MatLib.Lit("T_Road", Color.white, 0.12f, MatLib.Tex("road_lane.png"), Vector2.one);
            _tPathBlob = MatLib.Sprite("T_PathBlob", MatLib.Tex("soft_blob.png"), new Color(0.82f, 0.68f, 0.48f, 0.55f));
            _tPathBlob.renderQueue = 2980;
            _tTrunk = MatLib.Lit("T_Trunk", new Color(0.66f, 0.5f, 0.32f), 0.08f);
            _tPalmLeaf = MatLib.Lit("T_PalmLeaf", new Color(0.3f, 0.7f, 0.3f), 0.2f);
            _tBananaLeaf = MatLib.Lit("T_BananaLeaf", new Color(0.46f, 0.8f, 0.32f), 0.25f);
            _tMangoLeaf = MatLib.Lit("T_MangoLeaf", new Color(0.2f, 0.56f, 0.27f), 0.18f);
            _tPapayaLeaf = MatLib.Lit("T_PapayaLeaf", new Color(0.36f, 0.72f, 0.26f), 0.2f);
            _tMangoSkin = MatLib.Lit("T_MangoSkin", new Color(1f, 0.66f, 0.16f), 0.45f);
            _tMangoBlush = MatLib.Lit("T_MangoBlush", new Color(0.96f, 0.36f, 0.24f), 0.45f);
            _tBanana = MatLib.Lit("T_Banana", new Color(1f, 0.87f, 0.25f), 0.35f);
            _tPapayaSkin = MatLib.Lit("T_PapayaSkin", new Color(0.98f, 0.62f, 0.22f), 0.4f);
            _tThatch = MatLib.Lit("T_Thatch", Color.white, 0.05f, MatLib.Tex("thatch.png"), new Vector2(4f, 2f));
            _tShallows = MatLib.Sprite("T_Shallows", MatLib.Tex("gradient_strip.png"), new Color(0.5f, 0.95f, 0.9f, 0.55f));
            _tShallows.renderQueue = 2970;
            _tBamboo = MatLib.Lit("T_Bamboo", new Color(0.84f, 0.74f, 0.44f), 0.2f);
            _tSoil = MatLib.Lit("T_Soil", Color.white, 0.02f, MatLib.Tex("ground_soil.png"), new Vector2(3f, 3f));
            _tSoilRim = MatLib.Lit("T_SoilRim", new Color(0.5f, 0.36f, 0.22f), 0.05f);
            _tFlame = Emissive("T_Flame", new Color(1f, 0.62f, 0.15f), 2.2f);
            _tHibiscus = MatLib.Lit("T_Hibiscus", new Color(1f, 0.32f, 0.5f), 0.35f);
            _tDeck = MatLib.Lit("T_Deck", new Color(1f, 0.95f, 0.88f), 0.1f, MatLib.Tex("wood.png"), new Vector2(3f, 3f));
            _tRock = MatLib.Lit("T_Rock", new Color(0.66f, 0.64f, 0.62f), 0.15f);
            _tPadRound = MatLib.Sprite("Pad_Round", MatLib.Tex("pad_round.png"), Color.white);
            _tGlowPad = MatLib.Sprite("T_GlowRing", MatLib.Tex("glow_ring.png"), new Color(1f, 0.8f, 0.25f, 1f));
            _tGlowBase = MatLib.Sprite("T_GlowBase", MatLib.Tex("soft_blob.png"), new Color(1f, 0.82f, 0.4f, 0.62f));
            _tTruckGlass = MatLib.Lit("T_TruckGlass", new Color(0.45f, 0.7f, 0.9f), 0.9f);
            _tTyre = MatLib.Lit("T_Tyre", new Color(0.16f, 0.16f, 0.18f), 0.2f);
            _tHeadOn = Emissive("T_HeadOn", new Color(1f, 0.95f, 0.7f), 2.5f);
            _tHeadOff = MatLib.Lit("T_HeadOff", new Color(0.85f, 0.85f, 0.8f), 0.8f);
            _tBerry = MatLib.Lit("T_Berry", new Color(0.55f, 0.25f, 0.55f), 0.3f);
        }

        // ================================================================== meshes

        static void BuildTropicalMeshes()
        {
            _shFrond = LeafMesh("T_Frond", 1.7f, 0.3f, 1.2f, 0.3f, 14, 0.55f, 0.1f);
            _shBroadLeaf = LeafMesh("T_BroadLeaf", 1.9f, 0.46f, 0.75f, 0.7f, 10, 0.1f, 0.06f);
            _shPapayaLeaf = LeafMesh("T_PapayaLeaf", 1.1f, 0.42f, 0.3f, 0.35f, 8, 0.75f, 0.12f);
            _shPalmTrunk = TrunkMesh("T_PalmTrunk", 3.1f, 0.24f, 0.14f, 0.7f, 12, 10, 0.12f);
            _shShortTrunk = TrunkMesh("T_ShortTrunk", 1.5f, 0.22f, 0.15f, 0.12f, 5, 10, 0f);
            _shThinTrunk = TrunkMesh("T_ThinTrunk", 2.4f, 0.13f, 0.09f, 0.12f, 8, 8, 0.08f);
            _shStem = TrunkMesh("T_BananaStem", 1.35f, 0.2f, 0.13f, 0.05f, 4, 10, 0f);
            _shBanana = TrunkMesh("T_BananaFinger", 0.62f, 0.085f, 0.04f, 0.26f, 6, 8, 0f);
            _lowSphere = UVSphere("T_LowSphere", 10, 7);
            _ballSphere = UVSphere("T_BallSphere", 16, 10);
            for (int i = 0; i < _shLeafBall.Length; i++) _shLeafBall[i] = LeafBall("T_LeafBall" + i, i * 7.3f);
            var g = new MeshGen();
            g.Cone(0.5f, 0f, 0.55f, 10, 0, true);
            g.Side(0.5f, 0.52f, -0.12f, 0f, 10, 0);
            _shThatch = SaveMesh(g.ToMesh("T_Thatch"), "T_Thatch");
        }

        /// <summary>
        /// Double-sided leaf along +Z: rises by <paramref name="curl"/>, droops by <paramref name="droop"/> (quadratic),
        /// folded into a shallow V, with optional serrated edges (palm fronds).
        /// </summary>
        static Mesh LeafMesh(string name, float length, float width, float droop, float curl, int segs, float serration, float fold)
        {
            var v = new List<Vector3>();
            var tris = new List<int>();
            var uv = new List<Vector2>();
            for (int side = 0; side < 2; side++)
            {
                int start = v.Count;
                for (int i = 0; i <= segs; i++)
                {
                    float t = i / (float)segs;
                    float w = width * Mathf.Sin(Mathf.PI * Mathf.Pow(t, 0.75f)) * (i % 2 == 1 ? 1f - serration : 1f);
                    if (i == segs) w = 0f;
                    var c = new Vector3(0f, curl * t - droop * t * t, length * t);
                    float lift = side == 0 ? 0.004f : -0.004f;
                    v.Add(c + new Vector3(0f, lift, 0f));
                    v.Add(c + new Vector3(-w, -fold * w + lift, 0f));
                    v.Add(c + new Vector3(w, -fold * w + lift, 0f));
                    uv.Add(new Vector2(0.5f, t));
                    uv.Add(new Vector2(0f, t));
                    uv.Add(new Vector2(1f, t));
                }
                for (int i = 0; i < segs; i++)
                {
                    int a = start + i * 3, b = a + 3;
                    // centre-left strip and centre-right strip
                    if (side == 0)
                    {
                        tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
                        tris.AddRange(new[] { a, a + 2, b, a + 2, b + 2, b });
                    }
                    else
                    {
                        tris.AddRange(new[] { a, a + 1, b, a + 1, b + 1, b });
                        tris.AddRange(new[] { a, b, a + 2, a + 2, b, b + 2 });
                    }
                }
            }
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        /// <summary>Low-poly UV sphere (radius 0.5) for props: a fraction of the built-in sphere's 768 triangles.</summary>
        static Mesh UVSphere(string name, int seg, int rings)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            for (int r = 0; r <= rings; r++)
            {
                float phi = Mathf.PI * r / rings;
                for (int k = 0; k <= seg; k++)
                {
                    float th = Mathf.PI * 2f * k / seg;
                    var d = new Vector3(Mathf.Sin(phi) * Mathf.Cos(th), Mathf.Cos(phi), Mathf.Sin(phi) * Mathf.Sin(th));
                    v.Add(d * 0.5f);
                    n.Add(d);
                    uv.Add(new Vector2(k / (float)seg, 1f - r / (float)rings));
                }
            }
            for (int r = 0; r < rings; r++)
            for (int k = 0; k < seg; k++)
            {
                int a = r * (seg + 1) + k, b = a + seg + 1;
                if (r > 0) tris.AddRange(new[] { a, a + 1, b });
                if (r < rings - 1) tris.AddRange(new[] { a + 1, b + 1, b });
            }
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        /// <summary>Lumpy foliage ball: the unit sphere pushed in and out by noise, with normals welded across UV seams.</summary>
        static Mesh LeafBall(string name, float seed)
        {
            var m = UnityEngine.Object.Instantiate(_ballSphere);
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++)
            {
                var p = v[i];
                float k = Mathf.PerlinNoise(p.x * 3.1f + seed, p.z * 3.1f + p.y * 2.3f + seed * 0.5f);
                float k2 = Mathf.PerlinNoise(p.y * 6f + seed, p.x * 6f - p.z * 4f);
                v[i] = p + p.normalized * ((k - 0.5f) * 0.2f + (k2 - 0.5f) * 0.07f);
            }
            m.vertices = v;
            m.RecalculateNormals();
            var n = m.normals;
            var sum = new Dictionary<Vector3Int, Vector3>();
            Vector3Int Key(Vector3 p) => new Vector3Int(Mathf.RoundToInt(p.x * 1000f), Mathf.RoundToInt(p.y * 1000f), Mathf.RoundToInt(p.z * 1000f));
            for (int i = 0; i < v.Length; i++)
            {
                var key = Key(v[i]);
                sum[key] = (sum.TryGetValue(key, out var acc) ? acc : Vector3.zero) + n[i];
            }
            for (int i = 0; i < v.Length; i++) n[i] = sum[Key(v[i])].normalized;
            m.normals = n;
            m.name = name;
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        /// <summary>Tapered tube along +Y whose centre line bends toward +X (quadratic). Optional ring ridges.</summary>
        static Mesh TrunkMesh(string name, float height, float r0, float r1, float bend, int rings, int seg, float ridge)
        {
            var v = new List<Vector3>();
            var n = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            int rows = rings * 2;
            for (int i = 0; i <= rows; i++)
            {
                float t = i / (float)rows;
                var c = new Vector3(bend * t * t, height * t, 0f);
                float r = Mathf.Lerp(r0, r1, t) * (1f + (i % 2 == 0 ? ridge : 0f));
                for (int k = 0; k <= seg; k++)
                {
                    float a = k / (float)seg * Mathf.PI * 2f;
                    var dir = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                    v.Add(c + dir * r);
                    n.Add(dir);
                    uv.Add(new Vector2(k / (float)seg, t * 4f));
                }
            }
            for (int i = 0; i < rows; i++)
            for (int k = 0; k < seg; k++)
            {
                int a = i * (seg + 1) + k, b = a + seg + 1;
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
            // Top cap.
            var top = new Vector3(bend, height, 0f);
            int ci = v.Count;
            v.Add(top);
            n.Add(Vector3.up);
            uv.Add(new Vector2(0.5f, 0.5f));
            int ring = rows * (seg + 1);
            for (int k = 0; k < seg; k++) tris.AddRange(new[] { ci, ring + k + 1, ring + k });
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetNormals(n);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        /// <summary>Organic island: flat top out to radius(θ), then a sandy slope that dips below the water line.</summary>
        static Mesh IslandMesh(string name, Vector3 center, Func<float, float> radius, int seg, float slope, float depth)
        {
            var v = new List<Vector3> { center };
            var uv = new List<Vector2> { new Vector2(center.x, center.z) / 7f };
            var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float r = radius(a);
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                var p0 = center + d * r;
                var p1 = center + d * (r + slope) + Vector3.down * depth;
                v.Add(p0);
                uv.Add(new Vector2(p0.x, p0.z) / 7f);
                v.Add(p1);
                uv.Add(new Vector2(p1.x, p1.z) / 7f);
            }
            for (int i = 0; i < seg; i++)
            {
                int a = 1 + i * 2, b = a + 2;
                tris.AddRange(new[] { 0, b, a });
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        /// <summary>Flat band from radius(θ)+inner to radius(θ)+outer at height y (shore foam). UV u runs across the band.</summary>
        static Mesh BandMesh(string name, Vector3 center, Func<float, float> radius, int seg, float inner, float outer, float y)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float r = radius(a);
                var d = new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a));
                v.Add(center + d * (r + inner) + Vector3.up * y);
                v.Add(center + d * (r + outer) + Vector3.up * y);
                float along = i / (float)seg * 40f;
                uv.Add(new Vector2(0f, along));
                uv.Add(new Vector2(1f, along));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = i * 2, b = a + 2;
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }
            var m = new Mesh { name = name };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        /// <summary>Catmull-Rom resample of control points (y ignored) at roughly <paramref name="step"/> spacing.</summary>
        static List<Vector3> Spline(IList<Vector3> ctrl, float step)
        {
            var outPts = new List<Vector3>();
            for (int i = 0; i < ctrl.Count - 1; i++)
            {
                Vector3 p0 = ctrl[Mathf.Max(0, i - 1)], p1 = ctrl[i], p2 = ctrl[i + 1], p3 = ctrl[Mathf.Min(ctrl.Count - 1, i + 2)];
                int n = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(p1, p2) / step));
                for (int k = 0; k < n; k++)
                {
                    float t = k / (float)n;
                    float t2 = t * t, t3 = t2 * t;
                    var p = 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
                    outPts.Add(p);
                }
            }
            outPts.Add(ctrl[ctrl.Count - 1]);
            return outPts;
        }

        /// <summary>Road ribbon along a polyline with a skirt down each side (reads as a causeway over water).</summary>
        static Mesh RibbonMesh(string name, IList<Vector3> pts, float width, float y, float skirt, float vPerMetre)
        {
            var v = new List<Vector3>();
            var uv = new List<Vector2>();
            var tris = new List<int>();
            float along = 0f;
            for (int i = 0; i < pts.Count; i++)
            {
                Vector3 fwd = (i < pts.Count - 1 ? pts[i + 1] - pts[i] : pts[i] - pts[i - 1]);
                if (i > 0 && i < pts.Count - 1) fwd = pts[i + 1] - pts[i - 1];
                fwd.y = 0f;
                fwd.Normalize();
                var right = new Vector3(fwd.z, 0f, -fwd.x);
                if (i > 0) along += Vector3.Distance(pts[i], pts[i - 1]);
                var c = new Vector3(pts[i].x, y, pts[i].z);
                v.Add(c - right * width * 0.5f);
                v.Add(c + right * width * 0.5f);
                v.Add(c - right * width * 0.5f + Vector3.down * skirt);
                v.Add(c + right * width * 0.5f + Vector3.down * skirt);
                float vv = along * vPerMetre;
                uv.Add(new Vector2(0f, vv));
                uv.Add(new Vector2(1f, vv));
                uv.Add(new Vector2(0f, vv));
                uv.Add(new Vector2(1f, vv));
            }
            for (int i = 0; i < pts.Count - 1; i++)
            {
                int a = i * 4, b = a + 4;
                tris.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 }); // top
                tris.AddRange(new[] { a, a + 2, b, b, a + 2, b + 2 }); // left skirt
                tris.AddRange(new[] { a + 1, b + 1, a + 3, a + 3, b + 1, b + 3 }); // right skirt
            }
            var m = new Mesh { name = name, indexFormat = IndexFormat.UInt32 };
            m.SetVertices(v);
            m.SetUVs(0, uv);
            m.SetTriangles(tris, 0);
            m.RecalculateNormals();
            m.RecalculateBounds();
            return SaveMesh(m, name);
        }

        // ================================================================== plants (harvestable and decorative)

        static void Fronds(Transform parent, Vector3 top, Mesh leaf, Material mat, int count, float tilt, float scale, float phase = 0f)
        {
            for (int i = 0; i < count; i++)
            {
                float yaw = phase + i * 360f / count + (float)_rnd.NextDouble() * 14f;
                float pitch = tilt + (float)_rnd.NextDouble() * 12f - 6f;
                B.MeshObj("Leaf", parent, leaf, mat, top, Vector3.one * scale * (0.88f + (float)_rnd.NextDouble() * 0.24f), new Vector3(pitch, yaw, 0f));
            }
        }

        /// <summary>Coconut palm (trunk leans toward local +X). Returns where the fruit hangs: (0, height, distance in front).</summary>
        static Vector3 PalmShape(Transform plant, float scale)
        {
            B.MeshObj("Trunk", plant, _shPalmTrunk, _tTrunk, Vector3.zero, Vector3.one * scale);
            var top = new Vector3(0.7f, 3.1f, 0f) * scale;
            B.MeshObj("Crown", plant, _lowSphere, _tTrunk, top, new Vector3(0.36f, 0.24f, 0.36f) * scale);
            Fronds(plant, top, _shFrond, _tPalmLeaf, 7, -14f, scale);
            return new Vector3(0f, 2.7f * scale, 0.95f * scale);
        }

        static Vector3 MangoShape(Transform plant, float scale)
        {
            B.MeshObj("Trunk", plant, _shShortTrunk, _tTrunk, Vector3.zero, Vector3.one * scale);
            // Compact lumpy crown sitting high, so the mangoes hanging below it stay visible.
            Vector3[] blobs = { new Vector3(0f, 2.3f, 0.05f), new Vector3(0.46f, 2.1f, 0.15f), new Vector3(-0.46f, 2.12f, 0.12f), new Vector3(0.04f, 2.7f, 0.18f), new Vector3(0f, 2.15f, 0.48f) };
            float[] radii = { 0.68f, 0.5f, 0.52f, 0.48f, 0.5f };
            for (int i = 0; i < blobs.Length; i++)
                B.MeshObj("Canopy", plant, _shLeafBall[i % _shLeafBall.Length], _tMangoLeaf, blobs[i] * scale, Vector3.one * radii[i] * 2f * scale,
                    new Vector3(0f, i * 67f, 0f));
            return new Vector3(0f, 1.3f * scale, 0.85f * scale);
        }

        static Vector3 BananaShape(Transform plant, float scale)
        {
            B.MeshObj("Stem", plant, _shStem, _tBananaLeaf, Vector3.zero, Vector3.one * scale);
            var top = new Vector3(0.05f, 1.35f, 0f) * scale;
            Fronds(plant, top, _shBroadLeaf, _tBananaLeaf, 7, -32f, scale);
            return new Vector3(0f, 1.05f * scale, 0.42f * scale);
        }

        static Vector3 PapayaShape(Transform plant, float scale)
        {
            B.MeshObj("Trunk", plant, _shThinTrunk, _tTrunk, Vector3.zero, Vector3.one * scale);
            var top = new Vector3(0.12f, 2.4f, 0f) * scale;
            for (int i = 0; i < 7; i++)
            {
                float yaw = i * 360f / 7f;
                // Leaf stalk + palmate blade.
                var dir = Quaternion.Euler(-35f, yaw, 0f) * Vector3.forward;
                B.MeshObj("Stalk", plant, B.Cylinder, _tPapayaLeaf, top + dir * 0.35f * scale, new Vector3(0.04f, 0.36f, 0.04f) * scale,
                    Quaternion.FromToRotation(Vector3.up, dir).eulerAngles);
                B.MeshObj("Leaf", plant, _shPapayaLeaf, _tPapayaLeaf, top + dir * 0.65f * scale, Vector3.one * scale, new Vector3(-20f, yaw, 0f));
            }
            return new Vector3(0f, 1.9f * scale, 0.26f * scale);
        }

        /// <summary>The harvestable fruit cluster on a plant (becomes the node's visual).</summary>
        static void FruitCluster(int f, Transform vis)
        {
            switch ((FruitKind)f)
            {
                case FruitKind.Coconut:
                    Vector3[] c = { new Vector3(-0.19f, 0f, 0f), new Vector3(0.19f, 0.02f, 0.05f), new Vector3(0f, -0.09f, -0.19f), new Vector3(0.02f, 0.1f, 0.17f) };
                    foreach (var p in c) B.MeshObj("Nut", vis, _lowSphere, _mShell, p, Vector3.one * 0.42f);
                    break;
                case FruitKind.Mango:
                    Vector3[] m = { new Vector3(-0.3f, 0f, 0f), new Vector3(0.28f, 0.08f, 0.05f), new Vector3(0f, -0.12f, -0.18f) };
                    foreach (var p in m)
                    {
                        B.MeshObj("Stalk", vis, B.Cylinder, _tTrunk, p + new Vector3(0f, 0.36f, 0f), new Vector3(0.04f, 0.14f, 0.04f));
                        B.MeshObj("Mango", vis, _lowSphere, _tMangoSkin, p, new Vector3(0.52f, 0.68f, 0.46f), new Vector3(0f, 0f, 12f));
                        B.MeshObj("Blush", vis, _lowSphere, _tMangoBlush, p + new Vector3(0.05f, 0.12f, -0.07f), new Vector3(0.42f, 0.44f, 0.36f), new Vector3(0f, 0f, 12f));
                    }
                    break;
                case FruitKind.Banana:
                    B.MeshObj("Stalk", vis, B.Cylinder, _tBananaLeaf, new Vector3(0f, 0.1f, 0f), new Vector3(0.08f, 0.45f, 0.08f));
                    for (int tier = 0; tier < 3; tier++)
                    for (int k = 0; k < 5; k++)
                    {
                        float yaw = k * 72f + tier * 30f;
                        var dir = Quaternion.Euler(0f, yaw, 0f) * Vector3.forward;
                        // Finger axis points out and up; its bend (mesh +X) curls further up.
                        var axis = (dir * 0.8f + Vector3.up * 0.6f).normalized;
                        var curl = (-dir * 0.6f + Vector3.up * 0.8f).normalized;
                        var rot = Quaternion.LookRotation(Vector3.Cross(curl, axis), axis);
                        B.MeshObj("Finger", vis, _shBanana, _tBanana, dir * 0.08f + new Vector3(0f, 0.3f - tier * 0.24f, 0f), Vector3.one * 0.95f, rot.eulerAngles);
                    }
                    B.MeshObj("Bud", vis, _cone, _tBerry, new Vector3(0f, -0.6f, 0f), new Vector3(0.18f, -0.28f, 0.18f));
                    break;
                default:
                    Vector3[] q = { new Vector3(-0.2f, 0f, 0f), new Vector3(0.2f, 0.05f, 0.02f), new Vector3(0f, -0.2f, -0.12f) };
                    foreach (var p in q)
                    {
                        B.MeshObj("Papaya", vis, _lowSphere, _tPapayaSkin, p, new Vector3(0.36f, 0.62f, 0.36f));
                        B.MeshObj("Cap", vis, _lowSphere, _tPapayaLeaf, p + new Vector3(0f, 0.24f, 0f), new Vector3(0.2f, 0.16f, 0.2f));
                    }
                    break;
            }
        }

        /// <summary>A harvestable tropical plant: chainsaw the trunk, the fruit drops and breaks open.</summary>
        static TropicalFruitNode BuildTropicalPlant(int f, FruitField field, Transform parent, Vector3 pos)
        {
            var go = B.Node(FruitKey[f] + "Tree", parent, pos);
            B.MeshObj("Mound", go.transform, _disc, new[] { _tSoilRim, _tSoilRim }, Vector3.zero, new Vector3(1.5f, 0.08f, 1.5f), null, false);
            // Palms lean toward the camera (local +X faces south); other plants get a random turn.
            float yaw = f == 3 ? 90f + ((float)_rnd.NextDouble() - 0.5f) * 50f : (float)_rnd.NextDouble() * 360f;
            var plant = B.Node("Plant", go.transform, Vector3.zero, new Vector3(0f, yaw, 0f)).transform;
            float scale = 0.95f + (float)_rnd.NextDouble() * 0.12f;
            Vector3 fruitAt;
            TropicalFruitNode.Style style;
            switch ((FruitKind)f)
            {
                case FruitKind.Coconut: fruitAt = PalmShape(plant, scale) + new Vector3(0f, -0.35f, 0f); style = TropicalFruitNode.Style.CoconutCrack; break;
                case FruitKind.Mango: fruitAt = MangoShape(plant, scale); style = TropicalFruitNode.Style.MangoSplit; break;
                case FruitKind.Banana: fruitAt = BananaShape(plant, scale); style = TropicalFruitNode.Style.BananaBunch; break;
                default: fruitAt = PapayaShape(plant, scale); style = TropicalFruitNode.Style.PapayaBurst; break;
            }
            // The fruit hangs on the camera (south) side of the plant so it stays readable from above.
            var world = go.transform.position + new Vector3(0f, fruitAt.y, -fruitAt.z);
            var vis = B.Node("Visual", plant, Vector3.zero).transform;
            vis.position = world;
            vis.rotation = Quaternion.identity;
            FruitCluster(f, vis);

            var node = go.AddComponent<TropicalFruitNode>();
            node.kind = (FruitKind)f;
            node.visual = vis;
            node.radius = 0.6f;
            node.field = field;
            node.style = style;
            node.plant = plant;
            node.landOffset = new Vector3(0f, 0f, -1.05f);

            var hp = B.Node("HP", go.transform, new Vector3(0f, Mathf.Max(1.6f, world.y - pos.y + 0.75f), -0.7f), null, new Vector3(1.2f, 0.16f, 1f));
            hp.AddComponent<Billboard>();
            B.Sprite("Bg", hp.transform, _sWhite, Vector3.zero, 1f, false, 20, new Color(0.1f, 0.1f, 0.1f, 0.7f));
            var fill = B.Sprite("Fill", hp.transform, _sWhite, new Vector3(0f, 0f, -0.001f), 1f, false, 21, new Color(0.45f, 1f, 0.35f));
            node.hpBar = hp.transform;
            node.hpFill = fill.transform;

            var col = go.AddComponent<SphereCollider>();
            col.center = new Vector3(0f, 0.55f, 0f);
            col.radius = 0.45f;
            node.blocker = col;
            return node;
        }

        /// <summary>Decorative (static) version of a plant for jungle edges and the beach.</summary>
        static GameObject DecorPlant(int kind, Transform parent, Vector3 pos, float scale)
        {
            var go = B.Node(kind == 0 ? "Palm" : kind == 1 ? "Canopy" : kind == 2 ? "Banana" : "Papaya", parent, pos, new Vector3(0f, (float)_rnd.NextDouble() * 360f, 0f));
            switch (kind)
            {
                case 0: PalmShape(go.transform, scale); break;
                case 1: MangoShape(go.transform, scale); break;
                case 2: BananaShape(go.transform, scale); break;
                default: PapayaShape(go.transform, scale); break;
            }
            return go;
        }

        static GameObject Fern(Transform parent, Vector3 pos, float scale)
        {
            var go = B.Node("Fern", parent, pos);
            Fronds(go.transform, Vector3.zero, _shBroadLeaf, _tBananaLeaf, 6, -48f, scale * 0.55f, (float)_rnd.NextDouble() * 60f);
            return go;
        }

        static void Hibiscus(Transform parent, Vector3 pos)
        {
            var go = B.Node("Hibiscus", parent, pos).transform;
            B.MeshObj("Bush", go, _lowSphere, _tMangoLeaf, new Vector3(0f, 0.35f, 0f), new Vector3(0.9f, 0.7f, 0.9f));
            for (int i = 0; i < 4; i++)
            {
                float a = i * 1.6f + (float)_rnd.NextDouble();
                B.MeshObj("Flower", go, _lowSphere, i % 2 == 0 ? _tHibiscus : _mYellow, new Vector3(Mathf.Cos(a) * 0.36f, 0.5f + (i % 2) * 0.15f, Mathf.Sin(a) * 0.36f), new Vector3(0.22f, 0.12f, 0.22f), null, false);
            }
        }

        // ================================================================== props

        /// <summary>Thatched roof on four bamboo posts.</summary>
        static void ThatchShelter(Transform parent, Vector3 center, Vector2 size, float height, float yaw = 0f)
        {
            var root = B.Node("Shelter", parent, center, new Vector3(0f, yaw, 0f)).transform;
            foreach (var sx in new[] { -1f, 1f })
            foreach (var sz in new[] { -1f, 1f })
                B.Cyl("Post", root, _tBamboo, new Vector3(sx * size.x * 0.45f, height * 0.5f, sz * size.y * 0.45f), 0.16f, height);
            B.MeshObj("Roof", root, _shThatch, _tThatch, new Vector3(0f, height, 0f), new Vector3(size.x * 1.2f, 2.2f, size.y * 1.35f));
        }

        static void TikiTorch(Transform parent, Vector3 pos)
        {
            var t = B.Node("TikiTorch", parent, pos).transform;
            B.Cyl("Pole", t, _tBamboo, new Vector3(0f, 0.9f, 0f), 0.1f, 1.8f);
            B.MeshObj("Cup", t, _cup, _tTrunk, new Vector3(0f, 1.8f, 0f), new Vector3(0.26f, 0.22f, 0.26f));
            var flame = B.MeshObj("Flame", t, _lowSphere, _tFlame, new Vector3(0f, 2.1f, 0f), new Vector3(0.2f, 0.36f, 0.2f), null, false);
            Sway(flame.transform, 10f, 7f);
        }

        static void BeachUmbrella(Transform parent, Vector3 pos, Material canopy)
        {
            var t = B.Node("BeachUmbrella", parent, pos, new Vector3(0f, 0f, (float)_rnd.NextDouble() * 10f - 5f)).transform;
            B.Cyl("Pole", t, _mWhite, new Vector3(0f, 1.2f, 0f), 0.07f, 2.4f);
            B.MeshObj("Canopy", t, MakeUmbrella(), canopy, new Vector3(0f, 2.15f, 0f), new Vector3(2.2f, 0.7f, 2.2f));
        }

        static void Lounger(Transform parent, Vector3 pos, float yaw, Material towel)
        {
            var t = B.Node("Lounger", parent, pos, new Vector3(0f, yaw, 0f)).transform;
            RB("Frame", t, _mWhite, new Vector3(0f, 0.28f, 0f), new Vector3(0.7f, 0.08f, 1.8f), 0.08f);
            RB("Towel", t, towel, new Vector3(0f, 0.34f, -0.1f), new Vector3(0.6f, 0.04f, 1.4f), 0.06f, false);
            B.Box("Back", t, _mWhite, new Vector3(0f, 0.5f, 0.78f), new Vector3(0.68f, 0.06f, 0.6f), new Vector3(-50f, 0f, 0f));
            foreach (var z in new[] { -0.75f, 0.75f })
            foreach (var x in new[] { -0.3f, 0.3f })
                B.Cyl("Leg", t, _mWhite, new Vector3(x, 0.13f, z), 0.05f, 0.26f);
        }

        static void Surfboard(Transform parent, Vector3 pos, float yaw, Material mat)
        {
            var t = B.Node("Surfboard", parent, pos, new Vector3(0f, yaw, 0f)).transform;
            B.MeshObj("Board", t, _lowSphere, mat, new Vector3(0f, 0.95f, 0f), new Vector3(0.5f, 1.9f, 0.1f), new Vector3(-12f, 0f, 0f));
            B.MeshObj("Stripe", t, _lowSphere, _mWhite, new Vector3(0f, 0.95f, -0.012f), new Vector3(0.12f, 1.75f, 0.1f), new Vector3(-12f, 0f, 0f), false);
        }

        static void RockCluster(Transform parent, Vector3 pos, float scale)
        {
            string[] rocks = { "Survival/rock-a", "Survival/rock-b", "Survival/rock-c" };
            var t = B.Node("Rocks", parent, pos).transform;
            for (int i = 0; i < 3; i++)
            {
                var p = new Vector3(((float)_rnd.NextDouble() - 0.5f) * 1.6f * scale, 0f, ((float)_rnd.NextDouble() - 0.5f) * 1.2f * scale);
                B.Prop(rocks[_rnd.Next(rocks.Length)], t, p, (1.6f + (float)_rnd.NextDouble()) * scale, (float)_rnd.NextDouble() * 360f);
            }
        }

        /// <summary>Wooden arch with a painted board over a farm entrance.</summary>
        static void FarmSign(Transform parent, Vector3 pos, string text, Sprite icon, float yaw = 0f)
        {
            var t = B.Node("FarmSign", parent, pos, new Vector3(0f, yaw, 0f)).transform;
            // Posts stand at the board's ends and behind it, so they frame the board instead of crossing its face.
            B.Cyl("PostL", t, _tBamboo, new Vector3(-1.5f, 1.2f, 0.1f), 0.16f, 2.4f);
            B.Cyl("PostR", t, _tBamboo, new Vector3(1.5f, 1.2f, 0.1f), 0.16f, 2.4f);
            RB("Board", t, _mWood, new Vector3(0f, 2.15f, 0f), new Vector3(2.8f, 0.6f, 0.12f), 0.08f);
            var txt = B.Text("Text", t, text, 3.6f, Color.white, new Vector3(0.18f, 2.15f, -0.08f));
            txt.rectTransform.sizeDelta = new Vector2(2.2f, 0.5f);
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 2f;
            txt.fontSizeMax = 3.6f;
            if (icon != null) B.Sprite("Icon", t, icon, new Vector3(-1.05f, 2.15f, -0.08f), SpriteScale(icon, 0.5f), false, 6);
            B.MeshObj("Leaf", t, _shBroadLeaf, _tBananaLeaf, new Vector3(1.5f, 2.42f, 0.1f), Vector3.one * 0.45f, new Vector3(-30f, 60f, 0f));
            B.MeshObj("Leaf", t, _shBroadLeaf, _tBananaLeaf, new Vector3(-1.5f, 2.42f, 0.1f), Vector3.one * 0.45f, new Vector3(-30f, -60f, 0f));
            foreach (var x in new[] { -1.5f, 1.5f })
            {
                var c = B.Node("Col", t, new Vector3(x, 1f, 0f)).AddComponent<CapsuleCollider>();
                c.radius = 0.12f;
                c.height = 2f;
            }
        }

        static void CrateOfFruit(Transform parent, Vector3 pos, int f, float yaw)
        {
            var t = B.Node("FruitCrate", parent, pos, new Vector3(0f, yaw, 0f)).transform;
            B.Prop("Survival/box-open", t, Vector3.zero, 2.6f);
            var mat = f == 3 ? _mShell : f == 4 ? _tMangoSkin : f == 5 ? _tBanana : _tPapayaSkin;
            for (int i = 0; i < 4; i++)
                B.MeshObj("Fruit", t, _lowSphere, mat, new Vector3(-0.18f + (i % 2) * 0.36f, 0.62f + (i / 2) * 0.06f, -0.12f + (i / 2) * 0.24f),
                    f == 5 ? new Vector3(0.18f, 0.18f, 0.42f) : Vector3.one * 0.32f, null, false);
        }

        // ================================================================== trucks

        /// <summary>Delivery vehicle (one per <see cref="TruckKind"/>), facing +Z, cargo door on the left (-X) side.</summary>
        static DeliveryTruck BuildTruck(TruckKind kind, Transform parent)
        {
            var root = B.Node("Truck_" + kind, parent, Vector3.zero);
            var t = root.transform;
            var body = B.Node("Body", t, Vector3.zero).transform;

            float len, cabLen, cargoH;
            Material paint, cab, trim;
            switch (kind)
            {
                case TruckKind.Van:
                    len = 3.8f; cabLen = 1.3f; cargoH = 1.6f;
                    paint = MatLib.Lit("Truck_Van", new Color(0.25f, 0.78f, 0.78f), 0.5f);
                    cab = paint; trim = _mWhite; break;
                case TruckKind.JuiceTruck:
                    len = 4.8f; cabLen = 1.5f; cargoH = 2.1f;
                    paint = MatLib.Lit("Truck_Juice", new Color(1f, 0.62f, 0.18f), 0.5f);
                    cab = MatLib.Lit("Truck_JuiceCab", new Color(1f, 0.85f, 0.25f), 0.5f); trim = _mWhite; break;
                case TruckKind.Resort:
                    len = 5.2f; cabLen = 1.5f; cargoH = 2.1f;
                    paint = MatLib.Lit("Truck_Resort", new Color(1f, 0.62f, 0.72f), 0.5f);
                    cab = MatLib.Lit("Truck_ResortCab", new Color(0.98f, 0.95f, 0.9f), 0.4f); trim = _mBlue; break;
                default:
                    len = 5.2f; cabLen = 1.6f; cargoH = 2.2f;
                    paint = MatLib.Lit("Truck_Premium", new Color(0.14f, 0.13f, 0.18f), 0.8f, null, null, 0.3f);
                    cab = paint; trim = _mGold; break;
            }
            const float w = 2.3f;
            float cargoLen = len - cabLen - 0.1f;
            float zRear = -len * 0.5f;
            float zCab = len * 0.5f - cabLen * 0.5f;
            float zCargo = zRear + cargoLen * 0.5f;

            // Chassis, cab, cargo box.
            RB("Chassis", body, _mDark, new Vector3(0f, 0.55f, 0f), new Vector3(w - 0.1f, 0.3f, len), 0.12f);
            RB("Cab", body, cab, new Vector3(0f, 1.25f, zCab), new Vector3(w, 1.3f, cabLen), 0.3f);
            RB("Windshield", body, _tTruckGlass, new Vector3(0f, 1.55f, zCab + cabLen * 0.5f - 0.02f), new Vector3(w - 0.3f, 0.6f, 0.06f), 0.1f, false);
            RB("SideWinL", body, _tTruckGlass, new Vector3(-w * 0.5f - 0.005f, 1.55f, zCab + 0.1f), new Vector3(0.04f, 0.5f, cabLen * 0.55f), 0.08f, false);
            RB("SideWinR", body, _tTruckGlass, new Vector3(w * 0.5f + 0.005f, 1.55f, zCab + 0.1f), new Vector3(0.04f, 0.5f, cabLen * 0.55f), 0.08f, false);
            RB("Bumper", body, trim, new Vector3(0f, 0.55f, len * 0.5f + 0.02f), new Vector3(w, 0.22f, 0.14f), 0.08f);
            var cargo = RB("Cargo", body, paint, new Vector3(0f, 0.7f + cargoH * 0.5f, zCargo), new Vector3(w + 0.05f, cargoH, cargoLen), 0.18f);
            RB("Stripe", body, trim, new Vector3(0f, 0.95f, zCargo), new Vector3(w + 0.08f, 0.18f, cargoLen - 0.1f), 0.2f, false);
            RB("RoofTrim", body, trim, new Vector3(0f, 0.7f + cargoH + 0.04f, zCargo), new Vector3(w - 0.1f, 0.08f, cargoLen - 0.2f), 0.2f, false);

            // Headlights and exhaust.
            var lights = new List<Renderer>();
            foreach (var sx in new[] { -0.75f, 0.75f })
                lights.Add(B.MeshObj("Headlight", body, _lowSphere, _tHeadOff, new Vector3(sx, 0.85f, len * 0.5f + 0.04f), new Vector3(0.3f, 0.22f, 0.1f), null, false).GetComponent<Renderer>());
            var exhaust = B.Node("Exhaust", body, new Vector3(0.8f, 0.45f, zRear - 0.1f)).transform;
            B.MeshObj("Pipe", body, B.Cylinder, _mSteel, new Vector3(0.8f, 0.45f, zRear), new Vector3(0.12f, 0.12f, 0.12f), new Vector3(90f, 0f, 0f), false);

            // Wheels (node axis = local X so they roll forward).
            var wheels = new List<Transform>();
            foreach (var wz in new[] { zCab, zRear + 0.9f })
            foreach (var sx in new[] { -1f, 1f })
            {
                var wheel = B.Node("Wheel", t, new Vector3(sx * (w * 0.5f - 0.1f), 0.42f, wz)).transform;
                B.MeshObj("Tyre", wheel, B.Cylinder, _tTyre, Vector3.zero, new Vector3(0.84f, 0.16f, 0.84f), new Vector3(0f, 0f, 90f));
                B.MeshObj("Hub", wheel, B.Cylinder, trim, new Vector3(sx * 0.1f, 0f, 0f), new Vector3(0.4f, 0.04f, 0.4f), new Vector3(0f, 0f, 90f), false);
                wheels.Add(wheel);
            }

            // Cargo door (left side) and the load that grows inside.
            float doorLen = Mathf.Min(1.8f, cargoLen - 0.5f);
            float doorZ = zCargo + doorLen * 0.5f;
            var hinge = B.Node("DoorHinge", body, new Vector3(-w * 0.5f - 0.04f, 0.7f + cargoH * 0.5f, doorZ)).transform;
            RB("Door", hinge, trim == _mGold ? _mGold : _mWhite, new Vector3(-0.02f, 0f, -doorLen * 0.5f), new Vector3(0.06f, cargoH - 0.25f, doorLen), 0.05f);
            B.Node("DoorOpening", body, new Vector3(-w * 0.5f - 0.01f, 0.7f + cargoH * 0.5f, doorZ - doorLen * 0.5f));
            B.Box("Hold", body, _mDark, new Vector3(-w * 0.5f + 0.01f, 0.7f + cargoH * 0.5f, doorZ - doorLen * 0.5f), new Vector3(0.02f, cargoH - 0.3f, doorLen - 0.1f), null, false);
            var fill = B.Node("CargoFill", body, new Vector3(-w * 0.5f + 0.06f, 0.78f, doorZ - doorLen * 0.5f)).transform;
            B.Box("Crates", fill, _mWood, new Vector3(0.02f, (cargoH - 0.3f) * 0.5f, 0f), new Vector3(0.04f, cargoH - 0.3f, doorLen - 0.2f), null, false);
            var drop = B.Node("DropPoint", body, new Vector3(-w * 0.5f + 0.4f, 0.7f + cargoH * 0.45f, doorZ - doorLen * 0.5f)).transform;

            // Livery per kind.
            float roofY = 0.7f + cargoH + 0.08f;
            switch (kind)
            {
                case TruckKind.Van:
                {
                    var tx = B.Text("Logo", body, "FRESH", 5f, Color.white, new Vector3(w * 0.5f + 0.06f, 0.7f + cargoH * 0.62f, zCargo));
                    tx.transform.localRotation = Quaternion.Euler(0f, -90f, 0f);
                    break;
                }
                case TruckKind.JuiceTruck:
                    // Giant juice cup on the roof.
                    B.MeshObj("RoofCup", body, _cup, _mJuice[4], new Vector3(0f, roofY, zCargo), new Vector3(1.1f, 1.2f, 1.1f));
                    B.MeshObj("RoofLid", body, _disc, new[] { _mWhite, _mWhite }, new Vector3(0f, roofY + 1.2f, zCargo), new Vector3(1.3f, 0.12f, 1.3f));
                    B.MeshObj("RoofStraw", body, B.Cylinder, _mRed, new Vector3(0.2f, roofY + 1.7f, zCargo), new Vector3(0.14f, 0.55f, 0.14f), new Vector3(0f, 0f, -14f));
                    break;
                case TruckKind.Resort:
                    for (int i = 0; i < 3; i++)
                        RB("Window", body, _tTruckGlass, new Vector3(w * 0.5f + 0.02f, 0.7f + cargoH * 0.62f, zCargo - cargoLen * 0.3f + i * cargoLen * 0.3f), new Vector3(0.04f, 0.55f, cargoLen * 0.22f), 0.1f, false);
                    B.MeshObj("Board1", body, _lowSphere, _mYellow, new Vector3(-0.4f, roofY + 0.1f, zCargo), new Vector3(0.5f, 0.1f, cargoLen * 0.8f));
                    B.MeshObj("Board2", body, _lowSphere, _mBlue, new Vector3(0.4f, roofY + 0.1f, zCargo), new Vector3(0.5f, 0.1f, cargoLen * 0.75f));
                    break;
                default:
                    B.MeshObj("Crown", body, _crown, _mGold, new Vector3(0f, roofY, zCargo), Vector3.one * 0.9f);
                    RB("GoldBand", body, _mGold, new Vector3(0f, 0.7f + cargoH * 0.8f, zCargo), new Vector3(w + 0.08f, 0.1f, cargoLen - 0.1f), 0.2f, false);
                    break;
            }
            _ = cargo;

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
            truck.engine = src;
            truck.board = null; // the order is shown on the sign at the bay
            truck.maxSpeed = kind == TruckKind.Premium ? 8f : 7f;
            SetLayerRecursive(root, 2);
            root.SetActive(false);
            return truck;
        }

        /// <summary>
        /// Standing order sign beside the truck stop (tilted back to face the camera): header plank, client, juice icon
        /// with "x / y", progress bar, reward and time left; between trucks it shows the countdown to the next one.
        /// </summary>
        static TruckBoard BuildOrderBoard(Transform parent, Vector3 localPos)
        {
            var root = B.Node("OrderBoard", parent, localPos).transform;
            foreach (var sx in new[] { -1.55f, 1.55f })
            {
                B.Cyl("Post", root, _tBamboo, new Vector3(sx, 1.5f, 0.12f), 0.16f, 3f);
                B.MeshObj("PostCap", root, _lowSphere, _tThatch, new Vector3(sx, 3.02f, 0.12f), new Vector3(0.3f, 0.2f, 0.3f));
            }
            var panel = B.Node("Panel", root, new Vector3(0f, 2.05f, 0f), new Vector3(22f, 0f, 0f)).transform;
            RB("Board", panel, _mWood, Vector3.zero, new Vector3(3.4f, 2.1f, 0.14f), 0.12f);
            RB("Face", panel, _mCream, new Vector3(0f, -0.04f, -0.075f), new Vector3(3.08f, 1.68f, 0.02f), 0.1f, false);
            RB("Header", panel, MatLib.Lit("BoardHeader", new Color(1f, 0.55f, 0.18f), 0.3f), new Vector3(0f, 1.02f, -0.06f), new Vector3(2.5f, 0.5f, 0.1f), 0.12f);
            var ink = new Color(0.36f, 0.22f, 0.14f);
            TextMeshPro T(string name, Transform p, string text, float size, Color c, Vector3 pos, float w, bool outline = false)
            {
                var t = B.Text(name, p, text, size, c, pos, outline);
                t.rectTransform.sizeDelta = new Vector2(w, 0.6f);
                t.enableAutoSizing = true;
                t.fontSizeMin = size * 0.5f;
                t.fontSizeMax = size;
                t.GetComponent<MeshRenderer>().sortingOrder = 6;
                return t;
            }
            var title = T("Title", panel, "NEXT TRUCK", 3.6f, Color.white, new Vector3(0f, 1.03f, -0.12f), 2.3f, true);

            var order = B.Node("Order", panel, Vector3.zero).transform;
            var client = T("Client", order, "CLIENT", 3.4f, new Color(0.95f, 0.45f, 0.1f), new Vector3(0f, 0.52f, -0.1f), 2.8f);
            var icon = B.Sprite("Juice", order, _sJuiceIcons[3] ? _sJuiceIcons[3] : _sJuice, new Vector3(-1.05f, 0.02f, -0.1f), SpriteScale(_sJuice, 0.66f), false, 6);
            var count = T("Count", order, "0 / 20", 6f, ink, new Vector3(0.3f, 0.02f, -0.1f), 2.1f);
            var bar = B.Node("Bar", order, new Vector3(0f, -0.4f, -0.1f), null, new Vector3(2.7f, 0.2f, 1f));
            B.Sprite("Bg", bar.transform, _sWhite, Vector3.zero, 1f, false, 6, new Color(0.3f, 0.22f, 0.18f, 0.8f));
            var fill = B.Sprite("Fill", bar.transform, _sWhite, new Vector3(0f, 0f, -0.001f), 1f, false, 7, new Color(0.4f, 0.9f, 0.35f));
            var coinSprite = _uCoin ? _uCoin : _sCoin;
            B.Sprite("Coin", order, coinSprite, new Vector3(-1.1f, -0.72f, -0.1f), SpriteScale(coinSprite, 0.34f), false, 6);
            var reward = T("Reward", order, "$0", 3.8f, new Color(0.25f, 0.62f, 0.2f), new Vector3(-0.35f, -0.72f, -0.1f), 1.3f);
            reward.alignment = TextAlignmentOptions.MidlineLeft;
            var timer = T("Timer", order, "3:00", 3.6f, ink, new Vector3(1.0f, -0.72f, -0.1f), 0.9f);
            timer.alignment = TextAlignmentOptions.MidlineRight;

            var wait = B.Node("Wait", panel, Vector3.zero).transform;
            B.Sprite("Truck", wait, _sTruck, new Vector3(0f, 0.28f, -0.1f), SpriteScale(_sTruck, 0.95f), false, 6);
            var waitT = T("Countdown", wait, "0:20", 6f, ink, new Vector3(0f, -0.48f, -0.1f), 2f);
            wait.gameObject.SetActive(false);

            var tb = root.gameObject.AddComponent<TruckBoard>();
            tb.titleText = title;
            tb.clientText = client;
            tb.countText = count;
            tb.rewardText = reward;
            tb.timerText = timer;
            tb.juiceIcon = icon;
            tb.progressFill = fill.transform;
            tb.progressRenderer = fill;
            tb.orderGroup = order.gameObject;
            tb.waitGroup = wait.gameObject;
            tb.waitText = waitT;
            tb.panel = panel;
            var col = B.Node("Col", root, new Vector3(0f, 1f, 0.12f)).AddComponent<BoxCollider>();
            col.size = new Vector3(3.4f, 2f, 0.3f);
            return tb;
        }

        // ================================================================== ambient life

        static void BuildBirds(Transform parent, Vector3[] centers, bool gulls)
        {
            var root = B.Node(gulls ? "Gulls" : "Parrots", parent, Vector3.zero);
            var comp = root.AddComponent<Birds>();
            var bodyMat = gulls ? _mWhite : MatLib.Lit("Parrot", new Color(0.95f, 0.25f, 0.2f), 0.3f);
            var wingMat = gulls ? MatLib.Lit("GullWing", new Color(0.78f, 0.8f, 0.84f), 0.2f) : MatLib.Lit("ParrotWing", new Color(0.2f, 0.62f, 0.95f), 0.3f);
            var list = new List<Birds.Bird>();
            for (int i = 0; i < centers.Length; i++)
            {
                var body = B.Node("Bird", root.transform, centers[i]).transform;
                B.MeshObj("Body", body, _lowSphere, bodyMat, Vector3.zero, new Vector3(0.22f, 0.2f, 0.55f), null, false);
                B.MeshObj("Head", body, _lowSphere, bodyMat, new Vector3(0f, 0.08f, 0.28f), Vector3.one * 0.18f, null, false);
                B.MeshObj("Beak", body, _cone, _mDuckBeak, new Vector3(0f, 0.07f, 0.4f), new Vector3(0.06f, 0.12f, 0.06f), new Vector3(90f, 0f, 0f), false);
                Transform Wing(float side)
                {
                    var wgo = B.Node(side < 0 ? "WingL" : "WingR", body, new Vector3(side * 0.08f, 0.04f, 0f)).transform;
                    B.Box("Blade", wgo, wingMat, new Vector3(side * 0.42f, 0f, 0f), new Vector3(0.8f, 0.03f, 0.3f), new Vector3(0f, side * 10f, 0f), false);
                    return wgo;
                }
                list.Add(new Birds.Bird
                {
                    body = body, wingL = Wing(-1f), wingR = Wing(1f), center = centers[i],
                    radius = 5f + (float)_rnd.NextDouble() * 5f, height = gulls ? 7f + (float)_rnd.NextDouble() * 2f : 5.5f + (float)_rnd.NextDouble(),
                    speed = (gulls ? 4.5f : 5.5f) * (_rnd.Next(2) == 0 ? 1f : -1f), phase = (float)_rnd.NextDouble() * 6f
                });
            }
            comp.birds = list.ToArray();
        }

        static void Sailboat(Transform parent, Vector3[] path, Material sail, float speed, float offset)
        {
            var root = B.Node("Sailboat", parent, path[0]);
            var bob = B.Node("Bob", root.transform, Vector3.zero).transform;
            RB("Hull", bob, _mWhite, new Vector3(0f, 0.15f, 0f), new Vector3(1.4f, 0.5f, 3.6f), 0.6f);
            RB("Deck", bob, _mWood, new Vector3(0f, 0.42f, 0f), new Vector3(1.2f, 0.06f, 3.2f), 0.5f, false);
            B.Cyl("Mast", bob, _mWood, new Vector3(0f, 2.2f, 0.3f), 0.12f, 3.8f);
            B.MeshObj("Sail", bob, FlagMesh(), sail, new Vector3(0f, 3.9f, 0.3f), new Vector3(2.4f, 3.6f, 1f), new Vector3(0f, 90f, 0f));
            var pm = root.AddComponent<PathMover>();
            pm.points = path;
            pm.speed = speed;
            pm.pingPong = true;
            pm.endPause = 3f;
            pm.startOffset = offset;
            pm.bob = bob;
            pm.bobAmount = 0.12f;
        }
    }
}
