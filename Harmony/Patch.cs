﻿using DMT;
using Harmony;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SdtdEsp
{
    public class Patch : MonoBehaviour
    {
        public const float distForCountingEnemies = 120f;
        public const float INF = 1234567890f;
        public const string defaultMsg = "ESP ENABLED";

        static GameObject go;
        static Shader wallhackShader;
        static Shader normalShader;
        static Entity player;
        static AIDirectorZombieManagementComponent aiDirector;
        public static ESP esp;
        static Sprite[] sprites;

        //static int zombieCnt = 0;
        //static double nearestDist = INF;

        static bool AreSpritesLoaded()
        {
            foreach (Sprite sprite in sprites)
            {
                if (sprite == null)
                    return false;
            }
            return true;
        }

        static bool IsNormalShader(String shaderName)
        {
            return shaderName.StartsWith("Standard") || shaderName == "Autodesk Interactive";
        }

        static void ReplaceShaders(GameObject gameObject, Shader shader)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (var material in renderer.materials)
                {
                    if (normalShader == null && IsNormalShader(material.shader.name))
                    {
                        normalShader = material.shader;
                    }
                    if (IsNormalShader(material.shader.name) || material.shader.name.Equals(wallhackShader.name))
                    {
                        material.shader = shader;
                        material.SetColor("_FirstOutlineColor", Color.red);
                        material.SetFloat("_FirstOutlineWidth", 0.015f);
                    }
                }
            }
            foreach (Transform transform in gameObject.transform)
            {
                ReplaceShaders(transform.gameObject, shader);
            }
        }

        static bool NameContains(String str, String key)
        {
            // TODO returns true only if the string contains a exact word
            return str.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        static EnemyIcon GetIconNum(EntityEnemy enemy)
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

        static bool IsRunning(EntityEnemy enemy)
        {
            EnemyIcon iconNum;
            if (enemy.IsRunning)
                return true;
            iconNum = GetIconNum(enemy);
            if (iconNum == EnemyIcon.Dog || iconNum == EnemyIcon.Bear)
                return true;
            return false;
        }

        static bool IsFlying(EntityEnemy enemy)
        {
            return (enemy is EntityFlying);
        }

        void Start()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string modPath = Path.Combine(assemblyPath, @"..\..");
            string resPath = Path.Combine(modPath, "Resources");
            string bundlePath = Path.Combine(resPath, "esp.unity3d");
            Debug.Log("bundlePath: " + bundlePath);

            var bundle = AssetBundle.LoadFromFile(bundlePath);
            wallhackShader = bundle.LoadAsset<Shader>("WallhackShader.shader");
            sprites = new Sprite[(int)EnemyIcon.ItemCount];
            sprites[(int)EnemyIcon.Zombie] = bundle.LoadAsset<Sprite>("Zombie.png");
            sprites[(int)EnemyIcon.Flying] = bundle.LoadAsset<Sprite>("Flying.png");
            sprites[(int)EnemyIcon.Dog] = bundle.LoadAsset<Sprite>("Dog.png");
            sprites[(int)EnemyIcon.Bear] = bundle.LoadAsset<Sprite>("Bear.png");
            sprites[(int)EnemyIcon.Animal] = bundle.LoadAsset<Sprite>("Animal.png");
            sprites[(int)EnemyIcon.Arrow] = bundle.LoadAsset<Sprite>("Arrow.png");

            esp = new ESP();
            esp.Init();
            esp.UpdateText(defaultMsg, Color.white);
        }

        public class Patch_Init : IHarmony
        {
            public void Start()
            {
                go = new GameObject();
                go.AddComponent<Patch>();
                DontDestroyOnLoad(go);

                Debug.Log(" Loading Patch: " + GetType().ToString());
                var harmony = HarmonyInstance.Create(GetType().ToString());
                harmony.PatchAll(Assembly.GetExecutingAssembly());
            }
        }

        [HarmonyPatch(typeof(AIDirectorZombieManagementComponent))]
        [HarmonyPatch("Tick")]
        [HarmonyPatch(new Type[] { typeof(double) })]
        public class PatchUpdateEnemies
        {
            static void Postfix(AIDirectorZombieManagementComponent __instance, double _dt)
            {
                aiDirector = __instance;
                var zombies = __instance.trackedZombies;
                int zombieCnt = 0;
                float nearestDist = INF;
                int attackingEnemeies = 0;
                int investigatingEnemies = 0;
                bool hasRunner = false;
                bool hasFlyer = false;
                string uiStr = "NO ENEMIES";
                Color OrangeColor = new Color(1.0f, 0.647f, 0f);
                Color uiStrColor = Color.green;

                // Reset indicator targets before every updates
                if (esp != null)
                    esp.targets.Clear();

                for (int i = 0; i < zombies.Count; i++)
                {
                    AIDirectorZombieState zs = zombies.list[i];
                    float dist = INF;
                    bool isSleeping;
                    bool isAttacking = false;
                    bool isInvestigating = false;
                    Color targetColor;
                    EnemyIcon iconNum;

                    if (!zs.Zombie.IsAlive())
                        continue;
                    if (player != null)
                    {
                        dist = zs.Zombie.GetDistance(player);
                    }

                    if (dist > distForCountingEnemies)
                        continue;
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                    }

                    // Set enemy state
                    if (zs.Zombie.GetAttackTarget() == player)
                        isAttacking = true;
                    if (zs.Zombie.HasInvestigatePosition)
                        isInvestigating = true;
                    isSleeping = zs.Zombie.IsSleeping;

                    // Set target color
                    if (isAttacking)
                        targetColor = Color.red;
                    else if (isInvestigating)
                        targetColor = Color.yellow;
                    else if (isSleeping)
                        targetColor = Color.gray;
                    else
                        targetColor = Color.white;

                    if (isAttacking)
                        attackingEnemeies++;
                    if (isInvestigating)
                        investigatingEnemies++;

                    // Is running or flying?
                    if (isAttacking || isInvestigating)
                    {
                        if (IsFlying(zs.Zombie))
                            hasFlyer = true;
                        if (IsRunning(zs.Zombie))
                            hasRunner = true;
                    }

                    // Add target to indicator
                    int zId = zs.Zombie.GetInstanceID();
                    if (esp != null && !esp.targets.ContainsKey(zId))
                    {

                        iconNum = GetIconNum(zs.Zombie);
                        esp.targets.Add(
                            zId,
                            new EnemyInfo(
                                zs.Zombie.gameObject,
                                targetColor,
                                sprites[(int)iconNum],
                                iconNum == EnemyIcon.Arrow));
                    }

                    zombieCnt++;
                }

                // Update indicator
                if(esp != null)
                    esp.DoUpdate(Camera.main);
                if (zombieCnt > 0 && nearestDist < INF)
                {
                    uiStr = zombieCnt + " ENEMIES" + "  (" + nearestDist.ToString("0.00") + "M)";
                    uiStr += "\n";

                    if (attackingEnemeies > 0)
                    {
                        uiStr += attackingEnemeies + " ATTACKING";
                        uiStrColor = Color.red;
                    }
                    else if (investigatingEnemies > 0)
                    {
                        uiStr += investigatingEnemies + " INVESTIGATING";
                        uiStrColor = Color.yellow;
                    }
                    else
                    {
                        uiStr += "IDLE";
                        uiStrColor = Color.white;
                    }

                    if (hasRunner && hasFlyer)
                    {
                        uiStr += "\nRUN / FLY";
                    }
                    else if (hasFlyer)
                    {
                        uiStr += "\nFLY";
                    }
                    else if (hasRunner)
                    {
                        uiStr += "\nRUN";
                    }
                }
                else if (player == null)
                {
                    uiStr = "No player found. Exit to main menu and reload the game.";
                    uiStrColor = Color.white;
                }
                if (esp != null)
                    esp.UpdateText(uiStr, uiStrColor);
            }
        }

        [HarmonyPatch(typeof(AIDirectorZombieManagementComponent))]
        [HarmonyPatch("AddZombie")]
        [HarmonyPatch(new Type[] { typeof(EntityEnemy) })]
        public class PatchReplaceEnemyShader
        {
            static void Postfix(EntityEnemy _zombie)
            {
                if (wallhackShader != null)
                {
                    ReplaceShaders(_zombie.gameObject, wallhackShader);
                }
            }
        }

        [HarmonyPatch(typeof(World))]
        [HarmonyPatch("SetLocalPlayer")]
        [HarmonyPatch(new Type[] { typeof(EntityPlayerLocal) })]
        public class PatchGetPlayer
        {
            static void Postfix(World __instance)
            {
                player = __instance.GetPrimaryPlayer();
            }
        }

        [HarmonyPatch(typeof(EntityAlive))]
        [HarmonyPatch("OnEntityDeath")]
        [HarmonyPatch(new Type[] { })]
        public class PatchRecoverEnemyShader
        {
            static void Postfix(EntityAlive __instance)
            {
                int entityId = __instance.entityId;
                var zombies = (aiDirector != null) ? aiDirector.trackedZombies : null;
                if (zombies != null)
                {
                    if (zombies.dict.ContainsKey(entityId))
                    {
                        if (normalShader != null)
                            ReplaceShaders(__instance.gameObject, normalShader);
                    }
                }
            }
        }

        [HarmonyPatch(typeof(World))]
        [HarmonyPatch("Cleanup")]
        [HarmonyPatch(new Type[] { })]
        public class PatchCleanUp
        {
            static void Postfix()
            {
                aiDirector = null;
                if (esp != null)
                    esp.DoUpdate(null); // Clear targets
                player = null;
                if (esp != null)
                    esp.UpdateText(defaultMsg, Color.white);
            }
        }
    }
}
