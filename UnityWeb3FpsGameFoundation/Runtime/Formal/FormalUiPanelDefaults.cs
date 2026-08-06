using UnityEngine;
using UnityEngine.UIElements;

namespace Web3Fps.GameFoundation.Formal
{
    public static class FormalUiPanelDefaults
    {
        public const int ReferenceWidth = 1920;
        public const int ReferenceHeight = 1080;
        public const int SortingOrder = 20;

        public static void Configure(PanelSettings settings)
        {
            if (settings == null) return;
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(ReferenceWidth, ReferenceHeight);
            settings.match = 0.5f;
            settings.sortingOrder = SortingOrder;
        }
    }
}
