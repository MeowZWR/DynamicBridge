﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Configuration
{
    [Serializable]
    public class Preset
    {
        [NonSerialized] internal string GUID = Guid.NewGuid().ToString();
        public string Name = "";
        internal string CensoredName => C.NoNames ? GUID : Name;
        public List<string> Glamourer = [];
        public List<string> ComplexGlamourer = [];
        public List<string> Honorific = [];
        public List<string> Customize = [];
        public List<string> Penumbra = [];
        public List<MoodleInfo> Moodles = [];
        public SpecialPenumbraAssignment PenumbraType = SpecialPenumbraAssignment.使用独立分配;
        public bool IsStatic = false;
    }
}
