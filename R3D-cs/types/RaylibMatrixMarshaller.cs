using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.Marshalling;

namespace R3D_cs;

[StructLayout(LayoutKind.Sequential)]
internal struct NativeRaylibMatrix
{
    public Matrix4x4 Value;
}

[CustomMarshaller(
    typeof(Matrix4x4),
    MarshalMode.Default,
    typeof(RaylibMatrixMarshaller))]
internal static class RaylibMatrixMarshaller
{
    public static NativeRaylibMatrix ConvertToUnmanaged(Matrix4x4 managed)
    {
        return new NativeRaylibMatrix
        {
            Value = Matrix4x4.Transpose(managed)
        };
    }

    public static Matrix4x4 ConvertToManaged(NativeRaylibMatrix unmanaged)
    {
        return Matrix4x4.Transpose(unmanaged.Value);
    }
}
