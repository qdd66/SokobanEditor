using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    [CreateAssetMenu(
        fileName = "ToastHudConfig",
        menuName = MapEditorInfo.CreateMenu + "/Toast HUD Config")]
    public sealed class ToastHudConfig : ScriptableObject
    {
        [LabelText("停留时间")]
        [Tooltip("淡入完成后文字保持可见的时间。")]
        [SuffixLabel("秒", Overlay = true)]
        [SerializeField, MinValue(0.05f)]
        float holdSeconds = 1.4f;

        [LabelText("淡入")]
        [Tooltip("提示出现时的淡入时长。0 表示立刻显示。")]
        [SuffixLabel("秒", Overlay = true)]
        [SerializeField, MinValue(0f)]
        float fadeInSeconds = 0.12f;

        [LabelText("淡出")]
        [Tooltip("提示消失时的淡出时长。")]
        [SuffixLabel("秒", Overlay = true)]
        [SerializeField, MinValue(0f)]
        float fadeOutSeconds = 0.2f;

        public float HoldSeconds => holdSeconds;
        public float FadeInSeconds => fadeInSeconds;
        public float FadeOutSeconds => fadeOutSeconds;
    }
}
