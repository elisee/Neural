namespace Neural.UI
{
    public class FontStyle
    {
        public Font Font;
        public int Scale = 1;
        public int LetterSpacing = 0;
        public int LineSpacing = 0;

        public FontStyle(Font font)
        {
            Font = font;
        }

        public int GetAdvanceWithKerning(char charIndex, int previousCharIndex) => Font.GetAdvanceWithKerning(charIndex, previousCharIndex, Scale, LetterSpacing);
        public int MeasureText(string text) => Font.MeasureText(text, Scale, LetterSpacing);
        public void DrawText(int x, int y, string text) => Font.DrawText(x, y + Size, text, Scale, LetterSpacing);
        public int Ascent => Font.Metrics.Ascent * Scale;
        public int Size => (Font.Metrics.Ascent - Font.Metrics.Descent) * Scale;
        public int LineHeight => Size + LineSpacing;
    }
}
