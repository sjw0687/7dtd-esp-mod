using UnityEngine;

namespace SdtdEsp
{
    public struct EnemyInfo
    {
        public GameObject gameObject;
        public Color color;
        public Sprite icon;

        public EnemyInfo(GameObject _gameObject, Color _color, Sprite _icon)
        {
            gameObject = _gameObject;
            color = _color;
            icon = _icon;
        }
    }
}
