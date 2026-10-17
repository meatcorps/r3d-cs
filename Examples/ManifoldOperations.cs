using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;

namespace Examples;

// Port of r3d/examples/manifold_operations.c: one-shot R3D manifold operations.
public static unsafe class ManifoldOperations
{
    public static int Main()
    {
        InitWindow(1152, 648, "[r3d] Manifold Operations (C#)");
        SetTargetFPS(60);
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        var cubeData = R3D.GenMeshDataCube(2, 2, 2);
        var originalData = R3D.CopyMeshData(cubeData);
        var cutterData = R3D.GenMeshDataCube(1.45f, 1.45f, 1.45f);
        R3D.TranslateMeshData(ref cutterData, new Vector3(0.65f));
        Vector2[] outline =
        [
            new(-0.8f, -0.8f), new(0.8f, -0.8f), new(0.8f, -0.3f),
            new(0.3f, -0.3f), new(0.3f, 0.8f), new(-0.8f, 0.8f)
        ];

        var mesh = R3D.LoadMesh(PrimitiveType.Triangles, cubeData, null);
        var cutter = R3D.LoadMesh(PrimitiveType.Triangles, cutterData, null);
        R3D.LinkMeshData(ref mesh, ref cubeData);
        R3D.LinkMeshData(ref cutter, ref cutterData);
        var ground = R3D.GenMeshPlane(12, 12, 1, 1);

        var image = GenImageChecked(256, 256, 32, 32, Color.RayWhite, Color.DarkGray);
        var checker = LoadTextureFromImage(image);
        UnloadImage(image);
        var material = R3D.GetDefaultMaterial();
        material.Albedo.Texture = checker;
        var groundMaterial = R3D.GetDefaultMaterial();
        groundMaterial.Albedo.Color = Color.LightGray;
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = new Color(80, 80, 80, 255));
        var light = R3D.CreateDirLight(new Vector3(-0.5f, -1, -0.7f), Color.White, 2);
        var shadow = R3D.LoadShadowMap(LightType.Dir);
        var camera = new Camera3D
        {
            Position = new Vector3(4, 3, 4), Target = Vector3.Zero,
            Up = Vector3.UnitY, FovY = 45, Projection = CameraProjection.Perspective
        };

        ManifoldResult result = default;
        int selected = 0;
        string[] names = ["Original cube", "Union", "Subtraction", "Intersection", "Plane slice", "Extrusion"];

        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);
            int requested = -1;
            if (IsKeyPressed(KeyboardKey.Space)) requested = 0;
            if (IsKeyPressed(KeyboardKey.One)) requested = 1;
            if (IsKeyPressed(KeyboardKey.Two)) requested = 2;
            if (IsKeyPressed(KeyboardKey.Three)) requested = 3;
            if (IsKeyPressed(KeyboardKey.Four)) requested = 4;
            if (IsKeyPressed(KeyboardKey.Five)) requested = 5;

            if (requested >= 0)
            {
                R3D.UnloadManifoldResult(ref result);
                if (requested == 0)
                {
                    var restored = R3D.CopyMeshData(originalData);
                    if (R3D.IsMeshDataValid(restored) && R3D.UpdateMesh(ref mesh, restored, null))
                    {
                        R3D.UnloadMeshData(cubeData);
                        cubeData = restored;
                        selected = 0;
                    }
                    else
                    {
                        R3D.UnloadMeshData(restored);
                    }
                }
                else
                {
                    switch (requested)
                    {
                        case 1: result = R3D.ManifoldUnion(ref mesh, ref cutter); break;
                        case 2: result = R3D.ManifoldSubtract(ref mesh, ref cutter); break;
                        case 3: result = R3D.ManifoldIntersect(ref mesh, ref cutter); break;
                        case 4: result = R3D.ManifoldSlicePlane(ref mesh, Vector3.UnitY, 0); break;
                        case 5: result = R3D.ManifoldExtrude(ref mesh, ref outline[0], outline.Length, 1.5f); break;
                    }

                    if ((int)result.Status == 0) selected = requested;
                    else Console.WriteLine($"Manifold failed: {result.Status} / {result.ManifoldError}");
                }
            }

            BeginDrawing();
            ClearBackground(Color.RayWhite);
            R3D.Begin(camera);
            R3D.PushLightEx(light, shadow, true);
            R3D.DrawMesh(ground, groundMaterial, new Vector3(0, -2, 0), 1);
            R3D.DrawMesh(mesh, material, Vector3.Zero, 1);
            for (int i = 0; i < result.ExtraMeshes.Length; i++)
                R3D.DrawMesh(result.ExtraMeshes[i], material, new Vector3(0.25f * (i + 1), -0.35f, 0.25f * (i + 1)), 1);
            R3D.End();
            DrawText("1 Union | 2 Subtract | 3 Intersect | 4 Slice | 5 Extrude | SPACE Reset", 20, 20, 18, Color.Black);
            DrawText(names[selected], 20, 50, 20, Color.Black);
            DrawText($"Additional pieces: {result.ExtraMeshes.Length}", 20, 80, 18, Color.DarkGray);
            EndDrawing();
        }

        R3D.UnloadManifoldResult(ref result);
        R3D.UnloadMesh(mesh);
        R3D.UnloadMesh(cutter);
        R3D.UnloadMesh(ground);
        R3D.UnloadMeshData(cubeData);
        R3D.UnloadMeshData(cutterData);
        R3D.UnloadMeshData(originalData);
        R3D.UnloadShadowMap(shadow);
        UnloadTexture(checker);
        R3D.Close();
        CloseWindow();
        return 0;
    }
}
