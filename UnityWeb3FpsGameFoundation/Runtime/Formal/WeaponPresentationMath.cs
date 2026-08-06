using UnityEngine;

namespace Web3Fps.GameFoundation.Formal
{
    public readonly struct ReloadPresentationPose
    {
        public readonly float Arc;
        public readonly float MagazineDrop;

        public ReloadPresentationPose(float arc, float magazineDrop)
        {
            Arc = arc;
            MagazineDrop = magazineDrop;
        }
    }

    public static class WeaponPresentationMath
    {
        public static ReloadPresentationPose EvaluateReload(float progress)
        {
            var t = Mathf.Clamp01(progress);
            var arc = Mathf.Sin(t * Mathf.PI);
            var remove = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.12f, 0.38f, t));
            var insert = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.62f, 0.9f, t));
            return new ReloadPresentationPose(arc, Mathf.Clamp01(remove - insert));
        }
    }
}
