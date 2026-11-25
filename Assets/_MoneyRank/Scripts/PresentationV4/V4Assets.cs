using System.Collections.Generic;
using MoneyRank.Domain.V4;
using UnityEngine;

namespace MoneyRank.V4.Presentation
{
    /// <summary>Loads the shipped V4 content (CSV is the single source) and the tile art from Resources.</summary>
    public static class V4Assets
    {
        private static V4ContentSet _content;
        private static readonly Dictionary<string, Texture2D> Textures = new Dictionary<string, Texture2D>();

        public static V4ContentSet Content
        {
            get
            {
                if (_content == null) _content = V4ContentLoader.Load(ReadSources());
                return _content;
            }
        }

        public static V4ContentLoader.RawSources ReadSources() => new V4ContentLoader.RawSources
        {
            Casas = Text("casas"),
            Opcoes = Text("cartas_opcoes"),
            Personagens = Text("personagens"),
            Progressao = Text("progressao"),
            Dificuldades = Text("dificuldades"),
            Atributos = Text("atributos"),
            Moedas = Text("moedas"),
            EventosSistema = Text("eventos_sistema"),
            RotasLudo = Text("rotas_ludo"),
            ParametrosJson = Text("parametros")
        };

        private static string Text(string name)
        {
            var asset = Resources.Load<TextAsset>("V4/Data/" + name);
            if (asset == null) throw new System.InvalidOperationException("Missing V4 data: Resources/V4/Data/" + name);
            return asset.text;
        }

        public static Texture2D Icon(string houseId)
        {
            Texture2D tex;
            if (Textures.TryGetValue(houseId, out tex)) return tex;
            tex = Resources.Load<Texture2D>("V4/Art/icones/" + houseId);
            Textures[houseId] = tex;
            return tex;
        }

        private static readonly Dictionary<string, Sprite> Sprites = new Dictionary<string, Sprite>();

        public static Sprite IconSprite(string houseId)
        {
            Sprite s;
            if (Sprites.TryGetValue(houseId, out s)) return s;
            var tex = Icon(houseId);
            s = tex == null ? null : Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            Sprites[houseId] = s;
            return s;
        }

        public static Color HouseColor(string houseId)
        {
            Color c;
            return ColorUtility.TryParseHtmlString(Content.House(houseId).ColorHex, out c) ? c : Color.gray;
        }

        public static Color PawnColor(string color)
        {
            switch (color)
            {
                case "verde": return new Color32(0x2D, 0xB8, 0x4B, 255);
                case "vermelho": return new Color32(0xFF, 0x30, 0x38, 255);
                case "azul": return new Color32(0x3A, 0xA8, 0xFF, 255);
                default: return new Color32(0xFF, 0xD0, 0x00, 255);
            }
        }
    }
}
