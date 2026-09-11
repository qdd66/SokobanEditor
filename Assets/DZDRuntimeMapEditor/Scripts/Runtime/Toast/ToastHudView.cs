using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class ToastHudView : MonoBehaviour
    {
        [LabelText("提示频道")]
        [Tooltip("任意系统往这里发消息。")]
        [SerializeField, Required("请指定提示频道"), AssetsOnly]
        ToastChannel channel;

        [LabelText("提示配置")]
        [Tooltip("淡入、停留、淡出时长。")]
        [SerializeField, Required("请指定提示配置"), AssetsOnly]
        ToastHudConfig config;

        [LabelText("文本")]
        [SerializeField, Required("请指定 TMP 文本")]
        TMP_Text label;

        [LabelText("透明度")]
        [SerializeField, Required("请指定 CanvasGroup")]
        CanvasGroup group;

        enum Phase
        {
            Hidden,
            FadeIn,
            Hold,
            FadeOut
        }

        Phase phase = Phase.Hidden;
        float elapsed;
        float holdSeconds;

        void OnEnable()
        {
            if (channel != null)
                channel.Raised += HandleRaised;
            HideImmediate();
        }

        void OnDisable()
        {
            if (channel != null)
                channel.Raised -= HandleRaised;
            HideImmediate();
        }

        void Update()
        {
            if (phase == Phase.Hidden || config == null)
                return;

            elapsed += Time.unscaledDeltaTime;
            if (phase == Phase.FadeIn)
            {
                var duration = Mathf.Max(0.0001f, config.FadeInSeconds);
                SetAlpha(Mathf.Clamp01(elapsed / duration));
                if (elapsed >= config.FadeInSeconds)
                    Enter(Phase.Hold);
                return;
            }

            if (phase == Phase.Hold)
            {
                SetAlpha(1f);
                if (elapsed >= holdSeconds)
                    Enter(Phase.FadeOut);
                return;
            }

            var fadeOut = Mathf.Max(0.0001f, config.FadeOutSeconds);
            SetAlpha(1f - Mathf.Clamp01(elapsed / fadeOut));
            if (elapsed >= config.FadeOutSeconds)
                HideImmediate();
        }

        void HandleRaised(ToastRequest request)
        {
            if (label != null)
                label.text = request.Message.Trim();

            holdSeconds = request.DurationSeconds < 0f
                ? config != null ? config.HoldSeconds : 1.4f
                : request.DurationSeconds;

            if (config != null && config.FadeInSeconds <= 0f)
            {
                SetAlpha(1f);
                Enter(Phase.Hold);
                return;
            }

            SetAlpha(0f);
            Enter(Phase.FadeIn);
        }

        void Enter(Phase next)
        {
            phase = next;
            elapsed = 0f;
        }

        void HideImmediate()
        {
            phase = Phase.Hidden;
            elapsed = 0f;
            SetAlpha(0f);
        }

        void SetAlpha(float alpha)
        {
            if (group == null)
                return;
            group.alpha = alpha;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
    }
}
