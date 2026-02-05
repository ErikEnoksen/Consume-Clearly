using NavMeshPlus.NavMeshPlus_master.NavMeshComponents.Scripts;
using UnityEditor;

namespace NavMeshPlus_master.NavMeshComponents.Editor
{ 
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NavMeshModifier))]
    class NavMeshModifierEditor : UnityEditor.Editor
    {
        SerializedProperty m_AffectedAgents;
        SerializedProperty m_Area;
        SerializedProperty m_IgnoreFromBuild;
        SerializedProperty m_OverrideArea;

        void OnEnable()
        {
            m_AffectedAgents = serializedObject.FindProperty("m_AffectedAgents");
            m_Area = serializedObject.FindProperty("m_Area");
            m_IgnoreFromBuild = serializedObject.FindProperty("m_IgnoreFromBuild");
            m_OverrideArea = serializedObject.FindProperty("m_OverrideArea");


        }



        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(m_IgnoreFromBuild);

            EditorGUILayout.PropertyField(m_OverrideArea);
            if (m_OverrideArea.boolValue)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.PropertyField(m_Area);
                EditorGUI.indentLevel--;
            }

            NavMeshComponentsGUIUtility.AgentMaskPopup("Affected Agents", m_AffectedAgents);
            EditorGUILayout.Space();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
