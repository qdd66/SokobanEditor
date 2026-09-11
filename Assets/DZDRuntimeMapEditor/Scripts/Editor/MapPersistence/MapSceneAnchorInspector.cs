using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine;

namespace DZDMapEditor
{
    [CustomEditor(typeof(MapSceneAnchor))]
    public sealed class MapSceneAnchorInspector : OdinEditor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
            EditorGUILayout.Space();
            using (new EditorGUI.DisabledScope(Application.isPlaying))
            {
                if (GUILayout.Button("选择存档并应用到当前场景"))
                    SceneContentApplier.ApplyFromFileDialog((MapSceneAnchor)target);
            }

            if (Application.isPlaying)
                EditorGUILayout.HelpBox("退出 Play 后再把本地存档写进场景。", MessageType.Info);
        }
    }
}
