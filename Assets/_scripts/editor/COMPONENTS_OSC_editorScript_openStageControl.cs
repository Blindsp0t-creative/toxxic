using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


[CustomEditor(typeof(oscAutoAssignComponents_openStage),true)]

public class COMPONENTS_OSC_editorScript_openStageControl : Editor
{
    private oscAutoAssignComponents_openStage _target;
    private bool scanHierarchyDone = false;

    public void OnEnable()
    {
        _target = (oscAutoAssignComponents_openStage)target;

        //oscAutoAssignComponents_openStage oscAu
        EditorUtility.SetDirty(_target.parametres);
        /*
        _target.parametres.floats = new List<osc_parameter_float>();
        _target.parametres.ints = new List<osc_parameter_int>();
        _target.parametres.bools = new List<osc_parameter_bool>();
        _target.parametres.strings = new List<osc_parameter_string>();
        */
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("SCAN HIERARCHY"))
        {
            _target.scanHierarchy();
            scanHierarchyDone = true;

            AssetDatabase.SaveAssets();

        }
        GUILayout.Label("FLOATING POINT PARAMETERS");
        DrawUILine(Color.black);

        if (scanHierarchyDone)
        {
            for (int i = 0; i < _target.parametres.floats.Count; i++)
            {
                GUILayout.BeginHorizontal();

                //NOM DU PARAM
                string label = "    " + _target.parametres.floats[i].name;
                GUILayout.Label(label, GUILayout.Width(100));

                //SPACE
                GUILayout.Space(20);

                //SLIDER 
                float min = 0.0f;
                float max = 1.0f;
                if (_target.parametres.floats[i].hasRange)
                {
                    min = _target.parametres.floats[i].minRange;
                    max = _target.parametres.floats[i].maxRange;
                }
                GUILayout.HorizontalSlider(_target.parametres.floats[i].currentValue, min, max, GUILayout.Width(250));

                //SPACE
                GUILayout.Space(20);

                string alias;
                /*
                if (_target.parametres.floats[i].isInt)
                    alias = "[INT]         ";
                else
                    */
                    alias = "[FLOAT]   ";
                alias += _target.parametres.floats[i].compName;

                //EXPOSED OR NOT ?
                _target.parametres.floats[i].exposed = EditorGUILayout.Toggle(alias, _target.parametres.floats[i].exposed);

                //SLIDER SELECTOR ?
                _target.parametres.floats[i].faderId = EditorGUILayout.IntField(_target.parametres.floats[i].faderId, GUILayout.Width(25));

                GUILayout.EndHorizontal();
                //GUILayout.Space(5);
            }

            DrawUILine(Color.black);
            GUILayout.Label("BOOLEAN PARAMETERS");
            DrawUILine(Color.black);

            for (int i = 0; i < _target.parametres.bools.Count; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Button(_target.parametres.bools[i].name, GUILayout.Width(200));

                //SPACE
                GUILayout.Space(20);

                //string alias = _target.parametres.listBool[i].compName;

                //EXPOSED OR NOT ?
                _target.parametres.bools[i].exposed = EditorGUILayout.Toggle(_target.parametres.bools[i].compName, _target.parametres.bools[i].exposed);

                GUILayout.EndHorizontal();
                //GUILayout.Space(5);
            }

            if (GUILayout.Button("generate JSON"))
            {
                _target.createJSonComponents();
            }
        }
    }

    public static void DrawUILine(Color color, int thickness = 2, int padding = 10)
    {
        Rect r = EditorGUILayout.GetControlRect(GUILayout.Height(padding + thickness));
        r.height = thickness;
        r.y += padding / 2;
        r.x -= 2;
        r.width += 6;
        EditorGUI.DrawRect(r, color);
    }



}
