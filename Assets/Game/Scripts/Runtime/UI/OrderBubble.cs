using TMPro;
using UnityEngine;

namespace JuiceKing
{
    /// <summary>Speech bubble above a customer showing what they want.</summary>
    public class OrderBubble : MonoBehaviour
    {
        public SpriteRenderer icon;
        public TextMeshPro countText;
        public SpriteRenderer happy;
        public SpriteRenderer background;

        Vector3 _iconScale = Vector3.one, _happyScale = Vector3.one;

        void Awake()
        {
            if (icon != null) _iconScale = icon.transform.localScale;
            if (happy != null) _happyScale = happy.transform.localScale;
        }

        public void SetOrder(Sprite fruit, int count)
        {
            if (icon != null)
            {
                icon.sprite = fruit;
                icon.gameObject.SetActive(true);
            }
            if (happy != null) happy.gameObject.SetActive(false);
            SetCount(count);
        }

        public void SetCount(int remaining)
        {
            if (countText == null) return;
            countText.gameObject.SetActive(true);
            countText.text = remaining.ToString();
            if (icon != null) Tweener.Punch(icon.transform, 0.3f, 0.25f, _iconScale);
        }

        public void ShowHappy()
        {
            if (icon != null) icon.gameObject.SetActive(false);
            if (countText != null) countText.gameObject.SetActive(false);
            if (happy != null)
            {
                happy.gameObject.SetActive(true);
                Tweener.Scale(happy.transform, Vector3.zero, _happyScale, 0.35f, Ease.OutBack);
            }
            Tweener.Delay(1.4f, () =>
            {
                if (this != null && gameObject != null)
                    Tweener.Scale(transform, transform.localScale, Vector3.zero, 0.25f, Ease.InQuad);
            });
        }
    }
}
