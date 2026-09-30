using UnityEngine;

namespace JuiceKing
{
    /// <summary>Golden Apple rewards and spends: the currency change plus the sparkle, sound and flying apples.</summary>
    public static class AppleFx
    {
        static readonly Color Gold = new Color(1f, 0.82f, 0.25f);

        /// <summary>Give <paramref name="count"/> apples earned at a world position (they fly up to the counter).</summary>
        public static void Reward(Vector3 worldPos, int count)
        {
            if (GameManager.I == null || count <= 0) return;
            GameManager.I.AddApples(count);
            Fx.Sparkle(worldPos, Gold, 10);
            Fx.Stars(worldPos, 6, Gold);
            Sfx.Play(SfxId.Chime, 0.5f);
            FloatingText.Show("+" + count + " GOLDEN APPLE" + (count > 1 ? "S" : ""), worldPos + Vector3.up * 0.6f, Gold, 1.1f, 1.3f, 1.1f);
            if (HUD.I != null) HUD.I.FlyApples(worldPos, count);
        }

        /// <summary>Feedback where apples were spent (the counter change itself comes from GameManager).</summary>
        public static void Spend(Vector3 worldPos)
        {
            Fx.Sparkle(worldPos, Gold, 14);
            Fx.Ring(new Vector3(worldPos.x, 0.05f, worldPos.z), new Color(1f, 0.85f, 0.3f, 0.9f), 4f);
            Sfx.Play(SfxId.Chime, 0.55f, 0.9f);
            Sfx.Play(SfxId.Sparkle, 0.4f);
        }
    }
}
