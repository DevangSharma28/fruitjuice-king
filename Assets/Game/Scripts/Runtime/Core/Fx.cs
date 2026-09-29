using UnityEngine;

namespace JuiceKing
{
    /// <summary>Particle textures, index into <see cref="GameRefs.fxMaterials"/>.</summary>
    public enum FxShape { Circle = 0, Star = 1, Heart = 2, Ring = 3, Drop = 4, Leaf = 5, Sparkle = 6, Coin = 7, Splat = 8, Count = 9 }

    /// <summary>Shared burst particle systems, emitted on demand (no per-effect instantiation).</summary>
    public static class Fx
    {
        static ParticleSystem _splash, _chips, _confetti, _poof, _sparkle;
        static ParticleSystem _drops, _splat, _ring, _stars, _hearts, _dust, _leaves, _coins, _glints, _trail, _bubbles, _smoke;

        static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.35f, 0.4f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.85f, 0.45f),
            new Color(0.3f, 0.6f, 1f), new Color(0.85f, 0.45f, 1f), Color.white
        };

        static Material Mat(FxShape s)
        {
            var refs = GameRefs.I;
            if (refs == null) return null;
            if (refs.fxMaterials != null && (int)s < refs.fxMaterials.Length && refs.fxMaterials[(int)s] != null)
                return refs.fxMaterials[(int)s];
            return refs.particleMaterial;
        }

        static void Ensure()
        {
            if (_splash != null) return;
            var root = new GameObject("[Fx]").transform;
            Object.DontDestroyOnLoad(root.gameObject);

            _splash = Create(root, "Splash", Mat(FxShape.Circle), 0.35f, 0.75f, 2.5f, 6f, 0.16f, 0.38f, 2.2f, ParticleSystemShapeType.Hemisphere);
            _chips = Create(root, "Chips", Mat(FxShape.Circle), 0.25f, 0.5f, 1.5f, 4f, 0.09f, 0.2f, 2f, ParticleSystemShapeType.Sphere);
            _confetti = Create(root, "Confetti", Mat(FxShape.Circle), 1.2f, 2f, 5f, 9f, 0.18f, 0.32f, 0.9f, ParticleSystemShapeType.Cone);
            _poof = Create(root, "Poof", Mat(FxShape.Circle), 0.4f, 0.7f, 0.6f, 1.6f, 0.3f, 0.6f, -0.1f, ParticleSystemShapeType.Sphere);
            _sparkle = Create(root, "Sparkle", Mat(FxShape.Sparkle), 0.4f, 0.8f, 1f, 3f, 0.2f, 0.4f, -0.2f, ParticleSystemShapeType.Sphere);

            var shape = _confetti.shape;
            shape.angle = 30f;
            shape.rotation = new Vector3(-90f, 0f, 0f);
            var rot = _confetti.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-6f, 6f);
            var noise = _confetti.noise;
            noise.enabled = true;
            noise.strength = 0.6f;
            noise.frequency = 0.8f;

            // Juice droplets: stretched along velocity so they read as liquid.
            _drops = Create(root, "Drops", Mat(FxShape.Drop), 0.45f, 0.8f, 3f, 7f, 0.14f, 0.28f, 2.6f, ParticleSystemShapeType.Hemisphere);
            var dr = _drops.GetComponent<ParticleSystemRenderer>();
            dr.renderMode = ParticleSystemRenderMode.Stretch;
            dr.velocityScale = 0.06f;
            dr.lengthScale = 1.4f;
            var dshape = _drops.shape;
            dshape.radius = 0.3f;

            // Ground splats that linger, then fade.
            _splat = Create(root, "Splat", Mat(FxShape.Splat), 2.4f, 3.2f, 0f, 0f, 0.5f, 1.1f, 0f, ParticleSystemShapeType.Circle, false);
            _splat.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            _splat.GetComponent<ParticleSystemRenderer>().sortingFudge = 10f;
            SizeCurve(_splat, new Keyframe(0f, 0.2f), new Keyframe(0.06f, 1.1f), new Keyframe(0.12f, 1f), new Keyframe(1f, 0.9f));
            AlphaFade(_splat, 0.75f);
            var sshape = _splat.shape;
            sshape.radius = 0.5f;
            sshape.rotation = new Vector3(90f, 0f, 0f);

            // Expanding shockwave ring on the ground.
            _ring = Create(root, "Ring", Mat(FxShape.Ring), 0.45f, 0.45f, 0f, 0f, 1f, 1f, 0f, ParticleSystemShapeType.Sphere, false);
            _ring.GetComponent<ParticleSystemRenderer>().renderMode = ParticleSystemRenderMode.HorizontalBillboard;
            SizeCurve(_ring, new Keyframe(0f, 0.1f, 0f, 4f), new Keyframe(1f, 1f));
            AlphaFade(_ring, 0.2f);
            var rshape = _ring.shape;
            rshape.radius = 0.001f;

            // Spinning stars (unlocks, upgrades, rewards).
            _stars = Create(root, "Stars", Mat(FxShape.Star), 0.7f, 1.2f, 3f, 7f, 0.34f, 0.6f, 1.3f, ParticleSystemShapeType.Hemisphere);
            Spin(_stars, 4f);

            // Hearts floating up from happy customers.
            _hearts = Create(root, "Hearts", Mat(FxShape.Heart), 0.9f, 1.3f, 0.6f, 1.4f, 0.34f, 0.5f, -0.35f, ParticleSystemShapeType.Sphere);
            var hr = _hearts.main;
            hr.startRotation = 0f;
            var hn = _hearts.noise;
            hn.enabled = true;
            hn.strength = 0.4f;
            hn.frequency = 1.2f;

            // Footstep dust.
            _dust = Create(root, "Dust", Mat(FxShape.Circle), 0.35f, 0.6f, 0.3f, 0.8f, 0.2f, 0.4f, -0.15f, ParticleSystemShapeType.Sphere);
            SizeCurve(_dust, new Keyframe(0f, 0.4f), new Keyframe(0.3f, 1f), new Keyframe(1f, 1.3f));
            AlphaFade(_dust, 0.1f);

            // Leaves flying off a burst fruit.
            _leaves = Create(root, "Leaves", Mat(FxShape.Leaf), 0.8f, 1.4f, 2.5f, 5f, 0.22f, 0.38f, 0.8f, ParticleSystemShapeType.Hemisphere);
            Spin(_leaves, 5f);
            var ln = _leaves.noise;
            ln.enabled = true;
            ln.strength = 0.8f;
            ln.frequency = 1.5f;

            // Gold coins bursting out.
            _coins = Create(root, "Coins", Mat(FxShape.Coin), 0.6f, 0.9f, 3f, 6f, 0.3f, 0.45f, 2.4f, ParticleSystemShapeType.Hemisphere);
            Spin(_coins, 6f);

            // Tiny glints (juice cups, money on the counter).
            _glints = Create(root, "Glints", Mat(FxShape.Sparkle), 0.3f, 0.5f, 0.2f, 0.8f, 0.3f, 0.5f, -0.1f, ParticleSystemShapeType.Sphere);
            SizeCurve(_glints, new Keyframe(0f, 0f), new Keyframe(0.3f, 1f), new Keyframe(1f, 0f));

            // Turbo trail.
            _trail = Create(root, "Trail", Mat(FxShape.Circle), 0.3f, 0.45f, 0f, 0.2f, 0.18f, 0.3f, 0f, ParticleSystemShapeType.Sphere);
            AlphaFade(_trail, 0f);

            // Bubbles rising from blending juice.
            _bubbles = Create(root, "Bubbles", Mat(FxShape.Ring), 0.4f, 0.7f, 0.4f, 1f, 0.05f, 0.11f, -0.4f, ParticleSystemShapeType.Cone);
            // Truck exhaust: soft grey puffs that grow and drift up.
            _smoke = Create(root, "Smoke", Mat(FxShape.Circle), 0.8f, 1.3f, 0.3f, 0.8f, 0.2f, 0.34f, -0.25f, ParticleSystemShapeType.Sphere);
            SizeCurve(_smoke, new Keyframe(0f, 0.5f), new Keyframe(1f, 2.4f));
            AlphaFade(_smoke, 0.05f);

            var bshape = _bubbles.shape;
            bshape.angle = 12f;
            bshape.radius = 0.22f;
        }

        static ParticleSystem Create(Transform root, string name, Material mat, float lifeMin, float lifeMax,
            float speedMin, float speedMax, float sizeMin, float sizeMax, float gravity, ParticleSystemShapeType shapeType, bool shrink = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            // Keep the system alive (emission rate is zero): particles emitted into a *stopped* system are cleared next frame.
            main.loop = true;
            main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifeMin, lifeMax);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speedMin, speedMax);
            main.startSize = new ParticleSystem.MinMaxCurve(sizeMin, sizeMax);
            main.gravityModifier = gravity;
            main.maxParticles = 1000;
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);

            var em = ps.emission;
            em.enabled = false;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = shapeType;
            shape.radius = 0.15f;
            // Cones and hemispheres face +Z by default; point them up.
            if (shapeType == ParticleSystemShapeType.Hemisphere || shapeType == ParticleSystemShapeType.Cone)
                shape.rotation = new Vector3(-90f, 0f, 0f);

            if (shrink)
            {
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                    new Keyframe(0f, 1f), new Keyframe(0.7f, 0.85f), new Keyframe(1f, 0f)));
            }

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (mat != null) r.sharedMaterial = mat;

            ps.Play();
            return ps;
        }

        static void SizeCurve(ParticleSystem ps, params Keyframe[] keys)
        {
            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(keys));
        }

        /// <summary>Fade alpha out, starting at <paramref name="from"/> (0..1 of lifetime).</summary>
        static void AlphaFade(ParticleSystem ps, float from)
        {
            var col = ps.colorOverLifetime;
            col.enabled = true;
            var g = new Gradient();
            g.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, Mathf.Clamp01(from)), new GradientAlphaKey(0f, 1f) });
            col.color = g;
        }

        static void Spin(ParticleSystem ps, float speed)
        {
            var rot = ps.rotationOverLifetime;
            rot.enabled = true;
            rot.z = new ParticleSystem.MinMaxCurve(-speed, speed);
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Color c, int count, float size = -1f)
        {
            var ep = new ParticleSystem.EmitParams
            {
                position = pos,
                applyShapeToPosition = true,
                startColor = c
            };
            if (size > 0f) ep.startSize = size;
            ps.Emit(ep, count);
        }

        public static void Splash(Vector3 pos, Color c, int count = 18)
        {
            Ensure();
            Emit(_splash, pos, c, count);
        }

        public static void Chips(Vector3 pos, Color c, int count = 4)
        {
            Ensure();
            Emit(_chips, pos, c, count);
        }

        public static void Poof(Vector3 pos, int count = 6)
        {
            Ensure();
            Emit(_poof, pos, new Color(1f, 1f, 1f, 0.8f), count);
        }

        public static void Sparkle(Vector3 pos, Color c, int count = 8)
        {
            Ensure();
            Emit(_sparkle, pos, c, count);
        }

        public static void Confetti(Vector3 pos, int count = 70)
        {
            Ensure();
            for (int i = 0; i < count; i++)
                Emit(_confetti, pos, ConfettiColors[i % ConfettiColors.Length], 1);
        }

        /// <summary>A big juicy burst: droplets flying out and a splat left on the ground.</summary>
        public static void JuiceBurst(Vector3 pos, Vector3 ground, Color c, float scale = 1f)
        {
            Ensure();
            Emit(_drops, pos, c, Mathf.RoundToInt(22 * scale));
            Emit(_splash, pos, c, Mathf.RoundToInt(10 * scale));
            var light = Color.Lerp(c, Color.white, 0.45f);
            Emit(_drops, pos, light, Mathf.RoundToInt(6 * scale));
            var sc = c;
            sc.a = 0.85f;
            for (int i = 0; i < 3; i++)
                Emit(_splat, ground + new Vector3(Random.Range(-0.6f, 0.6f), 0.03f + i * 0.002f, Random.Range(-0.6f, 0.6f)) * scale, sc, 1,
                    Random.Range(0.6f, 1.25f) * scale);
        }

        public static void Drops(Vector3 pos, Color c, int count = 6)
        {
            Ensure();
            Emit(_drops, pos, c, count);
        }

        public static void Ring(Vector3 groundPos, Color c, float size = 3f)
        {
            Ensure();
            Emit(_ring, groundPos + Vector3.up * 0.05f, c, 1, size);
        }

        public static void Stars(Vector3 pos, int count = 12, Color? c = null)
        {
            Ensure();
            Emit(_stars, pos, c ?? new Color(1f, 0.86f, 0.25f), count);
        }

        public static void Hearts(Vector3 pos, int count = 3)
        {
            Ensure();
            Emit(_hearts, pos, Color.white, count);
        }

        public static void Dust(Vector3 pos, int count = 2)
        {
            Ensure();
            Emit(_dust, pos, new Color(0.93f, 0.88f, 0.78f, 0.55f), count);
        }

        public static void Leaves(Vector3 pos, int count = 5)
        {
            Ensure();
            Emit(_leaves, pos, new Color(0.45f, 0.82f, 0.32f), count);
        }

        public static void Coins(Vector3 pos, int count = 8)
        {
            Ensure();
            Emit(_coins, pos, Color.white, count);
        }

        public static void Glint(Vector3 pos, Color? c = null, int count = 2)
        {
            Ensure();
            Emit(_glints, pos, c ?? Color.white, count);
        }

        public static void Trail(Vector3 pos, Color c)
        {
            Ensure();
            Emit(_trail, pos, c, 1);
        }

        public static void Smoke(Vector3 pos, float darkness = 0.55f, int count = 1)
        {
            Ensure();
            Emit(_smoke, pos, new Color(darkness, darkness, darkness, 0.55f), count);
        }

        public static void Bubbles(Vector3 pos, Color c, int count = 2)
        {
            Ensure();
            Emit(_bubbles, pos, c, count);
        }
    }
}
