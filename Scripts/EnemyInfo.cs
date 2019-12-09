using UnityEngine;

namespace SdtdEsp
{
    public struct EnemyInfo
    {
        public GameObject gameObject;
        public Color color;
        public Sprite icon;
        public bool applyRotation;

        public EnemyInfo(GameObject _gameObject, Color _color, Sprite _icon, bool _applyRotation)
        {
            gameObject = _gameObject;
            color = _color;
            icon = _icon;
            applyRotation = _applyRotation;
        }
    }
}
