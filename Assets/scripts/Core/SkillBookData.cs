using UnityEngine;

namespace TOP.Core
{

    [CreateAssetMenu(fileName = "NewSkillBook", menuName = "TOP/Skill Book")]
    public class SkillBookData : ItemData
    {
        [Header("Skill")]
        public SkillData skillData;
        public bool consumedOnUse = true;
    }
}
