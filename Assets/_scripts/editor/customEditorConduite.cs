using UnityEngine;
using UnityEditor;


[CustomEditor(typeof(conduite_script02))]

public class conduiteScriptEditor : Editor
{
    conduite_script02 _target;

    void OnEnable()
    {
        //ref to parent script
        _target = (conduite_script02)target;
    }


    public override void OnInspectorGUI()
    { 
        DrawDefaultInspector();

        GUILayout.BeginHorizontal();
            if (GUILayout.Button(" [ BACK ] ",  GUILayout.Width(100), GUILayout.Height(100)))
            {
                _target.ButtonBACK();
            }

            GUILayout.Space(50);
            if (GUILayout.Button(" [ NEXT ] ",  GUILayout.Width(150), GUILayout.Height(100)))
            {
                _target.ButtonNEXT();
            }
        GUILayout.EndHorizontal();


        GUILayout.Space(50);



        GUILayout.BeginHorizontal();
            if (GUILayout.Button(" [ GO DOWN ] ", GUILayout.Width(100), GUILayout.Height(100)))
        {
                _target.goDown = true;
            }
            GUILayout.Space(100);
            if (GUILayout.Button(" [ GO UP ] ", GUILayout.Width(100), GUILayout.Height(100)))
        {
                _target.goDown = false;
            }
        GUILayout.EndHorizontal();



        GUILayout.BeginHorizontal();
        GUILayout.Space(100);
        if (GUILayout.Button(" [ ** CALIBRATE ** ] ", GUILayout.Width(100), GUILayout.Height(100)))
        {
            // LANCE CALIB
            _target.rokoko.CalibrateAll();
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        //GUILayout.Space(100);
        if (GUILayout.Button(" [ ** BLACK OUT ** ] ", GUILayout.Width(100), GUILayout.Height(100)))
        {
            // LANCE CALIB
            _target.canvasBlackOut.SetActive(!_target.canvasBlackOut.active);
        }

        GUILayout.EndHorizontal();
    }
}

