using System;
using System.IO;
using System.Numerics;
using System.Collections.Generic;
using Raylib_cs;

using Color = Raylib_cs.Color;
using Rectangle = Raylib_cs.Rectangle;

namespace gamescene
{
    internal class Program
    {
        const int ScreenWidth = 1280;
        const int ScreenHeight = 720;

        const float MapWidth = 1448f;
        const float MapHeight = 1086f;

        const float HeroSpeed = 135f;
        const float MonsterSpeed = 85f;

        // 원본 맵 기준 크기. 화면에서는 높이 약 30픽셀입니다.
        const float CharacterHeight = 46f;

        const int StartNode = 0;
        const int PrincessNode = 24;

        static readonly Random Random = new Random();

        enum GameState
        {
            Start,
            Play,
            Clear
        }

        // 원본 미로 이미지의 돌길 중심점입니다.
        // 캐릭터의 발은 이 점들을 연결한 길 위에서만 움직입니다.
        static readonly Vector2[] Points =
        {
            new Vector2(195, 860),   // 0: 시작
            new Vector2(245, 780),   // 1
            new Vector2(365, 653),   // 2
            new Vector2(306, 600),   // 3
            new Vector2(232, 548),   // 4
            new Vector2(318, 491),   // 5
            new Vector2(431, 411),   // 6
            new Vector2(525, 468),   // 7
            new Vector2(628, 512),   // 8
            new Vector2(751, 558),   // 9
            new Vector2(913, 637),   // 10
            new Vector2(998, 511),   // 11
            new Vector2(907, 464),   // 12
            new Vector2(967, 380),   // 13
            new Vector2(1072, 425),  // 14
            new Vector2(1195, 468),  // 15
            new Vector2(1307, 414),  // 16
            new Vector2(1360, 365),  // 17
            new Vector2(1247, 322),  // 18
            new Vector2(1139, 278),  // 19
            new Vector2(1096, 320),  // 20
            new Vector2(1071, 350),  // 21
            new Vector2(1171, 269),  // 22
            new Vector2(1221, 202),  // 23
            new Vector2(1261, 176),  // 24: 공주
            new Vector2(643, 371),   // 25
            new Vector2(750, 420),   // 26
            new Vector2(844, 331),   // 27
            new Vector2(721, 278),   // 28
            new Vector2(661, 214),   // 29
            new Vector2(559, 165),   // 30
            new Vector2(487, 217),   // 31
            new Vector2(732, 174),   // 32
            new Vector2(938, 212),   // 33
            new Vector2(1046, 260),  // 34
            new Vector2(215, 434),   // 35
            new Vector2(185, 379),   // 36
            new Vector2(541, 563),   // 37
            new Vector2(315, 680)    // 38
        };

        // 서로 이동할 수 있는 점의 연결 관계입니다.
        static readonly int[,] Edges =
        {
            {0,1}, {1,38}, {38,2}, {2,3}, {3,4},
            {4,5}, {5,6}, {6,7}, {7,8}, {8,9}, {9,10},
            {10,11}, {11,12}, {12,13}, {13,14},
            {14,15}, {15,16}, {16,17}, {17,18},
            {18,19}, {19,20}, {20,21}, {21,13},
            {19,22}, {22,23}, {23,24},
            {7,25}, {25,26}, {26,27}, {27,28},
            {28,29}, {29,30}, {30,31}, {29,32},
            {27,33}, {33,34}, {34,20},
            {5,35}, {35,36}, {8,37}
        };

        static readonly List<int>[] Links = CreateLinks();

        class Character
        {
            public int Node;
            public int Next = -1;
            public int Previous = -1;
            public float Distance;

            public Character(int node)
            {
                Node = node;
            }

            public Vector2 Position
            {
                get
                {
                    if (Next < 0)
                        return Points[Node];

                    Vector2 direction = Vector2.Normalize(
                        Points[Next] - Points[Node]);

                    return Points[Node] + direction * Distance;
                }
            }
        }

        static void Main()
        {
            var loadedTextures = new List<Texture2D>();

            Raylib.InitWindow(ScreenWidth, ScreenHeight, "SCRUPLE");
            Raylib.SetTargetFPS(60);

            try
            {
                Texture2D startImage = LoadImage(
                    "gamestart.png", loadedTextures);

                Texture2D mapImage = LoadImage(
                    "scruplemap.png", loadedTextures);

                Texture2D clearImage = LoadImage(
                    "gameclearscene.png", loadedTextures);

                Texture2D heroImage = LoadImage(
                    "hero-cutout.png", loadedTextures);

                Texture2D princessImage = LoadImage(
                    "heroene-cutout.png", loadedTextures);

                Texture2D monsterImage = LoadImage(
                    "regret-cutout.png", loadedTextures);

                GameState state = GameState.Start;

                Character hero = new Character(StartNode);
                Character princess = new Character(PrincessNode);
                Character[] monsters = CreateMonsters();

                float protectionTime = 0f;
                bool showPaths = false;

                while (!Raylib.WindowShouldClose())
                {
                    float dt = Math.Min(
                        Raylib.GetFrameTime(), 0.05f);

                    if (Raylib.IsKeyPressed(KeyboardKey.F1))
                    {
                        showPaths = !showPaths;
                    }

                    switch (state)
                    {
                        case GameState.Start:
                            if (Raylib.IsKeyPressed(KeyboardKey.Space))
                            {
                                hero = new Character(StartNode);
                                monsters = CreateMonsters();
                                protectionTime = 0f;
                                state = GameState.Play;
                            }
                            break;

                        case GameState.Play:
                            if (Raylib.IsKeyPressed(KeyboardKey.R))
                            {
                                state = GameState.Start;
                                break;
                            }

                            protectionTime = Math.Max(
                                0f, protectionTime - dt);

                            Vector2 input = Vector2.Zero;

                            if (Raylib.IsKeyDown(KeyboardKey.W))
                                input.Y -= 1f;

                            if (Raylib.IsKeyDown(KeyboardKey.S))
                                input.Y += 1f;

                            if (Raylib.IsKeyDown(KeyboardKey.A))
                                input.X -= 1f;

                            if (Raylib.IsKeyDown(KeyboardKey.D))
                                input.X += 1f;

                            if (input != Vector2.Zero)
                            {
                                input = Vector2.Normalize(input);
                            }

                            MoveHero(hero, input, dt);

                            foreach (Character monster in monsters)
                            {
                                MoveMonster(monster, dt);
                            }

                            bool died = false;

                            // 괴물과 닿으면 시작점으로 돌아갑니다.
                            if (protectionTime <= 0f)
                            {
                                foreach (Character monster in monsters)
                                {
                                    if (Touching(hero, monster, 18f))
                                    {
                                        hero = new Character(StartNode);
                                        protectionTime = 1.5f;
                                        died = true;
                                        break;
                                    }
                                }
                            }

                            // 같은 순간 괴물과 공주에 닿으면 사망을 우선합니다.
                            if (!died && Touching(hero, princess, 20f))
                            {
                                state = GameState.Clear;
                            }
                            break;

                        case GameState.Clear:
                            if (Raylib.IsKeyPressed(KeyboardKey.Space) ||
                                Raylib.IsKeyPressed(KeyboardKey.R))
                            {
                                state = GameState.Start;
                            }
                            break;
                    }

                    Raylib.BeginDrawing();
                    Raylib.ClearBackground(Color.Black);

                    if (state == GameState.Start)
                    {
                        DrawScene(startImage);
                    }
                    else if (state == GameState.Clear)
                    {
                        DrawScene(clearImage);
                    }
                    else
                    {
                        DrawScene(mapImage);

                        if (showPaths)
                        {
                            DrawPaths();
                        }

                        // 뒤쪽 캐릭터부터 그리기 위해 발의 Y좌표로 정렬합니다.
                        var actors = new List<(
                            Character actor,
                            Texture2D texture,
                            Vector2 anchor)>();

                        actors.Add((
                            princess,
                            princessImage,
                            new Vector2(0.52f, 0.97f)));

                        foreach (Character monster in monsters)
                        {
                            actors.Add((
                                monster,
                                monsterImage,
                                new Vector2(0.5f, 0.97f)));
                        }

                        // 부활 직후 1.5초 동안 깜빡입니다.
                        bool showHero =
                            protectionTime <= 0f ||
                            (int)(protectionTime * 10f) % 2 == 0;

                        if (showHero)
                        {
                            actors.Add((
                                hero,
                                heroImage,
                                new Vector2(0.73f, 0.94f)));
                        }

                        actors.Sort((a, b) =>
                            a.actor.Position.Y.CompareTo(
                                b.actor.Position.Y));

                        foreach (var actor in actors)
                        {
                            DrawCharacter(
                                actor.texture,
                                actor.actor.Position,
                                actor.anchor);
                        }
                    }

                    Raylib.EndDrawing();
                }
            }
            catch (Exception error)
            {
                Console.WriteLine(error.Message);
                Console.WriteLine(
                    "필요한 이미지 파일의 위치를 확인하세요.");
            }
            finally
            {
                foreach (Texture2D texture in loadedTextures)
                {
                    Raylib.UnloadTexture(texture);
                }

                Raylib.CloseWindow();
            }
        }

        static List<int>[] CreateLinks()
        {
            var links = new List<int>[Points.Length];

            for (int i = 0; i < links.Length; i++)
            {
                links[i] = new List<int>();
            }

            for (int i = 0; i < Edges.GetLength(0); i++)
            {
                int a = Edges[i, 0];
                int b = Edges[i, 1];

                links[a].Add(b);
                links[b].Add(a);
            }

            return links;
        }

        static Character[] CreateMonsters()
        {
            return new Character[]
            {
                new Character(7),
                new Character(27),
                new Character(11),
                new Character(19)
            };
        }

        static void MoveHero(
            Character hero,
            Vector2 input,
            float dt)
        {
            if (input == Vector2.Zero)
                return;

            // 갈림길에서는 누른 방향과 가장 가까운 길을 선택합니다.
            if (hero.Next < 0)
            {
                float bestScore = 0.15f;

                foreach (int next in Links[hero.Node])
                {
                    Vector2 direction = Vector2.Normalize(
                        Points[next] - Points[hero.Node]);

                    float score = Vector2.Dot(input, direction);

                    if (score > bestScore)
                    {
                        bestScore = score;
                        hero.Next = next;
                    }
                }

                // 입력 방향에 길이 없으면 움직이지 않습니다.
                if (hero.Next < 0)
                    return;

                hero.Distance = 0f;
            }

            Vector2 pathDirection = Vector2.Normalize(
                Points[hero.Next] - Points[hero.Node]);

            float amount = Vector2.Dot(input, pathDirection);

            if (Math.Abs(amount) < 0.15f)
                return;

            hero.Distance += amount * HeroSpeed * dt;

            float length = Vector2.Distance(
                Points[hero.Node], Points[hero.Next]);

            if (hero.Distance >= length)
            {
                Arrive(hero);
            }
            else if (hero.Distance <= 0f)
            {
                hero.Next = -1;
                hero.Distance = 0f;
            }
        }

        static void MoveMonster(Character monster, float dt)
        {
            if (monster.Next < 0)
            {
                var candidates = new List<int>(
                    Links[monster.Node]);

                // 다른 길이 있으면 방금 지나온 길은 우선 제외합니다.
                if (candidates.Count > 1)
                {
                    candidates.Remove(monster.Previous);
                }

                monster.Next = candidates[
                    Random.Next(candidates.Count)];

                monster.Distance = 0f;
            }

            monster.Distance += MonsterSpeed * dt;

            float length = Vector2.Distance(
                Points[monster.Node], Points[monster.Next]);

            if (monster.Distance >= length)
            {
                Arrive(monster);
            }
        }

        static void Arrive(Character character)
        {
            character.Previous = character.Node;
            character.Node = character.Next;
            character.Next = -1;
            character.Distance = 0f;
        }

        static bool Touching(
            Character a,
            Character b,
            float radius)
        {
            if (Vector2.Distance(a.Position, b.Position) > radius)
                return false;

            // 같은 길 위에 있는 두 캐릭터
            if (a.Next >= 0 && b.Next >= 0)
            {
                bool sameDirection =
                    a.Node == b.Node && a.Next == b.Next;

                bool oppositeDirection =
                    a.Node == b.Next && a.Next == b.Node;

                if (sameDirection || oppositeDirection)
                    return true;
            }

            // 같은 갈림길에 접근한 캐릭터
            int[] endsA = { a.Node, a.Next };
            int[] endsB = { b.Node, b.Next };

            foreach (int x in endsA)
            {
                foreach (int y in endsB)
                {
                    if (x < 0 || x != y)
                        continue;

                    float distance =
                        Vector2.Distance(a.Position, Points[x]) +
                        Vector2.Distance(b.Position, Points[y]);

                    if (distance <= radius)
                        return true;
                }
            }

            return false;
        }

        static Vector2 ToScreen(Vector2 mapPosition)
        {
            float scale = ScreenHeight / MapHeight;
            float offsetX =
                (ScreenWidth - MapWidth * scale) / 2f;

            return new Vector2(
                offsetX + mapPosition.X * scale,
                mapPosition.Y * scale);
        }

        static void DrawCharacter(
            Texture2D texture,
            Vector2 feet,
            Vector2 anchor)
        {
            Vector2 screen = ToScreen(feet);

            float height =
                CharacterHeight * ScreenHeight / MapHeight;

            float width =
                height * texture.Width / texture.Height;

            Rectangle source = new Rectangle(
                0, 0, texture.Width, texture.Height);

            Rectangle destination = new Rectangle(
                screen.X - width * anchor.X,
                screen.Y - height * anchor.Y,
                width,
                height);

            Raylib.DrawTexturePro(
                texture,
                source,
                destination,
                Vector2.Zero,
                0f,
                Color.White);
        }

        static void DrawScene(Texture2D texture)
        {
            float scale = Math.Min(
                (float)ScreenWidth / texture.Width,
                (float)ScreenHeight / texture.Height);

            float width = texture.Width * scale;
            float height = texture.Height * scale;

            Rectangle source = new Rectangle(
                0, 0, texture.Width, texture.Height);

            Rectangle destination = new Rectangle(
                (ScreenWidth - width) / 2f,
                (ScreenHeight - height) / 2f,
                width,
                height);

            Raylib.DrawTexturePro(
                texture,
                source,
                destination,
                Vector2.Zero,
                0f,
                Color.White);
        }

        static void DrawPaths()
        {
            for (int i = 0; i < Edges.GetLength(0); i++)
            {
                Vector2 start = ToScreen(Points[Edges[i, 0]]);
                Vector2 end = ToScreen(Points[Edges[i, 1]]);

                Raylib.DrawLineEx(
                    start, end, 2f, Color.Lime);
            }

            foreach (Vector2 point in Points)
            {
                Raylib.DrawCircleV(
                    ToScreen(point), 3f, Color.Yellow);
            }
        }

        static Texture2D LoadImage(
            string fileName,
            List<Texture2D> loadedTextures)
        {
            string[] folders =
            {
                Path.Combine(
                    AppContext.BaseDirectory, "resource"),

                Path.Combine(
                    Environment.CurrentDirectory, "resource"),

                Path.GetFullPath(Path.Combine(
                    AppContext.BaseDirectory,
                    "..", "..", "..", "resource")),

                // 사용자의 기존 게임 리소스 폴더
                @"C:\Users\user\OneDrive\문서\C#Practice\26031033_seokhyunHa_Gameproject\.git\gamescene\resource",

                // 이 PC에 준비해 둔 투명 PNG 및 배경 이미지
                @"C:\Users\user\Documents\Codex\2026-09-13\durl\outputs\Scruple-paths\resource"
            };

            foreach (string folder in folders)
            {
                string path = Path.Combine(folder, fileName);

                if (!File.Exists(path))
                    continue;

                Texture2D texture = Raylib.LoadTexture(path);

                if (texture.Id == 0)
                    continue;

                loadedTextures.Add(texture);
                return texture;
            }

            throw new FileNotFoundException(
                "이미지를 찾거나 읽을 수 없습니다: " + fileName);
        }
    }
}