using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class ScreenCrosshair : MonoBehaviour
    {
        [LabelText("颜色")]
        [SerializeField]
        Color color = Color.white;

        [LabelText("半长")]
        [Tooltip("准心十字从中心到端点的长度。")]
        [SuffixLabel("像素", Overlay = true)]
        [SerializeField]
        float halfSize = 8f;

        [LabelText("线宽")]
        [SuffixLabel("像素", Overlay = true)]
        [SerializeField]
        float thickness = 2f;

        [LabelText("鼠标锁定")]
        [Tooltip("背包占用鼠标时隐藏准星。可空，会在自身上查找。")]
        [SerializeField]
        GameplayCursor gameplayCursor;

        void Awake()
        {
            if (gameplayCursor == null)
                gameplayCursor = GetComponent<GameplayCursor>();
        }

        void OnGUI()
        {
            if (gameplayCursor != null && (!gameplayCursor.isActiveAndEnabled || gameplayCursor.IsUiHold))
                return;

            var x = Screen.width * 0.5f;
            var y = Screen.height * 0.5f;
            var previous = GUI.color;
            GUI.color = color;
            GUI.DrawTexture(new Rect(x - halfSize, y - thickness * 0.5f, halfSize * 2f, thickness), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(x - thickness * 0.5f, y - halfSize, thickness, halfSize * 2f), Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
