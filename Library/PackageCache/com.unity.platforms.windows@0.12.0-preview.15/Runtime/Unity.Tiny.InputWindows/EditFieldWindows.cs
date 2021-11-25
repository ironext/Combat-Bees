#if UNITY_WINDOWS
using System;
using System.Diagnostics;
using Unity.Entities;
using Unity.Tiny.Windows;
using Unity.Tiny.Input;
using Unity.Tiny.GLFW;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Tiny.Windows
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(GLFWInputSystem))]
    public class WindowsEditFieldHandler : EditFieldHandler
    {
        protected override void DeactivateEditField()
        {
            EditFieldDialogNative.HideSoftInput();
        }

        unsafe protected override void ActivateEditField(Entity e)
        {
            var windowSystem = World.GetExistingSystem<WindowSystem>();
            var glfw = (GLFWWindowSystem)windowSystem;
            var hwnd = glfw.GetPlatformWindowHandle();
            var keyboardInfo = GetKeyboardInfo(e);
            var limit = EntityManager.GetComponentData<CharacterLimit>(e);
            var selection = EntityManager.GetComponentData<InputSelection>(e);
            bool hidden = EntityManager.HasComponent<InputHidden>(e);
            var text = EntityManager.GetBuffer<InputFieldString>(e);
            var placeholderPtr = IntPtr.Zero;
            var placeholderLength = 0;
            if (EntityManager.HasComponent<PlaceholderString>(e))
            {
                var placeholder = EntityManager.GetBuffer<PlaceholderString>(e);
                placeholderPtr = (IntPtr)placeholder.GetUnsafeReadOnlyPtr();
                placeholderLength = placeholder.Length;
            }

            EditFieldDialogNative.ShowSoftInput(hwnd,
                (IntPtr)text.GetUnsafeReadOnlyPtr(), text.Length,
                (int)keyboardInfo.KeyboardType,
                keyboardInfo.InputType == InputType.AutoCorrect,
                keyboardInfo.LineType != LineType.SingleLine,
                keyboardInfo.InputType == InputType.Password,
                placeholderPtr, placeholderLength,
                limit.Value,
                hidden,
                selection.Start,
                selection.Length
                );
        }

        unsafe protected override void GetEditFieldData(Entity e)
        {
            int editFieldLen = 0;
            bool updated = false;
            bool finished;
            var editString = (char*)EditFieldDialogNative.GetSoftInputString(ref editFieldLen, ref updated);
            var state = EditFieldState.None;
            if (SetTextAndValidate(e, editString, editFieldLen, false, out finished) || updated)
            {
                state = EditFieldState.TextUpdated;
            }
            bool active = true, canceled = false;
            EditFieldDialogNative.GetSoftInputState(ref active, ref canceled);
            if (!active || finished)
            {
                if (active)
                {
                    DeactivateEditField();
                }
                EntityManager.RemoveComponent<EditFieldActive>(e);
                state = canceled ? EditFieldState.EditCanceled : EditFieldState.EditFinished;
            }
            EntityManager.SetComponentData(e, new CurrentEditFieldState() { Value = state });

            int start = 0;
            int length = 0;
            if (EditFieldDialogNative.GetInputSelection(ref start, ref length))
            {
                EntityManager.SetComponentData(e, new InputSelection() { Start = start, Length = length });
            }
        }

        unsafe protected override void UpdateInputText(Entity e)
        {
            var data = EntityManager.GetBuffer<InputFieldString>(e);
            EditFieldDialogNative.SetSoftInputString((IntPtr)data.GetUnsafeReadOnlyPtr(), data.Length);
        }

        protected override void UpdateSelection(Entity e)
        {
            var selection = EntityManager.GetComponentData<InputSelection>(e);
            EditFieldDialogNative.SetInputSelection(selection.Start, selection.Length);
        }

        protected override void UpdateCharacterLimit(Entity e)
        {
            var limit = EntityManager.GetComponentData<CharacterLimit>(e);
            EditFieldDialogNative.SetCharacterLimit(limit.Value);
        }

        protected override void UpdateInputHidden(Entity e)
        {
            EditFieldDialogNative.SetHideInputField(EntityManager.HasComponent<InputHidden>(e));
        }
    }
}
#endif