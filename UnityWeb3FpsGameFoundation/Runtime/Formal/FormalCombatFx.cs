using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Formal
{
    [DisallowMultipleComponent]
    public sealed class FormalCombatFx : MonoBehaviour
    {
        // Effects are pooled and recycled: at combined player + bot fire rates the
        // previous create/Destroy-per-shot pattern (GameObject + LineRenderer + Light
        // + primitive Quad each time) generated constant garbage and per-shot hitches.
        private const int TracerCapacity = 32;
        private const int FlashCapacity = 8;
        private const int ImpactCapacity = 32;

        [SerializeField] private HitscanWeapon playerWeapon;
        [SerializeField] private PrototypeBotController botWeapon;
        [SerializeField] private Transform playerVisualMuzzle;
        [SerializeField] private Material cobaltTracer;
        [SerializeField] private Material coralTracer;
        [SerializeField, Min(0.01f)] private float tracerLifetime = 0.075f;

        private readonly List<PooledFx> _tracers = new List<PooledFx>();
        private readonly List<PooledFx> _flashes = new List<PooledFx>();
        private readonly List<PooledFx> _impacts = new List<PooledFx>();
        private System.Func<PooledFx> _tracerFactory;
        private System.Func<PooledFx> _flashFactory;
        private System.Func<PooledFx> _impactFactory;

        public void Configure(
            HitscanWeapon localWeapon,
            PrototypeBotController botController,
            Transform visualMuzzle,
            Material playerTracerMaterial,
            Material botTracerMaterial)
        {
            Unsubscribe();
            playerWeapon = localWeapon;
            botWeapon = botController;
            playerVisualMuzzle = visualMuzzle;
            cobaltTracer = playerTracerMaterial;
            coralTracer = botTracerMaterial;
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            DeactivatePool(_tracers);
            DeactivatePool(_flashes);
            DeactivatePool(_impacts);
        }

        private void Update()
        {
            var now = Time.timeAsDouble;
            TickPool(_tracers, now);
            TickPool(_flashes, now);
            TickPool(_impacts, now);
        }

        private void Subscribe()
        {
            if (playerWeapon != null) playerWeapon.ShotResolved += OnPlayerShot;
            if (botWeapon != null) botWeapon.ShotResolved += OnBotShot;
        }

        private void Unsubscribe()
        {
            if (playerWeapon != null) playerWeapon.ShotResolved -= OnPlayerShot;
            if (botWeapon != null) botWeapon.ShotResolved -= OnBotShot;
        }

        private void OnPlayerShot(ShotResult result)
        {
            if (!result.Accepted || playerVisualMuzzle == null) return;
            SpawnTracer(playerVisualMuzzle.position, result.Point, cobaltTracer, new Color(0.24f, 0.82f, 1f));
            SpawnMuzzleFlash(playerVisualMuzzle.position, new Color(0.3f, 0.82f, 1f));
            if (result.Hit) SpawnImpact(result.Point, result.Normal, result.DamageApplied);
        }

        private void OnBotShot(Vector3 origin, Vector3 end, bool hit)
        {
            SpawnTracer(origin, end, coralTracer, hit ? new Color(1f, 0.32f, 0.22f) : new Color(1f, 0.55f, 0.28f));
            SpawnMuzzleFlash(origin, new Color(1f, 0.25f, 0.15f));
        }

        private void SpawnTracer(Vector3 origin, Vector3 end, Material material, Color color)
        {
            _tracerFactory ??= CreateTracer;
            var fx = Acquire(_tracers, TracerCapacity, _tracerFactory);
            var line = (LineRenderer)fx.Payload;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.08f);
            if (material != null) line.sharedMaterial = material;
            Activate(fx, Time.timeAsDouble + tracerLifetime);
        }

        private void SpawnMuzzleFlash(Vector3 position, Color color)
        {
            _flashFactory ??= CreateMuzzleFlash;
            var fx = Acquire(_flashes, FlashCapacity, _flashFactory);
            fx.Root.transform.position = position;
            ((Light)fx.Payload).color = color;
            Activate(fx, Time.timeAsDouble + 0.045d);
        }

        private void SpawnImpact(Vector3 position, Vector3 normal, bool damageApplied)
        {
            _impactFactory ??= CreateImpact;
            var fx = Acquire(_impacts, ImpactCapacity, _impactFactory);
            var impact = fx.Root.transform;
            impact.position = position + normal * 0.012f;
            impact.rotation = Quaternion.LookRotation(normal);
            impact.localScale = Vector3.one * (damageApplied ? 0.085f : 0.055f);
            ((Renderer)fx.Payload).sharedMaterial = damageApplied && coralTracer != null ? coralTracer : cobaltTracer;
            Activate(fx, Time.timeAsDouble + (damageApplied ? 0.18d : 0.1d));
        }

        private PooledFx CreateTracer()
        {
            var tracer = new GameObject("Combat Tracer");
            tracer.transform.SetParent(transform, true);
            var line = tracer.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.startWidth = 0.035f;
            line.endWidth = 0.009f;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            return new PooledFx { Root = tracer, Payload = line };
        }

        private PooledFx CreateMuzzleFlash()
        {
            var flash = new GameObject("Muzzle Flash");
            flash.transform.SetParent(transform, true);
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 2.8f;
            light.intensity = 4.5f;
            light.shadows = LightShadows.None;
            return new PooledFx { Root = flash, Payload = light };
        }

        private PooledFx CreateImpact()
        {
            var impact = GameObject.CreatePrimitive(PrimitiveType.Quad);
            impact.name = "Combat Impact";
            impact.transform.SetParent(transform, true);
            var collider = impact.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = impact.GetComponent<Renderer>();
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return new PooledFx { Root = impact, Payload = renderer };
        }

        private static PooledFx Acquire(List<PooledFx> pool, int capacity, System.Func<PooledFx> factory)
        {
            PooledFx oldest = null;
            for (var i = 0; i < pool.Count; i++)
            {
                var candidate = pool[i];
                if (!candidate.Root.activeSelf) return candidate;
                if (oldest == null || candidate.ExpireAt < oldest.ExpireAt) oldest = candidate;
            }
            if (pool.Count < capacity)
            {
                var created = factory();
                created.Root.SetActive(false);
                pool.Add(created);
                return created;
            }
            return oldest;
        }

        private static void Activate(PooledFx fx, double expireAt)
        {
            fx.ExpireAt = expireAt;
            fx.Root.SetActive(true);
        }

        private static void TickPool(List<PooledFx> pool, double now)
        {
            for (var i = 0; i < pool.Count; i++)
            {
                var fx = pool[i];
                if (fx.Root == null || !fx.Root.activeSelf || now < fx.ExpireAt) continue;
                fx.Root.SetActive(false);
            }
        }

        private static void DeactivatePool(List<PooledFx> pool)
        {
            for (var i = 0; i < pool.Count; i++)
            {
                if (pool[i].Root != null) pool[i].Root.SetActive(false);
            }
        }

        private sealed class PooledFx
        {
            public GameObject Root;
            public Object Payload;
            public double ExpireAt;
        }
    }
}
