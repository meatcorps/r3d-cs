using System;
using System.Numerics;
using System.Runtime.InteropServices;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using Material = R3D_cs.Material;
using Mesh = R3D_cs.Mesh;

namespace Examples;

// Port of r3d/examples/line_showroom.c: persistent screen/world paths, updates and projected overlay.
public static unsafe class LineShowroom
{
    private const int W = 1600, H = 900, StreamMax = 512, EllipseCount = 48;
    private static readonly string[] Names = ["ORBITAL GLYPHS", "FLOW FIELD", "SHAPE LAB"];
    private static readonly string[] Details =
    [
        "3D orbital rings / pixel-constant lines / depth",
        "Textured world ribbons / animated gradients / HDR",
        "Rectangles / circles / ellipses / arcs / live updates"
    ];
    private static readonly Color[] Accents = [new(55,214,248,255), new(248,99,180,255), new(255,188,78,255)];

    // The current generated LinePath exposes its points/count as internal fields.
    // This local layout mirror lets the example provide a caller-owned pointer without changing generated code.
    [StructLayout(LayoutKind.Sequential)]
    private struct NativeLinePath
    {
        public LinePoint* Points;
        public int Count;
        public byte Closed;
        public LineStyle Style;
    }

    private static LinePath MakePath(LinePoint* points, int count, bool closed, LineStyle style)
    {
        NativeLinePath native = new() { Points = points, Count = count, Closed = (byte)(closed ? 1 : 0), Style = style };
        return *(LinePath*)&native;
    }

    private static LineStyle Style(bool screen, float width) => new()
    {
        Width = width,
        WidthMode = screen ? LineWidthMode.Screen : LineWidthMode.World,
        Normal = Vector3.UnitZ,
        MiterLimit = 4,
        Caps = LineCapFlags.LineCapBoth
    };

    private static void SetGradient(LinePoint* points, int count, float offset)
    {
        for (int i = 0; i < count; ++i)
            points[i].Color = ColorFromHSV((offset + (float)i / count * 320) % 360, 0.85f, 1);
    }

    private static void AnimateStream(LinePoint* points, int count, float seconds, bool screen)
    {
        for (int i = 0; i < count; ++i)
        {
            float t = (float)i / (count - 1);
            float wave = MathF.Sin(t * 19 - seconds * 2.1f);
            float detail = MathF.Sin(t * 37 + seconds * 1.2f);
            float envelope = 0.6f + 0.4f * MathF.Sin(t * MathF.PI);
            float variation = 0.5f + 0.5f * MathF.Sin(t * 13 + seconds * 1.3f);
            points[i].Position = new Vector3(-4.15f + t * 8.3f,
                (wave * 0.38f + detail * 0.14f) * envelope,
                MathF.Sin(t * 10 + seconds * 0.8f) * 0.18f);
            points[i].Width = screen ? 5f + variation * 15f : 0.065f + variation * 0.18f;
            points[i].Color = ColorFromHSV((190f + t * 290f + seconds * 22f) % 360f, 0.9f, 1f);
            points[i].ArcToNext = default;
        }
    }

    private static Mesh WorldLine(LinePoint* points, int n, bool closed, float width)
    {
        var path = MakePath(points, n, closed, Style(false, width));
        return R3D.GenMeshLinePath(ref path);
    }

    private static Mesh ScreenLine(LinePoint* points, int n, bool closed, float width)
    {
        var path = MakePath(points, n, closed, Style(true, width));
        return R3D.GenMeshScreenLinePath(ref path);
    }

    private static void DrawWorld(Mesh mesh, Material material, Matrix4x4 transform, bool glow, BuiltRenderMode mode)
    {
        if (glow) R3D.PushBuiltRenderMode(mode);
        R3D.DrawMeshPro(mesh, material, transform);
        if (glow) R3D.PopBuiltRenderMode();
    }

    private static void Hud(int scene, bool paused, bool orbit, bool bloom, bool glow, bool overlay, int count, float scale, double updateMs)
    {
        Color accent = Accents[scene], muted = new(138,158,182,255), soft = new(213,230,243,255);
        DrawRectangle(0, 0, 322, H, new Color(7, 13, 27, 245));
        DrawRectangle(321, 0, 2, H, Fade(accent, 0.38f));
        DrawText("MEATCORPS / R3D", 30, 29, 21, soft);
        DrawRectangle(30, 70, 43, 3, accent);
        DrawText("LINE", 27, 106, 58, Color.RayWhite);
        DrawText("ATLAS", 29, 166, 58, accent);
        DrawText("DYNAMIC GEOMETRY SHOWROOM", 30, 236, 15, muted);
        for (int i = 0; i < Names.Length; ++i)
        {
            int y = 306 + i * 76;
            bool selected = i == scene;
            if (selected) DrawRectangle(18, y - 12, 287, 55, Fade(accent, 0.16f));
            DrawRectangle(29, y, 3, 29, selected ? accent : new Color(52,70,93,255));
            DrawText($"0{i + 1}", 44, y + 1, 17, selected ? accent : muted);
            DrawText(Names[i], 81, y, 19, selected ? Color.RayWhite : muted);
        }
        DrawText("LIVE GEOMETRY", 30, 629, 18, accent);
        DrawText($"{count} source points", 30, 662, 20, soft);
        DrawText($"Update + upload: {updateMs:F3} ms", 30, 693, 17, muted);
        DrawText($"{GetFPS()} FPS / x{scale:F2}", 30, 721, 18, soft);
        DrawText($"BLOOM {(bloom ? "ON" : "OFF")} / GLOW {(glow ? "ON" : "OFF")}", 30, 765, 16, accent);
        DrawText("PERSISTENT GPU MESHES", 30, 828, 15, muted);
        DrawRectangle(322, 0, W - 322, 95, new Color(7,13,27,185));
        DrawText(Names[scene], 375, 25, 33, Color.RayWhite);
        DrawText(Details[scene], 376, 65, 17, accent);
        DrawRectangle(322, H - 98, W - 322, 98, new Color(7,13,27,225));
        DrawRectangle(363, H - 99, 120, 3, accent);
        DrawText("LEFT/RIGHT SCENE    SPACE PAUSE    O CAMERA    B BLOOM    G GLOW", 367, H - 75, 17, soft);
        DrawText("N RESOLUTION    UP/DOWN PIXEL SCALE    P RAYLIB 2D    T ARC", 367, H - 43, 16, muted);
        DrawText(orbit ? "ORBIT ON" : "ORBIT OFF", W - 166, 26, 16, accent);
        DrawText(paused ? "PAUSED" : "ANIMATING", W - 170, 52, 16, muted);
        DrawText($"BLOOM {(bloom ? "ON" : "OFF")} / 2D {(overlay ? "ON" : "OFF")}", W - 280, H - 31, 15, accent);
    }

    public static int Main()
    {
        InitWindow(W, H, "[r3d] Meatcorps - Line Atlas (C#)");
        SetTargetFPS(60);
        R3D.SetHint(Hint.MeshVertexBufferCapacity, 262144);
        R3D.SetHint(Hint.MeshIndexBufferCapacity, 262144);
        if (!R3D.Init(W, H)) { CloseWindow(); return 1; }
        R3D.SetAntiAliasingMode(AntiAliasingMode.Fxaa);
        R3D.SetAntiAliasingPreset(AntiAliasingPreset.High);
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = new Color(5,9,21,255);
            env.Ambient.Color = new Color(35,55,82,255);
            env.Tonemap.Mode = Tonemap.Aces;
            env.Bloom.Mode = R3D_cs.Bloom.Additive;
            env.Bloom.Threshold = 0.70f;
            env.Bloom.SoftThreshold = 0.70f;
            env.Bloom.Levels = 1.0f;
            env.Bloom.FilterRadius = 1.4f;
        });
        var camera = new Camera3D
        {
            Position = new Vector3(0.7f, 2.4f, 12.5f), Target = new Vector3(0.7f, 0, 0),
            Up = Vector3.UnitY, FovY = 48, Projection = CameraProjection.Perspective
        };
        var light = R3D.CreateOmniLight(new Vector3(-2, 3.5f, 4.5f), 20, new Color(105,180,255,255), 2.5f);
        var screenMat = R3D.GetDefaultScreenLineMaterial();
        screenMat.Emission.Color = new Color(160,210,255,255);
        screenMat.Emission.Energy = 2.8f;
        var worldMat = R3D.GetDefaultMaterial();
        worldMat.CullMode = CullMode.None;
        worldMat.Unlit = false;
        worldMat.Emission.Color = new Color(100,155,255,255);
        worldMat.Emission.Energy = 1.7f;
        var sphereMat = R3D.GetDefaultMaterial();
        sphereMat.Albedo.Color = new Color(12,21,43,255);
        sphereMat.Emission.Color = new Color(22,73,130,255);
        sphereMat.Emission.Energy = 1.35f;
        var image = GenImageChecked(128, 128, 16, 16, new Color(245,245,255,255), new Color(45,65,105,255));
        var checker = LoadTextureFromImage(image);
        UnloadImage(image);
        SetTextureWrap(checker, TextureWrap.Repeat);
        var texturedMat = worldMat;
        texturedMat.Albedo.Texture = checker;
        var glowMode = R3D.RenderModeBuildOuterGlow(new Color(71,205,255,255), 17, 3.5f, 3.2f);
        var screenEffect = R3D.LoadScreenLineEffect();
        var sphere = R3D.GenMeshSphere(0.82f, 48, 48);

        // stackalloc keeps the source arrays fixed for the entire demo lifetime.
        LinePoint* orbit = stackalloc LinePoint[4];
        new Span<LinePoint>(orbit, 4).Clear();
        R3D.SetLineCircle(orbit, Vector3.Zero, 2);
        SetGradient(orbit, 4, 165);
        orbit[0].Width = 8; orbit[1].Width = 15; orbit[2].Width = 8; orbit[3].Width = 20;
        var orbitMesh = ScreenLine(orbit, 4, true, 12);

        LinePoint* ringPoints = stackalloc LinePoint[4];
        new Span<LinePoint>(ringPoints, 4).Clear();
        R3D.SetLineCircle(ringPoints, Vector3.Zero, 1.35f);
        SetGradient(ringPoints, 4, 5);
        var ring = WorldLine(ringPoints, 4, true, 0.12f);

        LinePoint* rectangle = stackalloc LinePoint[4];
        new Span<LinePoint>(rectangle, 4).Clear();
        R3D.SetLineRectangle(rectangle, Vector3.Zero, new Vector2(1.55f, 1.1f));
        SetGradient(rectangle, 4, 185);
        var rectangleMesh = WorldLine(rectangle, 4, true, 0.12f);

        LinePoint* circle = stackalloc LinePoint[4];
        new Span<LinePoint>(circle, 4).Clear();
        R3D.SetLineCircle(circle, Vector3.Zero, 0.73f);
        SetGradient(circle, 4, 15);
        var circleMesh = ScreenLine(circle, 4, true, 13);

        LinePoint* ellipse = stackalloc LinePoint[EllipseCount];
        new Span<LinePoint>(ellipse, EllipseCount).Clear();
        R3D.SetLineEllipse(ref ellipse[0], EllipseCount, Vector3.Zero, new Vector2(0.98f, 0.62f));
        SetGradient(ellipse, EllipseCount, 270);
        var ellipseMesh = WorldLine(ellipse, EllipseCount, true, 0.14f);

        LinePoint* arc = stackalloc LinePoint[2];
        new Span<LinePoint>(arc, 2).Clear();
        arc[0].Color = new Color(255,84,124,255);
        arc[1].Color = new Color(59,208,255,255);
        R3D.SetLineArc(arc, new Vector3(-0.88f, -0.3f, 0), new Vector3(0.88f, -0.3f, 0), Vector3.UnitZ, -MathF.PI * 0.78f, 32);
        var arcMesh = ScreenLine(arc, 2, false, 17);

        LinePoint* screenPts = stackalloc LinePoint[StreamMax];
        new Span<LinePoint>(screenPts, StreamMax).Clear();
        LinePoint* worldPts = stackalloc LinePoint[StreamMax];
        new Span<LinePoint>(worldPts, StreamMax).Clear();
        int[] counts = [128, 256, 512];
        int countIndex = 1, pointCount = counts[1];
        AnimateStream(screenPts, pointCount, 0, true);
        AnimateStream(worldPts, pointCount, 0, false);
        var screenPath = MakePath(screenPts, pointCount, false, Style(true, 9));
        var worldPath = MakePath(worldPts, pointCount, false, Style(false, 0.1f));
        var screenStream = R3D.GenMeshScreenLinePath(ref screenPath);
        var worldStream = R3D.GenMeshLinePath(ref worldPath);

        LinePoint* transient = stackalloc LinePoint[2];
        new Span<LinePoint>(transient, 2).Clear();
        transient[0].Color = new Color(255,94,144,255);
        transient[1].Color = new Color(80,221,255,255);
        R3D.SetLineArc(transient, new Vector3(-1.7f,-0.8f,0), new Vector3(1.7f,-0.8f,0), Vector3.UnitZ, -MathF.PI * 0.65f, 28);
        var transientPath = MakePath(transient, 2, false, Style(false, 0.15f));
        Mesh transientMesh = default;
        double transientUntil = 0;
        bool orbital = false, paused = false, bloom = true, glow = false, overlay = false;
        int scene = 0;
        float phase = 0, scale = 1;
        double updateMs = 0;

        while (!WindowShouldClose())
        {
            float dt = MathF.Min(GetFrameTime(), 0.05f);
            if (IsKeyPressed(KeyboardKey.Right)) scene = (scene + 1) % 3;
            if (IsKeyPressed(KeyboardKey.Left)) scene = (scene + 2) % 3;
            if (IsKeyPressed(KeyboardKey.Space)) paused = !paused;
            if (IsKeyPressed(KeyboardKey.O)) orbital = !orbital;
            if (IsKeyPressed(KeyboardKey.B)) bloom = !bloom;
            if (IsKeyPressed(KeyboardKey.G)) glow = !glow;
            if (IsKeyPressed(KeyboardKey.P)) overlay = !overlay;
            if (IsKeyPressed(KeyboardKey.T))
            {
                if (!R3D.IsMeshValid(transientMesh)) transientMesh = R3D.GenMeshLinePath(ref transientPath);
                transientUntil = GetTime() + 0.75;
            }
            if (IsKeyPressed(KeyboardKey.Up)) scale = MathF.Min(3, scale + 0.25f);
            if (IsKeyPressed(KeyboardKey.Down)) scale = MathF.Max(0.25f, scale - 0.25f);
            if (IsKeyPressed(KeyboardKey.N))
            {
                countIndex = (countIndex + 1) % counts.Length;
                pointCount = counts[countIndex];
                screenPath = MakePath(screenPts, pointCount, false, Style(true, 9));
                worldPath = MakePath(worldPts, pointCount, false, Style(false, 0.1f));
            }
            if (!paused) phase += dt;
            if (orbital) UpdateCamera(ref camera, CameraMode.Orbital);
            R3D.SetEnvironmentEx((ref env) => env.Bloom.Intensity = bloom ? 0.58f : 0f);
            double started = GetTime();
            AnimateStream(screenPts, pointCount, phase, true);
            AnimateStream(worldPts, pointCount, phase, false);
            bool updated = true;
            if (scene != 2) updated &= R3D.UpdateScreenLineMesh(ref screenStream, ref screenPath);
            if (scene != 0) updated &= R3D.UpdateLineMesh(ref worldStream, ref worldPath);
            updateMs = (GetTime() - started) * 1000;

            BeginDrawing();
            ClearBackground(new Color(5,9,21,255));
            R3D.Begin(camera);
            R3D.PushLight(light);
            var stage = Matrix4x4.CreateTranslation(0.7f, 0, 0);
            if (scene == 0)
            {
                R3D.DrawMesh(sphere, sphereMat, new Vector3(0.7f, 0, 0), 1);
                R3D.DrawScreenLineMesh(orbitMesh, screenMat, screenEffect, scale,
                    Matrix4x4.CreateRotationY(phase * 0.47f) * stage);
                R3D.DrawScreenLineMesh(orbitMesh, screenMat, screenEffect, scale * 0.8f,
                    Matrix4x4.CreateRotationX(1.06f + phase * 0.28f) * stage);
                DrawWorld(ring, worldMat, Matrix4x4.CreateRotationY(-phase * 0.67f) * stage, glow, glowMode);
                R3D.DrawScreenLineMesh(screenStream, screenMat, screenEffect, scale, Matrix4x4.CreateTranslation(0.7f, -2.05f, 0.15f));
            }
            else if (scene == 1)
            {
                R3D.DrawMesh(sphere, sphereMat, new Vector3(0.7f, -0.1f, -0.35f), 1);
                DrawWorld(ring, worldMat, Matrix4x4.CreateRotationY(phase * 0.55f) * stage, glow, glowMode);
                DrawWorld(worldStream, texturedMat, Matrix4x4.CreateTranslation(0.7f,1.35f,0.15f), glow, glowMode);
                R3D.DrawScreenLineMesh(screenStream, screenMat, screenEffect, scale, Matrix4x4.CreateTranslation(0.7f,-1.45f,0.55f));
                R3D.DrawScreenLineMesh(orbitMesh, screenMat, screenEffect, scale * 0.65f, Matrix4x4.CreateRotationY(-phase * 0.25f) * stage);
            }
            else
            {
                DrawWorld(rectangleMesh, worldMat, Matrix4x4.CreateTranslation(-2.7f, 0.55f, 0), glow, glowMode);
                R3D.DrawScreenLineMesh(circleMesh, screenMat, screenEffect, scale, Matrix4x4.CreateTranslation(-0.55f,0.55f,0));
                DrawWorld(ellipseMesh, worldMat, Matrix4x4.CreateTranslation(1.7f,0.55f,0), glow, glowMode);
                R3D.DrawScreenLineMesh(arcMesh, screenMat, screenEffect, scale, Matrix4x4.CreateTranslation(3.85f,0.55f,0));
                R3D.DrawMesh(sphere, sphereMat, new Vector3(0.7f,-1.5f,-0.3f), 0.65f);
                DrawWorld(worldStream, worldMat, Matrix4x4.CreateTranslation(0.7f,-1.5f,0), glow, glowMode);
            }
            if (R3D.IsMeshValid(transientMesh))
                DrawWorld(transientMesh, worldMat, Matrix4x4.CreateTranslation(0.7f,0,0.7f), glow, glowMode);
            R3D.End();
            if (R3D.IsMeshValid(transientMesh) && GetTime() >= transientUntil)
            {
                R3D.UnloadMesh(transientMesh);
                transientMesh = default;
            }
            if (overlay)
            {
                var view = new View { Camera = R3D.CameraFromRL(camera) };
                var projected = R3D.ProjectLinePath2D(ref screenPath, view);
                R3D.Draw2DProjectedLinePath(ref projected, scale * 0.55f);
                R3D.UnloadProjectedLinePath(projected);
            }
            Hud(scene, paused, orbital, bloom, glow, overlay, pointCount, scale, updateMs);
            if (scene == 2)
            {
                string[] shapeNames = ["RECTANGLE", "CIRCLE", "ELLIPSE", "ARC"];
                float[] positions = [-2.7f, -0.55f, 1.7f, 3.85f];
                for (int i = 0; i < shapeNames.Length; ++i)
                {
                    Vector2 label = GetWorldToScreen(new Vector3(positions[i], -0.53f, 0), camera);
                    DrawText(shapeNames[i], (int)label.X - MeasureText(shapeNames[i], 16) / 2, (int)label.Y, 16, Accents[scene]);
                }
            }
            if (!updated) DrawText("MESH UPDATE FAILED", 375, H - 133, 22, Color.Red);
            EndDrawing();
        }

        if (R3D.IsMeshValid(transientMesh)) R3D.UnloadMesh(transientMesh);
        foreach (var mesh in new[] { orbitMesh, ring, rectangleMesh, circleMesh, ellipseMesh, arcMesh, screenStream, worldStream, sphere })
            R3D.UnloadMesh(mesh);
        R3D.UnloadBuiltRenderMode(ref glowMode);
        R3D.UnloadSurfaceEffect(screenEffect);
        UnloadTexture(checker);
        R3D.Close();
        CloseWindow();
        return 0;
    }
}
