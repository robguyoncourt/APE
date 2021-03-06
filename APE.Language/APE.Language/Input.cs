﻿//
//Copyright 2016 David Beales
//
//Licensed under the Apache License, Version 2.0 (the "License");
//you may not use this file except in compliance with the License.
//You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
//Unless required by applicable law or agreed to in writing, software
//distributed under the License is distributed on an "AS IS" BASIS,
//WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//See the License for the specific language governing permissions and
//limitations under the License.
//
using System;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using APE.Communication;
using System.Windows.Forms;
using NM = APE.Native.NativeMethods;
using System.Reflection;

namespace APE.Language
{
    internal static class Input
    {
        public static int BlockCount = 0;
        private static Process WaitForInputIdleProcess = null;
        private static int WaitForInputIdleProcessId = 0;
        private static bool IsMouseDown = false;
        private static bool IsCapsLockOn = false;
        private static bool IsNumLockOn = false;
        private static bool IsScrollLockOn = false;

        private static void WaitToBeVisibleAndEnabled(IntPtr handle, string description)
        {
            Stopwatch timer = Stopwatch.StartNew();
            while (true)
            {
                if (timer.ElapsedMilliseconds > GUI.GetTimeOut())
                {
                    if (NM.IsWindowVisible(handle))
                    {
                        throw GUI.ApeException(description + " failed to become enabled");
                    }
                    else
                    {
                        throw GUI.ApeException(description + " failed to become visible");
                    }
                }

                if (NM.IsWindowEnabled(handle))
                {
                    if (NM.IsWindowVisible(handle))
                    {
                        break;
                    }
                }
            }
            timer.Stop();
        }

        public static void SendKeys(GUIFocusableObject focusableObject, string text)
        {
            WaitToBeVisibleAndEnabled(focusableObject.Handle, focusableObject.Description);
            if (!WaitForInputIdle(focusableObject.Handle, GUI.m_APE.TimeOut))
            {
                throw GUI.ApeException(focusableObject.Description + " did not go idle within timeout");
            }
            if (!focusableObject.HasFocus)
            {
                focusableObject.SetFocus();
                // Changed focus which probably will mean a repaint so wait for gui thread to be idle again
                if (!WaitForInputIdle(focusableObject.Handle, GUI.m_APE.TimeOut))
                {
                    throw GUI.ApeException(focusableObject.Description + " did not go idle within timeout");
                }
            }

            try
            {
                TimerResolution.SetMaxTimerResolution();
                System.Windows.Forms.SendKeys.SendWait(text);   //Doesn't actually wait on Vista and newer where sendinput is used
            }
            finally
            {
                TimerResolution.UnsetMaxTimerResolution();
            }
        }

        public static void MouseSingleClick(IntPtr parent, IntPtr control, string description, int x, int y, MouseButton button, MouseKeyModifier keys, GUIObject apeObject, int preClickDelay, int intraClickDelay)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(control, description);
            if (!WaitForInputIdle(control, GUI.m_APE.TimeOut))
            {
                throw GUI.ApeException(description + " did not go idle within timeout");
            }

            IsMouseDown = false;
            uint doubleClickTimer = (uint)SystemInformation.DoubleClickTime;
            Block();
            try
            {
                TimerResolution.SetMaxTimerResolution();
                NM.SetDoubleClickTime(NM.IntraClickDelay);
                
                ClickCommon(parent, control, description, x, y, apeObject);

                if (!(apeObject is GUIForm))    //TODO some hangs in vb6
                {
                    GUI.m_APE.AddFirstMessageAddMouseHook(control);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    hooked = true;
                }

                if (preClickDelay != -1)
                {
                    Thread.Sleep(preClickDelay);
                }

                MouseClick(button, true, false, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                if (hooked)
                {
                    GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, true, true);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                }

                // Some controls don't like it if the mouse is released too quick (For instance Listview
                // group selecting)
                if (intraClickDelay != -1)
                {
                    Thread.Sleep(intraClickDelay);
                }

                MouseClick(button, false, true, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                if (hooked)
                {
                    GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, false, true);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                }
            }
            catch when (Input.ResetInputFilter(doubleClickTimer))
            {
                // Will never be reached as ResetInputFilter always returns false
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(control);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    NM.SetDoubleClickTime(doubleClickTimer);
                    Unblock();
                }
            }
        }

        public static void MouseDoubleClick(IntPtr parent, IntPtr control, string description, int x, int y, MouseButton button, MouseKeyModifier keys, GUIObject apeObject)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(control, description);
            if (!WaitForInputIdle(control, GUI.m_APE.TimeOut))
            {
                throw GUI.ApeException(description + " did not go idle within timeout");
            }

            IsMouseDown = false;
            try
            {
                Block();
                
                TimerResolution.SetMaxTimerResolution();

                ClickCommon(parent, control, description, x, y, apeObject);

                GUI.m_APE.AddFirstMessageAddMouseHook(control);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                hooked = true;

                MouseClick(button, true, false, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(button, false, true, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, false, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(button, true, false, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, true, false);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(button, false, true, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));
                
                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, false, false);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
            }
            catch when (Input.ResetInputFilter())
            {
                // Will never be reached as ResetInputFilter always returns false
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(control);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    Unblock();
                }
            }
        }

        public static void MouseTripleClick(IntPtr parent, IntPtr control, string description, int x, int y, MouseButton button, MouseKeyModifier keys, GUIObject apeObject)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(control, description);
            if (!WaitForInputIdle(control, GUI.m_APE.TimeOut))
            {
                throw GUI.ApeException(description + " did not go idle within timeout");
            }

            IsMouseDown = false;
            try
            {
                Block();
                
                TimerResolution.SetMaxTimerResolution();

                ClickCommon(parent, control, description, x, y, apeObject);

                GUI.m_APE.AddFirstMessageAddMouseHook(control);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                hooked = true;

                MouseClick(button, true, false, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(button, false, true, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, false, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(button, true, false, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(button, false, true, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, false, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(button, true, false, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, true, false);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                MouseClick(button, false, true, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, false, false);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
            }
            catch when (Input.ResetInputFilter())
            {
                // Will never be reached as ResetInputFilter always returns false
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(control);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    Unblock();
                }
            }
        }

        public static void MouseDown(IntPtr parent, IntPtr control, string description, int x, int y, MouseButton button, MouseKeyModifier keys, GUIObject apeObject, int preClickDelay, int intraClickDelay)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(control, description);
            if (!WaitForInputIdle(control, GUI.m_APE.TimeOut))
            {
                throw GUI.ApeException(description + " did not go idle within timeout");
            }

            Block();
            try
            {
                TimerResolution.SetMaxTimerResolution();

                ClickCommon(parent, control, description, x, y, apeObject);

                GUI.m_APE.AddFirstMessageAddMouseHook(control);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                hooked = true;

                if (preClickDelay != -1)
                {
                    Thread.Sleep(preClickDelay);
                }

                MouseClick(button, true, false, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, true, true);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);

                if (intraClickDelay != -1)
                {
                    Thread.Sleep(intraClickDelay);
                }
            }
            catch when (Input.ResetInputFilter())
            {
                // Will never be reached as ResetInputFilter always returns false
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(control);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    Unblock();
                    IsMouseDown = true;
                }
            }
        }

        public static void MouseUp(IntPtr parent, IntPtr control, string description, int x, int y, MouseButton button, MouseKeyModifier keys, GUIObject apeObject)
        {
            bool hooked = false;

            WaitToBeVisibleAndEnabled(control, description);
            Block();
            try
            {
                NM.tagPoint thePoint = new NM.tagPoint();
                NM.GetWindowRect(control, out NM.tagRect WindowSize);

                thePoint.x = x + WindowSize.left;
                thePoint.y = y + WindowSize.top;
                IntPtr childHandle = NM.WindowFromPoint(thePoint);
                IntPtr actualParent;

                if (parent == IntPtr.Zero)
                {
                    actualParent = control;
                }
                else
                {
                    actualParent = parent;
                }

                TimerResolution.SetMaxTimerResolution();

                if (childHandle == control)
                {
                    ClickCommon(parent, control, description, x, y, apeObject);
                }
                else
                {
                    if (apeObject is GUILabel && apeObject?.TechnologyType == "Windows ActiveX")
                    {
                        MouseMoveLabelActiveX(apeObject, x, y);
                    }
                    else
                    {
                        MouseMove(control, description, x, y, false);
                    }
                }

                if (control == actualParent || actualParent == NM.GetAncestor(control, NM.GetAncestorFlags.GetParent))
                {
                    GUI.m_APE.AddFirstMessageAddMouseHook(control);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    hooked = true;
                }

                MouseClick(button, false, true, 1, keys.HasFlag(MouseKeyModifier.Control), keys.HasFlag(MouseKeyModifier.Shift));

                if (control == actualParent)
                {
                    GUI.m_APE.AddFirstMessageWaitForMouseState((APEIPC.MouseButton)button, false, true);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                }
            }
            catch when (Input.ResetInputFilter())
            {
                // Will never be reached as ResetInputFilter always returns false
            }
            finally
            {
                try
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(control);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
                finally
                {
                    TimerResolution.UnsetMaxTimerResolution();
                    Unblock();
                    IsMouseDown = false;
                }
            }
        }

        public static bool HasFocus(IntPtr parent, IntPtr control)
        {
            if (parent == IntPtr.Zero)
            {
                return IsActiveWindow(control);
            }
            else
            {
                if (IsActiveWindow(parent))
                {
                    return IsFocusWindow(control);
                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// Checks if the control or one of its children / parents is currently the active window
        /// </summary>
        /// <param name="control">Handle of the control to check the active window state of</param>
        /// <returns>True if the control or a child / parent of the control is the active window otherwise false</returns>
        public static bool IsActiveWindow(IntPtr control)
        {
            IntPtr active = GetActive();
            if (active == control || NM.IsChild(control, active) || NM.IsChild(active, control))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if the control or one of its children currently has focus
        /// </summary>
        /// <param name="control">Handle of the control to check the focus state of</param>
        /// <returns>True if the control or a child of the control has focus otherwise false</returns>
        public static bool IsFocusWindow(IntPtr control)
        {
            IntPtr focus = GetFocus();
            if (focus == control || NM.IsChild(control, focus))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the handle of the control which currently has keyboard focus
        /// </summary>
        /// <returns>Handle of the control</returns>
        public static IntPtr GetFocus()
        {
            NM.GUITHREADINFO CurrentGuiInfo = new NM.GUITHREADINFO();
            CurrentGuiInfo.cbSize = Marshal.SizeOf(CurrentGuiInfo);
            NM.GetGUIThreadInfo(0, ref CurrentGuiInfo);

            return CurrentGuiInfo.hwndFocus;
        }

        /// <summary>
        /// Gets the handle of the control which currently is the active window
        /// </summary>
        /// <returns>Handle of the control</returns>
        public static IntPtr GetActive()
        {
            NM.GUITHREADINFO CurrentGuiInfo = new NM.GUITHREADINFO();
            CurrentGuiInfo.cbSize = Marshal.SizeOf(CurrentGuiInfo);
            NM.GetGUIThreadInfo(0, ref CurrentGuiInfo);

            return CurrentGuiInfo.hwndActive;
        }

        public static bool SetFocus(IntPtr parent, IntPtr control, string description)
        {
            //string debugMessage = "";
            bool ret = false;

            IntPtr actualParent;
            if (parent == IntPtr.Zero)
            {
                actualParent = control;
            }
            else
            {
                actualParent = parent;
            }

            if (!NM.IsWindowEnabled(control))
            {
                throw GUI.ApeException(description + " is not enabled");
            }

            if (!IsFocusWindow(control))
            {
                //debugMessage += "1 ";
                GUI.m_APE.AddFirstMessageSetFocus(control);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                ret = true;
            }

            if (!IsActiveWindow(actualParent))
            {
                //debugMessage += "2 ";
                NM.SetForegroundWindow(actualParent);
                ret = true;
            }

            if (!HasFocus(parent, control))
            {
                Thread.Sleep(1);    //This seems to help when stepping through code in visual studio

                //debugMessage += "3 ";
                Stopwatch timer = Stopwatch.StartNew();
                if (!NM.SetForegroundWindow(control))
                {
                    //debugMessage += "4 ";
                    SetFocusForced(control);
                }
                ret = true;

                while (!HasFocus(parent, control))
                {
                    if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                    {
                        throw GUI.ApeException("Failed to setfocus to the " + description);
                    }

                    Thread.Sleep(15);
                }
            }

            //if (debugMessage != "")
            //{
            //    // Temporary debug message
            //    //GUI.Log(debugMessage, LogItemType.Warning);
            //}

            return ret;
        }

        public static void SetFocusForced(IntPtr control)
        {
            if (Input.GetFocus() != control)
            {
                // Fall back to the Hotkey (which will have SetForegroundWindow permission)
                GUI.m_ViewPort.Foreground = control;

                // Sendkeys won't work so use keybd_event (TODO could also use SendInput)
                NM.keybd_event(NM.VK_PAUSE, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                NM.keybd_event(NM.VK_PAUSE, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);

                Stopwatch timer = Stopwatch.StartNew();
                while (true)
                {
                    if (GUI.m_ViewPort.Foreground == IntPtr.Zero)
                    {
                        break;
                    }

                    if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                    {
                        throw GUI.ApeException("Viewport SetForegroundWindow appeared to not trigger");
                    }

                    Thread.Sleep(15);
                }
            }
        }

        public static void ClickCommon(IntPtr parent, IntPtr control, string description, int x, int y, GUIObject apeObject)
        {
            if (!NM.IsWindowVisible(control))
            {
                throw GUI.ApeException(description +" is not visible");
            }

            if (!NM.IsWindowEnabled(control))
            {
                throw GUI.ApeException(description + " is not enabled");
            }

            IntPtr actualParent;

            if (parent == IntPtr.Zero)
            {
                actualParent = control;
            }
            else
            {
                actualParent = parent;
            }

            if (NM.IsIconic(actualParent))
            {
                throw GUI.ApeException(description + " is minimised");
            }

            //Make sure the parent form is the active window
            if (SetFocus(IntPtr.Zero, actualParent, description))
            {
                // Changed focus which probably will mean a repaint so wait for gui thread to be idle again
                if (!WaitForInputIdle(control, GUI.m_APE.TimeOut))
                {
                    throw GUI.ApeException(description + " did not go idle within timeout");
                }
            }

            NM.tagPoint thePoint;
            if (apeObject is GUILabel && apeObject?.TechnologyType == "Windows ActiveX")
            {
                thePoint = MouseMoveLabelActiveX(apeObject, x, y);
                if (!WaitForInputIdle(control, GUI.m_APE.TimeOut))
                {
                    throw GUI.ApeException(description + " did not go idle within timeout");
                }
            }
            else
            {
                // hook mouse entered for toolstrips
                bool unHookToolStripItemMouseEntered = false;
                if (apeObject is GUIToolStrip && apeObject?.TechnologyType == "Windows Forms (WinForms)")
                {
                    NM.tagPoint p = new NM.tagPoint();
                    NM.ClientToScreen(apeObject.ParentForm.Handle, ref p);
                    MoveMouse(p.x, p.y);
                    GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, apeObject.ParentForm.Handle, apeObject.Handle);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "GetItemAt", MemberTypes.Method, new Parameter(GUI.m_APE, x), new Parameter(GUI.m_APE, y));
                    GUI.m_APE.AddQueryMessageToolStripItemEnteredHandler(DataStores.Store1, apeObject.Handle);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    unHookToolStripItemMouseEntered = true;
                }

                thePoint = MouseMove(control, description, x, y);
                if (!WaitForInputIdle(control, GUI.m_APE.TimeOut))
                {
                    throw GUI.ApeException(description + " did not go idle within timeout");
                }

                if (unHookToolStripItemMouseEntered)
                {
                    GUI.m_APE.AddFirstMessageWaitForAndRemoveToolStripItemEnteredHandler();
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                }
            }

            IntPtr windowAtPoint = NM.WindowFromPoint(thePoint);
            if (windowAtPoint != control)
            {
                throw GUI.ApeException(description + " is obscured");
            }
        }

        public static void Block()
        {
            if (BlockCount == 0)
            {
                if (GUI.IsElevatedAdmin)
                {
                    bool doCheck = false;
                    bool Return = NM.BlockInput(true);
                    if (!Return)
                    {
                        throw GUI.ApeException("Failed to block input");
                    }

                    byte[] state = new byte[256];


                    GetCurrentInputState(ref state);

                    //reset the global mouse states
                    if (IsMouseDown == false)
                    {
                        if (IsButtonDown(state[NM.VK_LBUTTON]))
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
                            doCheck = true;
                        }

                        if (IsButtonDown(state[NM.VK_MBUTTON]))
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_MIDDLEUP, 0, 0, 0, UIntPtr.Zero);
                            doCheck = true;
                        }

                        if (IsButtonDown(state[NM.VK_RBUTTON]))
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_RIGHTUP, 0, 0, 0, UIntPtr.Zero);
                            doCheck = true;
                        }

                        if (IsButtonDown(state[NM.VK_XBUTTON1]))
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_XUP, 0, 0, 0, new UIntPtr(1));
                            doCheck = true;
                        }

                        if (IsButtonDown(state[NM.VK_XBUTTON2]))
                        {
                            NM.mouse_event(NM.MOUSEEVENTF_XUP, 0, 0, 0, new UIntPtr(2));
                            doCheck = true;
                        }
                    }

                    // 3 is VK_CANCEL
                    if (IsButtonDown(state[NM.VK_CANCEL]))
                    {
                        NM.keybd_event(NM.VK_CANCEL, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_CANCEL, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                        doCheck = true;
                    }

                    // 7+ is non-mouse (apart from 3) and 255 seems to break things so we do till 254
                    for (byte vk = 7; vk < unchecked(255); vk++)
                    {
                        if (IsButtonDown(state[vk]))
                        {
                            NM.keybd_event(vk, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                            NM.keybd_event(vk, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                            doCheck = true;
                        }
                    }

                    //check for toggle keys
                    IsCapsLockOn = IsToggleKeyOn(state[NM.VK_CAPITAL]);
                    if (IsCapsLockOn)
                    {
                        NM.keybd_event(NM.VK_CAPITAL, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_CAPITAL, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                        doCheck = true;
                    }

                    IsNumLockOn = IsToggleKeyOn(state[NM.VK_NUMLOCK]);
                    if (IsNumLockOn)
                    {
                        NM.keybd_event(NM.VK_NUMLOCK, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_NUMLOCK, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                        doCheck = true;
                    }

                    IsScrollLockOn = IsToggleKeyOn(state[NM.VK_SCROLL]);
                    if (IsScrollLockOn)
                    {
                        NM.keybd_event(NM.VK_SCROLL, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                        NM.keybd_event(NM.VK_SCROLL, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                        doCheck = true;
                    }

                    if (doCheck)
                    {
                        bool ok = false;
                        while (!ok)
                        {
                            ok = true;
                            GetCurrentInputState(ref state);

                            // Check mouse button state
                            if (!IsMouseDown)
                            {
                                if (IsButtonDown(state[NM.VK_LBUTTON]) || IsButtonDown(state[NM.VK_MBUTTON]) || IsButtonDown(state[NM.VK_RBUTTON]) || IsButtonDown(state[NM.VK_XBUTTON1]) || IsButtonDown(state[NM.VK_XBUTTON2]))
                                {
                                    ok = false;
                                }
                            }

                            // Check cancel
                            if (IsButtonDown(state[NM.VK_CANCEL]))
                            {
                                ok = false;
                            }

                            // Check other keys
                            for (byte vk = 7; vk < unchecked(255); vk++)
                            {
                                if (IsButtonDown(state[vk]))
                                {
                                    ok = false;
                                    break;
                                }
                            }
                            
                            // check toggle keys
                            if (IsToggleKeyOn(state[NM.VK_CAPITAL]) || IsToggleKeyOn(state[NM.VK_NUMLOCK]) || IsToggleKeyOn(state[NM.VK_SCROLL]))
                            {
                                ok = false;
                            }
                        }
                    }
                }
            }

            BlockCount++;
        }

        private static void GetCurrentInputState(ref byte[] state)
        {
            Application.DoEvents();
            NM.GetKeyState(0);
            NM.GetKeyboardState(state);
        }

        private static bool IsToggleKeyOn(byte key)
        {
            if ((key & 0x1) == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        private static bool IsButtonDown(byte button)
        {
            if ((button & 0x80) == 0)
            {
                return false;
            }
            else
            {
                return true;
            }
        }

        public static void Unblock()
        {
            if (BlockCount > 0)
            {
                BlockCount--;
            }

            if (BlockCount == 0)
            {
                Reset();
            }
        }

        private static void Reset()
        {
            if (GUI.IsElevatedAdmin)
            {
                bool doCheck = false;
                byte[] state = new byte[256];

                // reset toggle buttons
                if (IsCapsLockOn)
                {
                    NM.keybd_event(NM.VK_CAPITAL, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    NM.keybd_event(NM.VK_CAPITAL, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    doCheck = true;
                }

                if (IsNumLockOn)
                {
                    NM.keybd_event(NM.VK_NUMLOCK, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    NM.keybd_event(NM.VK_NUMLOCK, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    doCheck = true;
                }

                if (IsScrollLockOn)
                {
                    NM.keybd_event(NM.VK_SCROLL, 0, NM.KEYEVENTF_KEYDOWN, UIntPtr.Zero);
                    NM.keybd_event(NM.VK_SCROLL, 0, NM.KEYEVENTF_KEYUP, UIntPtr.Zero);
                    doCheck = true;
                }

                if (doCheck)
                {
                    Stopwatch timer = Stopwatch.StartNew();
                    while (true)
                    {
                        GetCurrentInputState(ref state);
                        if (IsCapsLockOn == IsToggleKeyOn(state[NM.VK_CAPITAL]))
                        {
                            if (IsNumLockOn == IsToggleKeyOn(state[NM.VK_NUMLOCK]))
                            {
                                if (IsScrollLockOn == IsToggleKeyOn(state[NM.VK_SCROLL]))
                                {
                                    //Set to false so we don't flip the state if reset is called more than once
                                    IsScrollLockOn = false;
                                    IsNumLockOn = false;
                                    IsCapsLockOn = false;
                                    break;
                                }
                            }
                        }

                        if (timer.ElapsedMilliseconds > 7500)
                        {
                            // Not fatal so just break out the loop (rather than hang the code)
                            Debug.WriteLine("Failed to reset the toggle key state");
                            break;
                        }
                    }
                }

                NM.BlockInput(false);
            }

            BlockCount = 0;
        }

        internal static bool ResetInputFilter(uint doubleClickTimer)
        {
            NM.SetDoubleClickTime(doubleClickTimer);    //Reset double click timer
            Input.Reset();  //Reset the input blocking
            return false;
        }

        internal static bool ResetInputFilter()
        {
            Input.Reset();  //Reset the input blocking
            return false;
        }

        public static bool WaitForInputIdle(IntPtr handle, uint timeoutMs)
        {
            if (GUI.m_APESpy)
            {
                return true;
            }

            int threadId = NM.GetWindowThreadProcessId(handle, out int processId);
            if (threadId == 0)
            {
                return true;
            }

            bool isMaxTimerResolutionUnset = !TimerResolution.IsMaxTimerResolutionSet;
            try
            {
                if (isMaxTimerResolutionUnset)
                {
                    TimerResolution.SetMaxTimerResolution();
                }

                if (processId != WaitForInputIdleProcessId)
                {
                    WaitForInputIdleProcessId = processId;
                    WaitForInputIdleProcess = Process.GetProcessById(processId);
                }
                else
                {
                    WaitForInputIdleProcess.Refresh();
                }

                // Make sure there are no outstanding input messages
                try
                {
                    if (WaitForInputIdleProcess.HasExited) { return true; }
                    GUI.m_APE.AddFirstMessagePeakMessage(handle, (int)timeoutMs);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                }
                catch (Exception ex) when (ex.Message.Contains("has exited"))
                {
                    return true;
                }

                Stopwatch timer = Stopwatch.StartNew();
                int x = 0;
                while (true)
                {
                    ProcessThreadCollection threadCollection;
                    try
                    {
                        if (WaitForInputIdleProcess.HasExited) { return true; }
                        threadCollection = WaitForInputIdleProcess.Threads;
                    }
                    catch (InvalidOperationException ex) when (ex.Message.Contains("has exited"))
                    {
                        return true;
                    }

                    bool found = false;
                    for (int i = 0; i < threadCollection.Count; i++)
                    {
                        if (threadCollection[i].Id == threadId)
                        {
                            found = true;
                            if (threadCollection[i].ThreadState == System.Diagnostics.ThreadState.Wait)
                            {
                                if (threadCollection[i].WaitReason == ThreadWaitReason.UserRequest)
                                {
                                    x++;

                                    if (x == 2)
                                    {
                                        //Matched twice in a row so exit
                                        //(check twice as sometimes the process will go idle then immediately go not idle)
                                        return true;
                                    }
                                }
                                else
                                {
                                    x = 0;
                                }
                            }
                            else
                            {
                                x = 0;
                            }
                            break;
                        }
                    }

                    if (!found)
                    {
                        return true;
                    }

                    if (timer.ElapsedMilliseconds > timeoutMs)
                    {
                        return false;
                    }

                    if (x == 0)
                    {
                        Thread.Sleep(15);
                    }
                    else
                    {
                        Thread.Yield();
                    }
                    WaitForInputIdleProcess.Refresh();
                }
            }
            finally
            {
                if (isMaxTimerResolutionUnset)
                {
                    TimerResolution.UnsetMaxTimerResolution();  //Restore the state
                }
            }
        }

        public static NM.tagPoint MouseMove(IntPtr handle, string description, int x, int y, bool performCheck = true)
        {
            NM.tagRect windowRect;
            NM.tagPoint thePoint;
            int xOffset;
            int yOffset;
            int loops = 0;
            int screenWidth = SystemInformation.VirtualScreen.Width - 1;
            int screenHeight = SystemInformation.VirtualScreen.Height - 1;
            NM.GetCursorPos(out NM.tagPoint originalPoint);

            Stopwatch timer = Stopwatch.StartNew();
            while (true)
            {
                NM.GetWindowRect(handle, out windowRect);
                NM.GetClientRect(handle, out NM.tagRect clientRect);

                // TODO fix this as -1 might be a valid move,,, maybe 0 instead or...
                if (x == -1)
                {
                    xOffset = clientRect.right / 2;

                    // check if its off screen
                    if (windowRect.left > screenWidth)
                    {
                        //Its off screen
                    }
                    else if (windowRect.left + xOffset > screenWidth)
                    {
                        //Its partial off screen
                        xOffset = screenWidth - windowRect.left;
                    }
                }
                else
                {
                    xOffset = x;
                }

                if (y == -1)
                {
                    yOffset = clientRect.bottom / 2;

                    if (windowRect.top > screenHeight)
                    {
                        //Its off screen
                    }
                    else if (windowRect.top + yOffset > screenHeight)
                    {
                        //Its partial off screen
                        yOffset = screenHeight - windowRect.top;
                    }
                }
                else
                {
                    yOffset = y;
                }

                // Convert the window area to screen point
                thePoint.x = windowRect.left + xOffset;
                thePoint.y = windowRect.top + yOffset;

                if (NM.MonitorFromPoint(thePoint, NM.MonitorOptions.MONITOR_DEFAULTTONULL) == IntPtr.Zero)
                {
                    throw GUI.ApeException("Coordinates offscreen");
                }

                if (performCheck)
                {
                    IntPtr childHandle = NM.WindowFromPoint(thePoint);

                    // Make sure we are inside the controls window area
                    if (handle == childHandle)
                    {
                        break;
                    }
                    else
                    {
                        if (loops == 7)
                        {
                            // Try to scroll it into view
                            GUI.m_APE.AddFirstMessageScrollControlIntoView(handle);
                            GUI.m_APE.SendMessages(EventSet.APE);
                            GUI.m_APE.WaitForMessages(EventSet.APE);

                            // Make sure the AUT has painted and is idle
                            if (!WaitForInputIdle(handle, GUI.m_APE.TimeOut))
                            {
                                throw GUI.ApeException(description + " did not go idle within timeout");
                            }
                        }

                        if (timer.ElapsedMilliseconds > GUI.GetTimeOut())
                        {
                            if (NM.IsWindowEnabled(handle) && NM.IsWindowEnabled(NM.GetAncestor(handle, NM.GetAncestorFlags.GetParent)))
                            {
                                string childName = null;
                                string controlName = null;
                                try
                                {
                                    ControlIdentifier identity;
                                    
                                    //try to work out the child
                                    identity = GUI.BuildIdentity(null, null, new Identifier(Identifiers.Handle, childHandle));
                                    GUI.m_APE.AddFirstMessageControlExistsByProperty(identity);
                                    GUI.m_APE.SendMessages(EventSet.APE);
                                    GUI.m_APE.WaitForMessages(EventSet.APE);
                                    //Get the value(s) returned MUST be done straight after the WaitForMessages call
                                    GUI.m_APE.DecodeControl(1, out identity);
                                    if (identity.Handle != IntPtr.Zero)
                                    {
                                        childName = identity.Name;
                                    }

                                    //work out this control
                                    identity = GUI.BuildIdentity(null, null, new Identifier(Identifiers.Handle, handle));
                                    GUI.m_APE.AddFirstMessageControlExistsByProperty(identity);
                                    GUI.m_APE.SendMessages(EventSet.APE);
                                    GUI.m_APE.WaitForMessages(EventSet.APE);
                                    //Get the value(s) returned MUST be done straight after the WaitForMessages call
                                    GUI.m_APE.DecodeControl(1, out identity);
                                    if (identity.Handle != IntPtr.Zero)
                                    {
                                        controlName = identity.Name;
                                    }
                                }
                                catch
                                {
                                }

                                if (!string.IsNullOrEmpty(childName) && !string.IsNullOrEmpty(childName))
                                {
                                    throw GUI.ApeException("Coordinates are not inside the " + description + " control area (Found " + childName + " Expecting " + controlName + ")");
                                }
                                throw GUI.ApeException("Coordinates are not inside the " + description + " control area");
                            }
                            else
                            {
                                throw GUI.ApeException(description + " is not enabled");
                            }
                        }
                    }
                }
                else
                {
                    break;
                }

                loops++;

                Thread.Sleep(15);
            }

            int screenX = windowRect.left + xOffset;
            int screenY = windowRect.top + yOffset;

            if (performCheck)
            {
                int originalTimeout = GUI.GetTimeOut();
                bool hooked = false;
                try
                {
                    GUI.m_APE.AddFirstMessageAddMouseHook(handle);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    hooked = true;

                    GUI.SetTimeOut(200);
                    while (true)
                    {
                        MoveMouse(screenX - 1, screenY - 1);
                        MoveMouse(screenX, screenY);
                        MoveMouse(screenX + 1, screenY + 1);
                        MoveMouse(screenX, screenY);

                        // Make sure the AUT recieves the mouse move message
                        if (!WaitForInputIdle(handle, (uint)originalTimeout))
                        {
                            throw GUI.ApeException(description + " did not go idle within timeout");
                        }

                        try
                        {
                            GUI.m_APE.AddFirstMessageWaitForMouseMove(screenX, screenY);
                            GUI.m_APE.SendMessages(EventSet.APE);
                            GUI.m_APE.WaitForMessages(EventSet.APE);
                            break;
                        }
                        catch (Exception ex) when (ex.Message.Contains("Failed to find Mouse Move"))
                        {
                            if (timer.ElapsedMilliseconds > originalTimeout)
                            {
                                GUI.Log("Failed to MouseMove to " + screenX.ToString() + ", " + screenY.ToString() + " (" + screenWidth.ToString() + ", " + screenWidth.ToString() + ") in the " + description, LogItemType.Information);
                                throw;
                            }
                        }

                        MoveMouse(originalPoint.x, originalPoint.y);
                    }
                }
                finally
                {
                    GUI.SetTimeOut(originalTimeout);

                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(handle);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
            }
            else
            {
                MoveMouse(screenX - 1, screenY - 1);
                MoveMouse(screenX, screenY);
                MoveMouse(screenX + 1, screenY + 1);
                MoveMouse(screenX, screenY);

                // Make sure the AUT recieves the mouse move message
                if (!WaitForInputIdle(handle, GUI.m_APE.TimeOut))
                {
                    throw GUI.ApeException(description + " did not go idle within timeout");
                }
            }

            return thePoint;
        }

        public static NM.tagPoint MouseMoveLabelActiveX(GUIObject apeObject, int x, int y, bool performCheck = true)
        {
            NM.tagPoint thePoint;
            int xOffset;
            int yOffset;
            int loops = 0;

            Stopwatch timer = Stopwatch.StartNew();
            while (true)
            {
                // TODO fix this as -1 might be a valid move,,, maybe 0 instead or...
                if (x == -1)
                {
                    xOffset = apeObject.Width / 2;
                }
                else
                {
                    xOffset = x;
                }

                if (y == -1)
                {
                    yOffset = apeObject.Height / 2;
                }
                else
                {
                    yOffset = y;
                }

                thePoint.x = apeObject.Left + xOffset;
                thePoint.y = apeObject.Top + yOffset;

                if (NM.MonitorFromPoint(thePoint, NM.MonitorOptions.MONITOR_DEFAULTTONULL) == null)
                {
                    throw GUI.ApeException("Coordinates offscreen");
                }

                if (performCheck)
                {
                    IntPtr childHandle;
                    childHandle = NM.WindowFromPoint(thePoint);

                    // Make sure we are inside the controls window area
                    if (apeObject.Handle == childHandle)
                    {
                        break;
                    }
                    else
                    {
                        if (loops == 100)
                        {
                            // Make sure the AUT has painted and is idle
                            if (!WaitForInputIdle(apeObject.Handle, GUI.m_APE.TimeOut))
                            {
                                throw GUI.ApeException(apeObject.Description + " did not go idle within timeout");
                            }
                        }

                        if (timer.ElapsedMilliseconds > GUI.GetTimeOut())
                        {
                            if (NM.IsWindowEnabled(apeObject.Handle) && NM.IsWindowEnabled(NM.GetAncestor(apeObject.Handle, NM.GetAncestorFlags.GetParent)))
                            {
                                throw GUI.ApeException("Coordinates are not inside the " + apeObject.Description + " control area");
                            }
                            else
                            {
                                throw GUI.ApeException(apeObject.Description + " is not enabled");
                            }
                        }
                    }
                }
                else
                {
                    break;
                }

                loops++;
            }

            int screenX = thePoint.x;
            int screenY = thePoint.y;

            if (performCheck)
            {
                bool hooked = false;
                try
                {
                    GUI.m_APE.AddFirstMessageAddMouseHook(apeObject.Handle);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    hooked = true;

                    MoveMouse(screenX - 1, screenY - 1);
                    MoveMouse(screenX, screenY);
                    MoveMouse(screenX + 1, screenY + 1);
                    MoveMouse(screenX, screenY);

                    // Make sure the AUT recieves the mouse move message and has painted
                    if (!WaitForInputIdle(apeObject.Handle, GUI.m_APE.TimeOut))
                    {
                        throw GUI.ApeException(apeObject.Description + " did not go idle within timeout");
                    }

                    GUI.m_APE.AddFirstMessageWaitForMouseMove(screenX, screenY);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                }
                finally
                {
                    if (hooked)
                    {
                        GUI.m_APE.AddFirstMessageRemoveMouseHook(apeObject.Handle);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                    }
                }
            }
            else
            {
                MoveMouse(screenX - 1, screenY - 1);
                MoveMouse(screenX, screenY);
                MoveMouse(screenX + 1, screenY + 1);
                MoveMouse(screenX, screenY);

                // Make sure the AUT recieves the mouse move message
                if (!WaitForInputIdle(apeObject.Handle, GUI.m_APE.TimeOut))
                {
                    throw GUI.ApeException(apeObject.Description + " did not go idle within timeout");
                }
            }

            return thePoint;
        }

        internal static void MoveMouse(int x, int y)
        {
            NM.INPUT[] MouseEvent = new NM.INPUT[1];
            MouseEvent[0].type = NM.INPUT_TYPE.INPUT_MOUSE;
            //TODO do we want to include MOUSEEVENTF_VIRTUALDESK ?
            MouseEvent[0].U.mi = CreateMouseInput(x, y, 0, 0, NM.MOUSEEVENTF.ABSOLUTE | NM.MOUSEEVENTF.MOVE | NM.MOUSEEVENTF.MOVE_NOCOALESCE);
            NM.SendInput((uint)MouseEvent.Length, MouseEvent, Marshal.SizeOf(MouseEvent[0].GetType()));
        }

        private static NM.KEYBDINPUT CreateKeyboardInput(NM.VirtualKeyShort wVK, NM.ScanCodeShort wScan, NM.KEYEVENTF dwFlags, uint time, UIntPtr dwExtraInfo)
        {
            NM.KEYBDINPUT Result = new NM.KEYBDINPUT();

            Result.wVk = wVK;
            Result.wScan = wScan;
            Result.dwFlags = dwFlags;
            Result.time = time;
            Result.dwExtraInfo = dwExtraInfo;

            return Result;
        }

        private static NM.MOUSEINPUT CreateMouseInput(int x, int y, uint data, uint time, NM.MOUSEEVENTF flag)
        {
            NM.MOUSEINPUT Result = new NM.MOUSEINPUT();
            double dx = ((double)x * GetNormaliseFactor(Direction.HORIZONTAL));
            double dy = ((double)y * GetNormaliseFactor(Direction.VERTICAL));

            Result.dx = Convert.ToInt32(dx);
            Result.dy = Convert.ToInt32(dy);
            Result.mouseData = data;
            Result.time = time;
            Result.dwFlags = flag;

            //MessageBox.Show(dx.ToString() + " " + Result.dx.ToString() + " " + dy.ToString() + " " + Result.dy.ToString());
            return Result;
        }

        private enum Direction : int
        {
            VERTICAL = 1,
            HORIZONTAL = 0
        }

        private static double GetNormaliseFactor(Direction Axis)
        {
            switch (Axis)
            {
                case Direction.HORIZONTAL:
                    return (double)65535 / NM.GetSystemMetrics(NM.SystemMetric.SM_CXSCREEN);
                case Direction.VERTICAL:
                    return (double)65535 / NM.GetSystemMetrics(NM.SystemMetric.SM_CYSCREEN);
            }
            return 0;
        }

        /// <summary>
        /// Presses and releases the left alt key
        /// </summary>
        public static void SendAltKey()
        {
            NM.INPUT[] inputEvent = new NM.INPUT[2];
            inputEvent[0].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
            inputEvent[0].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.LMENU, NM.ScanCodeShort.LMENU, NM.KEYEVENTF.NONE, 0, UIntPtr.Zero);
            inputEvent[1].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
            inputEvent[1].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.LMENU, NM.ScanCodeShort.LMENU, NM.KEYEVENTF.KEYUP, 0, UIntPtr.Zero);
            NM.SendInput((uint)inputEvent.Length, inputEvent, NM.INPUT.Size);
        }

        public static void MouseClick(MouseButton Button, Boolean Down, Boolean Up, int Clicks, bool ControlKey, bool ShiftKey)
        {
            if (!TimerResolution.IsMaxTimerResolutionSet)
            {
                throw GUI.ApeException("MaxTimerResolution not set");
            }
            Thread.Sleep(NM.IntraClickDelay);

            NM.INPUT[] inputEvent = null;
            int Events = 0;

            if (Clicks != 1)
            {
                if (!Up)
                {
                    throw GUI.ApeException("Can only single click when using MouseDown");
                }

                if (!Down)
                {
                    throw GUI.ApeException("Can only single click when using MouseUp");
                }
            }

            if (Down)
            {
                if (ControlKey)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
                    inputEvent[inputEvent.GetUpperBound(0)].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.CONTROL, NM.ScanCodeShort.CONTROL, NM.KEYEVENTF.NONE, 0, UIntPtr.Zero);
                }

                if (ShiftKey)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
                    inputEvent[inputEvent.GetUpperBound(0)].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.SHIFT, NM.ScanCodeShort.SHIFT, NM.KEYEVENTF.NONE, 0, UIntPtr.Zero);
                }
            }

            //TODO pass in the x and y and set the absolute flag so we are 100% clicking in the right place
            for (int i = 0; i < Clicks; i++)
            {
                if (Down)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.Length - 1].type = NM.INPUT_TYPE.INPUT_MOUSE;

                    switch (Button)
                    {
                        case MouseButton.Left:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.LEFTDOWN);
                            break;
                        case MouseButton.Right:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.RIGHTDOWN);
                            break;
                        case MouseButton.Middle:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.MIDDLEDOWN);
                            break;
                    }
                }

                if (Up)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_MOUSE;

                    switch (Button)
                    {
                        case MouseButton.Left:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.LEFTUP);
                            break;
                        case MouseButton.Right:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.RIGHTUP);
                            break;
                        case MouseButton.Middle:
                            inputEvent[inputEvent.GetUpperBound(0)].U.mi = CreateMouseInput(0, 0, 0, 0, NM.MOUSEEVENTF.MIDDLEUP);
                            break;
                    }
                }
            }

            if (Up)
            {
                if (ControlKey)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
                    inputEvent[inputEvent.GetUpperBound(0)].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.CONTROL, NM.ScanCodeShort.CONTROL, NM.KEYEVENTF.KEYUP, 0, UIntPtr.Zero);
                }

                if (ShiftKey)
                {
                    Events++;
                    Array.Resize(ref inputEvent, Events);
                    inputEvent[inputEvent.GetUpperBound(0)].type = NM.INPUT_TYPE.INPUT_KEYBOARD;
                    inputEvent[inputEvent.GetUpperBound(0)].U.ki = CreateKeyboardInput(NM.VirtualKeyShort.SHIFT, NM.ScanCodeShort.SHIFT, NM.KEYEVENTF.KEYUP, 0, UIntPtr.Zero);
                }
            }

            NM.SendInput((uint)inputEvent.Length, inputEvent, NM.INPUT.Size);
        }
    }

    static internal class TimerResolution
    {
        private static bool m_ResolutionSet = false;
        private static uint m_MaximumResolution = 0;

        static TimerResolution()
        {
            uint minimumResolution;
            uint maximumResolution;
            uint currentResolution;

            NM.NtQueryTimerResolution(out minimumResolution, out maximumResolution, out currentResolution);

            m_MaximumResolution = maximumResolution;
        }

        public static bool IsMaxTimerResolutionSet
        {
            get
            {
                return m_ResolutionSet;
            }
        }

        public static void SetMaxTimerResolution()
        {
            if (!m_ResolutionSet)
            {
                uint currentResolution;
                uint result = NM.NtSetTimerResolution(m_MaximumResolution, true, out currentResolution);

                switch (result)
                {
                    case NM.STATUS_SUCCESS:
                        m_ResolutionSet = true;
                        break;
                    default:
                        break;
                }
            }
        }

        public static void UnsetMaxTimerResolution()
        {
            if (m_ResolutionSet)
            {
                uint currentResolution;
                uint result = NM.NtSetTimerResolution(m_MaximumResolution, false, out currentResolution);

                switch (result)
                {
                    case NM.STATUS_SUCCESS:
                        m_ResolutionSet = false;
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
