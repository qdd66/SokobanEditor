using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;

namespace DZDMapEditor
{
    public sealed class MapLoadDimmerClick : MonoBehaviour, IPointerClickHandler
    {
        [LabelText("读取面板")]
        [SerializeField]
        MapLoadPanelView panel;

        public void Bind(MapLoadPanelView view)
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
