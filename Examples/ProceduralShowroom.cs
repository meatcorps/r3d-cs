using System;
using System.IO;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using Material = R3D_cs.Material;
using Mesh = R3D_cs.Mesh;

namespace Examples;

// Port of r3d/examples/procedural_showroom.c: extruded fonts, mesh updates and timings.
public static unsafe class ProceduralShowroom
{
    private const int Width = 1600, Height = 900, SampleCount = 12;
    private static readonly string[] FontNames = ["DARKBYTE", "SCABBER"];
    private static readonly string[] Exhibits = ["TITLE GENERATION", "GLYPH TOPOLOGY", "INFORMATION PLAQUE"];
    private static readonly string[] ExhibitTexts = ["MEATCORPS", "B8Oa@ 2026\nABC 012", "PROCEDURAL\nGEOMETRY"];

    private static readonly string[] Details =
    [
        "Extruded typography, font switching and PBR lighting",
        "Multiple contours, counters, curved and sharp outlines",
        "Runtime text replacement using reusable glyph data"
    ];

    private static readonly Color[] FontColors = [new(245, 180, 67, 255), new(57, 190, 248, 255)];

    private sealed class State
    {
        public readonly TextFont[] Fonts = new TextFont[2];
        public Mesh Title, Info, Pedestal, Torus;
        public Material TitleMaterial, InfoMaterial, PedestalMaterial, TorusMaterial;
        public int FontIndex, ExhibitIndex;
        public bool Smooth, Automatic, Orbital, LastOk;
        public float Elapsed, AutoTimer;
        public double GenMs, UploadMs, PeakGenMs, PeakUploadMs;
        public double BenchGenTotal, BenchUploadTotal, AvgGen, AvgUpload;
        public int BenchRemaining, BenchCompleted, VertexCount, TriangleCount, Updates;
    }

    private static TextMeshSettings Settings(State s, bool small)
    {
        var options = R3D.DefaultTextMeshSettings();
        options.FontSize = small ? 0.36f : 0.86f;
        options.Depth = small ? 0.10f : 0.28f;
        options.CurveTolerance = 0.003f;
        options.LetterSpacing = small ? 0.025f : 0.015f;
        options.LineSpacing = 1.2f;
        options.Alignment = TextMeshAlign.Center;
        if (s.Smooth)
        {
            options.BevelSmoothness = 0.55f;
            options.BevelSubdivisions = 2;
        }

        return options;
    }

    private static bool Regenerate(State s)
    {
        string info = $"{FontNames[s.FontIndex]}\n{(s.Smooth ? "SMOOTH EDGES" : "HARD EDGES")}";
        double start = GetTime();
        MeshData titleData =
            R3D.GenMeshDataText(ref s.Fonts[s.FontIndex], ExhibitTexts[s.ExhibitIndex], Settings(s, false));
        MeshData infoData = R3D.GenMeshDataText(ref s.Fonts[s.FontIndex], info, Settings(s, true));
        double genMs = (GetTime() - start) * 1000;
        if (!R3D.IsMeshDataValid(titleData) || !R3D.IsMeshDataValid(infoData))
        {
            R3D.UnloadMeshData(titleData);
            R3D.UnloadMeshData(infoData);
            return s.LastOk = false;
        }

        start = GetTime();
        bool titleOK;
        if (R3D.IsMeshValid(s.Title)) titleOK = R3D.UpdateMesh(ref s.Title, titleData, null);
        else
        {
            s.Title = R3D.LoadMesh(PrimitiveType.Triangles, titleData, null);
            titleOK = R3D.IsMeshValid(s.Title);
        }

        bool infoOK;
        if (R3D.IsMeshValid(s.Info)) infoOK = R3D.UpdateMesh(ref s.Info, infoData, null);
        else
        {
            s.Info = R3D.LoadMesh(PrimitiveType.Triangles, infoData, null);
            infoOK = R3D.IsMeshValid(s.Info);
        }

        double uploadMs = (GetTime() - start) * 1000;
        if (titleOK && infoOK)
        {
            s.GenMs = genMs;
            s.UploadMs = uploadMs;
            s.PeakGenMs = Math.Max(s.PeakGenMs, genMs);
            s.PeakUploadMs = Math.Max(s.PeakUploadMs, uploadMs);
            s.VertexCount = titleData.VertexCount + infoData.VertexCount;
            s.TriangleCount = (titleData.IndexCount + infoData.IndexCount) / 3;
            s.Updates++;
            s.TitleMaterial.Albedo.Color = FontColors[s.FontIndex];
        }

        R3D.UnloadMeshData(titleData);
        R3D.UnloadMeshData(infoData);
        return s.LastOk = titleOK && infoOK;
    }

    private static void DrawScene(State s)
    {
        if (R3D.IsMeshValid(s.Title)) R3D.DrawMesh(s.Title, s.TitleMaterial, new Vector3(1.6f, 0.65f, 0.2f), 1);
        if (R3D.IsMeshValid(s.Info)) R3D.DrawMesh(s.Info, s.InfoMaterial, new Vector3(1.6f, -1.05f, 0.35f), 1);
        R3D.DrawMesh(s.Pedestal, s.PedestalMaterial, new Vector3(1.6f, -2.12f, -0.75f), 1);
        R3D.DrawMeshPro(s.Torus, s.TorusMaterial,
            Matrix4x4.CreateRotationY(s.Elapsed * 0.6f) * Matrix4x4.CreateTranslation(5.1f, 0, -0.7f));
    }

    private static void DrawHud(State s)
    {
        Color accent = FontColors[s.FontIndex], pale = new(220, 232, 244, 255), muted = new(136, 155, 180, 255);
        DrawRectangle(0, 0, 350, Height, new Color(9, 16, 29, 245));
        DrawRectangle(349, 0, 2, Height, accent);
        DrawText("MEATCORPS / R3D", 27, 31, 22, pale);
        DrawText("PROCEDURAL", 27, 105, 36, Color.RayWhite);
        DrawText("SHOWROOM", 27, 150, 43, accent);
        DrawText("PHASE 05 / GEOMETRY", 28, 214, 17, muted);
        for (int i = 0; i < Exhibits.Length; ++i)
        {
            int y = 276 + 67 * i;
            bool active = i == s.ExhibitIndex;
            if (active) DrawRectangle(18, y - 12, 315, 54, Fade(accent, 0.16f));
            DrawText($"0{i + 1}", 29, y, 18, active ? accent : muted);
            DrawText(Exhibits[i], 69, y, 18, active ? Color.RayWhite : muted);
        }

        DrawText("LIVE GEOMETRY", 28, 509, 21, accent);
        DrawText($"Vertices: {s.VertexCount}", 28, 549, 19, pale);
        DrawText($"Triangles: {s.TriangleCount}", 28, 579, 19, pale);
        DrawText($"Generate: {s.GenMs:F3} ms", 28, 626, 18, pale);
        DrawText($"Upload: {s.UploadMs:F3} ms", 28, 656, 18, pale);
        DrawText($"Peak: {s.PeakGenMs:F2} / {s.PeakUploadMs:F2} ms", 28, 688, 16, muted);
        DrawText($"Updates: {s.Updates}", 28, 725, 18, pale);
        DrawText($"FPS: {GetFPS()}", 28, 760, 18, accent);
        DrawText(s.LastOk ? "RESOURCE STATE: OK" : "RESOURCE STATE: FAILED", 28, 813, 16,
            s.LastOk ? Color.Green : Color.Red);
        DrawRectangle(350, 0, Width - 350, 112, new Color(8, 15, 27, 225));
        DrawText(Exhibits[s.ExhibitIndex], 390, 25, 34, Color.RayWhite);
        DrawText(Details[s.ExhibitIndex], 391, 73, 18, accent);
        DrawRectangle(350, Height - 132, Width - 350, 132, new Color(8, 15, 27, 235));
        DrawText("LEFT/RIGHT EXHIBIT   F FONT   S SMOOTH   U AUTO   R REBUILD", 387, Height - 107, 18, pale);
        DrawText("B BENCHMARK   O ORBIT   ESC EXIT", 387, Height - 72, 18, muted);
        DrawText(
            $"FONT: {FontNames[s.FontIndex]} | EDGE: {(s.Smooth ? "SMOOTH" : "HARD")} | AUTO: {(s.Automatic ? "ON" : "OFF")}",
            389, Height - 36, 17, accent);
        if (s.BenchRemaining > 0)
            DrawText($"BENCHMARK: {s.BenchCompleted} / {SampleCount}", Width - 275, 33, 18, accent);
        else if (s.BenchCompleted == SampleCount)
            DrawText($"AVG GEN: {s.AvgGen:F2} ms | UPDATE: {s.AvgUpload:F2} ms", Width - 435, 33, 17, accent);
    }

    public static int Main()
    {
        InitWindow(Width, Height, "[r3d] Procedural Geometry Showroom (C#)");
        SetTargetFPS(60);
        R3D.SetHint(Hint.MeshVertexBufferCapacity, 262144);
        R3D.SetHint(Hint.MeshIndexBufferCapacity, 262144);
        if (!R3D.Init(Width, Height))
        {
            CloseWindow();
            return 1;
        }

        var s = new State();
        s.Fonts[0] = R3D.LoadTextFontFromFile("resources/fonts/Darkbyte-4nly6.ttf");
        byte[] fontBytes = File.ReadAllBytes("resources/fonts/Scabber-q2Mn0.ttf");
        fixed (byte* bytes = fontBytes)
            s.Fonts[1] = R3D.LoadTextFontFromMemory(ref *(sbyte*)bytes, (nint)fontBytes.Length);

        if (!R3D.IsTextFontValid(s.Fonts[0]) || !R3D.IsTextFontValid(s.Fonts[1]))
        {
            R3D.UnloadTextFont(s.Fonts[0]);
            R3D.UnloadTextFont(s.Fonts[1]);
            R3D.Close();
            CloseWindow();
            return 1;
        }

        for (int i = 0; i < s.Fonts.Length; ++i) R3D.PreloadTextFontAllGlyphs(ref s.Fonts[i]);

        s.TitleMaterial = R3D.GetDefaultMaterial();
        s.InfoMaterial = R3D.GetDefaultMaterial();
        s.PedestalMaterial = R3D.GetDefaultMaterial();
        s.TorusMaterial = R3D.GetDefaultMaterial();
        s.TitleMaterial.Albedo.Color = FontColors[0];
        s.InfoMaterial.Albedo.Color = new Color(225, 233, 245, 255);
        s.PedestalMaterial.Albedo.Color = new Color(36, 49, 69, 255);
        s.TorusMaterial.Albedo.Color = new Color(135, 110, 240, 255);
        s.Pedestal = R3D.GenMeshCube(8, 0.24f, 2);
        s.Torus = R3D.GenMeshTorus(0.78f, 0.22f, 36, 24);
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = new Color(8, 14, 26, 255);
            env.Ambient.Color = new Color(82, 91, 109, 255);
        });
        var light = R3D.CreateDirLight(new Vector3(-0.6f, -0.7f, -1), Color.White, 2);
        var shadow = R3D.LoadShadowMap(LightType.Dir);
        var camera = new Camera3D
        {
            Position = new Vector3(1.6f, 1.4f, 11.5f), Target = new Vector3(1.6f, 0, 0),
            Up = Vector3.UnitY, FovY = 48, Projection = CameraProjection.Perspective
        };
        Regenerate(s);

        while (!WindowShouldClose())
        {
            float dt = MathF.Min(GetFrameTime(), 0.1f);
            s.Elapsed += dt;
            bool regenerate = false;
            if (IsKeyPressed(KeyboardKey.Right))
            {
                s.ExhibitIndex = (s.ExhibitIndex + 1) % Exhibits.Length;
                regenerate = true;
            }

            if (IsKeyPressed(KeyboardKey.Left))
            {
                s.ExhibitIndex = (s.ExhibitIndex + Exhibits.Length - 1) % Exhibits.Length;
                regenerate = true;
            }

            if (IsKeyPressed(KeyboardKey.F))
            {
                s.FontIndex = (s.FontIndex + 1) % s.Fonts.Length;
                regenerate = true;
            }

            if (IsKeyPressed(KeyboardKey.S))
            {
                s.Smooth = !s.Smooth;
                regenerate = true;
            }

            if (IsKeyPressed(KeyboardKey.R)) regenerate = true;
            if (IsKeyPressed(KeyboardKey.U))
            {
                s.Automatic = !s.Automatic;
                s.AutoTimer = 0;
            }

            if (IsKeyPressed(KeyboardKey.O)) s.Orbital = !s.Orbital;
            if (s.Orbital) UpdateCamera(ref camera, CameraMode.Orbital);
            if (IsKeyPressed(KeyboardKey.B))
            {
                s.BenchRemaining = SampleCount;
                s.BenchCompleted = 0;
                s.BenchGenTotal = s.BenchUploadTotal = s.AvgGen = s.AvgUpload = 0;
            }

            if (s.Automatic && s.BenchRemaining == 0)
            {
                s.AutoTimer += dt;
                if (s.AutoTimer >= 4f)
                {
                    s.AutoTimer = 0;
                    s.ExhibitIndex = (s.ExhibitIndex + 1) % Exhibits.Length;
                    if (s.ExhibitIndex == 0) s.FontIndex = (s.FontIndex + 1) % s.Fonts.Length;
                    regenerate = true;
                }
            }

            bool benchmark = s.BenchRemaining > 0;
            if (benchmark)
            {
                s.ExhibitIndex = (s.ExhibitIndex + 1) % Exhibits.Length;
                s.FontIndex = (s.FontIndex + 1) % s.Fonts.Length;
                regenerate = true;
            }

            if (regenerate)
            {
                bool ok = Regenerate(s);
                if (benchmark)
                {
                    if (ok)
                    {
                        s.BenchGenTotal += s.GenMs;
                        s.BenchUploadTotal += s.UploadMs;
                        s.BenchCompleted++;
                    }

                    s.BenchRemaining--;
                    if (s.BenchRemaining == 0 && s.BenchCompleted > 0)
                    {
                        s.AvgGen = s.BenchGenTotal / s.BenchCompleted;
                        s.AvgUpload = s.BenchUploadTotal / s.BenchCompleted;
                        Console.WriteLine(
                            $"Text benchmark: {s.BenchCompleted} samples, gen {s.AvgGen:F3} ms, upload {s.AvgUpload:F3} ms");
                    }
                }
            }

            BeginDrawing();
            ClearBackground(new Color(8, 14, 26, 255));
            R3D.Begin(camera);
            R3D.PushLightEx(light, shadow, true);
            DrawScene(s);
            R3D.End();
            DrawHud(s);
            EndDrawing();
        }

        if (R3D.IsMeshValid(s.Title)) R3D.UnloadMesh(s.Title);
        if (R3D.IsMeshValid(s.Info)) R3D.UnloadMesh(s.Info);
        R3D.UnloadMesh(s.Pedestal);
        R3D.UnloadMesh(s.Torus);
        R3D.UnloadTextFont(s.Fonts[0]);
        R3D.UnloadTextFont(s.Fonts[1]);
        R3D.UnloadShadowMap(shadow);
        R3D.Close();
        CloseWindow();
        return 0;
    }
}
