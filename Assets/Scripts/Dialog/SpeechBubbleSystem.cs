using UnityEngine;
using System.Collections.Generic;

public class SpeechBubbleSystem : MonoBehaviour
{
    public static SpeechBubbleSystem Instance;

    public GameObject bubblePrefab;
    public Transform bubbleContainer;

    [System.Serializable]
    public class EmotionSprite
    {
        public Emotion emotion;
        public Sprite sprite;
    }

    public List<EmotionSprite> emotionSprites;

    Dictionary<Emotion, Sprite> spriteMap;

    void Awake()
    {
        Instance = this;

        spriteMap = new Dictionary<Emotion, Sprite>();

        foreach (var e in emotionSprites)
        {
            spriteMap[e.emotion] = e.sprite;
        }
    }

    public void Say(string message, Emotion emotion = Emotion.Normal, float duration = 30f)
    {
        GameObject obj = Instantiate(bubblePrefab, bubbleContainer);

        SpeechBubble bubble = obj.GetComponent<SpeechBubble>();

        Sprite sprite = null;

        if (spriteMap.ContainsKey(emotion))
            sprite = spriteMap[emotion];

        bubble.Init(message, sprite, duration);
    }
}