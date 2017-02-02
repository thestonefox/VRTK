﻿// Keyboard Renderer|Keyboard|81020
namespace VRTK
{
    using UnityEngine;

    /// <summary>
    /// The Keyboard Renderer script renders a functional keyboard to a UI Canvas
    /// </summary>
    [ExecuteInEditMode]
    public class VRTK_KeyboardRenderer : MonoBehaviour
    {
        protected class RenderableKeyboard
        {
            public RenderableKeyset[] keysets;
        }

        protected class RenderableKeyset
        {
            public string name;
            public RenderableRow[] rows;
        }

        protected class RenderableRow
        {
            public Rect rect;
            public RenderableKey[] keys;
        }

        protected class RenderableKey
        {
            public Rect rect;
        }

        [Tooltip("Keyboard layout to render to canvas")]
        public VRTK_KeyboardLayout keyboardLayout;

        protected int currentKeyset = 0;

        protected virtual void Start()
        {
            SetupKeyboardUI();
        }

        protected virtual void Update()
        {
            if (Application.isEditor && !Application.isPlaying)
            {
                SetupKeyboardUI();
            }
        }

        protected void ProcessRuntimeObject(GameObject obj)
        {
            if (Application.isEditor)
            {
                obj.hideFlags = HideFlags.DontSave;// | HideFlags.NotEditable;
            }
        }

        protected void DestroyRuntimeObject(GameObject obj)
        {
            if (Application.isEditor)
            {
                DestroyImmediate(obj);
            }
            else
            {
                Destroy(obj);
            }
        }

        /// <summary>
        /// The SetupKeyboardUI method resets the canvas's children and creates
        /// the canvas objects that make up a rendered keyboard.
        /// </summary>
        public void SetupKeyboardUI()
        {
            // TODO: Better method of finding canvas(es)
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                DestroyRuntimeObject(transform.GetChild(i).gameObject);
            }

            Rect containerRect = gameObject.GetComponent<RectTransform>().rect;

            Vector2 rowPivot = Vector2.one * 0.5f;
            Vector2 keyPivot = Vector2.one * 0.5f;

            RenderableKeyboard rKeyboard = GenerateRenderableKeyboard(containerRect.size);

            for (int s = 0; s < rKeyboard.keysets.Length; s++)
            {
                // Keyset
                RenderableKeyset rKeyset = rKeyboard.keysets[s];
                GameObject uiKeyset = new GameObject(rKeyset.name, typeof(RectTransform));
                ProcessRuntimeObject(uiKeyset);
                uiKeyset.SetActive(s == 0);
                RectTransform keysetTransform = uiKeyset.GetComponent<RectTransform>();
                keysetTransform.SetParent(gameObject.transform, false);
                keysetTransform.pivot = new Vector2(0.5f, 0.5f);
                keysetTransform.anchorMin = new Vector2(0, 0);
                keysetTransform.anchorMax = new Vector2(1, 1);
                keysetTransform.offsetMin = new Vector2(0, 0);
                keysetTransform.offsetMax = new Vector2(0, 0);

                for (int r = 0; r < rKeyset.rows.Length; r++)
                {
                    // Row
                    RenderableRow rRow = rKeyset.rows[r];
                    GameObject uiRow = new GameObject("KeyboardRow", typeof(RectTransform));
                    ProcessRuntimeObject(uiRow);
                    RectTransform rowTransform = uiRow.GetComponent<RectTransform>();
                    rowTransform.SetParent(keysetTransform, false);
                    rowTransform.pivot = rowPivot;
                    ApplyRectLayoutToRectTransform(rRow.rect, rowTransform, containerRect.size);

                    for (int k = 0; k < rRow.keys.Length; k++)
                    {
                        // Key
                        RenderableKey rKey = rRow.keys[k];
                        GameObject uiKey = new GameObject("KeyboardKey", typeof(RectTransform));
                        ProcessRuntimeObject(uiKey);
                        RectTransform keyTransform = uiKey.GetComponent<RectTransform>();
                        keyTransform.SetParent(rowTransform, false);
                        keyTransform.pivot = keyPivot;
                        ApplyRectLayoutToRectTransform(rKey.rect, keyTransform, rRow.rect.size);

                    }
                }
            }
        }

        protected RenderableKeyboard GenerateRenderableKeyboard(Vector2 canvasSize)
        {
            RenderableKeyboard rKeyboard = new RenderableKeyboard();

            rKeyboard.keysets = new RenderableKeyset[keyboardLayout.keysets.Length];
            for (int s = 0; s < keyboardLayout.keysets.Length; s++)
            {
                // Keyset
                VRTK_KeyboardLayout.Keyset keyset = keyboardLayout.keysets[s];
                RenderableKeyset rKeyset = new RenderableKeyset();
                rKeyboard.keysets[s] = rKeyset;
                rKeyset.name = keyset.name;

                float rowHeight = canvasSize.y / ((float)keyset.rows.Length);

                rKeyset.rows = new RenderableRow[keyset.rows.Length]; // XXX: Implicit space+done row
                for (int r = 0; r < keyset.rows.Length; r++)
                {
                    // Row
                    VRTK_KeyboardLayout.Row row = keyset.rows[r];
                    RenderableRow rRow = new RenderableRow();
                    rKeyset.rows[r] = rRow;
                    rRow.rect = new Rect(0, rowHeight * ((float)r), canvasSize.x, rowHeight);

                    float keyWidth = canvasSize.x / ((float)row.keys.Length);

                    rRow.keys = new RenderableKey[row.keys.Length];
                    for (int k = 0; k < row.keys.Length; k++)
                    {
                        // Key
                        // VRTK_KeyboardLayout.Key key = row.keys[k];
                        RenderableKey rKey = new RenderableKey();
                        rRow.keys[k] = rKey;
                        rKey.rect = new Rect(keyWidth * ((float)k), 0, keyWidth, rowHeight);
                    }
                }
            }

            return rKeyboard;
        }

        /// <summary>
        /// Apply a layout Rect to a RectTransform
        /// </summary>
        /// <remarks>
        /// This method accepts a Rect in (x,y=right,down) coordinate space, coverts it to RectTransform's
        /// (x,y=right,up) coordinate space, and calculates the necessary anchors and offsets required.
        /// </remarks>
        /// <param name="layout">The (x,y=right,down) to apply</param>
        /// <param name="transform">The RectTransform to modify</param>
        /// <param name="containerSize">The dimensions of the container for reference</param>
        protected void ApplyRectLayoutToRectTransform(Rect layout, RectTransform transform, Vector2 containerSize)
        {
            // Convert keyboard rect coordinates (x,y=right,down) to RectTransform rect coordinates (x,y=right,up)
            Rect rRect = new Rect(layout);
            rRect.position = new Vector2(rRect.position.x, containerSize.y - rRect.size.y - rRect.position.y);

            // Anchor set to lower left
            transform.anchorMin = transform.anchorMax = Vector2.zero;
            // lower left corner is offset by the rect position
            transform.offsetMin = rRect.position;
            // upper right corner is offset by the size and position
            transform.offsetMax = rRect.size + rRect.position;
        }
    }
}
