using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using System.IO;
using UnityEngine.SceneManagement;

public class oscAutoAssign_vfx02 : MonoBehaviour
{

    public bool verbose = false;
    public VisualEffect vfx;
    public OSC server;

    private List<string> paramNamesFloats;
    private List<string> paramNamesBooleans;

    public List<string> notExposedParams;

    private int[] controllerSliderNbs;

    void Start()
    {
        scanVFX();
        createJSonFloats();
        createJSonBooleans();
    }

    public void scanVFX()
    {
        paramNamesFloats = new List<string>();
        paramNamesBooleans = new List<string>();

        controllerSliderNbs = new int[] {29,34,28,33,27,32,26,31,25,30 };

        List<VFXExposedProperty> vfxProps = new List<VFXExposedProperty>();
        vfx.visualEffectAsset.GetExposedProperties(vfxProps);
        
        foreach (var p in vfxProps)
        {
            if(p.type.ToString() == "System.Int32")
            {
                // TO DO !!!!!
            }

            // Float
            if (p.type.ToString() == "System.Single")
            {
                if(notExposedParams.Contains(p.name))
                {
                    if(verbose)
                    Debug.Log("skip this parameter : " + p.name);
                }
                else
                    paramNamesFloats.Add("/FromVDMX/" + p.name);
            }
            
            // Bool
            else if (p.type.ToString() == "System.Boolean")
            {
                if (notExposedParams.Contains(p.name))
                {
                    if(verbose)
                    Debug.Log("skip this parameter : " + p.name);
                }
                else
                    paramNamesBooleans.Add("/FromVDMX/" + p.name);
            }            
        }

        //server.SetAllMessageHandler(onRecieveFloatValue);

    }
    public void onRecieveFloatValue(OscMessage message)
    {
        //Debug.Log("message : " + message.address);

        if(paramNamesFloats.Contains(message.address))
        {
            //Debug.Log("address: " + message.address);
            string[] splitArray = message.address.Split(char.Parse("/"));

            name = splitArray[2];
            //Debug.Log("name: " + name);

            vfx.SetFloat(name, message.GetFloat(0));
        }

        if(paramNamesBooleans.Contains(message.address))
        {
            Debug.Log("address: " + message.address);
            string[] splitArray = message.address.Split(char.Parse("/"));

            name = splitArray[2];
            Debug.Log("name: " + name + "  value : " + message.GetFloat(0));

            float val = message.GetFloat(0);
            if (val > 0.01)
            {
                vfx.SetBool(name, true);
            }
            else
            {
                vfx.SetBool(name, false);
            }
        }
    }

    public void createJSonFloats()

    {

        string path = "Assets/Resources/VDMX_presets/vdmx_layout_" + SceneManager.GetActiveScene().name + "_VFX.json";

        //Write some text to the test.txt file

        StreamWriter writer = new StreamWriter(path, false);

        writer.WriteLine("{\n\"classArray\" : [");
        for(int i = 0; i<paramNamesFloats.Count; i++)
        {
            string sliders = " \"Slider\"";
            if (i + 1 < paramNamesFloats.Count)
            {
                sliders = sliders + ",";
            }

            writer.WriteLine(sliders);



        }
        writer.WriteLine("  ],\n\"keyArray\" : [");

        for (int i = 0; i < paramNamesFloats.Count; i++)
        {
            //string name_p = "\"" + paramNamesFloats[i] + "\"";
            if(verbose)
                Debug.Log("parameter VFX Float : " + paramNamesFloats[i]);

            string str1 = paramNamesFloats[i];
            string str2 = "/FromVDMX/";
            string result = str1.Replace(str2, "");
            string name_p = "\"" + result + "\"";

            if (i + 1 < paramNamesFloats.Count)
            {
                name_p = name_p + ",";
            }

            writer.WriteLine(name_p);

        }
        writer.WriteLine("],");

        writer.WriteLine("\"uiBuilder\" : {");

        float col = 0;
        float line = 1;
        float it = 0;
        float j = 0;

        int nbButton = 0;
        for (int i = 0; i < paramNamesFloats.Count; i++)
        {
            //string name_p = "\"" + paramNamesFloats[i] + "\"";

            string str1 = paramNamesFloats[i];
            string str2 = "/FromVDMX/";
            string result = str1.Replace(str2, "");        

            if (it <= 1)
            {
                col = 1 + it * 6;
                it++;
            }
            else
            {
                j++;
                line += 6;
                col = 1;
                it = 1;
                
            }
            string name_p = "\"" + result + "\"" + ": {\n\"grdFrm\" : \"{{"+col.ToString()+", "+ line.ToString() +"}, {5, 5}}\",\n\"sendEnabled\" : true,\n\"sliderMode\" : 1,";



            str2 = "/";
            result = str1.Replace(str2, "\\/");
            
            string sndr = "\"sndrs\" : [\n{\n\"momentarySenderType\" : 0,\n\"normalizeFlag\" : true,\n\"msgAddress\" : \"" + result + "\",\n\"floatInvertFlag\" : false,\n\"outputPort\" : 6969,\n\"boolInvertFlag\" : false,\n\"boolThreshVal\" : 0.10000000149011612,\n\"highIntVal\" : 100,\n\"outputIPAddress\" : \"10.154.1.129\",\"outputLabel\" : \"PC_maiz\",\n\"dataSenderType\" : 2,\n\"lowIntVal\" : 0,\n\"enabled\" : true,\"intInvertFlag\" : false,\n\"senderType\" : 4\n}\n]\n,";

            string midiSrc = "\\/MIDI\\/ch. 1\\/ctrl [" + controllerSliderNbs[nbButton++] + "]\"";
            string reciever = "\"rcvrs\" : [\n{\n\"src\" : \"" + midiSrc + ", \n\"echNm2\" : \"CS X51-Port 1\" ,\n\"wantsEnbl\" : true\n}\n]\n}";  //"\"rcvrs\" : \n[\n{\n\"echNm2\" : \"CS X51-Port 1\",\"echo\" : true,\"src\" : \"\\/MIDI\\/ch. 1\\/note [" + controllerSliderNbs[nbButton++] + "]\",\"wantsEnbl\" : true\n}\n]\n}";

            if (i + 1 < paramNamesFloats.Count)
            {
                reciever = reciever + ",";

            }

            writer.WriteLine(name_p);
            writer.WriteLine(sndr);
            writer.WriteLine(reciever);
        }

        writer.WriteLine("}");
        writer.WriteLine("}");

        writer.Close();

    }

    public void createJSonBooleans()
    {
        string path = "Assets/Resources/VDMX_presets/vdmx_layout_" + SceneManager.GetActiveScene().name + "_VFX_booleans.json";


        //Write some text to the test.txt file

        StreamWriter writer = new StreamWriter(path, false);

        writer.WriteLine("{\n\"classArray\" : [");
        for (int i = 0; i < paramNamesBooleans.Count; i++)
        {
            writer.WriteLine(" \"Button\",");
        }
        writer.WriteLine("  ],\n\"keyArray\" : [");

        for (int i = 0; i < paramNamesBooleans.Count; i++)
        {
            //string name_p = "\"" + paramNamesFloats[i] + "\"";

            string str1 = paramNamesBooleans[i];
            string str2 = "/FromVDMX/";
            string result = str1.Replace(str2, "");
            string name_p = "\"" + result + "\"";

            if (i + 1 < paramNamesBooleans.Count)
            {
                name_p = name_p + ",";
            }

            writer.WriteLine(name_p);

        }
        writer.WriteLine("],");

        writer.WriteLine("\"uiBuilder\" : {");

        float col = 0;
        float line = 1;
        float it = 0;
        float j = 0;

        for (int i = 0; i < paramNamesBooleans.Count; i++)
        {
            //string name_p = "\"" + paramNamesFloats[i] + "\"";
            if (verbose)
                Debug.Log("parameter bool : " + paramNamesBooleans[i]);

            string str1 = paramNamesBooleans[i];
            string str2 = "/FromVDMX/";
            string result = str1.Replace(str2, "");

            if (it <= 5)
            {
                col = 1 + it * 6;
                it++;
            }
            else
            {
                j++;
                line += j * 6;
                col = 1;
                it = 1;

            }
            //string name_p = "\"" + result + "\"" + ": {\n\"VVGridSize\" : \"{" + col.ToString() + ", " + line.ToString() + "}, {5, 5}\",\n\"sendEnabled\" : true,\n\"sliderMode\" : 1,";
            string name_p = "\"" + result + "\"" + ": {\n\"grdFrm\" : \"{ {" + col.ToString() + "," + line.ToString() + "}, {5, 5} }\",\"mtxGrp\" : \"Button Group\",\"toggle\" : true,\"mtxFlg\" : false,";

/*            if (i + 1 < paramNamesBooleans.Count)
            {
                name_p = name_p + ",";
            }*/

            
            str2 = "/";
            result = str1.Replace(str2, "\\/");

            string sndr = "\"sndrs\" : [\n{\n\"momentarySenderType\" : 0,\n\"normalizeFlag\" : true,\n\"msgAddress\" : \"" + result + "\",\n\"floatInvertFlag\" : false,\n\"outputPort\" : 6969,\n\"boolInvertFlag\" : false,\n\"boolThreshVal\" : 0.10000000149011612,\n\"highIntVal\" : 100,\n\"outputIPAddress\" : \"10.154.1.129\",\"outputLabel\" : \"PC_maiz\",\n\"dataSenderType\" : 2,\n\"lowIntVal\" : 0,\n\"enabled\" : true,\"intInvertFlag\" : false,\n\"senderType\" : 4\n}\n]\n}";

            if (i + 1 < paramNamesBooleans.Count)
            {
                //name_p = name_p + ",";
                sndr = sndr + ",";

            }
            
            writer.WriteLine(name_p);
            writer.WriteLine(sndr);
        }

        writer.WriteLine("}");
        writer.WriteLine("}");

        writer.Close();

    }
}
