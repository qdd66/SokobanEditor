using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;

namespace Sokoban
{
    public sealed class SokobanHud : MonoBehaviour
    {
        [Title("引用")]
        [LabelText("玩法配置")]
        [SerializeField, Required("请指定玩法配置"), AssetsOnly]
        SokobanConfig config;

        [LabelText("过关频道")]
        [SerializeField, Required("请指定过关频道"), AssetsOnly]
        SokobanWinChannel winChannel;

        [LabelText("提示文本")]
        [SerializeField, Required("请指定提示文本")]
        TMP_Text hintLabel;

        [LabelText("过关文本")]
        [SerializeField, Required("请指定过关文本")]
        TMP_Text winLabel;

        void OnEnable()
        {
            if (winChannel != null)
            {
                winChannel.Won += ShowWin;
                winChannel.Reset += HideWin;
            }

            ApplyHint();
            HideWin();
        }

        void OnDisable()
        {
            if (winChannel != null)
            {
                winChannel.Won -= ShowWin;
                winChannel.Reset -= HideWin;
            }
        }

        void ApplyHint()
        {
            if (hintLabel == null)
                return;
            hintLabel.text = config != null ? config.HintText : string.Empty;
        }

        void ShowWin()
        {
            if (winLabel == null)
                return;
            winLabel.text = config != null ? config.WinText : string.Empty;
            winLabel.gameObject.SetActive(true);
        }

        void HideWin()
        {
            if (winLabel == null)
                return;
            winLabel.gameObject.SetActive(false);
        }
    }
}
