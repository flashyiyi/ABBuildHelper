using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public class ABPackRuleConfig : ScriptableObject
{
    [Serializable]
    public class Rule
    {
        public string path;
        public string typeFilter;
        public int ruleType;

        public bool MatchType(string type)
        {
            if (type == "MonoScript" || type == "DefaultAsset")
                return false;

            return string.IsNullOrEmpty(typeFilter) ? true : Array.IndexOf(typeFilter.Split(','), type) >= 0;
        }
    }
    public List<Rule> rules = new List<Rule>();
}