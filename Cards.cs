using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnoServer
{
    [SerializableAttribute]
    public class Cards {

        public static UInt16 NO_NUMBER = 99;
        public static UInt16 NO_COLOR = 4;
        
        [JsonProperty()]
        UInt16 number { set; get; } // Values 0 - 9 - 99 for no number

        [JsonProperty()]
        UInt16 color { set; get; } // 0 = red - 1 = green - 2 = blue - 3 = yellow - 4 = no color
        
        [JsonProperty()]
        bool plus2 { set; get; }

        [JsonProperty()]
        bool reverse { set; get; }

        [JsonProperty()]
        bool skip { set; get; }

        [JsonProperty()]
        bool plus4 { set; get; }

        [JsonProperty()]
        bool changeColor { set; get; }

        public Cards(UInt16 number, UInt16 color, bool plus2, bool reverse, bool skip, bool plus4, bool changeColor) {
            this.number = number;
            this.color = color;
            this.plus2 = plus2;
            this.reverse = reverse;
            this.skip = skip;
            this.plus4 = plus4;
            this.changeColor = changeColor;
        }

        public bool isValidForThisturn(Cards card, bool isStackingOrChangeColor) {
            bool isValid = false;
            // Si la carta es de +2 o +4 solo es valido la carta es una de esas
            if (isStackingOrChangeColor) {
                isValid = (!card.changeColor && (this.plus2 || this.plus4)) 
                    || (card.changeColor && this.color == card.color);
            }
            else {
                bool matchesColor = this.color == card.color;
                bool matchesNumber = this.number == card.number;
                bool matchesActionCard = matchesColor && (card.reverse || card.skip || card.plus2 || card.changeColor);
                isValid = matchesNumber || matchesActionCard || card.plus4;
            }
            return isValid;
        }

        public override string ToString() {
            return $"Color:{color} - Number:{number} - is+2:{plus2} - isReverse:{reverse} - isSkip:{skip} - is+4:{plus4} - isChangeColor:{changeColor}";
        }

    }
}
