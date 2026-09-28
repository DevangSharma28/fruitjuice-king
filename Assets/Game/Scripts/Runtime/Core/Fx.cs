using UnityEngine;

namespace JuiceKing
{
    /// <summary>Shared burst particle systems, emitted on demand (no per-effect instantiation).</summary>
    public static class Fx
    {
        static ParticleSystem _splash, _chips, _confetti, _poof, _sparkle;

        static readonly Color[] ConfettiColors =
        {
            new Color(1f, 0.35f, 0.4f), new Color(1f, 0.8f, 0.2f), new Color(0.3f, 0.85f, 0.45f),
            new Color(0.3f, 0.6f, 1f), new Color(0.85f, 0.45f, 1f), Color.white
        };

        static void Ensure()
        {
            if (_splash != null) return;
            var root = new GameObject("[Fx]").transform;
            var mat = GameRefs.I != null ? GameRefs.I.particleMaterial : null;

            _splash = Create(root, "Splash", mat, 0.35f, 0.75f, 2.5f, 6f, 0.12f, 0.3f, 2.2f, ParticleSystemShapeType.Hemisphere);
            _chips = Create(root, "Chips", mat, 0.25f, 0.5f, 1.5f, 4f, 0.06f, 0.14f, 2f, ParticleSystemShapeType.Sphere);
            _confetti = Create(root, "Confetti", mat, 1.2f, 2f, 5f, 9f, 0.12f, 0.22f, 0.9f, ParticleSystemShapeType.Cone);
            _poof = Create(root, "Poof", mat, 0.4f, 0.7f, 0.6f, 1.6f, 0.3f, 0.6f, -0.1f, ParticleSystemShapeType.Sphere);
            _sparkle = Create(root, "Sparkle", mat, 0.4f, 0.8f, 1f, 3f, 0.08f, 0.18f, -0.2f, ParticleSystemShapeType.Sphere);

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
        }

        static ParticleSystem Create(Transform root, string name, Material mat, float lifeMin, float lifeMax,
            float speedMin, float speedMax, float sizeMin, float sizeMax, float gravity, ParticleSystemShapeType shapeType)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var ps = go.AddComponent<ParticleSystem>();
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

            var main = ps.main;
            main.playOnAwake = false;
            main.loop = false;
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

            var sol = ps.sizeOverLifetime;
            sol.enabled = true;
            sol.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
                new Keyframe(0f, 1f), new Keyframe(0.7f, 0.85f), new Keyframe(1f, 0f)));

            var r = go.GetComponent<ParticleSystemRenderer>();
            r.renderMode = ParticleSystemRenderMode.Billboard;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            r.receiveShadows = false;
            if (mat != null) r.sharedMaterial = mat;

            ps.Play();
            return ps;
        }

        static void Emit(ParticleSystem ps, Vector3 pos, Color c, int count)
        {
            var ep = new ParticleSystem.EmitParams
            {
                position = pos,
                applyShapeToPosition = true,
                startColor = c
            };
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
    }
}
