using System;
using UnityEngine;

namespace Web3Fps.GameFoundation.Services
{
    public interface IExternalUrlLauncher
    {
        void Open(string url);
    }

    public sealed class SystemBrowserUrlLauncher : IExternalUrlLauncher
    {
        public void Open(string url)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var parsed) ||
                (parsed.Scheme != Uri.UriSchemeHttps && parsed.Scheme != Uri.UriSchemeHttp))
                throw new ArgumentException("A valid HTTP(S) URL is required", nameof(url));
            Application.OpenURL(parsed.AbsoluteUri);
        }
    }
}
