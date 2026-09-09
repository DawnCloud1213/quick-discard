using System.Collections.Generic;
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
        private Image _background;
        private Text _label;
        private bool _isHovered;

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

            if (QuickDiscardPlugin.EnableDropZone == null || !QuickDiscardPlugin.EnableDropZone.Value)
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

            UpdateVisibility();
            SetHovered(false);
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
            _rectTransform.anchorMin = new Vector2(1f, 0f);
            _rectTransform.anchorMax = new Vector2(1f, 0f);
            _rectTransform.pivot = new Vector2(1f, 0f);
            _rectTransform.anchoredPosition = new Vector2(
                -QuickDiscardPlugin.DropZoneMargin.Value,
                QuickDiscardPlugin.DropZoneMargin.Value);
            _rectTransform.sizeDelta = new Vector2(
                QuickDiscardPlugin.DropZoneWidth.Value,
                QuickDiscardPlugin.DropZoneHeight.Value);

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
            _label.text = "DROP";
            _label.raycastTarget = false;

            Outline outline = labelObject.GetComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.8f);
            outline.effectDistance = new Vector2(1f, -1f);
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

        private void OnDisable()
        {
            SetHovered(false);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }
    }
}
