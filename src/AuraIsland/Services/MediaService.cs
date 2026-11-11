using System.Runtime.InteropServices;

namespace AuraIsland.Services;

public static class MediaService
{
    private const uint InputKeyboard = 1;
    private const uint KeyUp = 0x0002;
    private const ushort VkMediaNext = 0xB0, VkMediaPrevious = 0xB1, VkMediaPlayPause = 0xB3;
    private const ushort VkVolumeMute = 0xAD, VkVolumeDown = 0xAE, VkVolumeUp = 0xAF;

    [StructLayout(LayoutKind.Sequential)] private struct INPUT { public uint type; public InputUnion U; }
    [StructLayout(LayoutKind.Explicit)] private struct InputUnion { [FieldOffset(0)] public KEYBDINPUT ki; }
    [StructLayout(LayoutKind.Sequential)] private struct KEYBDINPUT { public ushort wVk, wScan; public uint dwFlags, time; public IntPtr dwExtraInfo; }

    [DllImport("user32.dll", SetLastError=true)] private static extern uint SendInput(uint nInputs, INPUT[] pInputs, int cbSize);

    public static void PlayPause() => Press(VkMediaPlayPause);
    public static void Next() => Press(VkMediaNext);
    public static void Previous() => Press(VkMediaPrevious);
    public static void VolumeUp() => Press(VkVolumeUp);
    public static void VolumeDown() => Press(VkVolumeDown);
    public static void Mute() => Press(VkVolumeMute);

    private static void Press(ushort key)
    {
        var inputs = new[] {
            new INPUT { type=InputKeyboard, U=new InputUnion { ki=new KEYBDINPUT { wVk=key } } },
            new INPUT { type=InputKeyboard, U=new InputUnion { ki=new KEYBDINPUT { wVk=key, dwFlags=KeyUp } } }
        };
        SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<INPUT>());
    }
}
