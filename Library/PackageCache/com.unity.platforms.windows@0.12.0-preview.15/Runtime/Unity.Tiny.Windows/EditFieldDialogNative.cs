#if !UNITY_EDITOR
using System;
using System.Runtime.InteropServices;

namespace Unity.Tiny.Windows
{
    internal static class EditFieldDialogNative
    {
        [DllImport("lib_unity_platforms_common")]
        public static extern void ShowSoftInput(IntPtr hwnd, IntPtr initialText, int length, int type,
                        bool correction, bool multiline, bool secure,
                        IntPtr placeholder, int placeholderLength, int characterLimit, bool isInputFieldHidden,
                        int selectionStart, int selectionLength);

        [DllImport("lib_unity_platforms_common")]
        public static extern void HideSoftInput();

        [DllImport("lib_unity_platforms_common")]
        public static extern void SetSoftInputString(IntPtr text, int length);

        [DllImport("lib_unity_platforms_common")]
        public static extern unsafe IntPtr GetSoftInputString(ref int len, ref bool updated);

        [DllImport("lib_unity_platforms_common")]
        public static extern void SetInputSelection(int start, int length);

        [DllImport("lib_unity_platforms_common")]
        public static extern bool GetInputSelection(ref int start, ref int length);

        [DllImport("lib_unity_platforms_common")]
        public static extern void SetCharacterLimit(int limit);

        [DllImport("lib_unity_platforms_common")]
        public static extern void SetHideInputField(bool hidden);

        [DllImport("lib_unity_platforms_common")]
        public static extern bool GetInputArea(ref bool visible, ref int x, ref int y, ref int width, ref int height);

        [DllImport("lib_unity_platforms_common")]
        public static extern void GetSoftInputState(ref bool active, ref bool canceled);
    }
}
#endif
