using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DZDMapEditor
{
    public sealed class BackpackDimmerClick : MonoBehaviour, IPointerClickHandler
    {
        [LabelText("背包面板")]
        [SerializeField]
        BackpackPanelView panel;

        public void Bind(BackpackPanelView view)
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
