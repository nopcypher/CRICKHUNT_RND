using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;

namespace CreateSquad
{
    public class Player
    {
        public string Name { get; set; }
        public string Points { get; set; }
        public string PictureUrl { get; set; }
        public string AuctionNumber { get; set; }
        public string PlayerType { get; set; }
        public string Skill1 { get; set; }
        public string Skill2 { get; set; }
    }

    public class Theme
    {
        public string Name { get; set; }
        public SKColor Primary { get; set; }
        public SKColor Secondary { get; set; }
        public SKColor Accent { get; set; }
    }

    class Program
    {
        static string Root = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", ".."));
        static string LogoCrickhunt = Path.Combine(Root, "001.png");
        static string LogoTournament = Path.Combine(Root, "btpl.jpg");
        static string OutputFolder = Path.Combine(Root, "Final_Squad_Output");

        static void Main(string[] args)
        {
            var themes = GetThemes();
            var players = GetMockData();
            if (!Directory.Exists(OutputFolder)) Directory.CreateDirectory(OutputFolder);

            Console.WriteLine("=== CRICKHUNT PRO BROADCAST GENERATOR ===");
            for (int i = 0; i < themes.Count; i++)
                Console.WriteLine((i + 1).ToString().PadRight(3) + ". " + themes[i].Name);

            Console.Write("\nSelection (Number or 'ALL'): ");
            string input = Console.ReadLine();
            string sharedTs = DateTime.Now.ToString("yyyyMMdd_HHmmss");

            if (input.Equals("ALL", StringComparison.OrdinalIgnoreCase))
                foreach (var t in themes) GenerateForTheme(players, t, sharedTs);
            else if (int.TryParse(input, out int choice) && choice >= 1 && choice <= themes.Count)
                GenerateForTheme(players, themes[choice - 1], sharedTs);

            Console.WriteLine("\nSuccess! Pro files saved in: " + OutputFolder);
            Console.ReadLine();
        }

        static SKColor GetSkillColor(Theme t)
        {
            switch (t.Name)
            {
                case "Bangalore Red":
                    return SKColors.LightGray;
                case "Chennai Gold":
                    return new SKColor(240, 240, 240);

                default:
                    return new SKColor(210, 210, 210);
            }
        }

        static void GenerateForTheme(List<Player> players, Theme t, string ts)
        {
            string safeName = t.Name.Replace(" ", "_").Replace("/", "-");

            // Portrait 1080x1920
            GenerateImage(players, 1080, 1920, Path.Combine(OutputFolder, safeName + "_Portrait_" + ts + ".png"), true, false, t);

            // Square 1500x1500
            GenerateImage(players, 1500, 1500, Path.Combine(OutputFolder, safeName + "_Square_" + ts + ".png"), true, true, t);
        }

        static void GenerateImage(List<Player> players, int w, int h, string path, bool showTeamLogo, bool showTournamentLogo, Theme theme)
        {
            using (var surface = SKSurface.Create(new SKImageInfo(w, h)))
            {
                var canvas = surface.Canvas;

                DrawComplexBroadcastBackground(canvas, w, h, theme);
                DrawProfessionalHeader(canvas, w, h, showTeamLogo, showTournamentLogo, theme);

                float pad = w * 0.01f;
                float startY = h * 0.085f;
                float gridH = h * 0.90f;

                int designSlots = 13; // small design style (like 7-13 player style)
                bool useSmallDesign = players.Count <= 16; // use 13-player layout for <=16 players
                int totalSlots = Math.Max(designSlots, players.Count); // enough slots for all players

                int cols, rows;
                if (w < h) // Portrait
                    cols = 2;
                else
                    cols = 3;

                rows = (int)Math.Ceiling((float)totalSlots / cols);

                float cardW = (w - (pad * (cols + 1))) / cols;
                float cardH = (gridH - (pad * (rows + 1))) / rows;

                // Draw actual players
                for (int i = 0; i < players.Count; i++)
                {
                    int col = i % cols;
                    int row = i / cols;
                    float x = pad + col * (cardW + pad);
                    float y = startY + row * (cardH + pad);

                    float imageScale = useSmallDesign ? 0.60f : 0.75f; // smaller scale for small design
                    DrawGlassPlayerCard(canvas, players[i], x, y, cardW, cardH, theme, imageScale);
                }

                DrawBrandedFooter(canvas, w, h);

                using (var img = surface.Snapshot())
                using (var data = img.Encode(SKEncodedImageFormat.Png, 100))
                    File.WriteAllBytes(path, data.ToArray());
            }
        }

        static void DrawComplexBroadcastBackground(SKCanvas canvas, int w, int h, Theme t)
        {
            // 1. Base Gradient
            using (var p = new SKPaint())
            {
                p.Shader = SKShader.CreateLinearGradient(new SKPoint(0, 0), new SKPoint(0, h),
                    new[] { t.Primary.WithAlpha(140), new SKColor(10, 5, 20), SKColors.Black }, null, SKShaderTileMode.Clamp);
                canvas.DrawRect(0, 0, w, h, p);
            }

            // 2. Light Watermark
            if (File.Exists(LogoCrickhunt))
            {
                using (var b = SKBitmap.Decode(LogoCrickhunt))
                using (var wp = new SKPaint { ColorFilter = SKColorFilter.CreateBlendMode(new SKColor(255, 255, 255, 12), SKBlendMode.Modulate), IsAntialias = true })
                {
                    float wmW = w * 0.85f; float sc = wmW / b.Width; float wmH = b.Height * sc;
                    canvas.DrawBitmap(b, new SKRect((w - wmW) / 2, (h - wmH) / 2, (w + wmW) / 2, (h + wmH) / 2), wp);
                }
            }

            // 3. Light-Dark Grid Pattern
            using (var gridP = new SKPaint
            {
                Color = t.Accent.WithAlpha(18), // Balanced "light-dark" opacity
                IsAntialias = true,
                StrokeWidth = 1.2f,
                Style = SKPaintStyle.Stroke
            })
            {
                float gap = w * 0.08f;
                for (float i = -h; i < w + h; i += gap)
                {
                    canvas.DrawLine(i, 0, i + h, h, gridP);
                }
                for (float i = w + h; i > -h; i -= gap)
                {
                    canvas.DrawLine(i, 0, i - h, h, gridP);
                }
            }
        }

        static void DrawProfessionalHeader(SKCanvas canvas, float w, float h, bool showTeamLogo, bool showTournamentLogo, Theme t)
        {
            float margin = w * 0.03f;
            float headerBaseY = h * 0.045f;
            float centerX = w / 2;

            float logoW = w * 0.15f;
            float logoH = h * 0.07f;

            SKRect left = DrawProportionalLogo(canvas, LogoCrickhunt, margin, headerBaseY - (logoH / 2), logoW, logoH, true, showTeamLogo);
            SKRect right = DrawProportionalLogo(canvas, LogoTournament, w - margin, headerBaseY - (logoH / 2), logoW, logoH, false, showTournamentLogo);

            centerX = left.Right + (right.Left - left.Right) / 2;

            using (var paint = new SKPaint { IsAntialias = true, TextAlign = SKTextAlign.Center })
            {
                // MAIN TITLE
                paint.Color = SKColors.White;
                paint.TextSize = h * 0.035f;
                paint.FakeBoldText = true;
                canvas.DrawText("RISING STAR MEDHA", centerX, headerBaseY + (h * 0.01f), paint);

                // SUB TITLE
                paint.TextSize = h * 0.017f;
                paint.FakeBoldText = false;

                // Slightly brighter silver (natural look)
                paint.Color = SKColors.Silver;

                switch (t.Name)
                {
                    case "Bangalore Red":
                        paint.Color = SKColors.LightGray;
                        break;
                    case "Chennai Gold":
                        paint.Color = SKColors.Gray;
                        break;
                    case "Hyderabad Orange":
                        paint.Color = new SKColor(60, 60, 60);
                        break;
                }
                canvas.DrawText("BOTAD TALUKA PREMIER LEAGUE - SEASON 4", centerX, headerBaseY + (h * 0.028f), paint);
            }

            // DIVIDER POSITION (balanced spacing)
            float divY = headerBaseY + (h * 0.038f);
            float divHeight = h * 0.0025f;

            using (var glassP = new SKPaint
            {
                Color = new SKColor(255, 255, 255, 30),
                IsAntialias = true
            })
            {
                canvas.DrawRect(new SKRect(margin, divY, w - margin, divY + divHeight), glassP);
            }

            using (var glowP = new SKPaint
            {
                Color = t.Accent.WithAlpha(180),
                StrokeWidth = 2,
                IsAntialias = true
            })
            {
                canvas.DrawLine(
                    margin,
                    divY + (divHeight / 2),
                    w - margin,
                    divY + (divHeight / 2),
                    glowP);
            }
        }

        static void DrawGlassPlayerCard(SKCanvas canvas, Player p, float x, float y, float w, float h, Theme t, float imageScale = 0.75f)
        {
            float topPadding = h * 0.05f; // 5% of height as top spacing

            // Draw card background
            var rect = new SKRect(x, y + topPadding, x + w, y + h);
            using (var bg = new SKPaint { Color = new SKColor(255, 255, 255, 35), IsAntialias = true })
                canvas.DrawRoundRect(rect, 20, 20, bg);
            using (var border = new SKPaint { Color = t.Secondary.WithAlpha(80), Style = SKPaintStyle.Stroke, StrokeWidth = 2, IsAntialias = true })
                canvas.DrawRoundRect(rect, 20, 20, border);

            // Draw player image with scale factor
            float imageSize = h * imageScale;
            float imageX = x + (w * 0.05f) + (imageSize / 2);
            float imageY = y + topPadding + (h / 2);
            DrawCircularImage(canvas, p.PictureUrl, imageX, imageY, imageSize);

            // AuctionNumber badge
            float badgeSize = h * 0.20f;
            float badgeX = imageX - (imageSize / 2) - (badgeSize * 0.2f);
            float badgeY = imageY - (imageSize / 2) - (badgeSize * 0.25f);

            var badgeCenter = new SKPoint(badgeX + badgeSize / 2, badgeY + badgeSize / 2);
            SKColor badgeColor = t.Secondary;

            switch (t.Name)
            {
                case "Bangalore Red":
                    badgeColor = new SKColor(80, 80, 80);
                    break;
                case "Crimson Silver":
                    badgeColor = new SKColor(130, 130, 130);
                    break;
                case "Forest Green":
                    badgeColor = new SKColor(70, 170, 70);
                    break;
                case "Hyderabad Orange":
                    badgeColor = new SKColor(115, 115, 115);
                    break;
                case "Electric Blue":
                    badgeColor = new SKColor(20, 40, 140);
                    break;
            }

            using (var badgePaint = new SKPaint { Color = badgeColor, IsAntialias = true })
            {
                canvas.DrawCircle(badgeCenter, badgeSize / 2, badgePaint);

                using (var textPaint = new SKPaint
                {
                    Color = SKColors.White,
                    TextSize = badgeSize * 0.6f,
                    TextAlign = SKTextAlign.Center,
                    FakeBoldText = true,
                    IsAntialias = true
                })
                {
                    var fm = textPaint.FontMetrics;
                    float textY = badgeCenter.Y - ((fm.Ascent + fm.Descent) / 2);
                    canvas.DrawText(p.AuctionNumber, badgeCenter.X, textY, textPaint);
                }
            }

            // Text position
            float textX = x + (w * 0.05f) + imageSize + (w * 0.04f);
            float maxTextW = (x + w) - textX - (w * 0.06f);
            float textOffset = h * 0.07f; // move content upward

            using (var tp = new SKPaint { Color = SKColors.White, IsAntialias = true, FakeBoldText = true })
            {
                tp.TextSize = h * 0.13f;
                string[] words = SplitNameStrict(p.Name, maxTextW, tp);

                if (words.Length > 1)
                {
                    canvas.DrawText(words[0], textX, y + topPadding + (h * 0.30f) - textOffset, tp);
                    canvas.DrawText(words[1], textX, y + topPadding + (h * 0.45f) - textOffset, tp);
                }
                else
                    canvas.DrawText(p.Name, textX, y + topPadding + (h * 0.40f) - textOffset, tp);

                tp.FakeBoldText = false;
                tp.TextSize = h * 0.10f;
                tp.Color = GetSkillColor(t);
                canvas.DrawText(p.Skill1, textX, y + topPadding + (h * 0.60f) - textOffset, tp);
                canvas.DrawText(p.Skill2, textX, y + topPadding + (h * 0.72f) - textOffset, tp);

                tp.Color = t.Accent;
                tp.TextSize = h * 0.11f;
                tp.FakeBoldText = true;
                canvas.DrawText("PTS: " + p.Points + " | " + p.PlayerType, textX, y + topPadding + (h * 0.88f) - textOffset, tp);
            }
        }

        static void DrawBrandedFooter(SKCanvas canvas, int w, int h)
        {
            string p1 = "Made with ";
            string p2 = " by CRICKHUNT";
            float footerY = h * 0.99f;

            using (var paint = new SKPaint { Color = SKColors.White, TextSize = h * 0.015f, IsAntialias = true, FakeBoldText = true })
            {
                float w1 = paint.MeasureText(p1);
                float hHeight = h * 0.011f;
                float hWidth = hHeight * 2.7f;
                float w2 = paint.MeasureText(p2);
                float logoH = h * 0.025f;

                float totalW = w1 + hWidth + w2 + (w * 0.01f) + (logoH * 2.5f);
                float startX = (w - totalW) / 2;

                canvas.DrawText(p1, startX, footerY, paint);

                using (var hp = new SKPaint { Color = SKColors.Red, IsAntialias = true, Style = SKPaintStyle.Fill })
                {
                    float hX = startX + w1 + (hWidth / 2);
                    float hY = footerY - (hHeight * 0.5f);
                    var path = new SKPath();
                    path.MoveTo(hX, hY + (hHeight * 0.4f));
                    path.CubicTo(hX - hWidth * 0.5f, hY - hHeight * 0.8f, hX, hY - hHeight * 1.1f, hX, hY - hHeight * 0.4f);
                    path.MoveTo(hX, hY + (hHeight * 0.4f));
                    path.CubicTo(hX + hWidth * 0.5f, hY - hHeight * 0.8f, hX, hY - hHeight * 1.1f, hX, hY - hHeight * 0.4f);
                    canvas.DrawPath(path, hp);
                }

                canvas.DrawText(p2, startX + w1 + hWidth, footerY, paint);

                if (File.Exists(LogoCrickhunt))
                {
                    using (var b = SKBitmap.Decode(LogoCrickhunt))
                    {
                        float sc = logoH / b.Height;
                        float sw = b.Width * sc;
                        canvas.DrawBitmap(b, new SKRect(startX + w1 + hWidth + w2 + (w * 0.01f), footerY - (logoH * 0.8f), startX + w1 + hWidth + w2 + (w * 0.01f) + sw, footerY + (logoH * 0.2f)));
                    }
                }
            }
        }

        static SKRect DrawProportionalLogo(SKCanvas canvas, string path, float x, float y, float maxW, float maxH, bool isLeftLogo, bool showLogo)
        {
            if (!showLogo || !File.Exists(path))
                return new SKRect(x, y, x, y);

            using (var b = SKBitmap.Decode(path))
            {
                float sc = Math.Min(maxW / b.Width, maxH / b.Height);
                float sw = b.Width * sc;
                float sh = b.Height * sc;

                // Align left or right
                float fx = isLeftLogo ? x : x - sw;
                float fy = y + (maxH - sh) / 2;

                SKRect dest = new SKRect(fx, fy, fx + sw, fy + sh);
                canvas.DrawBitmap(b, dest, new SKPaint { IsAntialias = true, FilterQuality = SKFilterQuality.High });
                return dest;
            }
        }

        static void DrawCircularImage(SKCanvas canvas, string url, float cx, float cy, float size)
        {
            canvas.Save();
            using (var path = new SKPath())
            {
                path.AddCircle(cx, cy, size / 2); canvas.ClipPath(path, antialias: true);
                try
                {
                    using (WebClient client = new WebClient())
                    {
                        byte[] data = client.DownloadData(url);
                        using (var bitmap = SKBitmap.Decode(data)) canvas.DrawBitmap(bitmap, new SKRect(cx - size / 2, cy - size / 2, cx + size / 2, cy + size / 2));
                    }
                }
                catch { using (var p = new SKPaint { Color = SKColors.Gray }) canvas.DrawCircle(cx, cy, size / 2, p); }
            }
            canvas.Restore();
        }

        static string[] SplitNameStrict(string name, float maxWidth, SKPaint paint)
        {
            // Set default smaller text size
            paint.TextSize = 25f;

            // If name fits, return one line
            if (paint.MeasureText(name) <= maxWidth)
                return new string[] { name };

            // Split into words
            string[] words = name.Split(' ');
            if (words.Length < 2) return new string[] { name };

            int mid = words.Length / 2;
            string l1 = string.Join(" ", words, 0, mid);
            string l2 = string.Join(" ", words, mid, words.Length - mid);

            // Reduce slightly if still too wide
            while ((paint.MeasureText(l1) > maxWidth || paint.MeasureText(l2) > maxWidth) && paint.TextSize > 6f)
            {
                paint.TextSize -= 0.5f;
            }

            return new string[] { l1, l2 };
        }

        static List<Theme> GetThemes()
        {
            return new List<Theme> {
                new Theme { Name = "Mumbai Blue", Primary = new SKColor(0, 75, 160), Secondary = new SKColor(210, 170, 0), Accent = new SKColor(255, 235, 59) },
                new Theme { Name = "Chennai Gold", Primary = new SKColor(235, 180, 0), Secondary = new SKColor(0, 100, 180), Accent = SKColors.Yellow},
                new Theme { Name = "Bangalore Red", Primary = new SKColor(180, 0, 0), Secondary = new SKColor(40, 40, 40), Accent = new SKColor(255, 215, 0) },
                new Theme { Name = "Classic Gold/Black", Primary = new SKColor(30, 30, 30), Secondary = new SKColor(212, 175, 55), Accent = new SKColor(255, 215, 0) },
                new Theme { Name = "Kolkata Purple", Primary = new SKColor(60, 20, 100), Secondary = new SKColor(180, 150, 0), Accent = SKColors.White },
                new Theme { Name = "Gujarat Pink/Blue", Primary = new SKColor(20, 50, 100), Secondary = new SKColor(230, 0, 120), Accent = SKColors.Cyan },
                new Theme { Name = "Rajasthan Pink", Primary = new SKColor(230, 0, 120), Secondary = new SKColor(0, 50, 120), Accent = SKColors.Yellow },
                new Theme { Name = "Hyderabad Orange", Primary = new SKColor(255, 100, 0), Secondary = new SKColor(40, 40, 40), Accent = SKColors.White },
                new Theme { Name = "Lucknow Cyan", Primary = new SKColor(0, 150, 180), Secondary = new SKColor(255, 120, 0), Accent = SKColors.White },
                new Theme { Name = "Punjab Silver", Primary = new SKColor(200, 0, 0), Secondary = new SKColor(192, 192, 192), Accent = SKColors.White },
                new Theme { Name = "Delhi Blue/Red", Primary = new SKColor(0, 80, 180), Secondary = new SKColor(200, 0, 0), Accent = SKColors.White },
                new Theme { Name = "Neon Green", Primary = new SKColor(20, 60, 20), Secondary = new SKColor(50, 255, 50), Accent = SKColors.White },
                new Theme { Name = "Electric Blue", Primary = new SKColor(10, 10, 80), Secondary = new SKColor(20, 40, 140),  Accent = SKColors.White },
                new Theme { Name = "Sunset Orange", Primary = new SKColor(100, 40, 0), Secondary = new SKColor(255, 165, 0), Accent = SKColors.Yellow },
                new Theme { Name = "Midnight Purple", Primary = new SKColor(30, 0, 50), Secondary = new SKColor(200, 100, 255), Accent = SKColors.White },
                new Theme { Name = "Forest Green", Primary = new SKColor(0, 50, 0), Secondary = new SKColor(150, 255, 150), Accent = SKColors.Yellow },
                new Theme { Name = "Crimson Silver", Primary = new SKColor(120, 0, 20), Secondary = new SKColor(160, 160, 160), Accent = SKColors.White },
                new Theme { Name = "Ocean Teal", Primary = new SKColor(0, 60, 60), Secondary = new SKColor(0, 200, 200), Accent = SKColors.Yellow },
                new Theme { Name = "Royal White", Primary = new SKColor(240, 240, 240), Secondary = new SKColor(180, 140, 0), Accent = new SKColor(40, 40, 40) },
                new Theme { Name = "Stealth Grey", Primary = new SKColor(50, 50, 50), Secondary = new SKColor(100, 100, 100), Accent = SKColors.Red }
            };
        }

        static List<Player> GetMockData()
        {
            var list = new List<Player>();
            for (int i = 1; i <= 20; i++)
            {
                list.Add(new Player
                {
                    Name = i == 1 ? "PALADIYA RAJUBHAI BHAGVANBHAI PATEL" : "PLAYER NAME " + i,
                    Points = (i * 10).ToString(),
                    PictureUrl = "https://crickhunt.com/images/thumbs/0039993_user_Pic_262663.png-4_300.webp",
                    AuctionNumber = i.ToString(),
                    PlayerType = i < 3 ? "OWNER" : "ICON",
                    Skill1 = "Right-Hand Bat",
                    Skill2 = "Wicket Keeper"
                });
            }
            return list;
        }
    }
}