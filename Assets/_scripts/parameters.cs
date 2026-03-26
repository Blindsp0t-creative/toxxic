using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    [CreateAssetMenu(fileName = "parameters", menuName = "ScriptableObjects/customParametres", order = 1)]
    public class parameters : ScriptableObject
    {
        [SerializeField]
        public List<osc_parameter_float> floats;
        [SerializeField]
        public List<osc_parameter_int> ints;
        [SerializeField]
        public List<osc_parameter_bool> bools;
        [SerializeField]
        public List<osc_parameter_string> strings;
    }

