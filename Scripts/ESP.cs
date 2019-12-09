using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SdtdEsp
{
    public class ESP
    {
        public const float targetColorAlpha = 0.65f;
        static float targetEdgeDistance = 1.0f;
        static float indicatorEdgeDistance = 0.925f;
        static float maxTargetDistance = 50f;

        public Dictionary<int, EnemyInfo> targets = new Dictionary<int, EnemyInfo>();
        Dictionary<int, GameObject> panels = new Dictionary<int, GameObject>();
        GameObject canvasObj;
        Text text;

        public void Init()
        {
            CreateCanvas();

            GameObject textObj = new GameObject();

            text = textObj.AddComponent<Text>();
            text.font = ((Font)Resources.GetBuiltinResource(typeof(Font), "Arial.ttf"));
            text.fontSize = 20;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            Outline outline = textObj.AddComponent<Outline>();
            outline.effectColor = Color.black;

            textObj.transform.SetParent(canvasObj.transform);
            textObj.SetActive(true);
            float textWidth = 350f;
            float textHeight = 80f;
            text.transform.position = new Vector3(
                50f + textWidth / 2f,
                (Screen.height - 25f) - textHeight / 2f,
                0);
            text.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, textWidth);
            text.GetComponent<RectTransform>().SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, textHeight);
        }

        public void UpdateText(string str, Color color)
        {
            text.text = str;
            text.color = color;
        }

        public void DoUpdate(Camera mainCamera)
        {
            if (mainCamera == null)
                targets.Clear();

            HashSet<int> updatedIDs = new HashSet<int>();
            foreach (KeyValuePair<int, EnemyInfo> pair in targets)
            {
                GameObject target = pair.Value.gameObject;
                //  Get the target's world position in screen coordinate position
                Vector3 targetPosOnScreen = mainCamera.WorldToScreenPoint(target.transform.position);
                if (OnScreen(targetPosOnScreen, mainCamera))
                    continue;
                float distanceFromViewer = GetDistance(target.transform.position, mainCamera.transform.position);
                if (distanceFromViewer > maxTargetDistance)
                    continue;
                UpdateOffScreen(
                    pair.Key,
                    targetPosOnScreen,
                    distanceFromViewer,
                    pair.Value.color,
                    pair.Value.icon,
                    pair.Value.applyRotation);
                updatedIDs.Add(pair.Key);
            }
            List<int> panelsToRemove = new List<int>();
            foreach (KeyValuePair<int, GameObject> panel in panels)
            {
                if (updatedIDs.Contains(panel.Key))
                    continue;
                UnityEngine.Object.Destroy(panel.Value);
                panelsToRemove.Add(panel.Key);
            }
            foreach (int idToRemove in panelsToRemove)
            {
                panels.Remove(idToRemove);
            }
        }

        void UpdateOffScreen(int id, Vector3 targetPosOnScreen, float distance, Color color, Sprite sprite, bool applyRotation)
        {
            //  Create a variable for the center position of the screen.
            Vector3 screenCenter = new Vector3(Screen.width, Screen.height, 0) / 2;

            //  Set newIndicatorPos anchor to the center of the screen instead of bottom left
            Vector3 newIndicatorPos = targetPosOnScreen - screenCenter;

            //  Flip the newIndicatorPos to correct the calculations for indicators behind the camera.
            if (newIndicatorPos.z < 0)
                newIndicatorPos *= -1;

            //  Get angle from center of screen to target position
            float angle = Mathf.Atan2(newIndicatorPos.y, newIndicatorPos.x);
            angle -= 90 * Mathf.Deg2Rad;

            //  y = mx + b (intercept forumla)
            float cos = Mathf.Cos(angle);
            float sin = Mathf.Sin(angle);
            float m = cos / sin;

            //  Create the screen boundaries that the indicators reside in.
            Vector3 screenBounds = new Vector3(screenCenter.x * indicatorEdgeDistance, screenCenter.y * indicatorEdgeDistance);

            //  Check which screen side the target is currently in.
            //  Check top & bottom
            if (cos > 0)
                newIndicatorPos = new Vector2(-screenBounds.y / m, screenBounds.y);
            else
                newIndicatorPos = new Vector2(screenBounds.y / m, -screenBounds.y);

            //  Check left & right
            if (newIndicatorPos.x > screenBounds.x)
                newIndicatorPos = new Vector2(screenBounds.x, -screenBounds.x * m);
            else if (newIndicatorPos.x < -screenBounds.x)
                newIndicatorPos = new Vector2(-screenBounds.x, screenBounds.x * m);

            //  Reset the newIndicatorPos anchor back to bottom left corner.
            newIndicatorPos += screenCenter;

            Image image;

            if (!panels.ContainsKey(id))
            {
                GameObject panel = new GameObject();

                image = panel.AddComponent<Image>();
                image.sprite = sprite;

                Outline outline = panel.AddComponent<Outline>();
                Color outlineColor = Color.black;
                outlineColor.a = targetColorAlpha;
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(4f, 4f);

                panel.GetComponent<RectTransform>().SetParent(canvasObj.transform);
                panel.SetActive(true);
                panels.Add(id, panel);
            }
            else
            {
                image = panels[id].GetComponent<Image>();
            }

            color.a = targetColorAlpha;
            image.color = color;

            //  Assign new position
            panels[id].transform.position = new Vector3(newIndicatorPos.x, newIndicatorPos.y, targetPosOnScreen.z);

            // Set the scale
            float scaleDenom = 0.05f * distance + 1.055f;
            scaleDenom = scaleDenom * scaleDenom;
            float newScale = 1f / scaleDenom * 1.111f;
            panels[id].transform.localScale = new Vector2(newScale, newScale);

            //  Assign new rotation
            if (applyRotation)
                panels[id].transform.rotation = Quaternion.Euler(0, 0, angle * Mathf.Rad2Deg);
        }

        float GetDistance(Vector3 PosA, Vector3 PosB)
        {
            Vector3 heading;

            //  Subtracting from both vectors returns the magnitude
            heading.x = PosA.x - PosB.x;
            heading.y = PosA.y - PosB.y;
            heading.z = PosA.z - PosB.z;

            //  Return the sqaure root of the sum of each sqaured vector axises.
            return Mathf.Sqrt((heading.x * heading.x) + (heading.y * heading.y) + (heading.z * heading.z));
        }

        void CreateCanvas()
        {
            //  Create gameobject that holds canvas
            canvasObj = new GameObject("Enemy_Indicator_Canvas");

            //  Create Canvas
            Canvas canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = -100;

            //  Create default components for canvas
            CanvasScaler cs = canvasObj.AddComponent<CanvasScaler>();
            cs.scaleFactor = 1;
            cs.dynamicPixelsPerUnit = 10;
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        bool OnScreen(Vector3 pos, Camera mainCamera)
        {
            if (pos.x < (Screen.width * targetEdgeDistance) && pos.x > (Screen.width - Screen.width * targetEdgeDistance) &&
                pos.y < (Screen.height * targetEdgeDistance) && pos.y > (Screen.height - Screen.height * targetEdgeDistance) &&
                pos.z > mainCamera.nearClipPlane && pos.z < mainCamera.farClipPlane)
                return true;
            return false;
        }
    }
}
