using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Speech bubble above a customer: what they want, how many are left, and how patient they still are.</summary>
    public class OrderBubble : MonoBehaviour
    {
        public SpriteRenderer icon;
        public TextMeshPro countText;
        public SpriteRenderer happy;
        public SpriteRenderer sad;
        public SpriteRenderer background;
        [Tooltip("Thin bar under the order that shrinks as patience runs out.")]
        public Transform patienceFill;
        public SpriteRenderer patienceFillRenderer;
        public GameObject patienceBar;

        [Tooltip("Place in the queue (0 = at the counter). Bubbles further back are smaller and drawn underneath.")]
        public int queueSlot;

        Vector3 _iconScale = Vector3.one, _happyScale = Vector3.one, _sadScale = Vector3.one, _fillScale = Vector3.one, _rootScale = Vector3.one;
        Renderer[] _renderers;
        int[] _baseOrder;
        int _appliedSlot = int.MinValue;
        bool _hiding;
        int _serial;
        float _patience;
        Vector3 _basePos;
        float _side;

        static readonly Color Calm = new Color(0.45f, 0.9f, 0.4f);
        static readonly Color Worried = new Color(1f, 0.8f, 0.2f);
        static readonly Color Angry = new Color(1f, 0.35f, 0.3f);

        void Awake()
        {
            _rootScale = transform.localScale;
            _basePos = transform.localPosition;
            _renderers = GetComponentsInChildren<Renderer>(true);
            _baseOrder = new int[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++) _baseOrder[i] = _renderers[i].sortingOrder;
            if (icon != null) _iconScale = icon.transform.localScale;
            if (happy != null) _happyScale = happy.transform.localScale;
            if (sad != null) _sadScale = sad.transform.localScale;
            if (patienceFill != null) _fillScale = patienceFill.localScale;
        }

        public void SetOrder(Sprite fruit, int count)
        {
            _serial++;
            _hiding = false;
            if (icon != null)
            {
                icon.sprite = fruit;
                icon.gameObject.SetActive(true);
                icon.transform.localScale = _iconScale;
            }
            if (happy != null) happy.gameObject.SetActive(false);
            if (sad != null) sad.gameObject.SetActive(false);
            if (patienceBar != null) patienceBar.SetActive(true);
            SetCount(count);
        }

        public void SetCount(int remaining)
        {
            if (countText == null) return;
            countText.gameObject.SetActive(true);
            countText.text = remaining.ToString();
            if (icon != null) Tweener.Punch(icon.transform, 0.3f, 0.25f, _iconScale);
        }

        /// <summary>0 = just arrived at the counter, 1 = about to leave.</summary>
        public void SetPatience(float used)
        {
            used = Mathf.Clamp01(used);
            _patience = used;
            if (patienceFill != null)
            {
                float left = 1f - used;
                patienceFill.localScale = new Vector3(_fillScale.x * Mathf.Max(0.001f, left), _fillScale.y, _fillScale.z);
                patienceFill.localPosition = new Vector3(-0.5f * (1f - left) * _fillScale.x, patienceFill.localPosition.y, patienceFill.localPosition.z);
            }
            if (patienceFillRenderer != null)
                patienceFillRenderer.color = used < 0.5f ? Color.Lerp(Calm, Worried, used * 2f) : Color.Lerp(Worried, Angry, (used - 0.5f) * 2f);
        }

        void LateUpdate()
        {
            // The queue stands in a line toward the camera, so every bubble overlaps the one in front of it: keep the
            // customer being served big and on top, and shrink the ones waiting behind.
            int slot = Mathf.Max(0, queueSlot);
            if (slot != _appliedSlot)
            {
                _appliedSlot = slot;
                int boost = (12 - Mathf.Min(slot, 12)) * 5;
                for (int i = 0; i < _renderers.Length; i++)
                    if (_renderers[i] != null) _renderers[i].sortingOrder = _baseOrder[i] + boost;
            }
            if (!_hiding)
            {
                float k = slot == 0 ? 1.08f : slot == 1 ? 0.86f : slot == 2 ? 0.76f : 0.68f;
                transform.localScale = Vector3.Lerp(transform.localScale, _rootScale * k, 1f - Mathf.Exp(-10f * Time.deltaTime));
                // Waiting bubbles zig-zag to either side of their customer, so the line of bubbles does not stack
                // into one column over everybody's heads.
                float side = slot == 0 ? 0f : slot % 2 == 1 ? 0.42f : -0.42f;
                _side = Mathf.Lerp(_side, side, 1f - Mathf.Exp(-8f * Time.deltaTime));
            }
            var parent = transform.parent;
            if (parent != null)
            {
                var cam = GameRefs.I != null ? GameRefs.I.mainCamera : null;
                Vector3 right = cam != null ? cam.transform.right : Vector3.right;
                right.y = 0f;
                transform.position = parent.TransformPoint(_basePos) + right.normalized * _side;
            }

            // Nervous jitter in the last stretch.
            if (_patience > 0.8f && icon != null && icon.gameObject.activeSelf)
            {
                float a = Mathf.Sin(Time.time * 40f) * 6f * (_patience - 0.8f) * 5f;
                icon.transform.localRotation = Quaternion.Euler(0f, 0f, a);
            }
        }

        void Hide(bool happyFace)
        {
            _hiding = true;
            if (icon != null)
            {
                icon.gameObject.SetActive(false);
                icon.transform.localRotation = Quaternion.identity;
            }
            if (countText != null) countText.gameObject.SetActive(false);
            if (patienceBar != null) patienceBar.SetActive(false);
            _patience = 0f;
            var face = happyFace ? happy : sad;
            var scale = happyFace ? _happyScale : _sadScale;
            if (face != null)
            {
                face.gameObject.SetActive(true);
                Tweener.Scale(face.transform, Vector3.zero, scale, 0.35f, Ease.OutBack);
            }
            int serial = ++_serial;
            Tweener.Delay(1.4f, () =>
            {
                // The customer may already be back in the pool (and re-used) by now.
                if (this == null || serial != _serial || !gameObject.activeInHierarchy) return;
                Tweener.Scale(transform, transform.localScale, Vector3.zero, 0.25f, Ease.InQuad);
            });
        }

        public void ShowHappy() => Hide(true);

        public void ShowSad()
        {
            Hide(false);
            Sfx.Play(SfxId.Error, 0.2f, 0.8f);
        }
    }
}
