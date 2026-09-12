using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using EFT.InventoryLogic;
using EFT.UI;
using EFT.UI.DragAndDrop;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace QuickDiscard
{
    public sealed class DropZoneController : MonoBehaviour
    {
        private static readonly Color NormalColor = new Color(0.18f, 0.04f, 0.04f, 0.82f);
        private static readonly Color HoverColor = new Color(0.72f, 0.08f, 0.04f, 0.95f);
        private static readonly Color TextColor = new Color(1f, 0.88f, 0.82f, 1f);

        internal static DropZoneController Instance;

        private InventoryScreen _screen;
        private InventoryScreen.InventoryScreenController _screenController;
        private RectTransform _rectTransform;
        private RectTransform _parentRect;
        private Image _background;
        private Text _label;
        private bool _isHovered;
        private bool _placementApplied;
        private float _appliedMinX;
        private float _appliedMinY;
        private float _appliedMaxX;
        private float _appliedMaxY;
        private float _appliedMargin;
        private static int _layoutDumpCount;
        private static bool _layoutDumpRunning;

        private const int LayoutDumpLimit = 4;
        private const int LayoutEntryLimit = 220;

        internal bool IsAvailable
        {
            get
            {
                if (Instance != this || !isActiveAndEnabled || !gameObject.activeInHierarchy)
                {
                    return false;
                }

                if (_screen == null || _screenController == null)
                {
                    return false;
                }

                if (QuickDiscardPlugin.EnableDropZone == null || !QuickDiscardPlugin.EnableDropZone.Value)
                {
                    return false;
                }

                if (QuickDiscardPlugin.RaidOnly != null && QuickDiscardPlugin.RaidOnly.Value && !_screenController.InRaid)
                {
                    return false;
                }

                return true;
            }
        }

        internal static void EnsureFor(
            InventoryScreen screen,
            InventoryScreen.InventoryScreenController screenController)
        {
            if (screen == null || screenController == null)
            {
                return;
            }

            bool zoneEnabled = QuickDiscardPlugin.EnableDropZone != null && QuickDiscardPlugin.EnableDropZone.Value;
            bool dumpEnabled = QuickDiscardPlugin.DumpInventoryLayout != null && QuickDiscardPlugin.DumpInventoryLayout.Value;
            if (!zoneEnabled && !dumpEnabled)
            {
                return;
            }

            DropZoneController controller = screen.GetComponent<DropZoneController>();
            if (controller == null)
            {
                controller = screen.gameObject.AddComponent<DropZoneController>();
            }

            controller.Initialize(screen, screenController);
        }

        private void Initialize(
            InventoryScreen screen,
            InventoryScreen.InventoryScreenController screenController)
        {
            _screen = screen;
            _screenController = screenController;
            Instance = this;

            if (_rectTransform == null)
            {
                BuildUi();
            }

            ApplyPlacement();
            DumpLayoutOnce();
            UpdateVisibility();
            SetHovered(false);
        }

        private void DumpLayoutOnce()
        {
            if (_layoutDumpCount >= LayoutDumpLimit ||
                _layoutDumpRunning ||
                QuickDiscardPlugin.DumpInventoryLayout == null ||
                !QuickDiscardPlugin.DumpInventoryLayout.Value)
            {
                return;
            }

            _layoutDumpRunning = true;
            StartCoroutine(DumpLayoutRoutine());
        }

        private IEnumerator DumpLayoutRoutine()
        {
            // The screen rect is not necessarily laid out on the frame the inventory opens.
            for (int i = 0; i < 60; i++)
            {
                yield return null;

                if (_parentRect != null && _parentRect.rect.width > 0f && _parentRect.rect.height > 0f)
                {
                    break;
                }
            }

            try
            {
                DumpLayout();
                _layoutDumpCount++;
            }
            catch (Exception exception)
            {
                if (QuickDiscardPlugin.Log != null)
                {
                    QuickDiscardPlugin.Log.LogWarning("Quick Discard layout dump failed: " + exception);
                }
            }
            finally
            {
                _layoutDumpRunning = false;
            }
        }

        private void DumpLayout()
        {
            if (_parentRect == null || QuickDiscardPlugin.Log == null)
            {
                return;
            }

            Canvas canvas = GetComponentInParent<Canvas>();
            Camera camera = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            {
                camera = canvas.worldCamera;
            }

            Vector3[] corners = new Vector3[4];
            _parentRect.GetWorldCorners(corners);
            Vector2 parentMin = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
            Vector2 parentMax = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);
            float parentWidth = parentMax.x - parentMin.x;
            float parentHeight = parentMax.y - parentMin.y;
            if (parentWidth <= 0f || parentHeight <= 0f)
            {
                QuickDiscardPlugin.Log.LogWarning("Quick Discard layout dump skipped: inventory screen rect is empty.");
                return;
            }

            QuickDiscardPlugin.Log.LogInfo(string.Format(
                CultureInfo.InvariantCulture,
                "LAYOUT-HEADER #{0} inRaid={1} controller={2} screen={3}x{4} camera={5}",
                _layoutDumpCount + 1,
                _screenController != null && _screenController.InRaid,
                _screenController == null ? "?" : _screenController.GetType().Name,
                parentWidth,
                parentHeight,
                camera == null ? "overlay" : camera.name));

            RectTransform[] rects = GetComponentsInChildren<RectTransform>(true);
            List<LayoutEntry> entries = new List<LayoutEntry>(rects.Length);

            for (int i = 0; i < rects.Length; i++)
            {
                RectTransform rect = rects[i];
                if (rect == null || rect == _parentRect || !rect.gameObject.activeInHierarchy)
                {
                    continue;
                }

                rect.GetWorldCorners(corners);
                Vector2 min = RectTransformUtility.WorldToScreenPoint(camera, corners[0]);
                Vector2 max = RectTransformUtility.WorldToScreenPoint(camera, corners[2]);

                float xMin = (min.x - parentMin.x) / parentWidth;
                float yMin = (min.y - parentMin.y) / parentHeight;
                float xMax = (max.x - parentMin.x) / parentWidth;
                float yMax = (max.y - parentMin.y) / parentHeight;
                float width = xMax - xMin;
                float height = yMax - yMin;

                if (width <= 0f || height <= 0f)
                {
                    continue;
                }

                entries.Add(new LayoutEntry
                {
                    Path = BuildPath(rect),
                    XMin = xMin,
                    YMin = yMin,
                    XMax = xMax,
                    YMax = yMax,
                    Area = width * height,
                    Components = DescribeComponents(rect.gameObject)
                });
            }

            entries.Sort(CompareByAreaDescending);

            int limit = Math.Min(entries.Count, LayoutEntryLimit);
            for (int i = 0; i < limit; i++)
            {
                LayoutEntry entry = entries[i];
                QuickDiscardPlugin.Log.LogInfo(string.Format(
                    CultureInfo.InvariantCulture,
                    "LAYOUT area={0:0.####} x=[{1:0.####},{2:0.####}] y=[{3:0.####},{4:0.####}] path={5} comps={6}",
                    entry.Area,
                    entry.XMin,
                    entry.XMax,
                    entry.YMin,
                    entry.YMax,
                    entry.Path,
                    entry.Components));
            }
        }

        private static int CompareByAreaDescending(LayoutEntry left, LayoutEntry right)
        {
            return right.Area.CompareTo(left.Area);
        }

        private string BuildPath(RectTransform rect)
        {
            StringBuilder builder = new StringBuilder(rect.name);
            Transform current = rect.parent;

            while (current != null && current != transform)
            {
                builder.Insert(0, "/");
                builder.Insert(0, current.name);
                current = current.parent;
            }

            return builder.ToString();
        }

        private static string DescribeComponents(GameObject target)
        {
            Component[] components;
            try
            {
                components = target.GetComponents<Component>();
            }
            catch (Exception)
            {
                return "?";
            }

            List<string> names = new List<string>();
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null)
                {
                    names.Add("MissingScript");
                    continue;
                }

                string name = component.GetType().Name;
                if (name == "RectTransform" || name == "CanvasRenderer")
                {
                    continue;
                }

                names.Add(name);
            }

            return string.Join(",", names.ToArray());
        }

        private void BuildUi()
        {
            GameObject root = new GameObject(
                "QuickDiscard_DropZone",
                typeof(RectTransform),
                typeof(Image),
                typeof(CanvasGroup));

            root.transform.SetParent(transform, false);
            root.transform.SetAsLastSibling();

            _rectTransform = root.GetComponent<RectTransform>();
            _rectTransform.pivot = new Vector2(0.5f, 0.5f);
            _parentRect = transform as RectTransform;

            _background = root.GetComponent<Image>();
            _background.color = NormalColor;
            _background.raycastTarget = false;

            CanvasGroup canvasGroup = root.GetComponent<CanvasGroup>();
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            GameObject labelObject = new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(Text),
                typeof(Outline));

            labelObject.transform.SetParent(root.transform, false);

            RectTransform labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            _label = labelObject.GetComponent<Text>();
            _label.font = Font.CreateDynamicFontFromOSFont("Arial", 18);
            _label.fontSize = 18;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = TextColor;
            _label.text = LabelText();
            _label.raycastTarget = false;

            Outline outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);

            ApplyPlacement();
        }

        private static string LabelText()
        {
            if (QuickDiscardPlugin.DropZoneLabel == null)
            {
                return "DROP";
            }

            string value = QuickDiscardPlugin.DropZoneLabel.Value;
            return string.IsNullOrEmpty(value) ? "DROP" : value;
        }

        internal void ApplyPlacement()
        {
            if (_rectTransform == null)
            {
                return;
            }

            float minX = Mathf.Clamp01(QuickDiscardPlugin.DropZoneAnchorMinX.Value);
            float minY = Mathf.Clamp01(QuickDiscardPlugin.DropZoneAnchorMinY.Value);
            float maxX = Mathf.Clamp01(QuickDiscardPlugin.DropZoneAnchorMaxX.Value);
            float maxY = Mathf.Clamp01(QuickDiscardPlugin.DropZoneAnchorMaxY.Value);
            float margin = Mathf.Max(0f, QuickDiscardPlugin.DropZoneMargin.Value);

            if (minX > maxX)
            {
                float swap = minX;
                minX = maxX;
                maxX = swap;
            }

            if (minY > maxY)
            {
                float swap = minY;
                minY = maxY;
                maxY = swap;
            }

            if (!_placementApplied ||
                !Mathf.Approximately(minX, _appliedMinX) ||
                !Mathf.Approximately(minY, _appliedMinY) ||
                !Mathf.Approximately(maxX, _appliedMaxX) ||
                !Mathf.Approximately(maxY, _appliedMaxY) ||
                !Mathf.Approximately(margin, _appliedMargin))
            {
                _rectTransform.anchorMin = new Vector2(minX, minY);
                _rectTransform.anchorMax = new Vector2(maxX, maxY);
                _rectTransform.offsetMin = new Vector2(margin, margin);
                _rectTransform.offsetMax = new Vector2(-margin, -margin);

                _appliedMinX = minX;
                _appliedMinY = minY;
                _appliedMaxX = maxX;
                _appliedMaxY = maxY;
                _appliedMargin = margin;
                _placementApplied = true;
            }

            string label = LabelText();
            if (_label != null && _label.text != label)
            {
                _label.text = label;
            }
        }

        private void UpdateLabelSize()
        {
            if (_label == null || _parentRect == null)
            {
                return;
            }

            float height = _parentRect.rect.height * (_appliedMaxY - _appliedMinY) - _appliedMargin * 2f;
            float width = _parentRect.rect.width * (_appliedMaxX - _appliedMinX) - _appliedMargin * 2f;
            float reference = Mathf.Min(height, width * 0.5f);
            int fontSize = Mathf.Clamp(Mathf.RoundToInt(reference * 0.22f), 14, 96);

            if (_label.fontSize != fontSize)
            {
                _label.fontSize = fontSize;
            }
        }

        internal void UpdateHover(PointerEventData eventData)
        {
            if (eventData == null)
            {
                SetHovered(false);
                return;
            }

            SetHovered(ContainsScreenPoint(eventData.position, eventData.pressEventCamera));
        }

        internal bool ContainsScreenPoint(Vector2 screenPoint, Camera camera)
        {
            if (!IsAvailable || _rectTransform == null)
            {
                return false;
            }

            return RectTransformUtility.RectangleContainsScreenPoint(
                _rectTransform,
                screenPoint,
                camera);
        }

        internal bool TryDiscardHoveredItem()
        {
            if (!IsAvailable || EventSystem.current == null)
            {
                return false;
            }

            PointerEventData pointer = new PointerEventData(EventSystem.current);
            pointer.position = Input.mousePosition;

            List<RaycastResult> results = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, results);

            for (int i = 0; i < results.Count; i++)
            {
                GameObject hitObject = results[i].gameObject;
                if (hitObject == null)
                {
                    continue;
                }

                ItemView itemView = hitObject.GetComponentInParent<ItemView>();
                if (itemView == null || itemView.Item == null)
                {
                    continue;
                }

                ItemUiContext itemUiContext = ItemViewAccess.GetItemUiContext(itemView);
                if (itemUiContext == null)
                {
                    continue;
                }

                QuickDiscardPlugin.RequestDiscard(itemUiContext, itemView.Item);
                return true;
            }

            return false;
        }

        private void UpdateVisibility()
        {
            if (_rectTransform == null)
            {
                return;
            }

            _rectTransform.gameObject.SetActive(IsAvailable);
        }

        private void SetHovered(bool hovered)
        {
            _isHovered = hovered;

            if (_background != null)
            {
                _background.color = hovered ? HoverColor : NormalColor;
            }

            if (_label != null)
            {
                _label.color = hovered ? Color.white : TextColor;
            }
        }

        private void OnEnable()
        {
            UpdateVisibility();
            SetHovered(false);
        }

        private void LateUpdate()
        {
            ApplyPlacement();
            UpdateLabelSize();
        }

        private void OnDisable()
        {
            SetHovered(false);

            if (_layoutDumpCount < LayoutDumpLimit)
            {
                _layoutDumpRunning = false;
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private sealed class LayoutEntry
        {
            internal string Path;
            internal float XMin;
            internal float YMin;
            internal float XMax;
            internal float YMax;
            internal float Area;
            internal string Components;
        }
    }
}
