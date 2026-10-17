using System;
using System.Collections.Generic;
using System.Numerics;
using R3D_cs;
using Raylib_cs;
using static Raylib_cs.Raylib;
using CubemapLayout = R3D_cs.CubemapLayout;
using Material = R3D_cs.Material;
using Mesh = R3D_cs.Mesh;
using Model = R3D_cs.Model;

namespace Examples;

// Port of r3d/examples/effect_showroom.c: 47 built-render-mode presets in a three-subject gallery.
public static class EffectShowroom
{
    private const int W = 1600, H = 900;
    private const float StageX = 3.65f, StageTop = -1.06f;
    private const int PAlpha = 2, POverlay = 13, PXray = 7, POccluded = 8, PDepthOnly = 14, PStencilTest = 16;
    private const int PHologram = 30, PDissolve = 31, PEmissionPulse = 32, PGlitch = 33;
    private const int PUvScroll = 34, PGradient = 36, PDitherFade = 38, PCutClip = 40, PCombo = 41;

    private static readonly string[] GroupNames =
        ["PIPELINE", "COLOR + TEXTURE", "SILHOUETTE", "ANIMATED", "PROCEDURAL", "COMBINATIONS"];

    private static readonly Color[] GroupColors =
    [
        new(71, 190, 248, 255), new(249, 190, 91, 255), new(165, 119, 255, 255), new(249, 100, 156, 255),
        new(98, 230, 190, 255), new(255, 153, 105, 255)
    ];

    private sealed class Preset(int group, string title, string description, string detail, BuiltRenderMode built)
    {
        public int Group = group;
        public string Title = title, Description = description, Detail = detail;
        public BuiltRenderMode Built = built;
    }

    private sealed class Gallery
    {
        public Texture2D Decal;
        public Mesh Cube, Floor, Pedestal, RimBar, Backdrop, Blocker, StencilSphere;
        public Model Helmet, Character;
        public AnimationLib Animations;
        public AnimationPlayer Player;
        public Cubemap Hdri;
        public AmbientMap Ambient;
        public ShadowMap Shadow;
        public Light Key, Fill, Rim;
        public Material CubeMat, FloorMat, PedestalMat, WallMat, LightMat, BlockerMat;
        public BuiltRenderMode StencilWriter;
        public List<Preset> Presets = new();
    }

    private static void Add(Gallery s, int group, string title, string description, string detail,
        BuiltRenderMode built)
        => s.Presets.Add(new Preset(group, title, description, detail, built));

    private static void MakePresets(Gallery s)
    {
        Color cyan = new(40, 220, 255, 255),
            coral = new(255, 98, 80, 255),
            violet = new(189, 95, 255, 255),
            gold = new(255, 179, 55, 255);

        // Pipeline: 0..17
        Add(s, 0, "BASE MATERIAL", "Unmodified PBR material, direct lighting and shadows.", "REFERENCE",
            R3D.RenderModeBuildDefault());
        Add(s, 0, "UNLIT", "Inspect material textures without scene lighting.", "LIGHTING OFF",
            R3D.RenderModeBuildUnlit());
        Add(s, 0, "ALPHA BLEND", "Transparent forward rendering over the test stage.", "FORWARD",
            R3D.RenderModeBuildAlphaBlend());
        R3D.RenderModeSetOpacity(ref s.Presets[PAlpha].Built, 0.58f);
        Add(s, 0, "ADDITIVE", "Add the object's color to what is behind it.", "BLEND", R3D.RenderModeBuildAdditive());
        Add(s, 0, "MULTIPLY", "Darken the background with the object color.", "BLEND", R3D.RenderModeBuildMultiply());
        Add(s, 0, "SCREEN", "Brighten the background using screen blending.", "BLEND", R3D.RenderModeBuildScreen());
        Add(s, 0, "DOUBLE SIDED", "Render both front and back faces of the subject.", "CULL OFF",
            R3D.RenderModeBuildDoubleSided());
        Add(s, 0, "X-RAY", "Draw the subject even behind a foreground obstacle.", "DEPTH",
            R3D.RenderModeBuildXRay(cyan, 0.82f));
        Add(s, 0, "OCCLUDED ONLY", "Only the portion hidden behind geometry remains visible.", "DEPTH",
            R3D.RenderModeBuildOccludedOnly(coral, 0.88f));
        Add(s, 0, "WIREFRAME", "Expose polygon topology and mesh density.", "GEOMETRY",
            R3D.RenderModeBuildWireframe(cyan));
        Add(s, 0, "WIREFRAME OVERLAY", "Show wire edges over original PBR surfaces.", "GEOMETRY",
            R3D.RenderModeBuildWireframeOverlay(gold));
        Add(s, 0, "SILHOUETTE", "Fill the entire object with a flat stylized color.", "MASK",
            R3D.RenderModeBuildSilhouette(violet));
        Add(s, 0, "SHADOW ONLY", "The object disappears, but still casts a physical shadow.", "SHADOW",
            R3D.RenderModeBuildShadowOnly());
        Add(s, 0, "OVERLAY", "Render the object as a forward overlay.", "FORWARD", R3D.RenderModeBuildOverlay());
        R3D.RenderModeSetOpacity(ref s.Presets[POverlay].Built, 0.8f);
        Add(s, 0, "DEPTH ONLY", "Write depth without visible surface color.", "DEPTH", R3D.RenderModeBuildDepthOnly());
        Add(s, 0, "DITHERED TRANSPARENCY", "Ordered coverage without alpha blending.", "MASK",
            R3D.RenderModeBuildDitheredTransparency(0.55f));
        Add(s, 0, "STENCIL TEST", "Visible only inside three invisible test volumes.", "STENCIL",
            R3D.RenderModeBuildStencilTest(1));
        Add(s, 0, "HARD OUTLINE", "Sharp selection outline around each model.", "SILHOUETTE",
            R3D.RenderModeBuildOutline(cyan, 5, false));

        // Color + texture: 18..25
        Add(s, 1, "COLOR REPLACE", "Replace albedo while retaining PBR lighting.", "COLOR",
            R3D.RenderModeBuildColorReplace(coral));
        Add(s, 1, "COLOR MULTIPLY", "Multiply the original material by aqua.", "COLOR",
            R3D.RenderModeBuildColorMultiply(cyan));
        Add(s, 1, "COLOR MIX", "Blend the original albedo with violet.", "COLOR",
            R3D.RenderModeBuildColorMix(violet, 0.65f));
        Add(s, 1, "COLOR ADD", "Add warm color to the original albedo.", "COLOR", R3D.RenderModeBuildColorAdd(gold));
        Add(s, 1, "COLOR SCREEN", "Screen blend on the material before lighting.", "COLOR",
            R3D.RenderModeBuildColorScreen(cyan));
        Add(s, 1, "EMISSION COLOR", "Inject bright HDR emission into the object.", "HDR",
            R3D.RenderModeBuildEmissionColor(violet, 3));
        Add(s, 1, "ALBEDO TEXTURE", "Layer decal over all subjects.", "TEXTURE",
            R3D.RenderModeBuildAlbedoTexture(s.Decal, TextureBlendMode.Mix, Color.White, 0.8f));
        Add(s, 1, "EMISSION TEXTURE", "Decal as an emissive overlay.", "TEXTURE + HDR",
            R3D.RenderModeBuildEmissionTexture(s.Decal, TextureBlendMode.Add, cyan, 0.8f, 3));

        // Silhouette: 26..29
        Add(s, 2, "RIM GLOW", "HDR Fresnel emission on visible edges.", "CAMERA ANGLE",
            R3D.RenderModeBuildRimGlow(cyan, 4.5f, 2.8f));
        Add(s, 2, "FRESNEL TRANSPARENCY", "Transparent center and opaque edges.", "CAMERA ANGLE",
            R3D.RenderModeBuildFresnelTransparency(0.09f, 0.96f, 2));
        Add(s, 2, "OUTER GLOW", "Soft selected-object screen-space halo.", "SCREEN SPACE",
            R3D.RenderModeBuildOuterGlow(cyan, 17, 4, 3));
        Add(s, 2, "DROP SHADOW", "Blurred and offset silhouette.", "SCREEN SPACE",
            R3D.RenderModeBuildDropShadow(Color.Black, new Vector2(20, 14), 18, 2, 0.85f));

        // Animated: 30..33
        Add(s, 3, "HOLOGRAM", "Scrolling cyan lines and signal dropouts.", "TIME / ALPHA",
            R3D.RenderModeBuildHologram(cyan, 0.78f, 23, 1.3f, 3.2f));
        Add(s, 3, "DISSOLVE", "Procedural holes with a burning edge.", "TIME / SHADOWS",
            R3D.RenderModeBuildDissolve(coral, 0.40f, 4, 0.10f, 3.5f));
        Add(s, 3, "EMISSION PULSE", "Continuous breathing HDR emission.", "TIME / HDR",
            R3D.RenderModeBuildEmissionPulse(violet, 0.2f, 3.4f, 0.65f));
        Add(s, 3, "GLITCH", "Digital corruption and tearing.", "TIME / VERTEX",
            R3D.RenderModeBuildGlitch(0.10f, 12, 17));

        // Procedural: 34..40
        Add(s, 4, "UV SCROLL", "Moving texture coordinates.", "UV / TIME",
            R3D.RenderModeBuildUVScroll(new Vector2(0.22f, 0.10f)));
        Add(s, 4, "UV DISTORTION", "Animated per-fragment UV waves.", "UV / TIME",
            R3D.RenderModeBuildUVDistortion(0.05f, 6, 1.4f));
        Add(s, 4, "GRADIENT", "Local-space violet-to-cyan gradient.", "LOCAL SPACE",
            R3D.RenderModeBuildGradient(violet, cyan, Vector3.UnitY, -1, 1, 0.92f));
        Add(s, 4, "PIXELATE + QUANTIZE", "Coarse UV grid and reduced palette.", "UV / COLOR",
            R3D.RenderModeBuildPixelate(10, 4));
        Add(s, 4, "DITHER FADE", "Ordered fragment coverage.", "TIME / MASK", R3D.RenderModeBuildDitherFade(0.40f, 2));
        Add(s, 4, "DISPLACED SLICE", "Shift horizontal mesh bands.", "VERTEX / TIME",
            R3D.RenderModeBuildSliceDisplace(7, 0.17f, 0.07f, 1.5f));
        Add(s, 4, "CUT CLIP", "Moving clipping plane with HDR edge.", "LOCAL SPACE",
            R3D.RenderModeBuildCutClip(Vector3.UnitY, 0, coral, 0.10f, 4));

        // Composed scopes: 41..46
        Add(s, 5, "HOLOGRAM + FRESNEL", "Holographic scanlines fade toward camera center.", "COMPOSED / 2",
            R3D.RenderModeBuildDefault());
        Add(s, 5, "DISSOLVE + RIM GLOW", "Molten holes and glowing edges.", "COMPOSED / 2",
            R3D.RenderModeBuildDefault());
        Add(s, 5, "OUTER GLOW + DROP SHADOW", "Two independent silhouettes.", "COMPOSED / 2",
            R3D.RenderModeBuildDefault());
        Add(s, 5, "CUT CLIP + EMISSION PULSE", "Laser cut and pulsing energy.", "COMPOSED / 2",
            R3D.RenderModeBuildDefault());
        Add(s, 5, "GLITCH + DITHER FADE", "Digital displacement and disappearance.", "COMPOSED / 2",
            R3D.RenderModeBuildDefault());
        Add(s, 5, "GRADIENT + UV SCROLL", "Gradient over scrolling texture.", "COMPOSED / 2",
            R3D.RenderModeBuildDefault());
    }

    private static int FirstOfGroup(Gallery s, int group) => s.Presets.FindIndex(p => p.Group == group);

    private static void UpdateParams(Gallery s, float phase, bool animate)
    {
        float pulse = 0.5f + 0.5f * MathF.Sin(phase * 0.83f);
        R3D.RenderModeSetProgress(ref s.Presets[PDissolve].Built, animate ? 0.12f + 0.71f * pulse : 0.46f);
        R3D.RenderModeSetProgress(ref s.Presets[PDitherFade].Built,
            animate ? 0.08f + 0.78f * (0.5f + 0.5f * MathF.Sin(phase * 0.55f)) : 0.48f);
        R3D.RenderModeSetCutDistance(ref s.Presets[PCutClip].Built, animate ? 0.5f * MathF.Sin(phase * 0.8f) : 0);
    }

    private static void PushEffect(Gallery s, int selected)
    {
        switch (selected)
        {
            case 41:
                R3D.PushBuiltRenderMode(s.Presets[PHologram].Built);
                R3D.PushBuiltRenderMode(s.Presets[27].Built);
                break;
            case 42:
                R3D.PushBuiltRenderMode(s.Presets[PDissolve].Built);
                R3D.PushBuiltRenderMode(s.Presets[26].Built);
                break;
            case 43:
                R3D.PushBuiltRenderMode(s.Presets[29].Built);
                R3D.PushBuiltRenderMode(s.Presets[28].Built);
                break;
            case 44:
                R3D.PushBuiltRenderMode(s.Presets[PCutClip].Built);
                R3D.PushBuiltRenderMode(s.Presets[PEmissionPulse].Built);
                break;
            case 45:
                R3D.PushBuiltRenderMode(s.Presets[PGlitch].Built);
                R3D.PushBuiltRenderMode(s.Presets[PDitherFade].Built);
                break;
            case 46:
                R3D.PushBuiltRenderMode(s.Presets[PUvScroll].Built);
                R3D.PushBuiltRenderMode(s.Presets[PGradient].Built);
                break;
            default: R3D.PushBuiltRenderMode(s.Presets[selected].Built); break;
        }
    }

    private static void PopEffect(int selected)
    {
        R3D.PopBuiltRenderMode();
        if (selected >= PCombo) R3D.PopBuiltRenderMode();
    }

    private static void SetupEnvironment()
    {
        R3D.SetAntiAliasingMode(AntiAliasingMode.Fxaa);
        R3D.SetAntiAliasingPreset(AntiAliasingPreset.High);
        R3D.SetEnvironmentEx((ref env) =>
        {
            env.Background.Color = new Color(4, 7, 17, 255);
            env.Background.Energy = 1;
            env.Ambient.Color = new Color(55, 78, 125, 255);
            env.Ambient.Energy = 0.75f;
            env.Ssao.Enabled = true;
            env.Ssao.SampleCount = 24;
            env.Ssao.Radius = 0.85f;
            env.Ssao.Intensity = 0.82f;
            env.Bloom.Mode = R3D_cs.Bloom.Mix;
            env.Bloom.Intensity = 0.072f;
            env.Bloom.Threshold = 1;
            env.Bloom.SoftThreshold = 0.65f;
            env.Bloom.Levels = 1;
            env.Tonemap.Mode = Tonemap.Aces;
            env.Tonemap.Exposure = 0.84f;
            env.Tonemap.White = 4;
            env.Color.Contrast = 1.08f;
            env.Color.Saturation = 1.1f;
        });
    }

    private static void InitStudio(Gallery s)
    {
        s.Floor = R3D.GenMeshPlane(24, 19, 1, 1);
        s.Backdrop = R3D.GenMeshCube(16, 7, 0.2f);
        s.Pedestal = R3D.GenMeshCube(2.9f, 0.3f, 2.9f);
        s.RimBar = R3D.GenMeshCube(2.96f, 0.065f, 2.96f);
        s.Blocker = R3D.GenMeshCube(0.78f, 2.75f, 0.23f);
        s.StencilSphere = R3D.GenMeshSphere(0.88f, 36, 36);
        s.Cube = R3D.GenMeshCubeEx(2, 2, 2, 24, 24, 24);

        s.FloorMat = R3D.GetDefaultMaterial();
        s.FloorMat.Albedo.Color = new Color(24, 32, 53, 255);
        s.FloorMat.Orm.Roughness = 0.5f;
        s.FloorMat.Orm.Metalness = 0.26f;
        s.PedestalMat = R3D.GetDefaultMaterial();
        s.PedestalMat.Albedo.Color = new Color(66, 76, 105, 255);
        s.PedestalMat.Orm.Roughness = 0.3f;
        s.PedestalMat.Orm.Metalness = 0.76f;
        s.WallMat = R3D.GetDefaultMaterial();
        s.WallMat.Albedo.Color = new Color(10, 17, 35, 255);
        s.WallMat.Orm.Roughness = 0.9f;
        s.LightMat = R3D.GetDefaultMaterial();
        s.LightMat.Albedo.Color = new Color(25, 65, 95, 255);
        s.LightMat.Emission.Color = new Color(40, 190, 250, 255);
        s.LightMat.Emission.Energy = 5;
        s.BlockerMat = R3D.GetDefaultMaterial();
        s.BlockerMat.Albedo.Color = new Color(190, 206, 225, 255);
        s.BlockerMat.Orm.Metalness = 0.72f;
        s.BlockerMat.Orm.Roughness = 0.25f;
        s.CubeMat = R3D.GetDefaultMaterial();
        s.CubeMat.Albedo.Color = Color.White;
        s.CubeMat.Albedo.Texture = s.Decal;
        s.CubeMat.Orm.Metalness = 0.28f;
        s.CubeMat.Orm.Roughness = 0.48f;
        s.Key = R3D.CreateSpotLight(new Vector3(-3.4f, 7.5f, 7), new Vector3(3.4f, -7, -7), 28,
            new Color(255, 215, 176, 255), 2.7f);
        s.Shadow = R3D.LoadShadowMap(LightType.Spot);
        s.Shadow.Softness = 4;
        s.Fill = R3D.CreateOmniLight(new Vector3(6.2f, 1.8f, 5), 18, new Color(70, 155, 255, 255), 2.25f);
        s.Rim = R3D.CreateSpotLight(new Vector3(2.4f, 4.1f, -4.6f), new Vector3(-2.4f, -4.1f, 4.6f), 22,
            new Color(244, 92, 186, 255), 2.2f);
    }

    private static void DrawStudio(Gallery s, Color accent)
    {
        var frame = s.LightMat;
        frame.Emission.Color = accent;
        R3D.DrawMesh(s.Floor, s.FloorMat, new Vector3(0, -1.74f, 0), 1);
        R3D.DrawMesh(s.Backdrop, s.WallMat, new Vector3(0, 0.5f, -4.25f), 1);
        for (int i = 0; i < 3; ++i)
        {
            float x = (i - 1) * StageX;
            R3D.DrawMesh(s.Pedestal, s.PedestalMat, new Vector3(x, -1.25f, 0), 1);
            R3D.DrawMesh(s.RimBar, frame, new Vector3(x, -1.067f, 0), 1);
        }

        R3D.DrawMesh(s.Blocker, frame, new Vector3(-6.4f, 1, -3.96f), 1);
        R3D.DrawMesh(s.Blocker, frame, new Vector3(6.4f, 1, -3.96f), 1);
    }

    private static Matrix4x4 ModelInPodium(Model model, float x, float targetHeight, float angle)
    {
        Vector3 size = model.Aabb.Max - model.Aabb.Min;
        Vector3 center = (model.Aabb.Min + model.Aabb.Max) * 0.5f;
        float biggest = MathF.Max(size.X, MathF.Max(size.Y, size.Z));
        float scale = biggest > 0.001f ? targetHeight / biggest : 1f;
        return Matrix4x4.CreateTranslation(-center) * Matrix4x4.CreateScale(scale) *
               Matrix4x4.CreateRotationY(angle) *
               Matrix4x4.CreateTranslation(x, StageTop + targetHeight * 0.5f + 0.08f, 0);
    }

    private static void DrawSubjects(Gallery s, int selected, float rotation, bool blocker)
    {
        bool depthDemo = selected == PXray || selected == POccluded || selected == PDepthOnly;
        if (blocker && depthDemo)
            for (int i = 0; i < 3; ++i)
                R3D.DrawMesh(s.Blocker, s.BlockerMat, new Vector3((i - 1) * StageX + 0.43f, -0.1f, 1.47f), 1);
        if (selected == PStencilTest)
        {
            R3D.PushBuiltRenderMode(s.StencilWriter);
            var baseMat = R3D.GetDefaultMaterial();
            for (int i = 0; i < 3; ++i)
                R3D.DrawMesh(s.StencilSphere, baseMat, new Vector3((i - 1) * StageX + 0.3f, 0.24f, 1), 1);
            R3D.PopBuiltRenderMode();
        }

        PushEffect(s, selected);
        var cubeTransform = Matrix4x4.CreateRotationY(rotation + 0.3f) * Matrix4x4.CreateRotationX(-0.18f)
                                                                       * Matrix4x4.CreateTranslation(-StageX, 0.12f, 0);
        R3D.DrawMeshPro(s.Cube, s.CubeMat, cubeTransform);
        R3D.DrawAnimatedModelPro(s.Character, s.Player, ModelInPodium(s.Character, 0, 2.7f, -0.22f));
        R3D.DrawModelPro(s.Helmet, ModelInPodium(s.Helmet, StageX, 2.5f, rotation + 0.3f));
        PopEffect(selected);
    }

    private static Rectangle NavArrow(int w, int h, bool right)
        => new(right ? w - 68 : 16, h * 0.5f - 36, 52, 72);

    private static void DrawArrow(Rectangle bounds, bool right, Color accent, bool hovered)
    {
        Color bg = hovered ? new Color(26, 50, 75, 244) : new Color(11, 23, 42, 230);
        DrawRectangleRounded(bounds, 0.2f, 8, bg);
        DrawRectangleRoundedLinesEx(bounds, 0.2f, 8, 2, Fade(accent, hovered ? 0.95f : 0.45f));
        float x = bounds.X + bounds.Width * 0.5f;
        float y = bounds.Y + bounds.Height * 0.5f;
        float step = right ? 1 : -1;
        Vector2 a = new(x - step * 8, y - 11), b = new(x + step * 8, y), c = new(x - step * 8, y + 11);
        DrawLineEx(a, b, 3.3f, accent);
        DrawLineEx(b, c, 3.3f, accent);
    }

    private static void DrawHud(Gallery s, int selected, float seconds, float transition, bool rotating, bool animate,
        bool blocker)
    {
        int w = GetScreenWidth(), h = GetScreenHeight();
        var p = s.Presets[selected];
        Color accent = GroupColors[p.Group];
        DrawRectangleGradientV(0, 0, w, 194, new Color(5, 10, 22, 250), new Color(6, 12, 25, 220));
        DrawRectangle(0, 191, w, 2, Fade(accent, 0.75f));
        DrawRectangleGradientV(0, h - 181, w, 181, new Color(6, 14, 28, 210), new Color(3, 8, 19, 252));
        DrawRectangle(0, h - 181, w, 2, Fade(accent, 0.55f));
        DrawRectangle(31, 28, 5, 25, accent);
        DrawText("MEATCORPS / R3D", 48, 29, 20, Color.RayWhite);
        DrawText("EFFECTS ATLAS // VISUAL VALIDATION LAB", 315, 32, 16, new Color(140, 164, 190, 255));
        DrawText("FXAA", w - 170, 32, 17, accent);
        DrawText($"{selected + 1:D2} / {s.Presets.Count:D2}", w - 96, 31, 18, Color.RayWhite);
        int tabX = 32;
        for (int i = 0; i < GroupNames.Length; ++i)
        {
            int tw = MeasureText(GroupNames[i], 16) + 34;
            Rectangle rect = new(tabX, 80, tw, 34);
            bool on = p.Group == i;
            DrawRectangleRounded(rect, 0.22f, 6, on ? Fade(accent, 0.20f) : new Color(15, 24, 42, 214));
            DrawRectangleRoundedLinesEx(rect, 0.22f, 6, 1, Fade(on ? accent : Color.LightGray, on ? 0.8f : 0.21f));
            DrawText(GroupNames[i], tabX + 17, 89, 16, on ? Color.RayWhite : new Color(115, 130, 154, 255));
            tabX += tw + 10;
        }

        float entrance = 1 - Math.Clamp(transition / 0.26f, 0, 1);
        int headingX = 49 + (int)((1 - entrance) * 12);
        DrawText(p.Title, headingX, 128, 37, accent);
        DrawText("SELECTED PRESET", headingX + MeasureText(p.Title, 37) + 25, 147, 14, new Color(132, 157, 183, 255));
        DrawText(p.Detail, w - MeasureText(p.Detail, 17) - 46, 142, 17, Color.RayWhite);
        string[] names = ["01 PROCEDURAL CUBE", "02 ANIMATED CHARACTER", "03 DAMAGED HELMET"];
        string[] meta = ["TEXTURE / TESSELLATED", "SKINNED / ANIMATION", "IMPORTED / PBR"];
        for (int i = 0; i < 3; ++i)
        {
            int midX = (int)(w * (0.30f + 0.2f * i));
            DrawText(names[i], midX - MeasureText(names[i], 17) / 2, h - 172, 17, Color.RayWhite);
            DrawText(meta[i], midX - MeasureText(meta[i], 13) / 2, h - 145, 13, new Color(126, 146, 170, 255));
            DrawRectangle(midX - 105, h - 121, 210, 2, Fade(accent, 0.42f + 0.15f * MathF.Sin(seconds * 1.8f + i)));
        }

        DrawText(p.Description, 62, h - 101, 20, new Color(206, 219, 235, 255));
        DrawText("LEFT/RIGHT MODE    TAB CATEGORY    SPACE ROTATE    A PARAM SWEEP    B OBSTACLE    R RESET", 62,
            h - 58, 15, new Color(130, 155, 183, 255));
        DrawText(
            $"{(rotating ? "ROTATING" : "PAUSED")} | {(animate ? "SWEEP ON" : "SWEEP OFF")} | {(blocker ? "OBSTACLE ON" : "OBSTACLE OFF")}",
            w - 375, h - 58, 15, accent);
        Vector2 mouse = GetMousePosition();
        var left = NavArrow(w, h, false);
        var right = NavArrow(w, h, true);
        DrawArrow(left, false, accent, CheckCollisionPointRec(mouse, left));
        DrawArrow(right, true, accent, CheckCollisionPointRec(mouse, right));
        float step = (w - 62f) / s.Presets.Count;
        for (int i = 0; i < s.Presets.Count; ++i)
        {
            Rectangle tick = new(31 + i * step, h - 8, MathF.Max(2, step - 3), 4);
            DrawRectangleRec(tick,
                i == selected ? accent : i < selected ? Fade(accent, 0.45f) : new Color(44, 60, 80, 255));
        }
    }

    private static int NavigateMouse(Gallery s, int selected)
    {
        if (!IsMouseButtonPressed(MouseButton.Left)) return selected;
        Vector2 mouse = GetMousePosition();
        int w = GetScreenWidth(), h = GetScreenHeight();
        if (CheckCollisionPointRec(mouse, NavArrow(w, h, false)))
            return (selected + s.Presets.Count - 1) % s.Presets.Count;
        if (CheckCollisionPointRec(mouse, NavArrow(w, h, true))) return (selected + 1) % s.Presets.Count;
        int tabX = 32;
        for (int i = 0; i < GroupNames.Length; ++i)
        {
            int tw = MeasureText(GroupNames[i], 16) + 34;
            if (CheckCollisionPointRec(mouse, new Rectangle(tabX, 80, tw, 34))) return FirstOfGroup(s, i);
            tabX += tw + 10;
        }

        return selected;
    }

    private static void Dispose(Gallery s)
    {
        foreach (Preset p in s.Presets) R3D.UnloadBuiltRenderMode(ref p.Built);
        R3D.UnloadBuiltRenderMode(ref s.StencilWriter);
        R3D.UnloadShadowMap(s.Shadow);
        R3D.UnloadAnimationPlayer(s.Player);
        R3D.UnloadAnimationLib(s.Animations);
        R3D.UnloadModel(s.Helmet, true);
        R3D.UnloadModel(s.Character, true);
        foreach (Mesh m in new[]
                     { s.Cube, s.Floor, s.Pedestal, s.RimBar, s.Backdrop, s.Blocker, s.StencilSphere })
            R3D.UnloadMesh(m);
        R3D.UnloadTexture(s.Decal);
        R3D.UnloadAmbientMap(s.Ambient);
        R3D.UnloadCubemap(s.Hdri);
    }

    public static int Main()
    {
        InitWindow(W, H, "[r3d] Meatcorps - Effects Atlas (C#)");
        SetTargetFPS(60);
        if (!R3D.Init(W, H))
        {
            CloseWindow();
            return 1;
        }

        SetupEnvironment();
        R3D.SetTextureFilter(TextureFilter.Anisotropic4X);
        var s = new Gallery();
        s.Decal = R3D.LoadTextureEx(
            "resources/images/decal.png",
            TextureWrap.Repeat,
            TextureFilter.Bilinear,
            true);
        s.Helmet = R3D.LoadModel("resources/models/DamagedHelmet.glb");
        s.Character = R3D.LoadModel("resources/models/CesiumMan.glb");
        if (s.Decal.Id == 0 || s.Helmet.Meshes.IsEmpty || s.Character.Meshes.IsEmpty)
        {
            if (!s.Helmet.Meshes.IsEmpty) R3D.UnloadModel(s.Helmet, true);
            if (!s.Character.Meshes.IsEmpty) R3D.UnloadModel(s.Character, true);
            if (s.Decal.Id != 0) R3D.UnloadTexture(s.Decal);
            R3D.Close();
            CloseWindow();
            return 1;
        }

        s.Animations = R3D.LoadAnimationLib("resources/models/CesiumMan.glb");
        s.Player = R3D.LoadAnimationPlayer(s.Character.Skeleton, s.Animations);
        R3D.SetAnimationLoop(ref s.Player, 0, true);
        R3D.PlayAnimation(ref s.Player, 0);
        s.Hdri = R3D.LoadCubemap("resources/panorama/indoor.hdr", CubemapLayout.AutoDetect);
        s.Ambient = R3D.GenAmbientMap(s.Hdri, AmbientFlags.Illumination | AmbientFlags.Reflection);
        R3D.SetEnvironmentEx((ref env) => env.Ambient.Map = s.Ambient);
        InitStudio(s);
        MakePresets(s);
        s.StencilWriter = R3D.RenderModeBuildStencilMask(1);
        var camera = new Camera3D
        {
            Position = new Vector3(0, 2.5f, 13.7f), Target = new Vector3(0, -0.1f, 0),
            Up = Vector3.UnitY, FovY = 42, Projection = CameraProjection.Perspective
        };

        int selected = 41, previous = selected;
        bool rotating = true, animate = true, blocker = true;
        float phase = 0, rotation = 0, transition = 0;
        while (!WindowShouldClose())
        {
            float dt = MathF.Min(GetFrameTime(), 0.05f);
            if (IsKeyPressed(KeyboardKey.Right)) selected = (selected + 1) % s.Presets.Count;
            if (IsKeyPressed(KeyboardKey.Left)) selected = (selected + s.Presets.Count - 1) % s.Presets.Count;
            if (IsKeyPressed(KeyboardKey.Tab))
            {
                bool backwards = IsKeyDown(KeyboardKey.LeftShift) || IsKeyDown(KeyboardKey.RightShift);
                int group = s.Presets[selected].Group;
                selected = FirstOfGroup(s, (group + (backwards ? GroupNames.Length - 1 : 1)) % GroupNames.Length);
            }

            if (IsKeyPressed(KeyboardKey.Space)) rotating = !rotating;
            if (IsKeyPressed(KeyboardKey.A)) animate = !animate;
            if (IsKeyPressed(KeyboardKey.B)) blocker = !blocker;
            if (IsKeyPressed(KeyboardKey.R)) phase = 0;
            selected = NavigateMouse(s, selected);
            if (selected != previous)
            {
                previous = selected;
                transition = 0.26f;
            }

            transition = MathF.Max(0, transition - dt);
            if (animate) phase += dt;
            if (rotating) rotation += dt;
            R3D.UpdateAnimationPlayer(ref s.Player, dt);
            UpdateParams(s, phase, animate);

            BeginDrawing();
            ClearBackground(new Color(4, 7, 16, 255));
            R3D.Begin(camera);
            R3D.PushLightEx(s.Key, s.Shadow, true);
            R3D.PushLight(s.Fill);
            R3D.PushLight(s.Rim);
            DrawStudio(s, GroupColors[s.Presets[selected].Group]);
            DrawSubjects(s, selected, rotation, blocker);
            R3D.End();
            DrawHud(s, selected, phase, transition, rotating, animate, blocker);
            EndDrawing();
        }

        Dispose(s);
        R3D.Close();
        CloseWindow();
        return 0;
    }
}
