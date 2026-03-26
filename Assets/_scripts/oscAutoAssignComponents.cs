using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement;
using System;



public class vdmx_parameter
{
    public string name;
    public string fullname;
    public string compName;
    public bool exposed;
    public bool hasRange;
    public float minRange;
    public float maxRange;
    public string type;
    public CompInfo compInfo;
    public bool isInt = false;
    public float currentValue;    
}
public class vdmx_parameter_bool
{
    public string name;
    public string fullname;
    public string compName;
    public bool exposed;
    public string type;
    public CompInfo compInfo;
    public bool currentValue;
}
//[System.Serializable]
public class theList
{
    public List<vdmx_parameter> listFloats;
    public List<vdmx_parameter_bool> listBool;
}

[Serializable]
public class CompInfo
{
    public CompInfo(Component comp, FieldInfo info)
    {
        infoType = InfoType.Field;
        fieldInfo = info;
        type = fieldInfo.FieldType;
        this.comp = comp;
    }

    public CompInfo(Component comp, PropertyInfo info)
    {
        infoType = InfoType.Property;
        propInfo = info;
        type = propInfo.PropertyType;
        this.comp = comp;
    }

    public CompInfo(Component comp, MethodInfo info)
    {
        infoType = InfoType.Method;
        methodInfo = info;
        this.comp = comp;
    }


    public Component comp;

    public enum InfoType { Property, Field, Method, VFX, Material };
    public InfoType infoType;

    public Type type;
    public FieldInfo fieldInfo;
    public PropertyInfo propInfo;
    public MethodInfo methodInfo;

    public struct GenericInfo
    {
        public GenericInfo(Type type, string name)
        {
            this.type = type;
            this.name = name;
        }
        public Type type;
        public string name;
    };

    public GenericInfo genericInfo;
}


public class oscAutoAssignComponents : MonoBehaviour
{
    public OSC osc;
    public theList parametres = new theList();

    public oscAutoAssign_vfx02 vfx_osc;

    private int[] controllerSliderNbs; 
    private int[] controllerButtonNbs;

    void Start()
    {
        scanHierarchy();
        osc.SetAllMessageHandler(oscMessageHandler);
    }


    public void scanHierarchy() //only floats for now !!
    {
        parametres.listFloats = new List<vdmx_parameter>();
        parametres.listBool = new List<vdmx_parameter_bool>();

        controllerSliderNbs = new int[] { 4, 8, 12, 16, 20, 24, 3, 7, 11, 15, 19, 23, 2, 6, 10, 14, 18, 22 };
        controllerButtonNbs = new int[] { 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83 };

        Component[] comps = this.GetComponents<Component>();

        foreach (Component comp in comps)
        {
            FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

            int dotIndex = comp.GetType().ToString().LastIndexOf(".");
            string compType = comp.GetType().ToString().Substring(Mathf.Max(dotIndex + 1, 0));

            foreach (FieldInfo info in fields)
            {
                if (info.Name != "parametres")
                {
                    //Debug.Log("PARAMETRE DETECTE : " + info.Name + "  > field type: " + info.FieldType.ToString() + " / script component : " + compType);
                    if (info.FieldType.ToString() == "System.Single"
                        || info.FieldType.ToString() == "System.Double"
                        || info.FieldType.ToString() == "System.Int32") // filter float, double and int  parameters
                    {
                        RangeAttribute rangeAttribute = info.GetCustomAttribute<RangeAttribute>();

                        //Debug.Log("PARAMETRE DETECTE : " + info.Name + "  > field type: " + info.FieldType.ToString() + " / script component : " + compType);
                        vdmx_parameter t = new vdmx_parameter();
                        t.name = info.Name;
                        t.fullname = info.Name + "_" + compType;
                        t.compName = compType;
                        t.exposed = true;
                        t.hasRange = false;
                        t.minRange = 0;
                        t.maxRange = 0;
                        t.type = info.FieldType.ToString();

                        if (info.FieldType.ToString() == "System.Int32")
                        {
                            t.isInt = true;
                        }

                        CompInfo infoT = new CompInfo(comp, info);
                        object data = null;
                        t.compInfo = infoT;

                        if (rangeAttribute != null)
                        {
                            //Debug.Log("range!  Min! " + rangeAttribute.min + " max: " + rangeAttribute.max);
                            t.hasRange = true;
                            t.minRange = rangeAttribute.min;
                            t.maxRange = rangeAttribute.max;
                        }

                        parametres.listFloats.Add(t);

                    }
                    else if (info.FieldType.ToString() == "System.Boolean")
                    {
                        //Debug.Log("I HAVE A BOOL HERE! ");

                        vdmx_parameter_bool t = new vdmx_parameter_bool();
                        t.name = info.Name;
                        t.fullname = info.Name + "_" + compType;
                        t.compName = compType;
                        t.exposed = true;
                        t.type = info.FieldType.ToString();

                        CompInfo infoT = new CompInfo(comp, info);
                        object data = null;
                        t.compInfo = infoT;

                        parametres.listBool.Add(t);

                    }
                }
            }
        }
    }

    public void createJSonComponents()
    {
        string path = "Assets/Resources/VDMX_presets/vdmx_layout_" + SceneManager.GetActiveScene().name + "_components.json";
        StreamWriter writer = new StreamWriter(path, false);

        writer.WriteLine("{\n\"classArray\" : [");
        for (int i = 0; i < parametres.listFloats.Count; i++)
        {
            if (parametres.listFloats[i].exposed)
            {
                string typeName = " \"Slider\"";
                if (i + 1 < parametres.listFloats.Count || parametres.listBool.Count > 0)
                {
                    typeName = typeName + ",";
                }
                writer.WriteLine(typeName);
            }


        }

        for (int i = 0; i < parametres.listBool.Count; i++)
        {
            if (parametres.listBool[i].exposed)
            {
                string typeName = " \"Button\"";
                if (i + 1 < parametres.listBool.Count)
                {
                    typeName = typeName + ",";
                }
                writer.WriteLine(typeName);
            }
        }
        writer.WriteLine("  ],\n\"keyArray\" : [");

        for (int i = 0; i < parametres.listFloats.Count; i++)
        {
            if (parametres.listFloats[i].exposed)
            {
                string name_p = "\"" + parametres.listFloats[i].fullname + "\"";
                if (i + 1 < parametres.listFloats.Count || parametres.listBool.Count > 0)
                {
                    name_p = name_p + ",";
                }
                //name_p.ToUpper();
                writer.WriteLine(name_p);
            }

        }

        for (int i = 0; i < parametres.listBool.Count; i++)
        {
            if (parametres.listBool[i].exposed)
            {
                string name_p = "\"" + parametres.listBool[i].fullname + "\"";
                if (i + 1 < parametres.listBool.Count)
                {
                    name_p = name_p + ",";
                }
                //name_p.ToUpper();
                writer.WriteLine(name_p);
            }
        }


        int nbSlider = 0;

        writer.WriteLine("],");

        writer.WriteLine("\"uiBuilder\" : {");

        float col = 0;
        float line = 1;
        float it = 0;
        float j = 0;

        float size = 5;

        for (int i = 0; i < parametres.listFloats.Count; i++)
        {
            if (parametres.listFloats[i].exposed)
            {
                if(col < (1+size*6))
                {
                    col = 1 + it * 6;
                }
                else
                {
                    line += 6;
                    col = 1;
                    it = 0;
                }
                it++;

                string name_p = "\"" + parametres.listFloats[i].fullname + "\"" + ": {\n" +
                    "\"grdFrm\" : \"{{" + col.ToString() + ", " + line.ToString() + "}, {5, 5}}\",\n" +
                    "\"sendEnabled\" : true,\n" +
                    "\"sliderMode\" : 1,";

                if (parametres.listFloats[i].hasRange)
                {
                    string max = parametres.listFloats[i].maxRange.ToString().Replace(",", ".");
                    string min = parametres.listFloats[i].minRange.ToString().Replace(",", ".");

                    name_p += "\n\"maxRange\" : " + max + ",\n\"minRange\" : " + min + ",";
                }

                string msgAddress = "/FromVDMX/" + parametres.listFloats[i].fullname;
                string str2 = "/";
                string result = msgAddress.Replace(str2, "\\/");

                string sndr = "\"sndrs\" : [\n{\n\"momentarySenderType\" : 0,\n" +
                    "\"normalizeFlag\" : false,\n\"msgAddress\" : \"" + result + "\",\n" +
                    "\"floatInvertFlag\" : false,\n\"outputPort\" : 6969,\n" +
                    "\"boolInvertFlag\" : false,\n" +
                    "\"boolThreshVal\" : 0.10000000149011612,\n" +
                    "\"highIntVal\" : 100,\n" +
                    "\"outputIPAddress\" : \"10.154.1.129\",\"outputLabel\" : \"PC_maiz\",\n" +
                    "\"dataSenderType\" : 2,\n" +
                    "\"lowIntVal\" : 0,\n" +
                    "\"enabled\" : true,\"intInvertFlag\" : false,\n" +
                    "\"senderType\" : 4\n" +
                    "}\n" +
                    "],"; 


                string midiSrc = "\\/MIDI\\/ch. 1\\/ctrl [" + controllerSliderNbs[nbSlider++] + "]\"";
                string receiver = "\"rcvrs\" : [\n{\n\"src\" : \"" + midiSrc + ", \n\"echNm2\" : \"CS X51-Port 1\" ,\n\"wantsEnbl\" : true\n}\n]\n}";

                if (parametres.listBool.Count > 0)
                {
                    //name_p = name_p + ",";
                    receiver = receiver + ",";

                }

                writer.WriteLine(name_p);
                writer.WriteLine(sndr);
                writer.WriteLine(receiver);
            }
        }


        //jump one line for buttons
        line=1;
        col = 42;
        it = 0;

        int nbButton = 0;
        for (int i = 0; i < parametres.listBool.Count; i++)
        {
            if (parametres.listBool[i].exposed)
            {

                if (col < 42 + size * 6) 
                {
                    col = 42 + it * 6;
                }
                else
                {
                    line += 6;
                    col = 42;// + 6*size;
                    it = 0;
                }
                it++;


                //string name_p = "\"" + result + "\"" + ": {\n\"VVGridSize\" : \"{" + col.ToString() + ", " + line.ToString() + "}, {5, 5}\",\n\"sendEnabled\" : true,\n\"sliderMode\" : 1,";
                string name_p = "\"" + parametres.listBool[i].fullname + "\"" + ": {\n\"grdFrm\" : \"{ {" + col.ToString() + "," + line.ToString() + "}, {5, 5} }\",\"mtxGrp\" : \"Button Group\",\"toggle\" : true,\"mtxFlg\" : false,";

                string msgAddress = "/FromVDMX/" + parametres.listBool[i].fullname;
                string str2 = "/";
                string result = msgAddress.Replace(str2, "\\/");

                string sndr = "\"sndrs\" : [\n{\n\"momentarySenderType\" : 0,\n\"normalizeFlag\" : true,\n\"msgAddress\" : \"" + result + "\",\n\"floatInvertFlag\" : false,\n\"outputPort\" : 6969,\n\"boolInvertFlag\" : false,\n\"boolThreshVal\" : 0.10000000149011612,\n\"highIntVal\" : 100,\n\"outputIPAddress\" : \"10.154.1.129\",\"outputLabel\" : \"PC_maiz\",\n\"dataSenderType\" : 2,\n\"lowIntVal\" : 0,\n\"enabled\" : true,\"intInvertFlag\" : false,\n\"senderType\" : 4\n}\n],";//\n}";

                string reciever = "\"rcvrs\" : \n[\n{\n\"echNm2\" : \"CS X51-Port 1\",\"echo\" : true,\"src\" : \"\\/MIDI\\/ch. 1\\/note ["+ controllerButtonNbs[nbButton++] + "]\",\"wantsEnbl\" : true\n}\n]\n}";



                if (i + 1 < parametres.listBool.Count)
                {
                    //name_p = name_p + ",";
                    reciever = reciever + ",";

                }
                writer.WriteLine(name_p);
                writer.WriteLine(sndr);
                writer.WriteLine(reciever);
            }

        }


        writer.WriteLine("}");
        writer.WriteLine("}");

        writer.Close();

    }

    public void oscMessageHandler(OscMessage message)
    {
        //Debug.Log("osc message : " + message.address);

        vfx_osc.onRecieveFloatValue(message);

        for (int i = 0; i < parametres.listFloats.Count; i++)
        {
            //string msgAddress = ;
            if ("/FromVDMX/" + parametres.listFloats[i].fullname == message.address)
            {

                switch (parametres.listFloats[i].compInfo.infoType)
                {
                    case CompInfo.InfoType.Field:

                        if (parametres.listFloats[i].isInt)
                        {
                            parametres.listFloats[i].compInfo.fieldInfo.SetValue(parametres.listFloats[i].compInfo.comp, message.GetInt(0));
                            parametres.listFloats[i].currentValue = message.GetInt(0);
                        }

                        else //it's a float
                        {
                            parametres.listFloats[i].compInfo.fieldInfo.SetValue(parametres.listFloats[i].compInfo.comp, message.GetFloat(0));
                            parametres.listFloats[i].currentValue = message.GetFloat(0);
                        }

                        break;

                    case CompInfo.InfoType.Property:
                        if (parametres.listFloats[i].isInt)
                        {
                            parametres.listFloats[i].compInfo.propInfo.SetValue(parametres.listFloats[i].compInfo.comp, message.GetInt(0));
                            parametres.listFloats[i].currentValue = message.GetInt(0);
                        }

                        else
                        {
                            parametres.listFloats[i].compInfo.propInfo.SetValue(parametres.listFloats[i].compInfo.comp, message.GetFloat(0));
                            parametres.listFloats[i].currentValue = message.GetFloat(0);
                        }

                        break;
                }
            }
        }

        for (int i = 0; i < parametres.listBool.Count; i++)
        {
            //string msgAddress = ;
            if ("/FromVDMX/" + parametres.listBool[i].fullname == message.address)
            {
                float val = message.GetFloat(0);
                if (val > 0.01)
                {
                    parametres.listBool[i].compInfo.fieldInfo.SetValue(parametres.listBool[i].compInfo.comp, true);
                }
                else
                {
                    parametres.listBool[i].compInfo.fieldInfo.SetValue(parametres.listBool[i].compInfo.comp, false);
                }


            }
        }
    }
}
