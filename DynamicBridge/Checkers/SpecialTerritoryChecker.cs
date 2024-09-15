﻿using ECommons.ExcelServices.TerritoryEnumeration;
using ECommons.Throttlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DynamicBridge.Core
{
    public static class SpecialTerritoryChecker
    {
        static readonly uint[] Houses = [282, 283, 284, 342, 343, 344, 345, 346, 347, 384, 385, 386, 649, 650, 651, 652, 980, 981, 982, 983, 423, 424, 425, 653, 984];
        static readonly uint[] Apartments = [573, 574, 575, 608, 609, 610, 654, 655, 985, 999];

        static readonly uint[] Ocean = [138, 263, 280, 330, 405, 406, 413, 453, 552, 675, 621, 684, 686, 687, 641, 614, 634, 685, 613, 657, 711, 726, 757, 820, 863, 814, 864, 870, 818, 932, 957, 915, 1055, 732];
        static readonly uint[] Lake = [139, 221, 328, 412, 454, 466, 180, 325, 486, 154, 228, 240, 321, 324, 140, 215, 255, 267, 269, 273, 278, 1049, 147, 410, 483, 156, 299, 300, 305, 308, 309, 315, 326, 335, 379, 480, 672, 401, 455, 461, 492, 478, 399, 476, 485, 503, 400, 501, 715, 682, 739, 759, 813, 861, 862, 877, 816, 869, 817, 871, 874, 875, 959, 961, 1014, 1061, 827, 920, 975];
        static readonly uint[] River = [148, 190, 219, 225, 226, 227, 230, 233, 237, 239, 319, 320, 1015, 152, 191, 234, 277, 289, 290, 303, 839, 153, 192, 220, 229, 231, 232, 235, 236, 291, 317, 394, 473, 141, 216, 248, 253, 258, 270, 271, 314, 145, 256, 257, 266, 268, 275, 465, 494, 668, 146, 260, 261, 306, 312, 318, 323, 491, 669, 402, 459, 398, 464, 481, 482, 1023, 635, 659, 612, 640, 647, 648, 670, 671, 678, 703, 760, 620, 716, 868, 1019, 622, 688, 713, 718, 723, 797, 819, 815, 860, 872, 962, 963, 399, 476, 485, 503, 512, 514, 515, 624, 625, 656, 901, 929, 939];
        static readonly uint[] Frozen = [397, 467, 470, 472, 477, 479, 489, 493, 497, 498, 709, 866, 155, 223, 298, 301, 302, 304, 313, 316, 322, 468, 469, 475, 487, 488, 496, 500, 533, 699, 958, 1011, 1120, 960, 1027, 763, 795, 419, 499];
        static readonly uint[] Hotsprings = [628, 664, 665, 667, 710, 886, 979];

        static readonly Dictionary<SpecialTerritory, Func<bool>> States = new()
        {
            [SpecialTerritory.House] = () => Houses.Contains(Svc.ClientState.TerritoryType),
            [SpecialTerritory.Inn] = () => Inns.List.Contains(Svc.ClientState.TerritoryType),
            [SpecialTerritory.Apartment] = () => Apartments.Contains(Svc.ClientState.TerritoryType),
            [SpecialTerritory.Residential_area] = () => ResidentalAreas.List.Contains(Svc.ClientState.TerritoryType),
            [SpecialTerritory.Duty] = () => Svc.Condition[ConditionFlag.BoundByDuty56],
            [SpecialTerritory.Aquatic_Ocean] = () => Ocean.Contains(Svc.ClientState.TerritoryType),
            [SpecialTerritory.Aquatic_Lake] = () => Lake.Contains(Svc.ClientState.TerritoryType),
            [SpecialTerritory.Aquatic_River] = () => River.Contains(Svc.ClientState.TerritoryType),
            [SpecialTerritory.Aquatic_Frozen] = () => Frozen.Contains(Svc.ClientState.TerritoryType),
            [SpecialTerritory.Aquatic_Hot_Springs] = () => Hotsprings.Contains(Svc.ClientState.TerritoryType),
        };

        public static readonly Dictionary<SpecialTerritory, string> Renames = new()
        {
            [SpecialTerritory.Inn] = "旅店",
            [SpecialTerritory.House] = "住宅",
            [SpecialTerritory.Apartment] = "公寓",
            [SpecialTerritory.Residential_area] = "住宅区",
            [SpecialTerritory.Duty] = "任务",
            [SpecialTerritory.Aquatic_Ocean] = "水域：海洋",
            [SpecialTerritory.Aquatic_Lake] = "水域：湖泊",
            [SpecialTerritory.Aquatic_River] = "水域：河流",
            [SpecialTerritory.Aquatic_Frozen] = "水域：冰川",
            [SpecialTerritory.Aquatic_Hot_Springs] = "水域：温泉",
        };

        public static bool Check(this SpecialTerritory terr)
        {
            if (States.TryGetValue(terr, out var func))
            {
                return func();
            }
            if (EzThrottler.Throttle("ErrorReport", 10000)) DuoLog.Error($"Cound not find checker for SpecialTerritory {terr}. Please report this error with logs.");
            return false;
        }
    }
}
