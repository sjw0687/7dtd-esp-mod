using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace SdtdEsp
{
    public class Asset
    {
        Shader normalShader;
        Shader wallhackShader;
        Sprite[] sprites;

        public Asset(string bundlePath)
        {
            var bundle = AssetBundle.LoadFromFile(bundlePath);
            wallhackShader = bundle.LoadAsset<Shader>("WallhackShader.shader");
            sprites = new Sprite[(int)EnemyIcon.ItemCount];
            sprites[(int)EnemyIcon.Zombie] = bundle.LoadAsset<Sprite>("Zombie.png");
            sprites[(int)EnemyIcon.Flying] = bundle.LoadAsset<Sprite>("Flying.png");
            sprites[(int)EnemyIcon.Dog] = bundle.LoadAsset<Sprite>("Dog.png");
            sprites[(int)EnemyIcon.Bear] = bundle.LoadAsset<Sprite>("Bear.png");
            sprites[(int)EnemyIcon.Animal] = bundle.LoadAsset<Sprite>("Animal.png");
            sprites[(int)EnemyIcon.Arrow] = bundle.LoadAsset<Sprite>("Arrow.png");
        }

        public void ApplyNormalShader(GameObject go)
        {
            if (normalShader != null)
                ReplaceShaders(go, normalShader);
        }

        public void ApplyWallhackShader(GameObject go)
        {
            if (wallhackShader != null)
                ReplaceShaders(go, wallhackShader);
        }

        public bool AreSpritesLoaded()
        {
            foreach (Sprite sprite in sprites)
            {
                if (sprite == null)
                    return false;
            }
            return true;
        }

        public void GetNormalShader(GameObject gameObject)
        {
            if (normalShader != null)
                return;

            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (var material in renderer.materials)
                {
                    if (IsNormalShader(material.shader.name))
                    {
                        normalShader = material.shader;
                        return;
                    }
                }
            }
            foreach (Transform transform in gameObject.transform)
            {
                GetNormalShader(transform.gameObject);
            }
        }

        public Sprite GetSpirit(EnemyIcon iconNum)
        {
            return sprites[(int)iconNum];
        }

        bool IsNormalShader(string shaderName)
        {
            return shaderName.StartsWith("Standard") || shaderName == "Autodesk Interactive";
        }

        void ReplaceShaders(GameObject gameObject, Shader shader)
        {
            var renderer = gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                foreach (var material in renderer.materials)
                {
                    if (IsNormalShader(material.shader.name) || material.shader.name == wallhackShader.name)
                    {
                        material.shader = shader;
                        if (shader.name == wallhackShader.name)
                        {
                            material.SetColor("_FirstOutlineColor", Color.red);
                            material.SetFloat("_FirstOutlineWidth", 0.015f);
                        }
                    }
                }
            }
            foreach (Transform transform in gameObject.transform)
            {
                ReplaceShaders(transform.gameObject, shader);
            }
        }
    }
}
