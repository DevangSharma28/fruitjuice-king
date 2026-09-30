using System.Collections.Generic;
using UnityEngine;

namespace JuiceKing.EditorTools
{
    /// <summary>Procedural mesh builder with sub-mesh support (for slices, cups, funnels, arrows...).</summary>
    public class MeshGen
    {
        readonly List<Vector3> _v = new List<Vector3>();
        readonly List<Vector3> _n = new List<Vector3>();
        readonly List<Vector2> _uv = new List<Vector2>();
        readonly List<List<int>> _subs = new List<List<int>>();

        List<int> Sub(int i)
        {
            while (_subs.Count <= i) _subs.Add(new List<int>());
            return _subs[i];
        }

        int Vert(Vector3 p, Vector3 n, Vector2 uv)
        {
            _v.Add(p);
            _n.Add(n);
            _uv.Add(uv);
            return _v.Count - 1;
        }

        void Tri(int sub, int a, int b, int c)
        {
            var s = Sub(sub);
            s.Add(a);
            s.Add(b);
            s.Add(c);
        }

        /// <summary>Frustum/cylinder side along +Y. Outward normals unless inside=true.</summary>
        public void Side(float r0, float r1, float y0, float y1, int seg, int sub, bool inside = false, float vScale = 1f)
        {
            float slope = (r0 - r1) / Mathf.Max(0.0001f, y1 - y0);
            int start = _v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float cx = Mathf.Cos(a), cz = Mathf.Sin(a);
                Vector3 n = new Vector3(cx, slope, cz).normalized;
                if (inside) n = -n;
                Vert(new Vector3(cx * r0, y0, cz * r0), n, new Vector2(i / (float)seg, 0f));
                Vert(new Vector3(cx * r1, y1, cz * r1), n, new Vector2(i / (float)seg, vScale));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = start + i * 2, b = a + 1, c = a + 2, d = a + 3;
                if (!inside)
                {
                    Tri(sub, a, b, c);
                    Tri(sub, c, b, d);
                }
                else
                {
                    Tri(sub, a, c, b);
                    Tri(sub, c, d, b);
                }
            }
        }

        /// <summary>Flat disc cap at height y facing up (or down). UV planar mapped to the unit circle.</summary>
        public void Cap(float r, float y, int seg, int sub, bool up, float uvScale = 1f)
        {
            Vector3 n = up ? Vector3.up : Vector3.down;
            int c = Vert(new Vector3(0, y, 0), n, new Vector2(0.5f, 0.5f));
            int start = _v.Count;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float x = Mathf.Cos(a), z = Mathf.Sin(a);
                Vert(new Vector3(x * r, y, z * r), n, new Vector2(0.5f + x * 0.5f * uvScale, 0.5f + z * 0.5f * uvScale));
            }
            for (int i = 0; i < seg; i++)
            {
                if (up) Tri(sub, c, start + i + 1, start + i);
                else Tri(sub, c, start + i, start + i + 1);
            }
        }

        /// <summary>Cone tip at (0, yTip, 0) with base radius r at y0.</summary>
        public void Cone(float r, float y0, float yTip, int seg, int sub, bool capBase = true)
        {
            int start = _v.Count;
            float h = yTip - y0;
            for (int i = 0; i <= seg; i++)
            {
                float a = i / (float)seg * Mathf.PI * 2f;
                float x = Mathf.Cos(a), z = Mathf.Sin(a);
                Vector3 n = new Vector3(x * h, r, z * h).normalized;
                if (h < 0) n = new Vector3(x * -h, -r, z * -h).normalized;
                Vert(new Vector3(x * r, y0, z * r), n, new Vector2(i / (float)seg, 0));
                Vert(new Vector3(0, yTip, 0), n, new Vector2(i / (float)seg, 1));
            }
            for (int i = 0; i < seg; i++)
            {
                int a = start + i * 2, t = a + 1, b = a + 2;
                if (h > 0) Tri(sub, a, t, b);
                else Tri(sub, a, b, t);
            }
            if (capBase) Cap(r, y0, seg, sub, h < 0);
        }

        public void Box(Vector3 c, Vector3 s, int sub)
        {
            Vector3 h = s * 0.5f;
            Face(c, new Vector3(0, 0, h.z), new Vector3(h.x, 0, 0), new Vector3(0, h.y, 0), sub);
            Face(c, new Vector3(0, 0, -h.z), new Vector3(-h.x, 0, 0), new Vector3(0, h.y, 0), sub);
            Face(c, new Vector3(h.x, 0, 0), new Vector3(0, 0, -h.z), new Vector3(0, h.y, 0), sub);
            Face(c, new Vector3(-h.x, 0, 0), new Vector3(0, 0, h.z), new Vector3(0, h.y, 0), sub);
            Face(c, new Vector3(0, h.y, 0), new Vector3(h.x, 0, 0), new Vector3(0, 0, -h.z), sub);
            Face(c, new Vector3(0, -h.y, 0), new Vector3(h.x, 0, 0), new Vector3(0, 0, h.z), sub);
        }

        /// <summary>Quad centred at c + normalOffset with half-axes u (right) and v (up) as seen from outside.</summary>
        void Face(Vector3 c, Vector3 nOff, Vector3 u, Vector3 v, int sub)
        {
            Vector3 n = nOff.normalized;
            Vector3 o = c + nOff;
            // Winding: Unity is clockwise front faces when viewed from the normal side.
            int a = Vert(o - u - v, n, new Vector2(0, 0));
            int b = Vert(o - u + v, n, new Vector2(0, 1));
            int cc = Vert(o + u + v, n, new Vector2(1, 1));
            int d = Vert(o + u - v, n, new Vector2(1, 0));
            // Ensure winding matches the normal.
            Vector3 cross = Vector3.Cross(_v[b] - _v[a], _v[cc] - _v[a]);
            if (Vector3.Dot(cross, n) > 0f)
            {
                Tri(sub, a, b, cc);
                Tri(sub, a, cc, d);
            }
            else
            {
                Tri(sub, a, cc, b);
                Tri(sub, a, d, cc);
            }
        }

        /// <summary>
        /// Box with rounded vertical edges (a rounded-rectangle prism), centred on c. Smooth normals around the corners.
        /// </summary>
        public void RoundedBox(Vector3 c, Vector3 size, float radius, int cornerSeg, int sub)
        {
            float hx = size.x * 0.5f, hz = size.z * 0.5f;
            float r = Mathf.Min(radius, Mathf.Min(hx, hz) - 0.0001f);
            float y0 = c.y - size.y * 0.5f, y1 = c.y + size.y * 0.5f;
            // Outline points counter-clockwise (seen from above), with outward normals.
            var pts = new List<Vector2>();
            var nrm = new List<Vector2>();
            Vector2[] centres = { new Vector2(hx - r, hz - r), new Vector2(-hx + r, hz - r), new Vector2(-hx + r, -hz + r), new Vector2(hx - r, -hz + r) };
            for (int k = 0; k < 4; k++)
            for (int i = 0; i <= cornerSeg; i++)
            {
                float a = (k * 90f + i * 90f / cornerSeg) * Mathf.Deg2Rad;
                var n = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                pts.Add(centres[k] + n * r);
                nrm.Add(n);
            }
            int m = pts.Count;
            // Side wall.
            int start = _v.Count;
            for (int i = 0; i <= m; i++)
            {
                int j = i % m;
                Vector3 n = new Vector3(nrm[j].x, 0f, nrm[j].y);
                float u = i / (float)m;
                Vert(new Vector3(c.x + pts[j].x, y0, c.z + pts[j].y), n, new Vector2(u * 4f, 0f));
                Vert(new Vector3(c.x + pts[j].x, y1, c.z + pts[j].y), n, new Vector2(u * 4f, 1f));
            }
            for (int i = 0; i < m; i++)
            {
                int a = start + i * 2, b = a + 1, cc = a + 2, d = a + 3;
                Tri(sub, a, b, cc);
                Tri(sub, cc, b, d);
            }
            // Caps (fans).
            for (int capSide = 0; capSide < 2; capSide++)
            {
                bool top = capSide == 1;
                float y = top ? y1 : y0;
                Vector3 n = top ? Vector3.up : Vector3.down;
                int ci = Vert(new Vector3(c.x, y, c.z), n, new Vector2(0.5f, 0.5f));
                int first = _v.Count;
                for (int i = 0; i < m; i++)
                    Vert(new Vector3(c.x + pts[i].x, y, c.z + pts[i].y), n, new Vector2(0.5f + pts[i].x / size.x, 0.5f + pts[i].y / size.z));
                for (int i = 0; i < m; i++)
                {
                    int a = first + i, b = first + (i + 1) % m;
                    if (top) Tri(sub, ci, b, a);
                    else Tri(sub, ci, a, b);
                }
            }
        }

        /// <summary>
        /// Box with every edge and corner rounded by <paramref name="radius"/> (a "toy" block), centred on c, smooth
        /// normals. Face grids are spaced with tan() so the rounded bands get evenly spread vertices.
        /// </summary>
        public void BeveledBox(Vector3 c, Vector3 size, float radius, int seg, int sub)
        {
            Vector3 h = size * 0.5f;
            float r = Mathf.Clamp(radius, 0f, Mathf.Min(h.x, Mathf.Min(h.y, h.z)) * 0.98f);
            seg = Mathf.Max(1, seg);
            Vector3 inner = new Vector3(h.x - r, h.y - r, h.z - r);

            float[] Axis(float half, float inr)
            {
                var list = new List<float>();
                for (int k = seg; k >= 1; k--) list.Add(-inr - r * Mathf.Tan(k / (float)seg * Mathf.PI * 0.25f));
                list.Add(-inr);
                if (inr > 0.0001f) list.Add(inr);
                for (int k = 1; k <= seg; k++) list.Add(inr + r * Mathf.Tan(k / (float)seg * Mathf.PI * 0.25f));
                return list.ToArray();
            }
            float[] ax = Axis(h.x, inner.x), ay = Axis(h.y, inner.y), az = Axis(h.z, inner.z);

            void FaceGrid(Vector3 n, Vector3 u, Vector3 v, float[] cu, float[] cv, float halfN, float sizeU, float sizeV)
            {
                int start = _v.Count;
                for (int j = 0; j < cv.Length; j++)
                for (int i = 0; i < cu.Length; i++)
                {
                    Vector3 p = n * halfN + u * cu[i] + v * cv[j];
                    Vector3 q = new Vector3(Mathf.Clamp(p.x, -inner.x, inner.x), Mathf.Clamp(p.y, -inner.y, inner.y), Mathf.Clamp(p.z, -inner.z, inner.z));
                    Vector3 d = p - q;
                    Vector3 nn = d.sqrMagnitude > 1e-10f ? d.normalized : n;
                    Vector3 pos = r > 0f ? q + nn * r : p;
                    Vert(c + pos, nn, new Vector2(cu[i] / sizeU + 0.5f, cv[j] / sizeV + 0.5f));
                }
                bool ccw = Vector3.Dot(Vector3.Cross(u, v), n) > 0f;
                int w = cu.Length;
                for (int j = 0; j < cv.Length - 1; j++)
                for (int i = 0; i < w - 1; i++)
                {
                    int a = start + j * w + i, b = a + 1, cc = a + w, d = cc + 1;
                    if (ccw)
                    {
                        Tri(sub, a, b, cc);
                        Tri(sub, b, d, cc);
                    }
                    else
                    {
                        Tri(sub, a, cc, b);
                        Tri(sub, b, cc, d);
                    }
                }
            }

            FaceGrid(Vector3.right, Vector3.forward, Vector3.up, az, ay, h.x, size.z, size.y);
            FaceGrid(Vector3.left, Vector3.back, Vector3.up, az, ay, h.x, size.z, size.y);
            FaceGrid(Vector3.up, Vector3.right, Vector3.forward, ax, az, h.y, size.x, size.z);
            FaceGrid(Vector3.down, Vector3.right, Vector3.back, ax, az, h.y, size.x, size.z);
            FaceGrid(Vector3.forward, Vector3.left, Vector3.up, ax, ay, h.z, size.x, size.y);
            FaceGrid(Vector3.back, Vector3.right, Vector3.up, ax, ay, h.z, size.x, size.y);
        }

        public Mesh ToMesh(string name)
        {
            var m = new Mesh { name = name };
            m.SetVertices(_v);
            m.SetNormals(_n);
            m.SetUVs(0, _uv);
            m.subMeshCount = _subs.Count;
            for (int i = 0; i < _subs.Count; i++) m.SetTriangles(_subs[i], i);
            m.RecalculateBounds();
            m.RecalculateTangents();
            return m;
        }
    }
}
