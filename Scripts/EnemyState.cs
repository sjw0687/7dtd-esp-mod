using System;

namespace SdtdEsp
{
    public class EnemyState
    {
        public static EnemyIcon GetIconNum(EntityEnemy enemy)
        {
            if (enemy is EntityFlying)
                return EnemyIcon.Flying;
            if (enemy is EntityZombieDog || NameContains(enemy.EntityName, "wolf"))
                return EnemyIcon.Dog;
            if (NameContains(enemy.EntityName, "bear"))
                return EnemyIcon.Bear;
            if (enemy is EntityEnemyAnimal || NameContains(enemy.EntityName, "snake"))
                return EnemyIcon.Animal;
            return EnemyIcon.Zombie;
        }

        public static bool IsRunning(EntityEnemy enemy)
        {
            EnemyIcon iconNum;
            if (enemy.IsRunning)
                return true;
            iconNum = EnemyState.GetIconNum(enemy);
            if (iconNum == EnemyIcon.Dog || iconNum == EnemyIcon.Bear)
                return true;
            return false;
        }

        public static bool IsFlying(EntityEnemy enemy)
        {
            return (enemy is EntityFlying);
        }

        static bool NameContains(String str, String key)
        {
            // TODO returns true only if the string contains a exact word
            return str.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
        }
    }
}
