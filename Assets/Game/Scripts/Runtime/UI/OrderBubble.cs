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

        Vector3 _iconScale = Vector3.one, _happyScale = Vector3.one, _sadScale = Vector3.one, _fillScale = Vector3.one;
        int _serial;
        float _patience;

        static readonly Color Calm = new Color(0.45f, 0.9f, 0.4f);
        static readonly Color Worried = new Color(1f, 0.8f, 0.2f);
        static readonly Color Angry = new Color(1f, 0.35f, 0.3f);

        void Awake()
        {
            if (icon != null) _iconScale = icon.transform.localScale;
            if (happy != null) _happyScale = happy.transform.localScale;
            if (sad != null) _sadScale = sad.transform.localScale;
            if (patienceFill != null) _fillScale = patienceFill.localScale;
        }

        public void SetOrder(Sprite fruit, int count)
        {
            _serial++;
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
            // Nervous jitter in the last stretch.
            if (_patience > 0.8f && icon != null && icon.gameObject.activeSelf)
            {
                float a = Mathf.Sin(Time.time * 40f) * 6f * (_patience - 0.8f) * 5f;
                icon.transform.localRotation = Quaternion.Euler(0f, 0f, a);
            }
        }

        void Hide(bool happyFace)
        {
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
