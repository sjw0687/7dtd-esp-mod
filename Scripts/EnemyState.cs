using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SdtdEsp
{
    public class EnemyState
    {
        public const float INF_DIST = 1234567890f;

        Asset asset;
        Entity player;
        List<EntityEnemy> zombies;

        public EnemyState(
            AIDirectorZombieManagementComponent aiComponent,
            Asset asset,
            Entity player)
        {
            this.asset = asset;
            this.player = player;
            var qry = from tz in aiComponent.trackedZombies.list
                      where tz.Zombie.IsAlive()
                      select tz.Zombie;
            zombies = qry.ToList();
        }

        EnemyIcon GetIconNum(EntityEnemy enemy)
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

        bool IsAttacking(EntityEnemy zombie)
        {
            return zombie.GetAttackTarget() == player;
        }

        bool IsFlying(EntityEnemy enemy)
        {
            return (enemy is EntityFlying);
        }

        bool IsInvestigating(EntityEnemy zombie)
        {
            return zombie.HasInvestigatePosition;
        }

        bool IsRunning(EntityEnemy enemy)
        {
            EnemyIcon iconNum;
            if (enemy.IsRunning)
                return true;
            iconNum = GetIconNum(enemy);
            if (iconNum == EnemyIcon.Dog || iconNum == EnemyIcon.Bear)
                return true;
            return false;
        }

        bool IsSleeping(EntityEnemy zombie)
        {
            return zombie.IsSleeping;
        }

        bool NameContains(string str, string key)
        {
            // TODO returns true only if the string contains a exact word
            return str.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public int Count()
        {
            return zombies.Count;
        }

        public int CountAttacking()
        {
            var qry = from z in zombies
                      where IsAttacking(z)
                      select z;
            return qry.Count();
        }

        public int CountInvestigating()
        {
            var qry = from z in zombies
                      where IsInvestigating(z)
                      select z;
            return qry.Count();
        }

        public List<EnemyInfo> GetEnemyInfos()
        {
            var enemyInfos = new List<EnemyInfo>();
            foreach (var zombie in zombies)
            {
                Color color = Color.white;
                if (IsAttacking(zombie))
                    color = Color.red;
                else if (IsInvestigating(zombie))
                    color = Color.yellow;
                else if (IsSleeping(zombie))
                    color = Color.gray;

                EnemyIcon iconNum = GetIconNum(zombie);

                enemyInfos.Add(
                    new EnemyInfo(
                        zombie.gameObject,
                        color,
                        asset.GetSpirit(iconNum)));
            }
            return enemyInfos;
        }

        public bool HasFlyer()
        {
            foreach (var zombie in zombies)
            {
                bool isDanger = IsInvestigating(zombie) || IsAttacking(zombie);
                if (isDanger && IsFlying(zombie))
                    return true;
            }
            return false;
        }

        public bool HasRunner()
        {
            foreach (var zombie in zombies)
            {
                bool isDanger = IsInvestigating(zombie) || IsAttacking(zombie);
                if (isDanger && IsRunning(zombie))
                    return true;
            }
            return false;
        }

        public float NearestDist()
        {
            float minDist = INF_DIST;
            if (player != null)
            {
                foreach (var zombie in zombies)
                {
                    float dist = zombie.GetDistance(player);
                    if (dist < minDist)
                        minDist = dist;
                }
            }
            return minDist;
        }
    }
}
