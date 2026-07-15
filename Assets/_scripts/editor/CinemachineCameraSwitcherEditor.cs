using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(CinemachineCameraSwitcher))]
public class CinemachineCameraSwitcherEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Dessiner l'Inspector par défaut
        DrawDefaultInspector();

        CinemachineCameraSwitcher switcher = (CinemachineCameraSwitcher)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Contrôles", EditorStyles.boldLabel);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("◀ Previous", GUILayout.Height(30)))
        {
            switcher.PreviousCamera();
        }

        if (GUILayout.Button("Next ▶", GUILayout.Height(30)))
        {
            switcher.NextCamera();
        }

        EditorGUILayout.EndHorizontal();


        if (GUILayout.Button("Toggle Video", GUILayout.Height(30)))
        {
            switcher.toggleVideo();
        }


        // Afficher le nom de la camera active
        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox($"Camera active : {switcher.CurrentCameraName}", MessageType.Info);

    }
}