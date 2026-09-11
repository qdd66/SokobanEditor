using Sirenix.OdinInspector;
using UnityEngine;

namespace DZDMapEditor
{
    public sealed class PlacedItem : MonoBehaviour
    {
        [LabelText("物品定义")]
        [Tooltip("对应目录里的可放置物品。运行时放置会自动绑定。")]
        [SerializeField]
        PlaceableItemDef definition;

        [LabelText("实例编号")]
        [Tooltip("存档和写回场景用这个编号认同一个物体。不要随便改。")]
        [SerializeField]
        string instanceId;

        public PlaceableItemDef Definition => definition;
        public string InstanceId => instanceId;

        public void Bind(PlaceableItemDef def)
        {
            definition = def;
        }

        public void BindInstanceId(string id)
        {
            if (!string.IsNullOrEmpty(id))
                instanceId = id;
        }

        public void EnsureInstanceId()
        {
            if (string.IsNullOrEmpty(instanceId))
                instanceId = System.Guid.NewGuid().ToString("N");
        }

        public static PlacedItem FindOn(Collider collider)
        {
            return collider != null ? collider.GetComponentInParent<PlacedItem>() : null;
        }

        public static PlacedItem Ensure(GameObject target, PlaceableItemDef def)
        {
            var item = target.GetComponent<PlacedItem>();
            if (item == null)
                item = target.AddComponent<PlacedItem>();
            if (def != null)
                item.Bind(def);
            return item;
        }
    }
}
