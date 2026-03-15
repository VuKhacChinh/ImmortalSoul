using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class SpeechBubble : MonoBehaviour
{
    public TextMeshProUGUI text;
    public Image emotionImage;

    public float duration = 2f;

    RectTransform rect;

    void Awake()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Init(string message, Sprite emotionSprite, float time)
    {
        duration = time;

        if (emotionImage != null)
        {
            emotionImage.sprite = emotionSprite;
            emotionImage.enabled = emotionSprite != null;
        }

        StartCoroutine(PlayAnimation(message));
    }

    IEnumerator PlayAnimation(string message)
    {
        Vector2 start = new Vector2(300, -200);
        Vector2 end = Vector2.zero;

        rect.anchoredPosition = start;

        float t = 0;

        while (t < 1)
        {
            t += Time.deltaTime * 4f;

            rect.anchoredPosition = Vector2.Lerp(start, end, t);

            yield return null;
        }

        yield return StartCoroutine(TypeText(message));

        yield return new WaitForSeconds(duration);

        Destroy(gameObject);
    }

    IEnumerator TypeText(string message)
    {
        text.text = "";

        foreach (char c in message)
        {
            text.text += c;
            yield return new WaitForSeconds(0.03f);
        }
    }
}