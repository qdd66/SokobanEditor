using UnityEngine;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "MapSessionRequest",
        menuName = MapEditorInfo.CreateMenu + "/Map Session Request")]
    public sealed class MapSessionRequest : ScriptableObject
    {
        static bool hasRequest;
        static bool startNew;
        static string loadPath;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPlayModeState()
        {
            Clear();
        }

        public void RequestNew()
        {
            hasRequest = true;
            startNew = true;
            loadPath = null;
        }

        public void RequestLoad(string path)
        {
            hasRequest = true;
            startNew = false;
            loadPath = path;
        }

        public bool TryConsume(out bool isNewMap, out string path)
        {
            isNewMap = true;
            path = null;
            if (!hasRequest)
                return false;

            isNewMap = startNew;
            path = loadPath;
            Clear();
            return true;
        }

        public static void Clear()
        {
            hasRequest = false;
            startNew = false;
            loadPath = null;
        }
    }
}
