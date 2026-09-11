using Sirenix.OdinInspector;
using UnityEngine;

namespace Sokoban
{
    public sealed class SokobanPiece : MonoBehaviour
    {
        [Title("角色")]
        [LabelText("类型")]
        [Tooltip("玩家、墙、箱子、目标点或地面。目标点不挡格，可与人或箱子重叠。地面不挡格，可与实体同格。")]
        [SerializeField]
        SokobanRole role = SokobanRole.Wall;

        public SokobanRole Role => role;

        public void BindRole(SokobanRole value)
        {
            role = value;
        }
    }
}
