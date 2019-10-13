using Neural.Math;
using System.Collections.Generic;
using System.IO;
using System.Json;

namespace Neural.UI
{
    public class FontMetrics
    {
        public string Name;
        public int Size;
        public int Ascent;
        public int Descent;

        public readonly Dictionary<int, CharacterData> Characters = new Dictionary<int, CharacterData>();

        public class CharacterData
        {
            public int Advance;
            public Point Offset;
            public Rectangle SourceRectangle;
            public readonly Dictionary<int, int> Kerning = new Dictionary<int, int>();
        }

        // See http://pixel-fonts.com
        public static FontMetrics FromChevyRayJson(string path)
        {
            var json = JsonValue.Parse(File.ReadAllText(path));

            var metrics = new FontMetrics
            {
                Name = json["name"],
                Size = json["size"],
                Ascent = json["ascent"],
                Descent = json["descent"],
            };

            int charCount = json["char_count"];
            int kerningCount = json["kerning_count"];

            {
                var chars = json["chars"];
                var advance = json["advance"];
                var offset_x = json["offset_x"];
                var offset_y = json["offset_y"];
                var width = json["width"];
                var height = json["height"];
                var pack_x = json["pack_x"];
                var pack_y = json["pack_y"];
                var kerning = json["kerning"];

                for (var i = 0; i < charCount; i++)
                {
                    int asciiIndex = chars[i];

                    metrics.Characters.Add(asciiIndex, new CharacterData
                    {
                        Advance = advance[i],
                        Offset = new Point(offset_x[i], offset_y[i]),
                        SourceRectangle = new Rectangle(pack_x[i], pack_y[i], width[i], height[i])
                    });
                }

                for (var i = 0; i < kerningCount; i++)
                {
                    int leftAsciiIndex = kerning[i * 3 + 0];
                    int rightAsciiIndex = kerning[i * 3 + 1];
                    int offset = kerning[i * 3 + 2];

                    metrics.Characters[rightAsciiIndex].Kerning.Add(leftAsciiIndex, offset);
                }

                return metrics;
            }
        }
    }
}
