using Newtonsoft.Json;
using System;

namespace UnoServer {
    [SerializableAttribute]
    public class Cards {

        public static UInt16 NO_NUMBER = 99;
        public static UInt16 NO_COLOR = 4;

        [JsonProperty()]
        public UInt16 cardId { set; get; } // Values 108 - to identify the card

        [JsonProperty()]
        public UInt16 number { set; get; } // Values 0 - 9 - 99 for no number

        [JsonProperty()]
        public UInt16 color { set; get; } // 0 = red - 1 = green - 2 = blue - 3 = yellow - 4 = no color
        
        [JsonProperty()]
        public bool plus2 { set; get; }

        [JsonProperty()]
        public bool reverse { set; get; }

        [JsonProperty()]
        public bool skip { set; get; }

        [JsonProperty()]
        public bool plus4 { set; get; }

        [JsonProperty()]
        public bool changeColor { set; get; }

        public Cards(UInt16 cardId, UInt16 number, UInt16 color, bool plus2, bool reverse, bool skip, bool plus4, bool changeColor) {
            this.cardId = cardId;
            this.number = number;
            this.color = color;
            this.plus2 = plus2;
            this.reverse = reverse;
            this.skip = skip;
            this.plus4 = plus4;
            this.changeColor = changeColor;
        }


        public override string ToString() {
            //return $"Color:{color} - Number:{number} - is+2:{plus2} - isReverse:{reverse} - isSkip:{skip} - is+4:{plus4} - isChangeColor:{changeColor}";
            string card = "";
            if (plus4) {
                card = "Plus4";
            }
            else if (plus2) {
                card = "Plus2 color: " + color;
            }
            else if (changeColor) {
                card = "Change Color";
            }
            else if (skip) {
                card = "Skip color: " + color;
            }
            else if (reverse) {
                card = "Reverse color: " + color;
            }
            else {
                card = "Number: " + number + " color: " + color;
            }

            return card;
        }
    }
}
