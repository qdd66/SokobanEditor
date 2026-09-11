using UnityEngine;

namespace DZDMapEditor
{
    public interface ISelectionVisual
    {
        void Show(GameObject target, Color color, float width);
        void Hide();
    }
}
