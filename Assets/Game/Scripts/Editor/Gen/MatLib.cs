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

        static Shader _lit, _unlit, _sprite;
        static Shader LitShader => _lit != null ? _lit : _lit = Shader.Find("Universal Render Pipeline/Lit");
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

        public static Material Lit(string name, Color color, float smoothness = 0.15f, Texture tex = null, Vector2? tiling = null, float metallic = 0f)
        {
            var m = GetOrCreate(name, LitShader);
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

        /// <summary>Transparent URP Lit (glass).</summary>
        public static Material Glass(string name, Color color, float smoothness = 0.9f)
        {
            var m = Lit(name, color, smoothness);
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
