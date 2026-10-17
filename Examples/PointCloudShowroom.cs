using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

// Port of r3d/examples/point_cloud_showroom.c: depth-silhouette samples + streamed topology wireframe.
public static unsafe class PointCloudShowroom
{
    private const int CloudW = 160, CloudH = 120, Capacity = CloudW * CloudH;

    private static int FillDepthCloud(PointCloudPoint[] points, float seconds, int shapeMode)
    {
        int count = 0;
        for (int y = 0; y < CloudH; ++y)
        {
            float v = 2f * y / (CloudH - 1) - 1f;
            for (int x = 0; x < CloudW; ++x)
            {
                float u = 2f * x / (CloudW - 1) - 1f;
                float head = u * u / (0.28f * 0.28f) + (v - 0.49f) * (v - 0.49f) / (0.3f * 0.3f);
                float torso = u * u / (0.66f * 0.66f) + (v + 0.49f) * (v + 0.49f) / (0.6f * 0.6f);
                if ((head >= 1 && torso >= 1) || (x * 17 + y * 37) % 43 == 0) continue;

                float surface = 0.25f * MathF.Sin(u * 5.5f + seconds * 0.4f)
                                + 0.13f * MathF.Cos(v * 9f - seconds * 0.7f);
                float depth = 0.35f + surface + 0.15f * MathF.Exp(-u * u * 45f - (v - 0.48f) * (v - 0.48f) * 35f);
                float hue = 185f + 115f * (0.5f + 0.5f * MathF.Sin(u * 3f + depth * 1.8f + seconds * 0.2f));
                points[count++] = new PointCloudPoint
                {
                    Position = new Vector3(-2.65f + u * 1.65f, v * 1.75f, depth),
                    Size = 0.028f + 0.018f * (0.5f + 0.5f * surface),
                    Color = ColorFromHSV(hue, 0.8f, 1f),
                    Shape = (PointShape)(shapeMode == 0 ? (x / 12 + y / 12) % (int)PointShape.Count : shapeMode - 1)
                };
            }
        }

        return count;
    }

    public static int Main()
    {
        InitWindow(1280, 720, "[r3d] Point Clouds & Wireframes (C#)");
        SetTargetFPS(60);
        R3D.SetHint(Hint.MeshVertexBufferCapacity, 262144);
        R3D.SetHint(Hint.MeshIndexBufferCapacity, 262144);
        if (!R3D.Init(GetScreenWidth(), GetScreenHeight()))
        {
            CloseWindow();
            return 1;
        }

        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = new Color(5, 11, 26, 255);
            env.Tonemap.Mode = Tonemap.Aces;
            env.Bloom.Mode = R3D_cs.Bloom.Additive;
            env.Bloom.Intensity = 0.43f;
            env.Bloom.Threshold = 0.22f;
            env.Bloom.SoftThreshold = 0.7f;
            env.Bloom.Levels = 0.85f;
        });
        var camera = new Camera3D
        {
            Position = new Vector3(0, 1, 11.5f), Target = Vector3.Zero,
            Up = Vector3.UnitY, FovY = 50, Projection = CameraProjection.Perspective
        };

        var cloud = R3D.LoadPointCloud(Capacity);
        var points = new PointCloudPoint[Capacity];
        var pointMaterial = R3D.GetDefaultPointCloudMaterial();
        var source = R3D.GenMeshDataSphere(1.35f, 18, 24);
        for (int i = 0; i < source.VertexCount; ++i)
        {
            var p = source.Vertices[i].Position;
            source.Vertices[i].Color = ColorFromHSV(165f + (p.Y + 1.35f) * 52f + (p.X + 1.35f) * 18f, 0.9f, 1f);
        }

        var style = new LineStyle { Width = 3, WidthMode = LineWidthMode.Screen, Caps = LineCapFlags.LineCapBoth };
        int retainedEdges = 0;
        var wireframe = R3D.GenMeshTopologyWireframe(ref source, style, 0, 0.0001f, ref retainedEdges);
        var rest = R3D.CopyMeshData(source);
        var screenEffect = R3D.LoadScreenLineEffect();
        var wireMaterial = R3D.GetDefaultScreenLineMaterial();
        var outerGlow = R3D.RenderModeBuildOuterGlow(new Color(55, 210, 255, 255), 13, 2.5f, 2.1f);

        bool orbital = false, animate = true, bloom = true, glow = false, streamMesh = false;
        bool updateOK = true, meshUpdateOK = true;
        int shapeMode = 0, pointCount = 0;
        float seconds = 0;
        double uploadMs = 0, meshUpdateMs = 0;

        while (!WindowShouldClose())
        {
            if (IsKeyPressed(KeyboardKey.O)) orbital = !orbital;
            if (IsKeyPressed(KeyboardKey.Space)) animate = !animate;
            if (IsKeyPressed(KeyboardKey.B)) bloom = !bloom;
            if (IsKeyPressed(KeyboardKey.G)) glow = !glow;
            if (IsKeyPressed(KeyboardKey.R)) streamMesh = !streamMesh;
            if (IsKeyPressed(KeyboardKey.S)) shapeMode = (shapeMode + 1) % 4;
            if (animate) seconds += MathF.Min(GetFrameTime(), 0.05f);
            if (orbital) UpdateCamera(ref camera, CameraMode.Orbital);
            R3D.SetEnvironmentEx((ref env) => env.Bloom.Intensity = bloom ? 0.43f : 0f);

            double start = GetTime();
            pointCount = FillDepthCloud(points, seconds, shapeMode);
            updateOK = R3D.UpdatePointCloud(ref cloud, ref points[0], pointCount);
            uploadMs = (GetTime() - start) * 1000;

            if (streamMesh)
            {
                start = GetTime();
                for (int i = 0; i < source.VertexCount; ++i)
                {
                    Vector3 p = rest.Vertices[i].Position;
                    float wave = MathF.Sin(p.Y * 3f + seconds * 2.5f) * MathF.Cos(p.X * 3.2f - seconds * 1.4f);
                    source.Vertices[i].Position = p * (1f + wave * 0.13f);
                    source.Vertices[i].Color = ColorFromHSV(165f + (p.Y + 1.35f) * 52f +
                                                            (p.X + 1.35f) * 18f + 35f * MathF.Sin(seconds * 0.8f), 0.9f,
                        1f);
                }

                meshUpdateOK =
                    R3D.UpdateTopologyWireframe(ref wireframe, ref source, style, 0f, 0.0001f, ref retainedEdges);
                meshUpdateMs = (GetTime() - start) * 1000;
            }

            var transform = Matrix4x4.CreateRotationX(-0.16f) * Matrix4x4.CreateRotationY(seconds * 0.32f)
                                                              * Matrix4x4.CreateTranslation(2.35f, 0, 0);
            BeginDrawing();
            ClearBackground(new Color(5, 11, 26, 255));
            R3D.Begin(camera);
            if (glow) R3D.PushBuiltRenderMode(outerGlow);
            R3D.DrawPointCloud(ref cloud, pointMaterial);
            if (glow) R3D.PopBuiltRenderMode();
            R3D.DrawScreenLineMesh(wireframe, wireMaterial, screenEffect, 1, transform);
            R3D.End();

            DrawText("MEATCORPS / R3D - POINT CLOUDS & TOPOLOGY WIREFRAMES", 22, 18, 23, Color.RayWhite);
            DrawText("SYNTHETIC DEPTH CAMERA", 138, 130, 18, new Color(95, 218, 249, 255));
            DrawText("PRESENTATION WIREFRAME", 786, 130, 18, new Color(250, 178, 90, 255));
            DrawText($"{pointCount} colored points / {retainedEdges} retained edges", 22, 629, 17, Color.LightGray);
            DrawText(
                $"Cloud: {uploadMs:F3} ms | Mesh: {(streamMesh ? "STREAMING" : "STATIC")} / {meshUpdateMs:F3} ms | {GetFPS()} FPS | {(updateOK && meshUpdateOK ? "OK" : "FAILED")}",
                22, 655, 17, updateOK && meshUpdateOK ? new Color(110, 240, 179, 255) : Color.Red);
            DrawText("S SHAPE   O ORBIT   SPACE PAUSE   B BLOOM   G GLOW   R STREAM MESH", 22, 687, 15,
                Color.LightGray);
            EndDrawing();
        }

        R3D.UnloadBuiltRenderMode(ref outerGlow);
        R3D.UnloadSurfaceEffect(screenEffect);
        R3D.UnloadMesh(wireframe);
        R3D.UnloadPointCloud(ref cloud);
        R3D.UnloadMeshData(source);
        R3D.UnloadMeshData(rest);
        R3D.Close();
        CloseWindow();
        return 0;
    }
}
