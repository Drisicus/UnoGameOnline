﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnoServer
{
    [SerializableAttribute]
    public class Game {
        public static Random random = new Random();

        [JsonProperty()]
        public int gameId { set; get; }

        [JsonProperty()]
        public bool isGameReady { set; get; }

        [JsonProperty()]
        public UInt16 numberOfPlayers { set; get; }

        [JsonProperty()]
        public UInt16 direction { set; get; } // 1 is incrementing (0 -> 1 -> 2...); -1 is decrementing (0 -> n -> n-1...)

        [JsonProperty()]
        public UInt16 activePlayerIdx { set; get; }

        [JsonProperty()]
        public List<Cards> cardsStack { set; get; }      

        [JsonProperty()]
        public Dictionary<UInt16, List<Cards>> playersCards { set; get; }

        [JsonProperty()]
        public Cards activeCard { set; get; }

        public Game(UInt16 numberOfPlayers, int gameId) {
            this.gameId = gameId;
            isGameReady = false;

            this.numberOfPlayers = numberOfPlayers;
            direction = 1;
            activePlayerIdx = (UInt16)random.Next(0, numberOfPlayers);

            // Create initial Stack
            cardsStack = initializeCards();

            // Randomize the position of the cards
            Shuffle(cardsStack);

            // Give initial cards to players (7)
            playersCards = initialCardsDealt();

            activeCard = cardsStack[0];
            cardsStack.RemoveAt(0);
        }

        public void Shuffle<T>(IList<T> list) {
            int n = list.Count;
            while (n > 1) {
                n--;
                int k = random.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        private List<Cards> initializeCards() {

            List<Cards> stackOfCards = new List<Cards>();

            for (UInt16 color = 0; color < 4; color++) {
                // 19 cartas con numero por color, solo hay una de 0 por color
                for (UInt16 number = 0; number <= 9; number++) {
                    stackOfCards.Add(new Cards(number, color, false, false, false, false, false));
                }
                for (UInt16 number = 1; number <= 9; number++) {
                    stackOfCards.Add(new Cards(number, color, false, false, false, false, false));
                }

                // Cartas de acción
                for (int idx = 0; idx < 2; idx++) {
                    // +2
                    stackOfCards.Add(new Cards(Cards.NO_NUMBER, color, true, false, false, false, false));
                    // reverse
                    stackOfCards.Add(new Cards(Cards.NO_NUMBER, color, false, true, false, false, false));
                    // skip
                    stackOfCards.Add(new Cards(Cards.NO_NUMBER, color, false, false, true, false, false));
                }
            }

            for (int idx = 0; idx < 4; idx++) {
                // +4
                stackOfCards.Add(new Cards(Cards.NO_NUMBER, Cards.NO_COLOR, false, false, true, true, false));
                // change color
                stackOfCards.Add(new Cards(Cards.NO_NUMBER, Cards.NO_COLOR, false, false, true, false, true));
            }

            return stackOfCards;
        }

        private Dictionary<UInt16, List<Cards>> initialCardsDealt() {
            Dictionary<UInt16, List<Cards>> dictionary = new Dictionary<UInt16, List<Cards>>();
            for (UInt16 player = 0; player < numberOfPlayers; player++) {
                List<Cards> cards = new List<Cards>();
                for (int cardCount = 0; cardCount < 7; cardCount++) {
                    // Random card. Add to player. Remove from stack.
                    int cardIdx = random.Next(0, cardsStack.Count);
                    cards.Add(cardsStack[cardIdx]);
                    cardsStack.RemoveAt(cardIdx);
                }
                dictionary.Add(player, cards);
            }
            return dictionary;
        }
    }
}
