using System;
using UnityEngine;

namespace Object.Data
{
    [Serializable]
    public class BaseData
    {
        [Header("Base")] 
        public string name;
        public int ID;
        public string desc;

        public PokeType TYPE;

        public enum PokeType
        {
            ACIER,
            COMBAT,
            DRAGON,
            EAU,
            ELECTRIK,
            FEE,
            FEU,
            GLACE,
            INSECTE,
            NORMALE,
            PLANTE,
            POISON,
            PSY,
            ROCHE,
            SOL,
            SPECTRE,
            TENEBRES,
            VOL,
            OBJECT
        }

        public BaseData(string name, int id, string caption, PokeType pokeType)
        {
            this.name = name;
            ID = id;
            desc = caption;
            TYPE = pokeType;
        }
    }
}
