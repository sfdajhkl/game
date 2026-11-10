using UnityEngine;

namespace Arvito.Runtime
{
    /// <summary>Load only the current event's illustration; generic landscapes remain a fallback.</summary>
    public static class EventArt
    {
        public static Texture2D Load(string eventId)
        {
            return string.IsNullOrEmpty(eventId) ? null : Resources.Load<Texture2D>("Arvito/Art/Events/" + eventId);
        }
    }
}
