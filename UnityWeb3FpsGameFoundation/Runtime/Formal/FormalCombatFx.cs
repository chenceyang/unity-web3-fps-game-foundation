using UnityEngine;
using UnityEngine.Rendering;
using Web3Fps.GameFoundation.Gameplay.Combat;
using Web3Fps.GameFoundation.Prototype;

namespace Web3Fps.GameFoundation.Formal
{
    [DisallowMultipleComponent]
    public sealed class FormalCombatFx : MonoBehaviour
    {
        [SerializeField] private HitscanWeapon playerWeapon;
        [SerializeField] private PrototypeBotController botWeapon;
        [SerializeField] private Transform playerVisualMuzzle;
        [SerializeField] private Material cobaltTracer;
        [SerializeField] private Material coralTracer;
        [SerializeField, Min(0.01f)] private float tracerLifetime = 0.075f;

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
            var tracer = new GameObject("Combat Tracer");
            tracer.transform.SetParent(transform, true);
            var line = tracer.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 2;
            line.SetPosition(0, origin);
            line.SetPosition(1, end);
            line.startWidth = 0.035f;
            line.endWidth = 0.009f;
            line.numCapVertices = 2;
            line.shadowCastingMode = ShadowCastingMode.Off;
            line.receiveShadows = false;
            line.startColor = color;
            line.endColor = new Color(color.r, color.g, color.b, 0.08f);
            if (material != null) line.sharedMaterial = material;
            Destroy(tracer, tracerLifetime);
        }

        private void SpawnMuzzleFlash(Vector3 position, Color color)
        {
            var flash = new GameObject("Muzzle Flash");
            flash.transform.SetParent(transform, true);
            flash.transform.position = position;
            var light = flash.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.range = 2.8f;
            light.intensity = 4.5f;
            light.shadows = LightShadows.None;
            Destroy(flash, 0.045f);
        }

        private void SpawnImpact(Vector3 position, Vector3 normal, bool damageApplied)
        {
            var impact = GameObject.CreatePrimitive(PrimitiveType.Quad);
            impact.name = damageApplied ? "Damage Impact" : "Surface Impact";
            impact.transform.SetParent(transform, true);
            impact.transform.position = position + normal * 0.012f;
            impact.transform.rotation = Quaternion.LookRotation(normal);
            impact.transform.localScale = Vector3.one * (damageApplied ? 0.085f : 0.055f);
            var collider = impact.GetComponent<Collider>();
            if (collider != null) Destroy(collider);
            var renderer = impact.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.shadowCastingMode = ShadowCastingMode.Off;
                renderer.receiveShadows = false;
                renderer.sharedMaterial = damageApplied && coralTracer != null ? coralTracer : cobaltTracer;
            }
            Destroy(impact, damageApplied ? 0.18f : 0.1f);
        }
    }
}
