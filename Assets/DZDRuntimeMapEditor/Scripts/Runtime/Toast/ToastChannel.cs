using System;
using UnityEngine;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "ToastChannel",
        menuName = MapEditorInfo.CreateMenu + "/Toast Channel")]
    public sealed class ToastChannel : ScriptableObject
    {
        public event Action<ToastRequest> Raised;

        public void Raise(string message)
        {
            Raise(new ToastRequest(message, -1f));
        }

        public void Raise(string message, float durationSeconds)
        {
            Raise(new ToastRequest(message, durationSeconds));
        }

        public void Raise(ToastRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Message))
                return;
            Raised?.Invoke(request);
        }
    }
}
