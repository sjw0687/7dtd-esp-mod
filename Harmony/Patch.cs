using DMT;
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
        public const double timeIntervalUpdate = 0.5f;
        public const string defaultMsg = "ESP ENABLED";

        static GameObject go;
        static Entity player;
        static AIDirectorZombieManagementComponent aiDirector;
        public static ESP esp;
        public static Asset asset;

        static double timeAfterTick = 0f;

        static string GetAssetBundlePath()
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string modPath = Path.Combine(assemblyPath, @"..\..");
            string resPath = Path.Combine(modPath, "Resources");
            return Path.Combine(resPath, "esp.unity3d");
        }

        void Start()
        {
            asset = new Asset(GetAssetBundlePath());

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

                Debug.Log("Loading Patch: " + GetType().ToString());
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
                timeAfterTick += _dt;
                if (timeAfterTick < timeIntervalUpdate)
                    return;
                else
                    timeAfterTick -= timeIntervalUpdate;

                if (esp == null)
                    return;

                var enemyState = new EnemyState(__instance, asset, player);
                int zombieCnt = enemyState.Count();
                int attackingEnemeies = enemyState.CountAttacking();
                int investigatingEnemies = enemyState.CountInvestigating();
                bool hasRunner = enemyState.HasRunner();
                bool hasFlyer = enemyState.HasFlyer();
                float nearestDist = enemyState.NearestDist();
                string uiStr = "NO ENEMIES";
                Color OrangeColor = new Color(1.0f, 0.647f, 0f);
                Color uiStrColor = Color.green;

                // Reset indicator targets before every updates
                esp.targets.Clear();

                // Add targets to indicator
                var enemyInfos = enemyState.GetEnemyInfos();
                foreach (var info in enemyInfos)
                {
                    int id = info.gameObject.GetInstanceID();
                    if (!esp.targets.ContainsKey(id))
                        esp.targets.Add(id, info);
                }

                // Update indicator
                esp.DoUpdate(Camera.main);
                if (zombieCnt > 0 && nearestDist < EnemyState.INF_DIST)
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
                var zombieGameObj = _zombie.gameObject;
                asset.GetNormalShader(zombieGameObj);
                asset.ApplyWallhackShader(zombieGameObj);
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
                asset.ApplyNormalShader(__instance.gameObject);
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
