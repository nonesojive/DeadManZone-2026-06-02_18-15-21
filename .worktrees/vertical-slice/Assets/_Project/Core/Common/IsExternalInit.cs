// Unity targets .NET Standard 2.1, which lacks IsExternalInit required by C# 9 init accessors.
#if !NET5_0_OR_GREATER
namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}
#endif
