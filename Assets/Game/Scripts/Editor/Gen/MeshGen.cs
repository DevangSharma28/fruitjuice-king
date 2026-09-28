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
