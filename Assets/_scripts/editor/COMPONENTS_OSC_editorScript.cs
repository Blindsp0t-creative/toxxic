using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


[CustomEditor(typeof(oscAutoAssignComponents),true)]

public class COMPONENTS_OSC_editorScript : Editor
{
    private oscAutoAssignComponents _target;
    private bool scanHierarchyDone = false;

    public void OnEnable()
    {
        _target = (oscAutoAssignComponents)target;
        _target.parametres.listFloats = new List<vdmx_parameter>();
        _target.parametres.listBool = new List<vdmx_parameter_bool>();
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (GUILayout.Button("SCAN HIERARCHY"))
        {
            _target.scanHierarchy();
            scanHierarchyDone = true;

        }
        GUILayout.Label("FLOATING POINT PARAMETERS");
        DrawUILine(Color.black);

        if (scanHierarchyDone)
        {
            for (int i = 0; i < _target.parametres.listFloats.Count; i++)
            {
                GUILayout.BeginHorizontal();

                //NOM DU PARAM
                string label = "    " + _target.parametres.listFloats[i].name;
                GUILayout.Label(label, GUILayout.Width(200));

                //SPACE
                GUILayout.Space(20);

                //SLIDER 
                float min = 0.0f;
                float max = 1.0f;
                if (_target.parametres.listFloats[i].hasRange)
                {
                    min = _target.parametres.listFloats[i].minRange;
                    max = _target.parametres.listFloats[i].maxRange;
                }
                GUILayout.HorizontalSlider(_target.parametres.listFloats[i].currentValue, min, max, GUILayout.Width(250));

                //SPACE
                GUILayout.Space(20);

                string alias;
                if (_target.parametres.listFloats[i].isInt)
                    alias = "[INT]         ";
                else
                    alias = "[FLOAT]   ";
                alias += _target.parametres.listFloats[i].compName;

                //EXPOSED OR NOT ?
                _target.parametres.listFloats[i].exposed = EditorGUILayout.Toggle(alias, _target.parametres.listFloats[i].exposed);

                GUILayout.EndHorizontal();
                //GUILayout.Space(5);
            }

            DrawUILine(Color.black);
            GUILayout.Label("BOOLEAN PARAMETERS");
            DrawUILine(Color.black);

            for (int i = 0; i < _target.parametres.listBool.Count; i++)
            {
                GUILayout.BeginHorizontal();

                GUILayout.Button(_target.parametres.listBool[i].name, GUILayout.Width(200));

                //SPACE
                GUILayout.Space(20);

                //string alias = _target.parametres.listBool[i].compName;

                //EXPOSED OR NOT ?
                _target.parametres.listBool[i].exposed = EditorGUILayout.Toggle(_target.parametres.listBool[i].compName, _target.parametres.listBool[i].exposed);



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
