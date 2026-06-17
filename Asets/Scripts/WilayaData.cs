using System.Collections.Generic;

public static class WilayaData
{
    public struct Wilaya
    {
        public int    Code;
        public string Name;
        public float  Lat;
        public float  Lon;
    }

    public static readonly Wilaya[] Wilayas =
    {
        
        new Wilaya { Code=1,  Name="Adrar",              Lat=27.87f, Lon=-0.29f },
        new Wilaya { Code=2,  Name="Chlef",              Lat=36.17f, Lon=1.33f  },
        new Wilaya { Code=3,  Name="Laghouat",           Lat=33.80f, Lon=2.87f  },
        new Wilaya { Code=4,  Name="Oum El Bouaghi",     Lat=35.87f, Lon=7.11f  },
        new Wilaya { Code=5,  Name="Batna",              Lat=35.55f, Lon=6.17f  },
        new Wilaya { Code=6,  Name="Bejaia",             Lat=36.75f, Lon=5.08f  },
        new Wilaya { Code=7,  Name="Biskra",             Lat=34.85f, Lon=5.73f  },
        new Wilaya { Code=8,  Name="Bechar",             Lat=31.32f, Lon=-2.22f },
        new Wilaya { Code=9,  Name="Blida",              Lat=36.47f, Lon=2.82f  },
        new Wilaya { Code=10, Name="Bouira",             Lat=36.37f, Lon=3.90f  },
        new Wilaya { Code=11, Name="Tamanrasset",        Lat=22.79f, Lon=5.52f  },
        new Wilaya { Code=12, Name="Tebessa",            Lat=35.40f, Lon=8.12f  },
        new Wilaya { Code=13, Name="Tlemcen",            Lat=34.87f, Lon=-1.32f },
        new Wilaya { Code=14, Name="Tiaret",             Lat=35.37f, Lon=1.32f  },
        new Wilaya { Code=15, Name="Tizi Ouzou",         Lat=36.72f, Lon=4.05f  },
        new Wilaya { Code=16, Name="Alger",              Lat=36.74f, Lon=3.06f  },
        new Wilaya { Code=17, Name="Djelfa",             Lat=34.67f, Lon=3.26f  },
        new Wilaya { Code=18, Name="Jijel",              Lat=36.82f, Lon=5.77f  },
        new Wilaya { Code=19, Name="Setif",              Lat=36.19f, Lon=5.41f  },
        new Wilaya { Code=20, Name="Saida",              Lat=34.83f, Lon=0.15f  },
        new Wilaya { Code=21, Name="Skikda",             Lat=36.88f, Lon=6.90f  },
        new Wilaya { Code=22, Name="Sidi Bel Abbes",     Lat=35.19f, Lon=-0.63f },
        new Wilaya { Code=23, Name="Annaba",             Lat=36.90f, Lon=7.76f  },
        new Wilaya { Code=24, Name="Guelma",             Lat=36.46f, Lon=7.43f  },
        new Wilaya { Code=25, Name="Constantine",        Lat=36.36f, Lon=6.61f  },
        new Wilaya { Code=26, Name="Medea",              Lat=36.27f, Lon=2.75f  },
        new Wilaya { Code=27, Name="Mostaganem",         Lat=35.94f, Lon=0.09f  },
        new Wilaya { Code=28, Name="M'Sila",             Lat=35.70f, Lon=4.54f  },
        new Wilaya { Code=29, Name="Mascara",            Lat=35.39f, Lon=0.14f  },
        new Wilaya { Code=30, Name="Ouargla",            Lat=31.95f, Lon=5.32f  },
        new Wilaya { Code=31, Name="Oran",               Lat=35.69f, Lon=-0.63f },
        new Wilaya { Code=32, Name="El Bayadh",          Lat=33.68f, Lon=1.02f  },
        new Wilaya { Code=33, Name="Illizi",             Lat=26.48f, Lon=8.47f  },
        new Wilaya { Code=34, Name="BBA",                Lat=36.07f, Lon=4.77f  },
        new Wilaya { Code=35, Name="Boumerdes",          Lat=36.76f, Lon=3.48f  },
        new Wilaya { Code=36, Name="El Tarf",            Lat=36.77f, Lon=8.31f  },
        new Wilaya { Code=37, Name="Tindouf",            Lat=27.67f, Lon=-7.80f },
        new Wilaya { Code=38, Name="Tissemsilt",         Lat=35.60f, Lon=1.81f  },
        new Wilaya { Code=39, Name="El Oued",            Lat=33.36f, Lon=6.86f  },
        new Wilaya { Code=40, Name="Khenchela",          Lat=35.43f, Lon=7.14f  },
        new Wilaya { Code=41, Name="Souk Ahras",         Lat=36.28f, Lon=7.95f  },
        new Wilaya { Code=42, Name="Tipaza",             Lat=36.59f, Lon=2.44f  },
        new Wilaya { Code=43, Name="Mila",               Lat=36.45f, Lon=6.27f  },
        new Wilaya { Code=44, Name="Ain Defla",          Lat=36.26f, Lon=1.97f  },
        new Wilaya { Code=45, Name="Naama",              Lat=33.27f, Lon=-0.31f },
        new Wilaya { Code=46, Name="Ain Temouchent",     Lat=35.30f, Lon=-1.14f },
        new Wilaya { Code=47, Name="Ghardaia",           Lat=32.49f, Lon=3.67f  },
        new Wilaya { Code=48, Name="Relizane",           Lat=35.74f, Lon=0.56f  },

        new Wilaya { Code=49, Name="In Salah",           Lat=27.20f, Lon=2.47f  },
        new Wilaya { Code=50, Name="In Guezzam",         Lat=19.57f, Lon=5.77f  },
        new Wilaya { Code=51, Name="Touggourt",          Lat=33.10f, Lon=6.07f  },
        new Wilaya { Code=52, Name="Djanet",             Lat=23.00f, Lon=9.48f  },
        new Wilaya { Code=53, Name="El M'Ghair",         Lat=33.95f, Lon=5.93f  },
        new Wilaya { Code=54, Name="El Meniaa",          Lat=30.58f, Lon=2.88f  },
        new Wilaya { Code=55, Name="Ouled Djellal",      Lat=34.42f, Lon=5.07f  },
        new Wilaya { Code=56, Name="Bordj Baji Mokhtar", Lat=21.33f, Lon=0.95f  },
        new Wilaya { Code=57, Name="Beni Abbes",         Lat=30.00f, Lon=-2.17f },
        new Wilaya { Code=58, Name="Timimoun",           Lat=29.26f, Lon=0.24f  },
    };

    public static readonly int[,] Roads =
    {
        {16,9,50},{16,35,30},{16,42,60},{16,26,90},{9,26,70},{9,2,130},{9,44,80},
        {35,15,85},{35,10,60},{15,6,175},{15,10,65},{10,28,130},{10,34,95},
        {6,18,130},{6,19,170},{18,21,90},{21,25,130},{21,23,75},
        {25,43,50},{25,19,100},{25,4,115},{23,36,60},{23,24,60},{23,41,95},
        {24,41,75},{24,12,115},{24,4,90},{4,40,80},{4,12,120},{4,5,90},
        {5,12,110},{5,7,130},{5,19,135},{12,41,90},{40,7,95},
        {31,22,100},{31,46,60},{31,27,80},{22,46,75},{22,13,115},{22,29,90},
        {13,46,100},{13,45,140},{46,27,90},{27,2,90},{27,48,60},
        {2,48,85},{2,44,90},{2,38,95},{29,48,80},{29,20,75},{29,38,90},
        {20,32,140},{20,14,85},{20,38,65},{14,38,70},{14,17,110},{14,3,130},
        {38,44,80},{44,26,55},{45,8,280},{45,32,100},{45,20,170},{45,13,130},
        {32,3,200},{32,47,200},{32,1,250},{32,8,260},{8,37,800},{8,1,350},
        {26,17,110},{26,3,180},{17,3,140},{17,47,200},{17,7,230},{17,28,130},
        {3,47,180},{3,7,270},{28,34,80},{28,19,110},{28,7,170},
        {7,30,300},{7,39,260},{30,33,500},{30,47,300},{30,39,180},{30,11,900},
        {39,7,200},{39,40,120},{47,1,800},{47,11,1300},{1,8,600},{1,37,1200},
        {11,33,1200},{11,30,900},{33,30,500},

        {49,11,700},{49,1,500},{49,47,600},{49,54,350},
        {50,11,700},{50,49,700},
        {51,30,180},{51,39,160},{51,53,100},{51,7,220},
        {52,33,500},{52,11,900},
        {53,39,80},{53,51,100},{53,7,180},
        {54,47,400},{54,1,650},{54,49,350},
        {55,7,120},{55,28,150},
        {56,1,900},{56,58,700},
        {57,8,280},{57,1,500},{57,58,300},
        {58,1,250},{58,57,300},{58,56,700},
    };

    public static Dictionary<int, List<(int neighbor, float dist)>> BuildAdjacency()
    {
        var adj = new Dictionary<int, List<(int, float)>>();
        foreach (var w in Wilayas)
            adj[w.Code] = new List<(int, float)>();

        for (int i = 0; i < Roads.GetLength(0); i++)
        {
            int a = Roads[i, 0], b = Roads[i, 1];
            float d = Roads[i, 2];
            if (adj.ContainsKey(a) && adj.ContainsKey(b))
            {
                if (!adj[a].Exists(x => x.Item1 == b)) adj[a].Add((b, d));
                if (!adj[b].Exists(x => x.Item1 == a)) adj[b].Add((a, d));
            }
        }
        return adj;
    }
}
