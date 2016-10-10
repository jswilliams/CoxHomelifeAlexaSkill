using System;

namespace CoxHomelifeAlexaSkill.Domain
{
    public sealed class ArmType
    {
        private readonly String name;
        private readonly int value;

        public static readonly ArmType NIGHT = new ArmType(1, "night");
        public static readonly ArmType STAY = new ArmType(2, "stay");
        public static readonly ArmType AWAY = new ArmType(3, "away");

        private ArmType(int value, String name)
        {
            this.name = name;
            this.value = value;
        }

        public override String ToString()
        {
            return name;
        }
    }
}
