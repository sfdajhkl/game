namespace Arvito.Core
{
    /// <summary>Cosmetic sprite choices; independent of gameplay rolls and statistics.</summary>
    public static class Appearance
    {
        public static bool HasFemaleVariant(string id) => id=="hero" || id=="elian" || id=="borin" || id=="bandit" || id=="archer" || id=="cultist";
        public static string Sprite(string id, bool female) => female && HasFemaleVariant(id) ? id+"_female" : id;
        public static string Sprite(Actor actor) => Sprite(actor.id,actor.femaleAppearance);
        public static string Sprite(Enemy enemy) => Sprite(enemy.type,enemy.femaleAppearance);
        public static bool EnemyFemale(string type, uint seed, int index)
        {
            if (!HasFemaleVariant(type)) return false;
            // Separate cosmetic hash: never advance GameState.rng for an illustration.
            unchecked
            {
                uint value=seed+(uint)(index+1)*0x9e3779b9u;
                value^=value>>16;value*=0x85ebca6bu;value^=value>>13;
                return (value&1)!=0;
            }
        }
    }
}
