using System;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using Mesh = R3D_cs.Mesh;

namespace Examples;

// Port of r3d/examples/manifold.c: direct Manifold C API and R3D mesh conversion.
public static unsafe class ManifoldDemo
{
    // The generated opaque handle contains one pointer. Manifold allocation functions
    // return that handle, while object constructors accept it as void* memory.
    private static IntPtr Memory(ManifoldManifold value) => *(nint*)&value;
    private static IntPtr Memory(ManifoldMeshGL value) => *(nint*)&value;

    private static bool Has(ManifoldManifold value) => !value.Equals(default(ManifoldManifold));
    private static bool Has(ManifoldMeshGL value) => !value.Equals(default(ManifoldMeshGL));

    private static Mesh CreateCutMesh(ref MeshData original)
    {
        Mesh result = default;
        var cutterData = R3D.GenMeshDataCube(1.45f, 1.45f, 1.45f);

        var originalGl = R3D.MeshDataToManifoldMeshGL(ref original);
        var cutterGl = R3D.MeshDataToManifoldMeshGL(ref cutterData);
        ManifoldManifold cube = default, cutter = default, moved = default, cut = default;

        if (Has(originalGl)) cube = Manifold.OfMeshgl(Memory(Manifold.AllocManifold()), originalGl);
        if (Has(cutterGl)) cutter = Manifold.OfMeshgl(Memory(Manifold.AllocManifold()), cutterGl);

        if (Has(cube) && Has(cutter) && (int)Manifold.Status(cube) == 0 && (int)Manifold.Status(cutter) == 0)
        {
            moved = Manifold.Translate(Memory(Manifold.AllocManifold()), cutter, 0.65, 0.65, 0.65);
            cut = Manifold.Difference(Memory(Manifold.AllocManifold()), cube, moved);
        }

        if (Has(cut) && (int)Manifold.Status(cut) == 0 && Manifold.IsEmpty(cut) == 0)
        {
            var outputGl = Manifold.GetMeshglWNormals(Memory(Manifold.AllocMeshgl()), cut, 0);
            if (Has(outputGl))
            {
                var output = R3D.ManifoldMeshGLToMeshData(outputGl);
                if (R3D.IsMeshDataValid(output))
                    result = R3D.LoadMesh(PrimitiveType.Triangles, output, null);
                R3D.UnloadMeshData(output);
                Manifold.DeleteMeshgl(outputGl);
            }
        }

        if (Has(cut)) Manifold.DeleteManifold(cut);
        if (Has(moved)) Manifold.DeleteManifold(moved);
        if (Has(cutter)) Manifold.DeleteManifold(cutter);
        if (Has(cube)) Manifold.DeleteManifold(cube);
        if (Has(cutterGl)) Manifold.DeleteMeshgl(cutterGl);
        if (Has(originalGl)) Manifold.DeleteMeshgl(originalGl);
        R3D.UnloadMeshData(cutterData);
        return result;
    }

    public static int Main()
    {
        InitWindow(1152, 648, "[r3d] Manifold Boolean Test (C#)");
        SetTargetFPS(60);
        R3D.Init(GetScreenWidth(), GetScreenHeight());

        var cubeData = R3D.GenMeshDataCube(2, 2, 2);
        var original = R3D.LoadMesh(PrimitiveType.Triangles, cubeData, null);
        var cut = CreateCutMesh(ref cubeData);
        R3D.UnloadMeshData(cubeData);

        if (!R3D.IsMeshValid(original) || !R3D.IsMeshValid(cut))
        {
            if (R3D.IsMeshValid(original)) R3D.UnloadMesh(original);
            if (R3D.IsMeshValid(cut)) R3D.UnloadMesh(cut);
            R3D.Close();
            CloseWindow();
            return 1;
        }

        var image = GenImageChecked(256, 256, 32, 32, Color.RayWhite, Color.DarkGray);
        var checker = LoadTextureFromImage(image);
        UnloadImage(image);

        var material = R3D.GetDefaultMaterial();
        material.Albedo.Texture = checker;
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Color = new Color(100, 100, 100, 255));
        var light = R3D.CreateDirLight(new Vector3(-0.5f, -1, -0.7f), Color.White, 2);
        var camera = new Camera3D
        {
            Position = new Vector3(3.5f, 2.5f, 3.5f),
            Target = Vector3.Zero, Up = Vector3.UnitY, FovY = 45,
            Projection = CameraProjection.Perspective
        };

        bool showCut = false;
        while (!WindowShouldClose())
        {
            UpdateCamera(ref camera, CameraMode.Orbital);
            if (IsKeyPressed(KeyboardKey.Space)) showCut = !showCut;

            BeginDrawing();
            ClearBackground(Color.RayWhite);
            R3D.Begin(camera);
            R3D.PushLight(light);
            R3D.DrawMesh(showCut ? cut : original, material, Vector3.Zero, 1);
            R3D.End();
            DrawText("SPACE - Toggle Boolean cut", 20, 20, 20, Color.Black);
            DrawText(showCut ? "CUT" : "ORIGINAL", 20, 50, 20, Color.Black);
            EndDrawing();
        }

        R3D.UnloadMesh(cut);
        R3D.UnloadMesh(original);
        UnloadTexture(checker);
        R3D.Close();
        CloseWindow();
        return 0;
    }
}
