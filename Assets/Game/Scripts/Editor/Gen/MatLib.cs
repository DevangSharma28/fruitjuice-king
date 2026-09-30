using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace JuiceKing.EditorTools
{
    /// <summary>Creates / updates material assets.</summary>
    public static class MatLib
    {
        public const string Dir = "Assets/Game/Generated/Materials/";

        static Shader _lit, _unlit, _sprite, _stylized, _water;
        /// <summary>URP Lit: only for transparent surfaces (glass, soft grass patches). Opaque materials use <see cref="StylizedShader"/>.</summary>
        static Shader LitShader => _lit != null ? _lit : _lit = Shader.Find("Universal Render Pipeline/Lit");
        static Shader StylizedShader => _stylized != null ? _stylized : _stylized = Shader.Find("JuiceKing/Stylized");
        static Shader WaterShader => _water != null ? _water : _water = Shader.Find("JuiceKing/Water");
        static Shader UnlitShader => _unlit != null ? _unlit : _unlit = Shader.Find("Universal Render Pipeline/Unlit");
        static Shader SpriteShader => _sprite != null ? _sprite : _sprite = Shader.Find("Sprites/Default");

        static Material GetOrCreate(string name, Shader shader)
        {
            string path = Dir + name + ".mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(path));
                m = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(m, path);
            }
            else if (m.shader != shader) m.shader = shader;
            return m;
        }

        public static Texture2D Tex(string file) => AssetDatabase.LoadAssetAtPath<Texture2D>(ArtGen.Dir + file);
        public static Sprite Spr(string file) => AssetDatabase.LoadAssetAtPath<Sprite>(ArtGen.Dir + file);

        /// <summary>Opaque stylized material (JuiceKing/Stylized): soft wrapped light, tinted shade, rim, contact darkening.</summary>
        public static Material Lit(string name, Color color, float smoothness = 0.15f, Texture tex = null, Vector2? tiling = null, float metallic = 0f)
        {
            var m = GetOrCreate(name, StylizedShader != null ? StylizedShader : LitShader);
            ClearTransparency(m);
            StylizeDefaults(m);
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Smoothness", smoothness);
            m.SetFloat("_Metallic", metallic);
            var t = tiling ?? Vector2.one;
            m.SetTextureScale("_BaseMap", t);
            m.mainTextureScale = t;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        public static Material Unlit(string name, Color color, Texture tex = null)
        {
            var m = GetOrCreate(name, UnlitShader);
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", tex);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Unlit, alpha blended, vertex-coloured: pads, pointers, particles, bars.</summary>
        public static Material Sprite(string name, Texture tex, Color color)
        {
            var m = GetOrCreate(name, SpriteShader);
            m.mainTexture = tex;
            m.color = color;
            m.renderQueue = 3000;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>URP Lit material (for transparent surfaces that need its blend modes).</summary>
        public static Material LitURP(string name, Color color, float smoothness = 0.15f, Texture tex = null, Vector2? tiling = null)
        {
            var m = GetOrCreate(name, LitShader);
            m.SetColor("_BaseColor", color);
            m.SetTexture("_BaseMap", tex);
            m.SetFloat("_Smoothness", smoothness);
            var t = tiling ?? Vector2.one;
            m.SetTextureScale("_BaseMap", t);
            m.mainTextureScale = t;
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>Stylized water (JuiceKing/Water): waves, drifting caustics, sun sparkles. Opaque.</summary>
        public static Material Water(string name, Color shallow, Color deep, float causticTile = 0.12f, float waveHeight = 0.05f, float sparkle = 0.8f)
        {
            var m = GetOrCreate(name, WaterShader != null ? WaterShader : LitShader);
            ClearTransparency(m);
            m.SetColor("_ShallowColor", shallow);
            m.SetColor("_DeepColor", deep);
            m.SetTexture("_BaseMap", Tex("caustics.png"));
            m.SetTextureScale("_BaseMap", new Vector2(causticTile, causticTile));
            m.SetFloat("_WaveHeight", waveHeight);
            m.SetFloat("_Sparkle", sparkle);
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        /// <summary>A material that used to be transparent URP Lit keeps its queue/tags after a shader switch: reset them.</summary>
        static void ClearTransparency(Material m)
        {
            m.renderQueue = -1;
            m.SetOverrideTag("RenderType", "");
            m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("ShadowCaster", true);
        }

        /// <summary>Stylized-shader extras every opaque material gets (painterly noise, tinted shade, contact darkening).</summary>
        static void StylizeDefaults(Material m)
        {
            if (!m.HasProperty("_DetailMap")) return;
            var noise = Tex("noise_soft.png");
            if (noise != null) m.SetTexture("_DetailMap", noise);
        }

        /// <summary>
        /// Switch every opaque URP Lit material under Generated/Materials (including the Kenney model materials) to the
        /// stylized shader. Colours, textures and tiling carry over (same property names).
        /// </summary>
        public static int StylizeAll()
        {
            if (StylizedShader == null) return 0;
            int n = 0;
            foreach (var g in AssetDatabase.FindAssets("t:Material", new[] { Dir.TrimEnd('/') }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(g));
                if (m == null || m.shader != LitShader) continue;
                if (m.HasProperty("_Surface") && m.GetFloat("_Surface") > 0.5f) continue; // transparent: keep URP Lit
                m.shader = StylizedShader;
                StylizeDefaults(m);
                EditorUtility.SetDirty(m);
                n++;
            }
            return n;
        }

        /// <summary>Transparent URP Lit (glass).</summary>
        public static Material Glass(string name, Color color, float smoothness = 0.9f)
        {
            var m = LitURP(name, color, smoothness);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            m.SetShaderPassEnabled("DepthOnly", false);
            m.SetShaderPassEnabled("ShadowCaster", false);
            EditorUtility.SetDirty(m);
            return m;
        }
    }
}
