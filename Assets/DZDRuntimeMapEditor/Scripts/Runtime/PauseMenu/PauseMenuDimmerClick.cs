using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DZDMapEditor
{
    public sealed class PauseMenuDimmerClick : MonoBehaviour, IPointerClickHandler
    {
        [LabelText("暂停面板")]
        [SerializeField]
        PauseMenuView panel;

        public void Bind(PauseMenuView view)
        {
            panel = view;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData == null || eventData.button != PointerEventData.InputButton.Left)
                return;
            if (panel != null)
                panel.CloseFromUi();
        }
    }
}
