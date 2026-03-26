using Avalonia.Controls;
using Avalonia.Platform;
using System;
using System.Runtime.InteropServices;

namespace MarkdownConverter.Desktop.Platform;

[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "cs/unmanaged-code", Justification = "X11 window class hinting is required for proper Linux desktop integration and cannot be achieved via managed code.")]
[System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "cs/call-to-unmanaged-code", Justification = "Native display operations are required for X11 compatibility.")]
internal static class LinuxDesktopIntegration
{
    internal const string LinuxAppId = "markdown-converter-pro";

    public static void TryApplyRuntimeIdentity(Window window)
    {
        if (window is null)
        {
            return;
        }

        if (!RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return;
        }

        // Wayland does not expose an X11 WM_CLASS, and Avalonia may not be using X11 at all.
        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable("DISPLAY")))
        {
            return;
        }

        try
        {
            TryApplyX11WindowClass(window);
        }
        catch (DllNotFoundException)
        {
            // X11 libraries are not available; nothing to do.
        }
        catch (EntryPointNotFoundException)
        {
            // Unexpected X11 library mismatch; skip integration.
        }
    }

    private static void TryApplyX11WindowClass(Window window)
    {
        var platformHandle = window.TryGetPlatformHandle();
        if (platformHandle?.Handle is not { } nativeHandle || nativeHandle == IntPtr.Zero)
        {
            return;
        }

        using var display = XOpenDisplay(null);
        if (display.IsInvalid)
        {
            return;
        }

        IntPtr hint = IntPtr.Zero;
        IntPtr resName = IntPtr.Zero;
        IntPtr resClass = IntPtr.Zero;

        try
        {
            hint = XAllocClassHint();
            if (hint == IntPtr.Zero)
            {
                return;
            }

            resName = Marshal.StringToHGlobalAnsi(LinuxAppId);
            resClass = Marshal.StringToHGlobalAnsi(LinuxAppId);
            var classHint = new XClassHint
            {
                res_name = resName,
                res_class = resClass
            };

            Marshal.StructureToPtr(classHint, hint, fDeleteOld: false);
            _ = XSetClassHint(display, nativeHandle, hint);
        }
        finally
        {
            if (resName != IntPtr.Zero) Marshal.FreeHGlobal(resName);
            if (resClass != IntPtr.Zero) Marshal.FreeHGlobal(resClass);
            if (hint != IntPtr.Zero) XFree(hint);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XClassHint
    {
        public IntPtr res_name;
        public IntPtr res_class;
    }

    private sealed class XDisplayHandle : SafeHandle
    {
        public XDisplayHandle() : base(IntPtr.Zero, true) { }
        public override bool IsInvalid => handle == IntPtr.Zero;
        protected override bool ReleaseHandle() => XCloseDisplay(handle) == 0;

        [DllImport("libX11", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        private static extern int XCloseDisplay(IntPtr display);
    }

    [DllImport("libX11", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern XDisplayHandle XOpenDisplay(string? display_name);

    [DllImport("libX11", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern IntPtr XAllocClassHint();

    [DllImport("libX11", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int XSetClassHint(XDisplayHandle display, IntPtr window, IntPtr class_hints);

    [DllImport("libX11", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
    private static extern int XFree(IntPtr data);
}
