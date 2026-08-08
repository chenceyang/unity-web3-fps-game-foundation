using UnityEngine;

namespace Web3Fps.GameFoundation.Formal
{
    /// <summary>
    /// Applies a skin visual spec to the renderers under a target root via
    /// MaterialPropertyBlock. Strictly cosmetic: it never touches damage, fire rate,
    /// colliders, recoil, animation timing or silhouette (AGENTS / design §6.2).
    /// Emission tint only shows on parts whose shared material already enables
    /// emission — a property block cannot switch shader keywords.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class FormalSkinApplicator : MonoBehaviour
    {
        [SerializeField] private Transform target;

        private MaterialPropertyBlock _block;

        public FormalSkinVisualSpec AppliedSpec { get; private set; }

        public void Configure(Transform root)
        {
            target = root;
            if (AppliedSpec != null) Apply(AppliedSpec);
        }

        public void Apply(FormalSkinVisualSpec spec)
        {
            if (spec == null) spec = FormalSkinCatalog.DefaultSpec;
            AppliedSpec = spec;
            var root = target != null ? target : transform;
            if (_block == null) _block = new MaterialPropertyBlock();
            var baseColorId = Shader.PropertyToID("_BaseColor");
            var colorId = Shader.PropertyToID("_Color");
            var emissionColorId = Shader.PropertyToID("_EmissionColor");
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++)
            {
                var renderer = renderers[i];
                var color = ResolvePartColor(renderer.name, spec);
                _block.Clear();
                _block.SetColor(baseColorId, color);
                _block.SetColor(colorId, color);
                if (spec.HasEmission && IsAccentPart(renderer.name))
                    _block.SetColor(emissionColorId, spec.emissionColor * spec.emissionIntensity);
                renderer.SetPropertyBlock(_block);
            }
        }

        // Default look = the weapon's own authored materials, so clearing the
        // property blocks (not painting a neutral spec) restores it exactly.
        public void ClearOverrides()
        {
            AppliedSpec = null;
            var root = target != null ? target : transform;
            var renderers = root.GetComponentsInChildren<Renderer>(true);
            for (var i = 0; i < renderers.Length; i++) renderers[i].SetPropertyBlock(null);
        }

        public static bool IsAccentPart(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return false;
            return partName.Contains("AccentRail") || partName.Contains("Magazine");
        }

        public static bool IsPatternPart(string partName)
        {
            if (string.IsNullOrEmpty(partName)) return false;
            return partName.Contains("BarrelShroud") ||
                   partName.Contains("RearSight") ||
                   partName.Contains("FrontSight");
        }

        public static Color ResolvePartColor(string partName, FormalSkinVisualSpec spec)
        {
            if (spec == null) spec = FormalSkinCatalog.DefaultSpec;
            if (IsAccentPart(partName)) return spec.secondaryColor;
            if (spec.hasPattern && IsPatternPart(partName)) return spec.secondaryColor;
            return spec.primaryColor;
        }
    }
}
