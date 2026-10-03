using UnityEngine;

namespace OrbitGuard
{
    // Tiny shared textures; no downloads, external art or per-enemy materials.
    public static class PixelArt
    {
        public static readonly Color Mint = new Color(.35f, 1, .83f);
        public static readonly Color Coral = new Color(1, .36f, .48f);
        public static readonly Color Gold = new Color(1, .79f, .37f);
        public static readonly Color Violet = new Color(.68f, .5f, 1);
        public static Sprite Make(params string[] rows)
        {
            int w = rows[0].Length, h = rows.Length;
            var texture = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) pixels[(h - 1 - y) * w + x] = rows[y][x] == '.' ? Color.clear : rows[y][x] == 'o' ? new Color(.05f, .12f, .23f) : rows[y][x] == '+' ? Color.white : Color.white;
            texture.SetPixels(pixels); texture.Apply(false, true);
            return Sprite.Create(texture, new Rect(0, 0, w, h), new Vector2(.5f, .5f), w);
        }
        public static Sprite Ship() => Make(".....#.....", "....###....", "....#+#....", "....#+#....", ".#.#####.#.", "###########", "###o###o###", "##..###..##", "....###....");
        public static Sprite Crab() => Make("..#.....#..", "...#...#...", "..#######..", ".##o###o##.", "###########", "#.#######.#", "#.#.....#.#", "...##.##...");
        public static Sprite Squid() => Make("....###....", "..#######..", ".#########.", "###oo#oo###", ".#########.", "..#.#.#.#..", ".#.#...#.#.", "#.........#");
        public static Sprite Beetle() => Make("...#####...", ".#########.", "###o###o###", "###########", "..##o#o##..", ".##.#.#.##.", "##.......##", ".#.......#.");
    }
}
