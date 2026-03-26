using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Reflection;
using UnityEngine.SceneManagement;
using System;

[Serializable]
public class osc_parameter_float
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
    public float currentValue;
    public int faderId = 0;
}
[Serializable]
public class osc_parameter_int
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
    public int currentValue;
}
[Serializable]
public class osc_parameter_bool
{
    public string name;
    public string fullname;
    public string compName;
    public bool exposed;
    public string type;
    public CompInfo compInfo;
    public bool currentValue;
}
[Serializable]
public class osc_parameter_string
{
    public string name;
    public string fullname;
    public string compName;
    public bool exposed;
    public string type;
    public CompInfo compInfo;
    public string currentValue;
}



public class oscAutoAssignComponents_openStage : MonoBehaviour
{
    public OSC osc;

    //[SerializeField]
    public parameters parametres;// = new parameters();

    private int[] controllerSliderNbs; 
    private int[] controllerButtonNbs;
    private int[] controllerFaderNbs;

    private int nbOfExposedParams=0;

    void Start()
    {


        //register all public parameters from attached components
        scanHierarchy();

        //create a json file to be read by open stage control
        createJSonComponents();

        //set osc listeners
        //osc.SetAllMessageHandler(handlerAll);
        osc.SetAllMessageHandler(oscMessageHandler);
    }

    public void handlerAll(OscMessage message)
    {
        Debug.Log("message : " + message.address);

        //PARSING BUTTON MESSAGES
        if(message.address == "/note")
        {
            Debug.Log(message.GetInt(0) + " " + message.GetInt(1) + " " + message.GetInt(2));
        }
    }
    public void scanHierarchy() 
    {
        /*
        if (parametres == null)
            parametres = new parameters();
        */

        //midi cc adresses for knobs on machineWerks controller CSX-51
        controllerSliderNbs = new int[] { 4, 8, 12, 16, 20, 24, 3, 7, 11, 15, 19, 23, 2, 6, 10, 14, 18, 22, 29, 34, 28, 33, 27, 32, 26, 31, 25, 30 };

        //midiKICK notes
        controllerButtonNbs = new int[] { 60, 61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83};

        controllerFaderNbs = new int[] { 1, 5, 9, 13, 17, 21, 35 };

        if(parametres.floats == null)
            parametres.floats = new List<osc_parameter_float>();
        if (parametres.ints == null)
            parametres.ints = new List<osc_parameter_int>();
        if (parametres.bools == null)
            parametres.bools = new List<osc_parameter_bool>();
        if (parametres.strings == null)
            parametres.strings = new List<osc_parameter_string>();
               
        //get list of all components attached
        Component[] comps = this.GetComponents<Component>();


        foreach (Component comp in comps)
        {
            //get infos
            FieldInfo[] fields = comp.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public);

            int dotIndex = comp.GetType().ToString().LastIndexOf(".");
            string compType = comp.GetType().ToString().Substring(Mathf.Max(dotIndex + 1, 0));

            //for each parameter
            foreach (FieldInfo info in fields)
            {
                if (info.Name != "parametres")
                {
                    //Debug.Log("PARAMETRE DETECTE : " + info.Name + "  > field type: " + info.FieldType.ToString() + " / script component : " + compType);

                    if (info.FieldType.ToString() == "System.Single" || info.FieldType.ToString() == "System.Double")
                    {
                        CompInfo infoT = new CompInfo(comp, info);
                        if (!parameterFloatAlreadyExists(info.Name, infoT))
                        {                            
                            createParameterFloat(info, compType, true, false, 0, 1, infoT);
                            nbOfExposedParams++;
                        }
                    }
                    else if (info.FieldType.ToString() == "System.Boolean")
                    {
                        CompInfo infoT = new CompInfo(comp, info);
                        if (!parameterBoolAlreadyExists(info.Name, infoT))
                        {                            
                            createParameterBool(info, compType, true, infoT);
                        }

                    }
                    else if (info.FieldType.ToString() == "System.Int32")
                    {
                        CompInfo infoT = new CompInfo(comp, info);
                        if (!parameterIntAlreadyExists(info.Name, infoT))
                        {                            
                            createParameterInt(info, compType, true, false, 0, 1, infoT);
                            nbOfExposedParams++;
                        }
                    }
                    else if (info.FieldType.ToString() == "System.String")
                    {
                        CompInfo infoT = new CompInfo(comp, info);
                        createParameterString(info, compType, true, infoT);
                    }
                }
            }            
        }
        Debug.Log(nbOfExposedParams + " parameters exposed! (max is 28 + 6 faders ;) ");
    }
    public void createParameterFloat(FieldInfo info, string compName, bool exposed, bool hasRange, float minRange, float maxRange, CompInfo infoComp)
    {
        osc_parameter_float t = new osc_parameter_float();
        t.name = info.Name;
        t.fullname = info.Name + "_" + compName;
        t.compName = compName;
        t.exposed = exposed;
        t.hasRange = hasRange;
        t.minRange = minRange;
        t.maxRange = maxRange;
        t.type = info.FieldType.ToString();
        t.compInfo = infoComp;

        RangeAttribute rangeAttribute = info.GetCustomAttribute<RangeAttribute>();
        if (rangeAttribute != null)
        {
            t.hasRange = true;
            t.minRange = rangeAttribute.min;
            t.maxRange = rangeAttribute.max;
        }

        parametres.floats.Add(t);
    }
    public void createParameterInt(FieldInfo info, string compName, bool exposed, bool hasRange, float minRange, float maxRange, CompInfo infoComp)
    {
        osc_parameter_int t = new osc_parameter_int();
        t.name = info.Name;
        t.fullname = info.Name + "_" + compName;
        t.compName = compName;
        t.exposed = exposed;
        t.hasRange = hasRange;
        t.minRange = minRange;
        t.maxRange = maxRange;
        t.type = info.FieldType.ToString();
        t.compInfo = infoComp;

        RangeAttribute rangeAttribute = info.GetCustomAttribute<RangeAttribute>();
        if (rangeAttribute != null)
        {
            t.hasRange = true;
            t.minRange = rangeAttribute.min;
            t.maxRange = rangeAttribute.max;
        }

        parametres.ints.Add(t);
    }
    public void createParameterBool(FieldInfo info, string compName, bool exposed, CompInfo infoComp)
    {
        osc_parameter_bool t = new osc_parameter_bool();
        t.name = info.Name;
        t.fullname = info.Name + "_" + compName;
        t.compName = compName;
        t.exposed = exposed;
        t.type = info.FieldType.ToString();
        t.compInfo = infoComp;

        parametres.bools.Add(t);

    }
    public void createParameterString(FieldInfo info, string compName, bool exposed, CompInfo infoComp)
    {
        osc_parameter_string t = new osc_parameter_string();
        t.name = info.Name;
        t.fullname = info.Name + "_" + compName;
        t.compName = compName;
        t.exposed = exposed;
        t.type = info.FieldType.ToString();

        parametres.strings.Add(t);

    }
    public void createJSonComponents()
    {
        controllerSliderNbs = new int[] { 4, 8, 12, 16, 20, 24, 3, 7, 11, 15, 19, 23, 2, 6, 10, 14, 18, 22, 29, 34, 28, 33, 27, 32, 26, 31, 25, 30 };

        //filepath for generated json file to be opened in open stage control
        string path = "Assets/Resources/openStageControl_" + SceneManager.GetActiveScene().name + "_components.json";

        StreamWriter writer = new StreamWriter(path, false);

        //write open stage control header to file
        writer.WriteLine("{  \"createdWith\": \"Open Stage Control\",  " +
            "\"version\": \"1.26.1\",  \"type\": \"session\",  \"content\": {    " +
            "\"type\": \"root\",    \"lock\": false,    \"id\": \"root\",    " +
            "\"visible\": true,    \"interaction\": true,    \"comments\": \"\",    \"width\": \"auto\",    " +
            "\"height\": \"auto\",    \"colorText\": \"auto\",    \"colorWidget\": \"auto\",    " +
            "\"alphaFillOn\": \"auto\",    \"borderRadius\": \"auto\",    \"padding\": \"auto\",    " +
            "\"html\": \"\",    \"css\": \"\",    \"colorBg\": \"auto\",    \"layout\": \"default\",    " +
            "\"justify\": \"start\",    \"gridTemplate\": \"\",    \"contain\": true,    \"scroll\": true,    " +
            "\"innerPadding\": true,    \"tabsPosition\": \"top\",    \"hideMenu\": false,    " +
            "\"variables\": \"@{parent.variables}\",    \"traversing\": false,    \"value\": \"\",    " +
            "\"default\": \"\",    \"linkId\": \"\",    \"address\": \"/midas\",    \"preArgs\": \"\",    " +
            "\"typeTags\": \"\",    \"decimals\": 2,    \"target\": \"\",    \"ignoreDefaults\": false,    " +
            "\"bypass\": false,    \"onCreate\": \"\",    \"onValue\": \"\",    \"onPreload\": \"\",    \"widgets\": [");



        //FLOAT PARAMETERS
        int j = 0; //knob's row
        int linesCpt = 0; //nb of knobs on a row
        int nbParam = 0; //knobs + faders
        int knobNb = 0; //knobs
        for (int i = 0; i < parametres.floats.Count; i++)
        {
            
            if (linesCpt > 5) // there are 6 knobs on a row on my controller
            {
                j++;
                linesCpt = 0;
            }
                

            if(parametres.floats[i].faderId !=0)
            {
                writer.WriteLine("\n");
                if (parametres.floats[i].faderId == 7)
                    createHorizontalFaderFloat(writer, parametres.floats[i]);
                else
                    createFaderFloat(writer, 60 + 180 * (parametres.floats[i].faderId-1), parametres.floats[i]);

                nbParam++;
            }
            else if (parametres.floats[i].exposed && knobNb < 18)
            {
                writer.WriteLine("\n");

                //write json code for one panel
                createPanelFloat(writer, 15 + j * 150, 15 + linesCpt * 180, i);

                //create widget inside panel
                createWidgetFloat(writer, controllerSliderNbs[knobNb]);
                
                //create text boxes for labels and value
                createTextAreasFloat(writer, "textArea_" + knobNb, "valueArea_" + knobNb, parametres.floats[nbParam], knobNb, "knob_" + controllerSliderNbs[knobNb].ToString());
                
                writer.WriteLine(",\n");

                linesCpt++; //knob number on the current row
                nbParam++;
                knobNb++;
            }
            else if(parametres.floats[i].exposed && knobNb < 28)
            {
                if (knobNb == 18) j = 0; //reset y position 

                if (linesCpt > 1) //two buutons by line
                {
                    j++; //increment y
                    linesCpt = 0;
                }

                writer.WriteLine("\n");

                //write json code for one panel
                createPanelFloat(writer, 15 + j * 150, 1100 + linesCpt * 180, i);

                //create widget inside panel
                createWidgetFloat(writer, controllerSliderNbs[knobNb]);

                //create text boxes for labels and value
                createTextAreasFloat(writer, "textArea_" + knobNb, "valueArea_" + knobNb, parametres.floats[nbParam], knobNb, "knob_" + controllerSliderNbs[knobNb].ToString());

                writer.WriteLine(",\n");

                linesCpt++; //knob number on the current row
                nbParam++;
                knobNb++;
            }
            else if(parametres.floats[i].exposed == false)
            { /*Debug.Log("not exposed : " + parametres.floats[i].name);*/ nbParam++; }
            
        }


        //INT PARAMETERS
        //ALWAYS PUT INT PARAMS ON CONTROLLER's VERTICAL ROWS OF KNOBS
        if(knobNb <= 18)
        {
            linesCpt = 0;
            j = 0;
            knobNb = 18;
        }
            

        for (int i = 0; i < parametres.ints.Count; i++)
        {
            if (parametres.ints[i].exposed && knobNb < 28)
            {
                if(knobNb < 28)
                {
                    if (knobNb == 18) j = 0; //reset y position 

                    if (linesCpt > 1) //two buutons by line
                    {
                        j++; //increment y
                        linesCpt = 0;
                    }
                }

                writer.WriteLine("\n");
                //write json code for one panel
                createPanelFloat(writer, 15 + j * 150, 1100 + linesCpt * 180, i);

                //create widget inside panel
                createWidgetInt(writer, controllerSliderNbs[knobNb]);

                //create text boxes for labels and value
                createTextAreasInt(writer, "textArea_" + knobNb, "valueArea_" + knobNb, parametres.ints[i], knobNb, "knob_" + controllerSliderNbs[knobNb].ToString());

                writer.WriteLine(",\n");

                linesCpt++; //knob number on the current row
                nbParam++;
                knobNb++;
            }
        }


        //BOOL PARAMETERS
        //write button's panel header
        createPanelBool(writer);

        j = 0;
        linesCpt = 0;
        nbParam = 0;

        for (int i = 0; i < parametres.bools.Count; i++)
        {
            if (parametres.bools[i].exposed && nbParam < 24)
            {
                if (linesCpt > 5) // there are 6 knobs on a row on my controller
                {
                    j++;
                    linesCpt = 0;
                }

                createWidgetBool(writer, 10 + j * 70, 10 + linesCpt * 100, controllerButtonNbs[nbParam], parametres.bools[i].name);
                if (i + 1 < parametres.bools.Count)
                    writer.WriteLine(",");

                linesCpt++;
                nbParam++;
            }
        }

        writer.WriteLine("],\"tabs\": []}],\"tabs\": []}}");
        writer.Close();
    }
    public void createPanelFloat(StreamWriter _writer, int top, int left, int panelId)
    {
        _writer.WriteLine("{  " +
            "\"type\": \"panel\",  " +
            "\"top\": "+top.ToString() +",  " +
            "\"left\": " + left.ToString() + ",  " +
            "\"lock\": false,  " +
            "\"id\": \"panel_"+ panelId.ToString() + "\",  " +
            "\"visible\": true,  " +
            "\"interaction\": true,  " +
            "\"comments\": \"\",  " +
            "\"width\": 170,  " +
            "\"height\": 150,  " +
            "\"expand\": false,  " +
            "\"colorText\": \"auto\",  " +
            "\"colorWidget\": \"auto\",  " +
            "\"colorStroke\": \"auto\",  " +
            "\"colorFill\": \"auto\",  " +
            "\"alphaStroke\": \"auto\",  " +
            "\"alphaFillOff\": \"auto\",  " +
            "\"alphaFillOn\": \"auto\",  " +
            "\"lineWidth\": \"auto\",  " +
            "\"borderRadius\": \"auto\",  " +
            "\"padding\": \"auto\",  " +
            "\"html\": \"\",  " +
            "\"css\": \"\",  " +
            "\"colorBg\": \"auto\",  " +
            "\"layout\": \"default\",  " +
            "\"justify\": \"start\",  " +
            "\"gridTemplate\": \"\",  " +
            "\"contain\": true,  " +
            "\"scroll\": true,  " +
            "\"innerPadding\": true,  " +
            "\"tabsPosition\": \"top\",  " +
            "\"variables\": \"@{parent.variables}\",  " +
            "\"traversing\": false,  " +
            "\"value\": \"\",  " +
            "\"default\": \"\",  " +
            "\"linkId\": \"\",  " +
            "\"address\": \"auto\",  " +
            "\"preArgs\": \"\",  " +
            "\"typeTags\": \"\",  " +
            "\"decimals\": 2,  " +
            "\"target\": \"\",  " +
            "\"ignoreDefaults\": false,  " +
            "\"bypass\": false,  " +
            "\"onCreate\": \"\",  " +
            "\"onValue\": \"\",  " +
            "\"widgets\": [    {");
    }
    public void createPanelBool(StreamWriter _writer)
    {
        _writer.WriteLine("{  " +
            "\"type\": \"panel\",  " +
            "\"top\": " + 10 + ",  " +
            "\"left\": " + 1460 + ",  " +
            "\"lock\": false,  " +
            "\"id\": \"panel_" + "buttons" + "\",  " +
            "\"visible\": true,  " +
            "\"interaction\": true,  " +
            "\"comments\": \"\",  " +
            "\"width\": 620,  " +
            "\"height\": 300,  " +
            "\"expand\": false,  " +
            "\"colorText\": \"auto\",  " +
            "\"colorWidget\": \"auto\",  " +
            "\"colorStroke\": \"auto\",  " +
            "\"colorFill\": \"auto\",  " +
            "\"alphaStroke\": \"auto\",  " +
            "\"alphaFillOff\": \"auto\",  " +
            "\"alphaFillOn\": \"auto\",  " +
            "\"lineWidth\": \"auto\",  " +
            "\"borderRadius\": \"auto\",  " +
            "\"padding\": \"auto\",  " +
            "\"html\": \"\",  " +
            "\"css\": \"\",  " +
            "\"colorBg\": \"auto\",  " +
            "\"layout\": \"default\",  " +
            "\"justify\": \"start\",  " +
            "\"gridTemplate\": \"\",  " +
            "\"contain\": true,  " +
            "\"scroll\": true,  " +
            "\"innerPadding\": true,  " +
            "\"tabsPosition\": \"top\",  " +
            "\"variables\": \"@{parent.variables}\",  " +
            "\"traversing\": false,  " +
            "\"value\": \"\",  " +
            "\"default\": \"\",  " +
            "\"linkId\": \"\",  " +
            "\"address\": \"auto\",  " +
            "\"preArgs\": \"\",  " +
            "\"typeTags\": \"\",  " +
            "\"decimals\": 2,  " +
            "\"target\": \"\",  " +
            "\"ignoreDefaults\": false,  " +
            "\"bypass\": false,  " +
            "\"onCreate\": \"\",  " +
            "\"onValue\": \"\",  " +
            "\"widgets\": [    ");
    }
    public void createWidgetFloat(StreamWriter _writer, int ccMidi)
    {

        float rangeMin = 0.0f;
        float rangeMax = 1.0f;
        
        _writer.WriteLine("\"type\": \"knob\"," +
            "\"top\": " + 40 + "," +
            "\"left\": "+ 10 + "," +
            "\"lock\": false," +
            "\"id\":\"" + "knob_" +  ccMidi.ToString() + "\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 140," +
            "\"height\": 90," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \""+ "VAR{ colorWidget, auto}"+"\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": \"auto\"," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"design\": \"default\"," +
            "\"colorKnob\": \"auto\"," +
            "\"pips\": false," +
            "\"dashed\": false," +
            "\"spring\": false," +
            "\"doubleTap\": false," +
            "\"range\": {  " +
            "\"min\": "+ rangeMin.ToString().Replace(",", ".") + ",  " +
            "\"max\":" +  rangeMax.ToString().Replace(",", ".") + "}," +
            "\"logScale\": false," +
            "\"sensitivity\": 1," +
            "\"steps\": \"\"," +
            "\"origin\": \"auto\"," +
            "\"value\": \"\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"" +  "/control" + "\"," +
            "\"preArgs\": [  1,  "+  ccMidi + "]," +
            "\"typeTags\": {    \"min\": 0,  \"max\": 127}," +
            "\"decimals\": 4," +
            "\"target\": \"midi:synth\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"let timeout;timeout = setTimeout(stopBlink, 200); function stopBlink() { setVar('this', 'colorWidget', 'auto') } setVar('this', 'colorWidget', '#f28018')\"," +
            "\"onTouch\": \"\"," +
            "\"angle\": 270," +
            "\"mode\": \"vertical\"");
        _writer.WriteLine("}, ");
    }
    public void createWidgetInt(StreamWriter _writer, int ccMidi)
    {

        int rangeMin = 0;
        int rangeMax = 1;

        _writer.WriteLine("\"type\": \"knob\"," +
            "\"top\": " + 40 + "," +
            "\"left\": " + 10 + "," +
            "\"lock\": false," +
            "\"id\":\"" + "knob_" + ccMidi.ToString() + "\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 140," +
            "\"height\": 90," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"" + "VAR{ colorWidget, auto}" + "\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": \"auto\"," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"design\": \"default\"," +
            "\"colorKnob\": \"auto\"," +
            "\"pips\": false," +
            "\"dashed\": false," +
            "\"spring\": false," +
            "\"doubleTap\": false," +
            "\"range\": {  " +
            "\"min\": " + rangeMin.ToString().Replace(",", ".") + ",  " +
            "\"max\":" + rangeMax.ToString().Replace(",", ".") + "}," +
            "\"logScale\": false," +
            "\"sensitivity\": 1," +
            "\"steps\": \"\"," +
            "\"origin\": \"auto\"," +
            "\"value\": \"\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"" + "/control" + "\"," +
            "\"preArgs\": [  1,  " + ccMidi + "]," +
            "\"typeTags\": \"int\"," +
            "\"decimals\": 2," +
            "\"target\": \"midi:synth\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"let timeout;timeout = setTimeout(stopBlink, 200); function stopBlink() { setVar('this', 'colorWidget', 'auto') } setVar('this', 'colorWidget', '#f28018')\"," +
            "\"onTouch\": \"\"," +
            "\"angle\": 270," +
            "\"mode\": \"vertical\"");
        _writer.WriteLine("},");
    }
    public void createFaderFloat(StreamWriter _writer, int left, osc_parameter_float param)
    {
        _writer.WriteLine("{\"type\": \"fader\"," +
            "\"top\": 560," +
            "\"left\": "+left+"," +
            "\"lock\": false," +
            "\"id\": \"fader_"+param.faderId+"\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 80," +
            "\"height\": 350," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"auto\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": \"auto\"," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"design\": \"compact\"," +
            "\"knobSize\": \"auto\"," +
            "\"colorKnob\": \"auto\"," +
            "\"horizontal\": false," +
            "\"pips\": false," +
            "\"dashed\": false," +
            "\"gradient\": []," +
            "\"snap\": false," +
            "\"spring\": false," +
            "\"doubleTap\": false," +
            "\"range\": {\"min\": 0,\"max\": 1}," +
            "\"logScale\": false," +
            "\"sensitivity\": 1," +
            "\"steps\": \"\"," +
            "\"origin\": \"auto\"," +
            "\"value\": \"\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"/fader/"+param.name+"\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \""+ "set('"+param.name + "_label', (get('min"+param.name+"') + value * (get('max"+param.name+"') - get('min"+param.name+"'))).toFixed(2));" +"\"," +
            "\"onTouch\": \"\"},");

        //add a textArea to display the fader's name
        _writer.WriteLine("{\"type\": \"textarea\"," +
            "\"top\": 500," +
            "\"left\": "+ left + "," +
            "\"lock\": false," +
            "\"id\": \""+param.name+"_label\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": \""+80+"\"," +
            "\"height\": \""+60+"\"," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"auto\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": \"auto\"," +
            "\"html\": \""+param.name+"\"," +
            "\"css\": \"\"," +
            "\"value\": \"\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"/control/"+param.name+"\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"float\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"},");

        //add variables for min & max values
        //variable min
        _writer.WriteLine("{" +
            "\"type\": \"variable\"," +
            "\"lock\": false," +
            "\"id\": \"min" + param.name + "\"," +
            "\"comments\": \"\"," +
            "\"value\":" + param.minRange.ToString().Replace(",", ".") + "," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"auto\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"},");

        //variable max
        _writer.WriteLine("{" +
            "\"type\": \"variable\"," +
            "\"lock\": false," +
            "\"id\": \"max" + param.name + "\"," +
            "\"comments\": \"\"," +
            "\"value\": " + param.maxRange.ToString().Replace(",", ".") + "," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"auto\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"},");

        //indicator line
        _writer.WriteLine("{\"type\": \"fader\",\"top\": 560,\"left\": 20,\"lock\": false,\"id\": \"fader_5\",\"visible\": true,\"interaction\": true,\"comments\": \"\",\"width\": 1100,\"height\": 350,\"expand\": \"false\",\"colorText\": \"auto\",\"colorWidget\": \"auto\",\"colorStroke\": \"auto\",\"colorFill\": \"auto\",\"alphaStroke\": \"auto\",\"alphaFillOff\": \"auto\",\"alphaFillOn\": \"auto\",\"lineWidth\": 0.2,\"borderRadius\": \"auto\",\"padding\": \"auto\",\"html\": \"\",\"css\": \"\",\"design\": \"default\",\"knobSize\": \"auto\",\"colorKnob\": \"auto\",\"horizontal\": true,\"pips\": false,\"dashed\": false,\"gradient\": [],\"snap\": false,\"spring\": false,\"doubleTap\": false,\"range\": {\"min\": 0,\"max\": 1},\"logScale\": false,\"sensitivity\": 1,\"steps\": \"\",\"origin\": \"auto\",\"value\": 1,\"default\": \"\",\"linkId\": \"\",\"address\": \"auto\",\"preArgs\": \"\",\"typeTags\": \"\",\"decimals\": 2,\"target\": \"\",\"ignoreDefaults\": false,\"bypass\": false,\"onCreate\": \"\",\"onValue\": \"\",\"onTouch\": \"\"},");
    }
    public void createHorizontalFaderFloat(StreamWriter _writer, osc_parameter_float param)
    {
        _writer.WriteLine("{\"type\": \"fader\"," +
            "\"top\": 800," +
            "\"left\": " + 1520 + "," +
            "\"lock\": false," +
            "\"id\": \"fader_" + param.faderId + "\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 570," +
            "\"height\": 110," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"auto\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": \"auto\"," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"design\": \"compact\"," +
            "\"knobSize\": \"auto\"," +
            "\"colorKnob\": \"auto\"," +
            "\"horizontal\": true," +
            "\"pips\": false," +
            "\"dashed\": false," +
            "\"gradient\": []," +
            "\"snap\": false," +
            "\"spring\": false," +
            "\"doubleTap\": false," +
            "\"range\": {\"min\": 0,\"max\": 1}," +
            "\"logScale\": false," +
            "\"sensitivity\": 1," +
            "\"steps\": \"\"," +
            "\"origin\": \"auto\"," +
            "\"value\": \"\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"/fader/" + param.name + "\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"," +
            "\"onTouch\": \"\"},");
    }
    public void createWidgetBool(StreamWriter _writer, int top, int left, int ccMidi, string label)
    {
        _writer.WriteLine("{\"type\": \"button\"," +
            "\"top\": "+top+"," +
            "\"left\": "+left+"," +
            "\"lock\": false," +
            "\"id\": \"button_"+ccMidi+"\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": \"auto\"," +
            "\"height\": \"auto\"," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"auto\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": \"auto\"," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"value\": \"\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"/control\"," +
            "\"preArgs\": [  1,  "+ccMidi+"]," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"midi:synth\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"if(value === 1){send('/note/"+label+"', 1, "+ccMidi+",126)}if(value === 0){send('/note/"+label+"', 1, "+ccMidi+", 0)}\"," +
            "\"colorTextOn\": \"auto\"," +
            "\"label\": \""+label+"\"," +
            "\"vertical\": false," +
            "\"wrap\": false," +
            "\"on\": 1," +
            "\"off\": 0," +
            "\"mode\": \"toggle\"," +
            "\"doubleTap\": false," +
            "\"decoupled\": false}");
        //_writer.WriteLine("}, ");
    }
    public void createTextAreasFloat(StreamWriter _writer, string id, string label, osc_parameter_float param, int knob, string knobName)
    {
        //name of the param
        _writer.WriteLine("{" +
            "\"type\": \"textarea\"," +
            "\"top\": 0," +
            "\"left\": 0," +
            "\"lock\": false," +
            "\"id\": \"" +  id + "\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 100," +
            "\"height\": 40," +
            "\"expand\": false," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"auto\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": \"auto\"," +
            "\"html\": \""+ param.name + "\"," +
            "\"css\": \"\"," +
            "\"value\": \"" + "" + "\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"auto\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"" +
            "},");

        //value raw
        _writer.WriteLine("{" +
            "\"type\": \"textarea\"," +
            "\"top\": 0," +
            "\"left\": 80," +
            "\"lock\": false," +
            "\"id\": \"" +  label + "\"," +
            "\"visible\": false," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 80," +
            "\"height\": 40," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"auto\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": 6," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"value\": \"@{" + knobName + "} \"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"" + "/control/" + param.name + "\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"float\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"set('valueArea_"+knob+"_mapped', (get('min" + knob + "') + value * (get('max" + knob + "') - get('min" + knob + "'))).toFixed(4));\"" +
            "},");

        //value mapped in range
        _writer.WriteLine("{" +
            "\"type\": \"textarea\"," +
            "\"top\": 0," +
            "\"left\": 90," +
            "\"lock\": false," +
            "\"id\": \"" + label + "_mapped" + "\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 70," +
            "\"height\": 40," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"0\"," +
            "\"colorStroke\": \"0\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"0\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"0\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": 6," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"value\": \"\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"" + "/control/" + param.name + "\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"float\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"},");

        //variable min
        _writer.WriteLine("{" +
            "\"type\": \"variable\"," +
            "\"lock\": false," +
            "\"id\": \"min"+ knob + "\"," +
            "\"comments\": \"\"," +
            "\"value\":"+ param.minRange.ToString().Replace(",", ".") + "," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"auto\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"},");

        //variable max
        _writer.WriteLine("{" +
            "\"type\": \"variable\"," +
            "\"lock\": false," +
            "\"id\": \"max"+ knob + "\"," +
            "\"comments\": \"\"," +
            "\"value\": "+ param.maxRange.ToString().Replace(",", ".") + "," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"auto\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"}");

        _writer.WriteLine("],\"tabs\": []}");
    }
    public void createTextAreasInt(StreamWriter _writer, string id, string label, osc_parameter_int param, int knob, string knobName)
    {
        //name of the param
        _writer.WriteLine("{" +
            "\"type\": \"textarea\"," +
            "\"top\": 0," +
            "\"left\": 0," +
            "\"lock\": false," +
            "\"id\": \"" + id + "\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 100," +
            "\"height\": 40," +
            "\"expand\": false," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"auto\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": \"auto\"," +
            "\"html\": \"" + param.name + "\"," +
            "\"css\": \"\"," +
            "\"value\": \"" + "" + "\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"auto\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"" +
            "},");

        //value raw
        _writer.WriteLine("{" +
            "\"type\": \"textarea\"," +
            "\"top\": 0," +
            "\"left\": 80," +
            "\"lock\": false," +
            "\"id\": \"" + label + "\"," +
            "\"visible\": false," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 80," +
            "\"height\": 40," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"auto\"," +
            "\"colorStroke\": \"auto\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"auto\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"auto\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": 6," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"value\": \"@{" + knobName + "} \"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"" + "/control/" + param.name + "\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"float\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"set('valueArea_" + knob + "_mapped', (get('min" + knob + "') + value * (get('max" + knob + "') - get('min" + knob + "'))).toFixed(2));\"" +
            "},");

        //value mapped in range
        _writer.WriteLine("{" +
            "\"type\": \"textarea\"," +
            "\"top\": 0," +
            "\"left\": 90," +
            "\"lock\": false," +
            "\"id\": \"" + label + "_mapped" + "\"," +
            "\"visible\": true," +
            "\"interaction\": true," +
            "\"comments\": \"\"," +
            "\"width\": 70," +
            "\"height\": 40," +
            "\"expand\": \"false\"," +
            "\"colorText\": \"auto\"," +
            "\"colorWidget\": \"0\"," +
            "\"colorStroke\": \"0\"," +
            "\"colorFill\": \"auto\"," +
            "\"alphaStroke\": \"0\"," +
            "\"alphaFillOff\": \"auto\"," +
            "\"alphaFillOn\": \"auto\"," +
            "\"lineWidth\": \"0\"," +
            "\"borderRadius\": \"auto\"," +
            "\"padding\": 6," +
            "\"html\": \"\"," +
            "\"css\": \"\"," +
            "\"value\": \"\"," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"" + "/control/" + param.name + "\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"float\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"},");

        //variable min
        _writer.WriteLine("{" +
            "\"type\": \"variable\"," +
            "\"lock\": false," +
            "\"id\": \"min" + knob + "\"," +
            "\"comments\": \"\"," +
            "\"value\":" + param.minRange.ToString().Replace(",", ".") + "," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"auto\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"},");

        //variable max
        _writer.WriteLine("{" +
            "\"type\": \"variable\"," +
            "\"lock\": false," +
            "\"id\": \"max" + knob + "\"," +
            "\"comments\": \"\"," +
            "\"value\": " + param.maxRange.ToString().Replace(",", ".") + "," +
            "\"default\": \"\"," +
            "\"linkId\": \"\"," +
            "\"address\": \"auto\"," +
            "\"preArgs\": \"\"," +
            "\"typeTags\": \"\"," +
            "\"decimals\": 2," +
            "\"target\": \"\"," +
            "\"ignoreDefaults\": false," +
            "\"bypass\": false," +
            "\"onCreate\": \"\"," +
            "\"onValue\": \"\"}");

        _writer.WriteLine("],\"tabs\": []}");
    }
    public void oscMessageHandler(OscMessage message)
    {
        Debug.Log("osc message : " + message.address);
        for (int i = 0; i < parametres.floats.Count; i++)
        {
            if ("/control/" + parametres.floats[i].name == message.address || "/fader/" + parametres.floats[i].name == message.address)
            {
                switch (parametres.floats[i].compInfo.infoType)
                {
                    //float
                    case CompInfo.InfoType.Field:
                            parametres.floats[i].compInfo.fieldInfo.SetValue(parametres.floats[i].compInfo.comp, message.GetFloat(0));
                            parametres.floats[i].currentValue = message.GetFloat(0);
                        
                        break;

                    case CompInfo.InfoType.Property:
                            parametres.floats[i].compInfo.propInfo.SetValue(parametres.floats[i].compInfo.comp, message.GetFloat(0));
                            parametres.floats[i].currentValue = message.GetFloat(0);
                        break;
                }
            }
        }

        for (int i = 0; i < parametres.ints.Count; i++)
        {
            if ("/control/" + parametres.ints[i].name == message.address || "/fader/" + parametres.ints[i].name == message.address)
            {
                switch (parametres.ints[i].compInfo.infoType)
                {
                    //int
                    case CompInfo.InfoType.Field:
                        parametres.ints[i].compInfo.fieldInfo.SetValue(parametres.ints[i].compInfo.comp, message.GetInt(0));
                        parametres.ints[i].currentValue = message.GetInt(0);

                        break;

                    case CompInfo.InfoType.Property:
                        parametres.ints[i].compInfo.propInfo.SetValue(parametres.ints[i].compInfo.comp, message.GetInt(0));
                        parametres.ints[i].currentValue = message.GetInt(0);
                        break;
                }
            }


        }
        for (int i = 0; i < parametres.bools.Count; i++)
        {
            if ("/note/" + parametres.bools[i].name == message.address)
            {
                float val = message.GetInt(2);
                if (val > 0.01)
                {
                    parametres.bools[i].compInfo.fieldInfo.SetValue(parametres.bools[i].compInfo.comp, true);
                }
                else
                {
                    parametres.bools[i].compInfo.fieldInfo.SetValue(parametres.bools[i].compInfo.comp, false);
                }
            }
        }
    }

    bool parameterFloatAlreadyExists(string name, CompInfo _compInfo)
    {
        for(int i=0; i<parametres.floats.Count; i++)
        {
            if (parametres.floats[i].name == name)
            {
                parametres.floats[i].compInfo = _compInfo;
                return true;
            }
        }
        return false;
    }

    bool parameterBoolAlreadyExists(string name, CompInfo _compInfo)
    {
        for (int i = 0; i < parametres.bools.Count; i++)
        {
            if (parametres.bools[i].name == name)
            {
                parametres.bools[i].compInfo = _compInfo;
                return true;
            }
        }
        return false;
    }

    bool parameterIntAlreadyExists(string name, CompInfo _compInfo)
    {
        for (int i = 0; i < parametres.ints.Count; i++)
        {
            if (parametres.ints[i].name == name)
            {
                parametres.ints[i].compInfo = _compInfo;
                return true;
            }
        }
        return false;
    }


}
