using Neural.Graphics;
using NeuralBasics.Math;
using SDL2;
using System;
using System.IO;

namespace Neural.UI
{
    public class Font
    {
        public readonly FontMetrics Metrics;

        readonly IntPtr _renderer;
        readonly TextureArea _textureArea;

        public static Font LoadFromChevyRayFolder(IntPtr renderer, string path)
        {
            var fontTexture = SDL_image.IMG_LoadTexture(renderer, Path.Combine(path, "atlas32.png"));
            SDL.SDL_QueryTexture(fontTexture, out _, out _, out var fontTextureWidth, out var fontTextureHeight);

            var fontMetrics = FontMetrics.FromChevyRayJson(Path.Combine(path, "metrics.json"));

            return new Font(renderer, new TextureArea(fontTexture, new Rectangle(0, 0, fontTextureWidth, fontTextureHeight)), fontMetrics);
        }

        public Font(IntPtr renderer, TextureArea textureArea, FontMetrics metrics)
        {
            _renderer = renderer;
            _textureArea = textureArea;
            Metrics = metrics;
        }

        public int GetAdvanceWithKerning(char charIndex, int previousCharIndex = -1, int scale = 1, int letterSpacing = 0)
        {
            var width = 0;

            if (!Metrics.Characters.TryGetValue(charIndex, out var charData)) charData = Metrics.Characters['?'];
            charData.Kerning.TryGetValue(previousCharIndex, out var kerning);

            if (previousCharIndex != -1) width += letterSpacing;
            width += charData.Advance + kerning;

            return width * scale;
        }

        public int MeasureText(string text, int scale = 1, int letterSpacing = 0)
        {
            var width = 0;
            var previousCharIndex = -1;

            for (var i = 0; i < text.Length; i++)
            {
                var charIndex = text[i];

                if (!Metrics.Characters.TryGetValue(charIndex, out var charData)) charData = Metrics.Characters['?'];
                charData.Kerning.TryGetValue(previousCharIndex, out var kerning);

                if (i > 0) width += letterSpacing;
                width += charData.Advance + kerning;

                previousCharIndex = charIndex;
            }

            return width * scale;
        }

        public void DrawText(int x, int y, string text, int scale = 1, int letterSpacing = 0)
        {
            var previousCharIndex = -1;
            var globalAdvance = 0;

            for (var i = 0; i < text.Length; i++)
            {
                var charIndex = text[i];
                if (!Metrics.Characters.TryGetValue(charIndex, out var charData)) charData = Metrics.Characters['?'];
                charData.Kerning.TryGetValue(previousCharIndex, out var kerning);

                var sourceRect = new SDL.SDL_Rect
                {
                    x = _textureArea.Rectangle.X + charData.SourceRectangle.X,
                    y = _textureArea.Rectangle.Y + charData.SourceRectangle.Y,
                    w = charData.SourceRectangle.Width,
                    h = charData.SourceRectangle.Height
                };

                var destRect = new SDL.SDL_Rect
                {
                    x = x + (globalAdvance + charData.Offset.X + kerning) * scale,
                    y = y + charData.Offset.Y * scale,
                    w = charData.SourceRectangle.Width * scale,
                    h = charData.SourceRectangle.Height * scale
                };

                SDL.SDL_RenderCopy(_renderer, _textureArea.Texture, ref sourceRect, ref destRect);

                globalAdvance += charData.Advance + letterSpacing + kerning;
                previousCharIndex = charIndex;
            }
        }
    }
}