using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Raylib_cs;

// ReSharper disable InconsistentNaming

namespace R3D_cs;

/// <summary>
///     Handwritten utility methods extending the generated R3D bindings.
///     This file is preserved when regenerating bindings.
/// </summary>
public static unsafe partial class R3D
{
    /// <summary>
    ///     Delegate for updating environment settings by reference.
    /// </summary>
    /// <param name="env">Reference to the environment to modify.</param>
    public delegate void EnvironmentUpdater(ref Environment env);

    /// <summary>
    ///     Gets a default material with sensible base values.
    ///     Use this as a starting point when creating new materials.
    /// </summary>
    /// <remarks>
    ///     Default values:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Albedo: White color, no texture</description>
    ///         </item>
    ///         <item>
    ///             <description>Emission: White color, no texture, 0 energy</description>
    ///         </item>
    ///         <item>
    ///             <description>Normal: No texture, scale 1.0</description>
    ///         </item>
    ///         <item>
    ///             <description>ORM: Occlusion 1.0, Roughness 1.0, Metalness 0.0</description>
    ///         </item>
    ///         <item>
    ///             <description>UV: Offset (0, 0), Scale (1, 1)</description>
    ///         </item>
    ///         <item>
    ///             <description>Alpha Cutoff: 0.01</description>
    ///         </item>
    ///         <item>
    ///             <description>Depth: Standard depth test (LESS)</description>
    ///         </item>
    ///         <item>
    ///             <description>Stencil: Disabled (ALWAYS)</description>
    ///         </item>
    ///         <item>
    ///             <description>Transparency: Disabled</description>
    ///         </item>
    ///         <item>
    ///             <description>Billboard: Disabled</description>
    ///         </item>
    ///         <item>
    ///             <description>Blend: Mix</description>
    ///         </item>
    ///         <item>
    ///             <description>Cull: Back</description>
    ///         </item>
    ///         <item>
    ///             <description>Unlit: false</description>
    ///         </item>
    ///         <item>
    ///             <description>Shader: null</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static Material MATERIAL_BASE =>
        new()
        {
            Albedo = new AlbedoMap
            {
                Texture = default,
                Color = new Color(255, 255, 255, 255)
            },
            Emission = new EmissionMap
            {
                Texture = default,
                Color = new Color(255, 255, 255, 255),
                Energy = 0.0f
            },
            Normal = new NormalMap
            {
                Texture = default,
                Scale = 1.0f
            },
            Orm = new OrmMap
            {
                Texture = default,
                Occlusion = 1.0f,
                Roughness = 1.0f,
                Metalness = 0.0f,
                Specular = 0.5f
            },
            UvOffset = Vector2.Zero,
            UvScale = Vector2.One,
            AlphaCutoff = 0.01f,
            Depth = new DepthState
            {
                Mode = CompareMode.Less,
                OffsetFactor = 0.0f,
                OffsetUnits = 0.0f,
                RangeNear = 0.0f,
                RangeFar = 1.0f
            },
            Stencil = new StencilState
            {
                Mode = CompareMode.Always,
                Ref = 0x00,
                Mask = 0xFF,
                OpFail = StencilOp.StencilKeep,
                OpZFail = StencilOp.StencilKeep,
                OpPass = StencilOp.StencilReplace
            },
            TransparencyMode = TransparencyMode.Blend,
            BillboardMode = BillboardMode.Disabled,
            BlendMode = BlendMode.Alpha,
            CullMode = CullMode.Back,
            Unlit = false,
            Priority = 0,
            Shader = default
        };

    /// <summary>
    ///     Gets a default R3D camera with sensible base values (matching <c>R3D_CAMERA_BASE</c>).
    ///     Use this as a starting point when creating a quaternion-based <see cref="Camera" />.
    /// </summary>
    /// <remarks>
    ///     Perspective projection, 60° vertical FOV, near 0.05, far 4000, identity rotation,
    ///     positioned at the origin, and rendering all layers.
    /// </remarks>
    public static Camera CAMERA_BASE =>
        new()
        {
            Position = Vector3.Zero,
            Rotation = Quaternion.Identity,
            Fovy = 60.0,
            NearPlane = 0.05,
            FarPlane = 4000.0,
            CullMask = Layer.All,
            Projection = Projection.Perspective
        };

    /// <summary>
    ///     Gets default procedural sky parameters for cubemap generation.
    ///     Use this as a starting point when creating procedural skyboxes with <see cref="GenProceduralSky" />.
    /// </summary>
    /// <remarks>
    ///     Creates a sky with blue-gray tones, brown ground, and a white sun.
    /// </remarks>
    public static ProceduralSky PROCEDURAL_SKY_BASE =>
        new()
        {
            SkyTopColor = new Color(98, 116, 140, 255),
            SkyHorizonColor = new Color(165, 167, 171, 255),
            SkyHorizonCurve = 0.15f,
            SkyEnergy = 1.0f,
            GroundBottomColor = new Color(51, 43, 34, 255),
            GroundHorizonColor = new Color(165, 167, 171, 255),
            GroundHorizonCurve = 0.02f,
            GroundEnergy = 1.0f,
            SunDirection = new Vector3(-1.0f, -1.0f, -1.0f),
            SunColor = new Color(255, 255, 255, 255),
            SunSize = 1.0f * Raylib.DEG2RAD,
            SunCurve = 0.15f,
            SunEnergy = 1.0f
        };

    /// <summary>
    ///     Gets default decal parameters.
    ///     Use this as a starting point when creating decals.
    /// </summary>
    /// <remarks>
    ///     Default values:
    ///     <list type="bullet">
    ///         <item>
    ///             <description>Albedo: White color, no texture</description>
    ///         </item>
    ///         <item>
    ///             <description>Emission: White color, no texture, 0 energy</description>
    ///         </item>
    ///         <item>
    ///             <description>Normal: No texture, scale 1.0</description>
    ///         </item>
    ///         <item>
    ///             <description>ORM: Occlusion 1.0, Roughness 1.0, Metalness 0.0</description>
    ///         </item>
    ///         <item>
    ///             <description>UV: Offset (0, 0), Scale (1, 1)</description>
    ///         </item>
    ///         <item>
    ///             <description>Alpha Cutoff: 0.01</description>
    ///         </item>
    ///         <item>
    ///             <description>Normal Threshold: 0</description>
    ///         </item>
    ///         <item>
    ///             <description>Fade Width: 0</description>
    ///         </item>
    ///         <item>
    ///             <description>ApplyColor: true</description>
    ///         </item>
    ///         <item>
    ///             <description>Shader: null</description>
    ///         </item>
    ///     </list>
    /// </remarks>
    public static Decal DECAL_BASE =>
        new()
        {
            Albedo = new AlbedoMap
            {
                Texture = default,
                Color = new Color(255, 255, 255, 255)
            },
            Emission = new EmissionMap
            {
                Texture = default,
                Color = new Color(255, 255, 255, 255),
                Energy = 0.0f
            },
            Normal = new NormalMap
            {
                Texture = default,
                Scale = 1.0f
            },
            Orm = new OrmMap
            {
                Texture = default,
                Occlusion = 1.0f,
                Roughness = 1.0f,
                Metalness = 0.0f,
                Specular = 0.5f
            },
            UvOffset = Vector2.Zero,
            UvScale = Vector2.One,
            AlphaCutoff = 0.01f,
            NormalThreshold = 0,
            FadeWidth = 0,
            ApplyColor = true,
            Shader = default
        };

    /// <summary>
    ///     Gets a reference to the current environment settings.
    ///     Allows direct modification of environment properties without copying.
    /// </summary>
    /// <returns>A reference to the current <see cref="Environment" />.</returns>
    public static ref Environment GetEnvironmentEx()
    {
        return ref Unsafe.AsRef<Environment>(GetEnvironment());
    }

    /// <summary>
    ///     Updates the current environment settings using a callback.
    ///     This is the recommended way to modify environment settings.
    /// </summary>
    /// <param name="updater">A delegate that receives the environment by reference for modification.</param>
    /// <example>
    ///     <code>
    /// R3D.SetEnvironmentEx((ref env) =>
    /// {
    ///     env.Bloom.Mode = Bloom.Mix;
    ///     env.Tonemap.Mode = Tonemap.Filmic;
    /// });
    /// </code>
    /// </example>
    public static void SetEnvironmentEx(EnvironmentUpdater updater)
    {
        updater(ref GetEnvironmentEx());
    }

    /// <summary>
    ///     Sets the environment from a struct value.
    ///     Useful for restoring a previously saved environment.
    /// </summary>
    /// <param name="env">The environment settings to apply.</param>
    /// <example>
    ///     <code>
    /// // Save current environment
    /// Environment saved = R3D.GetEnvironmentEx();
    ///
    /// // Modify current...
    /// R3D.SetEnvironmentEx((ref env) => env.Bloom.Mode = Bloom.Additive);
    ///
    /// // Restore saved environment
    /// R3D.SetEnvironmentEx(saved);
    /// </code>
    /// </example>
    public static void SetEnvironmentEx(Environment env)
    {
        SetEnvironment(&env);
    }

    /// <summary>
    ///     Maps instance buffer data to a typed span for direct CPU access.
    ///     Call <see cref="UnmapInstances" /> when done writing.
    /// </summary>
    /// <typeparam name="T">The unmanaged type to map (e.g., Vector3 for positions, Color for colors).</typeparam>
    /// <param name="buffer">The instance buffer to map.</param>
    /// <param name="flag">Which instance attribute to map (Position, Rotation, Scale, or Color).</param>
    /// <param name="discard">
    ///     If true, existing buffer contents are invalidated, allowing the driver to return a fresh
    ///     memory region without stalling on in-flight GPU reads.
    /// </param>
    /// <returns>A span allowing direct read/write access to the instance data.</returns>
    /// <example>
    ///     <code>
    /// var positions = R3D.MapInstances&lt;Vector3&gt;(instances, InstanceFlags.Position);
    /// for (int i = 0; i &lt; 100; i++)
    ///     positions[i] = new Vector3(i, 0, 0);
    /// R3D.UnmapInstances(instances, InstanceFlags.Position);
    /// </code>
    /// </example>
    public static Span<T> MapInstances<T>(InstanceBuffer buffer, InstanceFlags flag, bool discard = false) where T : unmanaged
    {
        return new Span<T>((void*)MapInstances(buffer, flag, discard), buffer.Capacity);
    }

    /// <summary>
    ///     Uploads instance data from a span to the GPU.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the data (e.g., Vector3 for positions).</typeparam>
    /// <param name="buffer">The instance buffer to upload to.</param>
    /// <param name="flag">Which instance attribute to upload (Position, Rotation, Scale, or Color).</param>
    /// <param name="offset">Starting index in the buffer to upload to.</param>
    /// <param name="data">The source data span.</param>
    /// <param name="count">Number of elements to upload. If null, uploads the entire span.</param>
    /// <param name="discard">
    ///     If true, the entire GPU buffer is orphaned before upload, avoiding a GPU/CPU sync at the
    ///     cost of discarding existing data. Safe when rewriting the full buffer each frame.
    /// </param>
    /// <example>
    ///     <code>
    /// Span&lt;Vector3&gt; positions = stackalloc Vector3[1000];
    /// // ... fill positions ...
    /// R3D.UploadInstances(instances, InstanceFlags.Position, 0, positions, activeCount);
    /// </code>
    /// </example>
    public static void UploadInstances<T>(InstanceBuffer buffer, InstanceFlags flag, int offset, ReadOnlySpan<T> data, int? count = null, bool discard = false) where T : unmanaged
    {
        int length = count ?? data.Length;
        if (length == 0) return;
        fixed (T* ptr = data)
        {
            UploadInstances(buffer, flag, offset, length, ptr, discard);
        }
    }

    // ---- Generic uniform convenience overloads ----

    /// <summary>
    ///     Sets a uniform value on a screen shader using a typed ref instead of void*.
    /// </summary>
    /// <typeparam name="T">The unmanaged uniform type (e.g., float, Vector2, Matrix4x4).</typeparam>
    /// <param name="shader">Target screen shader.</param>
    /// <param name="name">Name of the uniform.</param>
    /// <param name="value">Reference to the uniform value.</param>
    public static void SetScreenShaderUniform<T>(ScreenShader shader, string name, ref T value) where T : unmanaged
    {
        SetScreenShaderUniform(shader, name, Unsafe.AsPointer(ref value));
    }

    /// <summary>
    ///     Sets a uniform value on a surface shader using a typed ref instead of void*.
    /// </summary>
    /// <typeparam name="T">The unmanaged uniform type (e.g., float, Vector2, Matrix4x4).</typeparam>
    /// <param name="shader">Target surface shader.</param>
    /// <param name="name">Name of the uniform.</param>
    /// <param name="value">Reference to the uniform value.</param>
    public static void SetSurfaceShaderUniform<T>(SurfaceShader shader, string name, ref T value) where T : unmanaged
    {
        SetSurfaceShaderUniform(shader, name, Unsafe.AsPointer(ref value));
    }

    /// <summary>
    ///     Sets a uniform value on a sky shader using a typed ref instead of void*.
    /// </summary>
    /// <typeparam name="T">The unmanaged uniform type (e.g., float, Vector2, Matrix4x4).</typeparam>
    /// <param name="shader">Target sky shader.</param>
    /// <param name="name">Name of the uniform.</param>
    /// <param name="value">Reference to the uniform value.</param>
    public static void SetSkyShaderUniform<T>(SkyShader shader, string name, ref T value) where T : unmanaged
    {
        SetSkyShaderUniform(shader, name, Unsafe.AsPointer(ref value));
    }

    /// <summary>
    ///     Creates a mesh data container with counts pre-set to the requested sizes.
    ///     Unlike <see cref="LoadMeshData" /> (which starts with count 0 for append workflows),
    ///     this method sets vertex/index counts equal to their capacities so that
    ///     <see cref="MeshData.Vertices" /> and <see cref="MeshData.Indices" /> are
    ///     immediately writable at full size.
    /// </summary>
    /// <param name="vertexCount">Number of vertices to allocate and mark as valid.</param>
    /// <param name="indexCount">Number of indices to allocate and mark as valid.</param>
    /// <returns>A new MeshData ready for direct Span writes.</returns>
    public static MeshData CreateMeshData(int vertexCount, int indexCount)
    {
        var data = LoadMeshData(vertexCount, indexCount);
        data.VertexCount = vertexCount;
        data.IndexCount = indexCount;
        return data;
    }

    /// <summary>
    ///     Creates a 3D mesh from CPU-side mesh data, computing the bounding box automatically.
    /// </summary>
    /// <param name="type">Primitive type used to interpret vertex data.</param>
    /// <param name="data">MeshData containing vertices and indices.</param>
    /// <returns>Created Mesh.</returns>
    public static Mesh LoadMesh(PrimitiveType type, MeshData data)
        => LoadMesh(type, data, null);

    /// <summary>
    ///     Creates a 3D mesh from CPU-side mesh data with an explicit bounding box.
    /// </summary>
    /// <param name="type">Primitive type used to interpret vertex data.</param>
    /// <param name="data">MeshData containing vertices and indices.</param>
    /// <param name="aabb">Bounding box for the mesh.</param>
    /// <returns>Created Mesh.</returns>
    public static Mesh LoadMesh(PrimitiveType type, MeshData data, ref BoundingBox aabb)
    {
        fixed (BoundingBox* ptr = &aabb)
            return LoadMesh(type, data, ptr);
    }

    /// <summary>
    ///     Updates an existing mesh with new CPU-side data, computing the bounding box automatically.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="data">New MeshData to upload.</param>
    /// <returns>True if the update was successful.</returns>
    public static bool UpdateMesh(ref Mesh mesh, MeshData data)
        => UpdateMesh(ref mesh, data, null);

    /// <summary>
    ///     Updates an existing mesh with new CPU-side data and an explicit bounding box.
    /// </summary>
    /// <param name="mesh">The mesh to update.</param>
    /// <param name="data">New MeshData to upload.</param>
    /// <param name="aabb">Bounding box for the mesh.</param>
    /// <returns>True if the update was successful.</returns>
    public static bool UpdateMesh(ref Mesh mesh, MeshData data, ref BoundingBox aabb)
    {
        fixed (BoundingBox* ptr = &aabb)
            return UpdateMesh(ref mesh, data, ptr);
    }

    /// <summary>
    ///     Sets the screen shader chain for a given stage from a span of shaders.
    /// </summary>
    /// <param name="stage">Screen shader stage to configure.</param>
    /// <param name="shaders">The shaders to execute, in order.</param>
    public static void SetScreenShaderChain(ScreenShaderStage stage, ReadOnlySpan<ScreenShader> shaders)
    {
        fixed (ScreenShader* ptr = shaders)
            SetScreenShaderChain(stage, ptr, shaders.Length);
    }


    /// <summary>
    ///     Maps a subrange of an instance buffer to a typed span for direct CPU access.
    ///     Call <see cref="UnmapInstances" /> when finished writing.
    /// </summary>
    /// <typeparam name="T">The unmanaged type stored in the attribute buffer.</typeparam>
    /// <param name="buffer">The instance buffer to map.</param>
    /// <param name="flag">The instance attribute to map.</param>
    /// <param name="offset">Starting instance index.</param>
    /// <param name="count">Number of instances to map.</param>
    /// <param name="discard">Whether to invalidate existing contents in the mapped range.</param>
    /// <returns>A span over the mapped GPU memory, valid until the buffer is unmapped.</returns>
    public static Span<T> MapInstancesEx<T>(
        InstanceBuffer buffer,
        InstanceFlags flag,
        int offset,
        int count,
        bool discard = false) where T : unmanaged
    {
        return new Span<T>(
            (void*)MapInstancesEx(buffer, flag, offset, count, discard),
            count);
    }

    /// <summary>
    ///     Sets a uniform value on a surface effect using a typed reference
    ///     instead of a raw pointer.
    /// </summary>
    /// <typeparam name="T">The unmanaged type of the uniform value.</typeparam>
    /// <param name="effect">The surface effect to update.</param>
    /// <param name="name">The uniform name.</param>
    /// <param name="value">The uniform value.</param>
    public static void SetSurfaceEffectUniform<T>(
        SurfaceEffect effect,
        string name,
        ref T value) where T : unmanaged
    {
        fixed (T* ptr = &value)
            SetSurfaceEffectUniform(effect, name, ptr);
    }

    /// <summary>
    ///     Populates four line points to form a rectangle in the XY plane.
    ///     Existing point colors and widths are preserved.
    /// </summary>
    /// <param name="points">Destination span containing at least four line points.</param>
    /// <param name="center">Center of the rectangle.</param>
    /// <param name="size">Width and height of the rectangle.</param>
    public static void SetLineRectangle(
        Span<LinePoint> points,
        Vector3 center,
        Vector2 size)
    {
        fixed (LinePoint* ptr = points[..4])
            SetLineRectangle(ptr, center, size);
    }

    /// <summary>
    ///     Populates four line points to form a circle using quarter-circle arcs.
    ///     Existing point colors and widths are preserved.
    /// </summary>
    /// <param name="points">Destination span containing at least four line points.</param>
    /// <param name="center">Center of the circle.</param>
    /// <param name="radius">Radius of the circle.</param>
    public static void SetLineCircle(
        Span<LinePoint> points,
        Vector3 center,
        float radius)
    {
        fixed (LinePoint* ptr = points[..4])
            SetLineCircle(ptr, center, radius);
    }

    /// <summary>
    ///     Populates a line path with points forming an ellipse in the XY plane.
    ///     The number of generated points is determined by the span length.
    /// </summary>
    /// <param name="points">Destination span containing at least three line points.</param>
    /// <param name="center">Center of the ellipse.</param>
    /// <param name="radii">Horizontal and vertical radii.</param>
    public static void SetLineEllipse(
        Span<LinePoint> points,
        Vector3 center,
        Vector2 radii)
    {
        if (points.Length < 3)
            return;

        fixed (LinePoint* ptr = points)
            SetLineEllipse(ref *ptr, points.Length, center, radii);
    }

    /// <summary>
    ///     Configures two line points as the endpoints of an arc.
    ///     Existing point colors and widths are preserved.
    /// </summary>
    /// <param name="points">Destination span containing at least two line points.</param>
    /// <param name="start">Starting position of the arc.</param>
    /// <param name="end">Ending position of the arc.</param>
    /// <param name="normal">Normal defining the arc plane.</param>
    /// <param name="sweep">Sweep angle in radians.</param>
    /// <param name="subdivisions">Number of subdivisions used to render the arc.</param>
    public static void SetLineArc(
        Span<LinePoint> points,
        Vector3 start,
        Vector3 end,
        Vector3 normal,
        float sweep,
        int subdivisions)
    {
        fixed (LinePoint* ptr = points[..2])
            SetLineArc(ptr, start, end, normal, sweep, subdivisions);
    }

    /// <summary>
    ///     Computes the eight corner positions of a view frustum.
    /// </summary>
    /// <param name="invViewProj">Inverse view-projection matrix.</param>
    /// <param name="corners">Destination span containing at least eight vectors.</param>
    public static void ComputeFrustumCorners(
        Matrix4x4 invViewProj,
        Span<Vector3> corners)
    {
        fixed (Vector3* ptr = corners[..8])
            ComputeFrustumCorners(invViewProj, ptr);
    }

    /// <summary>
    ///     Replaces the visible points in an existing point cloud.
    ///     Reuses the point cloud's allocated GPU buffers.
    /// </summary>
    /// <param name="cloud">The point cloud to update.</param>
    /// <param name="points">The new collection of visible points.</param>
    /// <returns>True if the point cloud was updated successfully.</returns>
    public static bool UpdatePointCloud(
        ref PointCloud cloud,
        ReadOnlySpan<PointCloudPoint> points)
    {
        fixed (PointCloudPoint* ptr = points)
        {
            return UpdatePointCloud(
                ref cloud,
                ref Unsafe.AsRef<PointCloudPoint>(ptr),
                points.Length);
        }
    }

    /// <summary>
    ///     Appends vertices and indices to an existing CPU-side mesh data container.
    ///     The destination container grows when additional capacity is required.
    /// </summary>
    /// <param name="meshData">The mesh data container to modify.</param>
    /// <param name="vertices">Vertices to append.</param>
    /// <param name="indices">Indices to append.</param>
    public static void AppendMeshData(
        ref MeshData meshData,
        ReadOnlySpan<Vertex> vertices,
        ReadOnlySpan<uint> indices)
    {
        fixed (Vertex* vertexPtr = vertices)
        fixed (uint* indexPtr = indices)
        {
            AppendMeshData(
                ref meshData,
                ref Unsafe.AsRef<Vertex>(vertexPtr),
                vertices.Length,
                ref Unsafe.AsRef<uint>(indexPtr),
                indices.Length);
        }
    }

    /// <summary>
    ///     Creates a manifold extrusion from a two-dimensional polygon outline.
    /// </summary>
    /// <param name="mesh">Destination mesh to populate with the extrusion.</param>
    /// <param name="outline">Ordered polygon vertices defining the outline.</param>
    /// <param name="height">Extrusion height.</param>
    /// <returns>The manifold operation result.</returns>
    public static ManifoldResult ManifoldExtrude(
        ref Mesh mesh,
        ReadOnlySpan<Vector2> outline,
        float height)
    {
        fixed (Vector2* ptr = outline)
        {
            return ManifoldExtrude(
                ref mesh,
                ref Unsafe.AsRef<Vector2>(ptr),
                outline.Length,
                height);
        }
    }

    /// <summary>
    ///     Creates a spring chain from a collection of bone descriptors.
    ///     The native implementation sorts the descriptors into parent-child order.
    /// </summary>
    /// <param name="player">Animation player containing the skeleton.</param>
    /// <param name="descriptors">Bone descriptors defining the spring chain.</param>
    /// <returns>The created spring chain, or a default value if creation fails.</returns>
    public static SpringChain LoadSpringChain(
        ref AnimationPlayer player,
        ReadOnlySpan<SpringChainBoneDesc> descriptors)
    {
        if (descriptors.IsEmpty)
            return default;

        fixed (SpringChainBoneDesc* ptr = descriptors)
        {
            return LoadSpringChain(
                ref player,
                ref *ptr,
                descriptors.Length);
        }
    }

    /// <summary>
    ///     Expands a bounding box by the specified padding along each axis.
    /// </summary>
    /// <param name="bounds">Original bounding box.</param>
    /// <param name="padding">Padding to apply along each axis.</param>
    /// <param name="result">The expanded bounding box.</param>
    /// <returns>True if the expanded bounding box was calculated successfully.</returns>
    public static bool ExpandBoundingBox(
        BoundingBox bounds,
        Vector3 padding,
        out BoundingBox result)
    {
        result = default;

        fixed (BoundingBox* ptr = &result)
            return ExpandBoundingBox(bounds, padding, ptr);
    }

    /// <summary>
    ///     Calculates conservative bounds for a mesh affected by shape keys.
    /// </summary>
    /// <param name="baseBounds">The original mesh bounding box.</param>
    /// <param name="gpu">GPU shape-key data.</param>
    /// <param name="instance">Current shape-key instance state.</param>
    /// <param name="result">The calculated bounding box.</param>
    /// <returns>True if the bounds were calculated successfully.</returns>
    public static bool CalculateConservativeShapeKeyBounds(
        BoundingBox baseBounds,
        ref ShapeKeyGpu gpu,
        ref ShapeKeyInstance instance,
        out BoundingBox result)
    {
        result = default;

        fixed (BoundingBox* ptr = &result)
        {
            return CalculateConservativeShapeKeyBounds(
                baseBounds,
                ref gpu,
                ref instance,
                ptr);
        }
    }

    /// <summary>
    ///     Loads an animation library from a memory buffer.
    /// </summary>
    /// <param name="data">The file contents.</param>
    /// <param name="hint">Hint identifying the source file format.</param>
    /// <returns>The loaded animation library.</returns>
    public static AnimationLib LoadAnimationLibFromMemory(
        ReadOnlySpan<byte> data,
        string hint)
    {
        fixed (byte* ptr = data)
            return LoadAnimationLibFromMemory(ptr, (uint)data.Length, hint);
    }

    /// <summary>
    ///     Creates an importer from model data stored in memory.
    /// </summary>
    /// <param name="data">The source model data.</param>
    /// <param name="hint">Hint identifying the source file format.</param>
    /// <param name="flags">Import processing options.</param>
    /// <returns>The loaded importer.</returns>
    public static Importer LoadImporterFromMemory(
        ReadOnlySpan<byte> data,
        string hint,
        ImportFlags flags)
    {
        fixed (byte* ptr = data)
            return LoadImporterFromMemory(ptr, (uint)data.Length, hint, flags);
    }

    /// <summary>
    ///     Loads a model from data stored in memory using default import options.
    /// </summary>
    /// <param name="data">The source model data.</param>
    /// <param name="hint">Hint identifying the source file format.</param>
    /// <returns>The loaded model.</returns>
    public static Model LoadModelFromMemory(
        ReadOnlySpan<byte> data,
        string hint)
    {
        fixed (byte* ptr = data)
            return LoadModelFromMemory(ptr, (uint)data.Length, hint);
    }

    /// <summary>
    ///     Loads a model from data stored in memory with custom import options.
    /// </summary>
    /// <param name="data">The source model data.</param>
    /// <param name="hint">Hint identifying the source file format.</param>
    /// <param name="flags">Import processing options.</param>
    /// <returns>The loaded model.</returns>
    public static Model LoadModelFromMemoryEx(
        ReadOnlySpan<byte> data,
        string hint,
        ImportFlags flags)
    {
        fixed (byte* ptr = data)
        {
            return LoadModelFromMemoryEx(
                ptr, (uint)data.Length, hint, flags);
        }
    }

    /// <summary>
    ///     Loads a skeleton from model data stored in memory.
    /// </summary>
    /// <param name="data">The source model data.</param>
    /// <param name="hint">Hint identifying the source file format.</param>
    /// <returns>The loaded skeleton.</returns>
    public static Skeleton LoadSkeletonFromMemory(
        ReadOnlySpan<byte> data,
        string hint)
    {
        fixed (byte* ptr = data)
            return LoadSkeletonFromMemory(ptr, (uint)data.Length, hint);
    }

    /// <summary>
    ///     Loads an R3D texture from encoded image data in memory.
    /// </summary>
    /// <param name="fileType">Image file type or extension.</param>
    /// <param name="data">Encoded image data.</param>
    /// <param name="isColor">Whether the texture contains color data.</param>
    /// <returns>The loaded texture.</returns>
    public static Texture2D LoadTextureFromMemory(
        string fileType,
        ReadOnlySpan<byte> data,
        bool isColor)
    {
        fixed (byte* ptr = data)
            return LoadTextureFromMemory(
                fileType, ptr, data.Length, isColor);
    }

    /// <summary>
    ///     Loads an R3D texture from encoded image data with custom
    ///     wrapping and filtering options.
    /// </summary>
    /// <param name="fileType">Image file type or extension.</param>
    /// <param name="data">Encoded image data.</param>
    /// <param name="wrap">Texture wrapping mode.</param>
    /// <param name="filter">Texture filtering mode.</param>
    /// <param name="isColor">Whether the texture contains color data.</param>
    /// <returns>The loaded texture.</returns>
    public static Texture2D LoadTextureFromMemoryEx(
        string fileType,
        ReadOnlySpan<byte> data,
        TextureWrap wrap,
        TextureFilter filter,
        bool isColor)
    {
        fixed (byte* ptr = data)
        {
            return LoadTextureFromMemoryEx(
                fileType,
                ptr,
                data.Length,
                wrap,
                filter,
                isColor);
        }
    }

    /// <summary>
    ///     Loads a text font from font data stored in memory.
    /// </summary>
    /// <param name="data">The source font data.</param>
    /// <returns>The loaded text font, or a default value for empty data.</returns>
    public static TextFont LoadTextFontFromMemory(
        ReadOnlySpan<byte> data)
    {
        if (data.IsEmpty)
            return default;

        fixed (byte* ptr = data)
        {
            return LoadTextFontFromMemory(
                ref *(sbyte*)ptr,
                (IntPtr)data.Length);
        }
    }

}
