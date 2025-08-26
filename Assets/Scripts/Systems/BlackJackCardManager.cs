using System.Collections.Generic;
using UnityEngine;

namespace LostAndFound.Systems
{
    public class BlackJackCardManager : MonoBehaviour
    {
        public static BlackJackCardManager Instance;
        private Dictionary<string, Sprite> cardSprites = new Dictionary<string, Sprite>();

        void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                LoadAllCards();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        void LoadAllCards()
        {
            Sprite[] allSprites = Resources.LoadAll<Sprite>("BlackJack");
            foreach (Sprite sprite in allSprites)
            {
                if (!cardSprites.ContainsKey(sprite.name))
                    cardSprites[sprite.name] = sprite;
            }
            Debug.Log($"[BlackJackCardManager] Загружено карт: {cardSprites.Count}");
            
            // Выводим список всех загруженных карт
            Debug.Log("[BlackJackCardManager] Список загруженных карт:");
            foreach (var card in cardSprites)
            {
                Debug.Log($"[BlackJackCardManager] - {card.Key}");
            }
        }

        public Sprite GetCardSprite(string cardName)
        {
            if (cardSprites.TryGetValue(cardName, out Sprite sprite))
                return sprite;
            Debug.LogWarning($"[BlackJackCardManager] Карта не найдена: {cardName}");
            return null;
        }
    }
}