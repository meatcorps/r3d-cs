using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

// Port of r3d/examples/sprite_surface.c: spritesheet source rect, flip, and 3D surface rendering.
public static class SpriteSurface
{
    public static int Main()
    {
        const int width = 1280, height = 720;
        const int frameWidth = 64, frameHeight = 64, frameCount = 4;
        const float animationFps = 10;
        var backdrop = new Color(13, 19, 33, 255);

        InitWindow(width, height, "[r3d] Sprite Surface - Texture3D (C#)");
        SetTargetFPS(60);
        if (!R3D.Init(width, height))
        {
            CloseWindow();
            return 1;
        }

        Texture2D sheet = R3D.LoadTextureEx(
            "resources/images/spritesheet.png",
            TextureWrap.Clamp,
            TextureFilter.Point,
            true);
        if (sheet.Id == 0 || !R3D.InitTexture3DResources())
        {
            if (sheet.Id != 0) R3D.UnloadTexture(sheet);
            R3D.UnloadTexture3DResources();
            R3D.Close();
            CloseWindow();
            return 1;
        }

        R3D.SetEnvironmentEx((ref env) => env.Background.Color = backdrop);
        var camera = new Camera3D
        {
            Position = new Vector3(0, 0, 9), Target = Vector3.Zero,
            Up = Vector3.UnitY, FovY = 45, Projection = CameraProjection.Perspective
        };
        var options = R3D.DefaultTextureDrawOptions();
        options.AspectMode = TextureAspectMode.Stretch;
        options.TransparencyMode = TransparencyMode.Blend;
        options.BillboardMode = BillboardMode.Front;
        options.CullMode = CullMode.None;

        int frame = 0;
        float frameTimer = 0;
        bool paused = false, flipped = false;
        while (!WindowShouldClose())
        {
            if (IsKeyPressed(KeyboardKey.Space)) paused = !paused;
            if (IsKeyPressed(KeyboardKey.F)) flipped = !flipped;
            if (IsKeyPressed(KeyboardKey.Right))
            {
                frame = (frame + 1) % frameCount;
                frameTimer = 0;
                paused = true;
            }

            if (IsKeyPressed(KeyboardKey.Left))
            {
                frame = (frame + frameCount - 1) % frameCount;
                frameTimer = 0;
                paused = true;
            }

            if (!paused)
            {
                frameTimer += GetFrameTime();
                if (frameTimer >= 1.0f / animationFps)
                {
                    frameTimer -= 1.0f / animationFps;
                    frame = (frame + 1) % frameCount;
                }
            }

            Rectangle source = new(frame * frameWidth, 0, frameWidth, frameHeight);
            if (flipped)
            {
                source.X += source.Width;
                source.Width = -source.Width;
            }

            BeginDrawing();
            ClearBackground(backdrop);
            R3D.Begin(camera);
            R3D.DrawTexture3DPro(sheet, source, new Vector3(0, 1.1f, 0), Quaternion.Identity,
                new Vector2(2.5f), new Vector2(0.5f), options);
            for (int i = 0; i < frameCount; i++)
            {
                Rectangle section = new(i * frameWidth, 0, frameWidth, frameHeight);
                R3D.DrawTexture3DPro(sheet, section, new Vector3(-2.7f + i * 1.8f, -1.5f, 0),
                    Quaternion.Identity, new Vector2(1.1f), new Vector2(0.5f), options);
            }

            R3D.End();

            DrawText("MEATCORPS / R3D - KISS", 25, 22, 28, Color.RayWhite);
            DrawText("TEXTURE3D SOURCE RECT VALIDATION", 25, 57, 18, Color.SkyBlue);
            DrawText(
                $"Frame: {frame + 1} / {frameCount}     Source: ({source.X:0}, {source.Y:0}, {source.Width:0}, {source.Height:0})",
                25, 91, 18, Color.LightGray);
            DrawText($"SPACE PAUSE   LEFT/RIGHT FRAME   F FLIP [{(flipped ? "ON" : "OFF")}]", 25, 121, 17, Color.Gray);
            int atlasX = (width - sheet.Width * 2) / 2;
            int atlasY = height - 148;
            DrawText("ORIGINAL SPRITESHEET / SELECTED FRAME", atlasX, atlasY - 27, 16, Color.LightGray);
            DrawTextureEx(sheet, new Vector2(atlasX, atlasY), 0, 2, Color.White);
            DrawRectangleLinesEx(
                new Rectangle(atlasX + frame * frameWidth * 2, atlasY, frameWidth * 2, frameHeight * 2), 3,
                Color.Yellow);
            DrawFPS(width - 105, 25);
            EndDrawing();
        }

        R3D.UnloadTexture3DResources();
        R3D.UnloadTexture(sheet);
        R3D.Close();
        CloseWindow();
        return 0;
    }
}
