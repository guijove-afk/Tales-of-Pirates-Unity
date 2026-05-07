using System;

namespace TOP.Core
{
    [Serializable]
    public class CharacterDbModel
    {
        public ulong id;
        public ulong account_id;
        public string name;
        public int job;
        public int level;
        public uint exp;
        
        // Status Base
        public int base_str;
        public int base_agi;
        public int base_con;
        public int base_spr;
        public int base_sta;

        // HP/MP/SP
        public int max_hp;
        public int max_mp;
        public int max_sp;
        public int current_hp;
        public int current_mp;
        public int current_sp;

        // Posição
        public string map_name;
        public float pos_x;
        public float pos_y;
        public float pos_z;
    }
}