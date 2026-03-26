using UnityEngine;
using System.Collections;
using UnityEditor;

[CustomEditor(typeof(oscAutoAssign_vfx02))]

public class VFX_OSC_editorScript : Editor
{

    private oscAutoAssign_vfx02 _target;


    public void OnEnable()
    {
        _target = (oscAutoAssign_vfx02)target;
    }


    public override void OnInspectorGUI()
    { 
        DrawDefaultInspector();

        if (GUILayout.Button("GENERATE VDMX JSON FILE"))
        {
            _target.scanVFX();
            _target.createJSonFloats();
            _target.createJSonBooleans();
        }
    }
}
