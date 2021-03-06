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
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Reflection;
using System.Diagnostics;
using System.Runtime.InteropServices;
using APE.Capture;
using APE.Communication;
using System.Threading;
using System.Drawing.Imaging;
using System.Security.Principal;
using NM = APE.Native.NativeMethods;

namespace APE.Language
{
    /// <summary>
    /// Automation class used to automate controls derived from the following:
    /// System.Windows.Forms.ListView
    /// </summary>
    public sealed class GUIListView : GUIFocusableObject
    {
        /// <summary>
        /// Constructor used for non-form controls
        /// </summary>
        /// <param name="parentForm">The top level form the control belongs to</param>
        /// <param name="descriptionOfControl">A description of the control which would make sense to a human.
        /// <para/>This text is used in the logging method.  For example: OK button</param>
        /// <param name="identParams">One or more identifier object(s) used to locate the control.
        /// <para/>Normally you would just use the name identifier</param>
        public GUIListView(GUIForm parentForm, string descriptionOfControl, params Identifier[] identParams)
            : base(parentForm, descriptionOfControl, identParams)
        {
        }

        /// <summary>
        /// Selects the specified item in the listview by clicking on it
        /// </summary>
        /// <param name="listViewItem">The item to select</param>
        public void SingleClickItem(string listViewItem)
        {
            GUI.Log("Select item [" + listViewItem + "] from " + Identity.Description, LogItemType.Action);

            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Count", MemberTypes.Property);
            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store2);
            GUI.m_APE.SendMessages(EventSet.APE);
            GUI.m_APE.WaitForMessages(EventSet.APE);
            // Get the value(s) returned MUST be done straight after the WaitForMessages call
            int Items = GUI.m_APE.GetValueFromMessage();

            for (int Item = 0; Item < Items; Item++)
            {
                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Text", MemberTypes.Property);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                string ItemText = GUI.m_APE.GetValueFromMessage();

                if (ItemText == listViewItem)
                {
                    GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "EnsureVisible", MemberTypes.Method);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store4, "Index", MemberTypes.Property);
                    GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store4);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    // Get the value(s) returned MUST be done straight after the WaitForMessages call
                    int Index = GUI.m_APE.GetValueFromMessage();

                    GUI.m_APE.AddFirstMessageGetListViewItemRectangle(Identity.Handle, Index);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    int Top = GUI.m_APE.GetValueFromMessage();
                    int Left = GUI.m_APE.GetValueFromMessage();
                    int Bottom = GUI.m_APE.GetValueFromMessage();
                    int Right = GUI.m_APE.GetValueFromMessage();

                    base.SingleClickInternal(Left + 3, Top + ((Bottom - Top) / 2), MouseButton.Left, MouseKeyModifier.None);

                    //Wait for it to be selected
                    bool Selected = false;
                    Stopwatch timer = Stopwatch.StartNew();
                    do
                    {
                        GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Selected", MemberTypes.Property);
                        GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                        // Get the value(s) returned MUST be done straight after the WaitForMessages call
                        Selected = GUI.m_APE.GetValueFromMessage();

                        if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                        {
                            throw GUI.ApeException("Failed to select listview item");
                        }

                        Thread.Sleep(15);
                    }
                    while (!Selected);
                    timer.Stop();

                    return;
                }
            }

            throw GUI.ApeException("Failed to select listview item");
        }

        /// <summary>
        /// Selects the specified group in the listview
        /// </summary>
        /// <param name="listViewGroup">The group to select</param>
        public void SingleClickGroup(string listViewGroup)
        {
            GUI.Log("Select group [" + listViewGroup + "] from " + Identity.Description, LogItemType.Action);

            IntPtr Return;
            IntPtr Result;
            int Group = 0;
            // scroll to top
            Return = NM.SendMessageTimeout(Identity.Handle, NM.WM_HSCROLL, new IntPtr(NM.SB_LEFT), IntPtr.Zero, NM.SendMessageTimeoutFlags.SMTO_NORMAL, GUI.m_APE.TimeOut, out Result);
            Return = NM.SendMessageTimeout(Identity.Handle, NM.WM_VSCROLL, new IntPtr(NM.SB_LEFT), IntPtr.Zero, NM.SendMessageTimeoutFlags.SMTO_NORMAL, GUI.m_APE.TimeOut, out Result);

            // locate group and scroll the group
            int GroupID = -1;
            if (listViewGroup == "<Default>")
            {
                // Get the id of the default group
                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "DefaultGroup", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "ID", MemberTypes.Property);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store2);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                GroupID = GUI.m_APE.GetValueFromMessage();
            }
            else
            {
                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Count", MemberTypes.Property);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store2);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                int Groups = GUI.m_APE.GetValueFromMessage();

                for (Group = 0; Group < Groups; Group++)
                {
                    // Get the group header and id
                    GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Header", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store4, "ID", MemberTypes.Property);
                    GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
                    GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store4);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    // Get the value(s) returned MUST be done straight after the WaitForMessages call
                    string Text = GUI.m_APE.GetValueFromMessage();
                    GroupID = GUI.m_APE.GetValueFromMessage();

                    if (Text == listViewGroup)
                    {
                        // scroll to first item in group
                        GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Items", MemberTypes.Property);
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, 0));
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "EnsureVisible", MemberTypes.Method);
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store6, "Index", MemberTypes.Property);
                        GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store6);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                        // Get the value(s) returned MUST be done straight after the WaitForMessages call
                        int Index = GUI.m_APE.GetValueFromMessage();

                        break;
                    }
                }

                if (GroupID == -1)
                {
                    throw GUI.ApeException("Could not find group");
                }
            }

            // click group
            GUI.m_APE.AddFirstMessageGetListViewGroupRectangle(Identity.Handle, GroupID);
            GUI.m_APE.SendMessages(EventSet.APE);
            GUI.m_APE.WaitForMessages(EventSet.APE);
            int Top = GUI.m_APE.GetValueFromMessage();
            int Left = GUI.m_APE.GetValueFromMessage();
            int Bottom = GUI.m_APE.GetValueFromMessage();
            int Right = GUI.m_APE.GetValueFromMessage();

            Input.Block();
            try
            {
                // Listview group selecting has issue if the mouse up is recieved to quickly after the mouse down
                // so sleep a bit between the mouse down and up
                base.SingleClickInternal(Left + 3, Top + ((Bottom - Top) / 2), MouseButton.Left, MouseKeyModifier.None, -1, 32);

                //wait for all group to be selected
                bool Selected = false;
                if (listViewGroup == "<Default>")
                {
                    // the default group isn't a real group so the count of items in it isnt populated
                    bool[] ListviewItemsHaveGroup = GetDefaultGroupItems();

                    Stopwatch timer = Stopwatch.StartNew();
                    do
                    {
                        for (int Item = 0; Item < ListviewItemsHaveGroup.Length; Item++)
                        {
                            //
                            if (!ListviewItemsHaveGroup[Item])
                            {
                                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Selected", MemberTypes.Property);
                                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
                                GUI.m_APE.SendMessages(EventSet.APE);
                                GUI.m_APE.WaitForMessages(EventSet.APE);
                                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                                Selected = GUI.m_APE.GetValueFromMessage();

                                if (!Selected)
                                {
                                    break;
                                }
                            }
                        }

                        if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                        {
                            throw GUI.ApeException("Failed to select listview group");
                        }

                        Thread.Sleep(15);
                    }
                    while (!Selected);
                    timer.Stop();
                }
                else
                {
                    // Get a count of the number of items in the group
                    GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Items", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "Count", MemberTypes.Property);
                    GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store4);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    // Get the value(s) returned MUST be done straight after the WaitForMessages call
                    int ItemsInGroup = GUI.m_APE.GetValueFromMessage();

                    Stopwatch timer = Stopwatch.StartNew();
                    do
                    {
                        for (int ItemInGroup = 0; ItemInGroup < ItemsInGroup; ItemInGroup++)
                        {
                            // Check if they are selected
                            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Items", MemberTypes.Property);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, ItemInGroup));
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "Selected", MemberTypes.Property);
                            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store5);
                            GUI.m_APE.SendMessages(EventSet.APE);
                            GUI.m_APE.WaitForMessages(EventSet.APE);
                            // Get the value(s) returned MUST be done straight after the WaitForMessages call
                            Selected = GUI.m_APE.GetValueFromMessage();

                            if (!Selected)
                            {
                                break;
                            }
                        }

                        if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                        {
                            throw GUI.ApeException("Failed to select listview group");
                        }

                        Thread.Sleep(15);
                    }
                    while (!Selected);
                    timer.Stop();
                }
            }
            catch when (Input.ResetInputFilter())
            {
                // Will never be reached as ResetInputFilter always returns false
            }
            finally
            {
                Input.Unblock();
            }
        }

        private bool[] GetDefaultGroupItems()
        {
            // so loop through all items building a list of which are in a group
            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Count", MemberTypes.Property);
            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store2);
            GUI.m_APE.SendMessages(EventSet.APE);
            GUI.m_APE.WaitForMessages(EventSet.APE);
            // Get the value(s) returned MUST be done straight after the WaitForMessages call
            int TotalCount = GUI.m_APE.GetValueFromMessage();

            bool[] ListviewItemsHaveGroup = new bool[TotalCount];

            //
            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Count", MemberTypes.Property);
            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store2);
            GUI.m_APE.SendMessages(EventSet.APE);
            GUI.m_APE.WaitForMessages(EventSet.APE);
            // Get the value(s) returned MUST be done straight after the WaitForMessages call
            int Groups = GUI.m_APE.GetValueFromMessage();

            for (int Group = 0; Group < Groups; Group++)
            {
                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Items", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "Count", MemberTypes.Property);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store4);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                int Items = GUI.m_APE.GetValueFromMessage();

                for (int Item = 0; Item < Items; Item++)
                {
                    GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Items", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "Index", MemberTypes.Property);
                    GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store5);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    // Get the value(s) returned MUST be done straight after the WaitForMessages call
                    int Index = GUI.m_APE.GetValueFromMessage();

                    ListviewItemsHaveGroup[Index] = true;
                }
            }

            return ListviewItemsHaveGroup;
        }

        /// <summary>
        /// Selects the specified item in the specified group in the listview by single clicking
        /// </summary>
        /// <param name="listViewGroup">The group the item to select belongs to</param>
        /// /// <param name="listViewItem">The item to select</param>
        public void SingleClickItem(string listViewGroup, string listViewItem)
        {
            GUI.Log("Select item [" + listViewItem + "] in group [" + listViewGroup + "] from " + Identity.Description, LogItemType.Action);

            IntPtr Return;
            IntPtr Result;
            // scroll to top
            Return = NM.SendMessageTimeout(Identity.Handle, NM.WM_HSCROLL, new IntPtr(NM.SB_LEFT), IntPtr.Zero, NM.SendMessageTimeoutFlags.SMTO_NORMAL, GUI.m_APE.TimeOut, out Result);
            Return = NM.SendMessageTimeout(Identity.Handle, NM.WM_VSCROLL, new IntPtr(NM.SB_LEFT), IntPtr.Zero, NM.SendMessageTimeoutFlags.SMTO_NORMAL, GUI.m_APE.TimeOut, out Result);

            if (listViewGroup == "<Default>")
            {
                // the default group isn't a real group so the count of items in it isnt populated
                bool[] ListviewItemsHaveGroup = GetDefaultGroupItems();

                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Count", MemberTypes.Property);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store2);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                int Items = GUI.m_APE.GetValueFromMessage();

                for (int Item = 0; Item < Items; Item++)
                {
                    if (!ListviewItemsHaveGroup[Item])
                    {
                        GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                        GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Text", MemberTypes.Property);
                        GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
                        GUI.m_APE.SendMessages(EventSet.APE);
                        GUI.m_APE.WaitForMessages(EventSet.APE);
                        // Get the value(s) returned MUST be done straight after the WaitForMessages call
                        string ItemText = GUI.m_APE.GetValueFromMessage();

                        if (ItemText == listViewItem)
                        {
                            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "EnsureVisible", MemberTypes.Method);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store4, "Index", MemberTypes.Property);
                            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store4);
                            GUI.m_APE.SendMessages(EventSet.APE);
                            GUI.m_APE.WaitForMessages(EventSet.APE);
                            // Get the value(s) returned MUST be done straight after the WaitForMessages call
                            int Index = GUI.m_APE.GetValueFromMessage();

                            GUI.m_APE.AddFirstMessageGetListViewItemRectangle(Identity.Handle, Index);
                            GUI.m_APE.SendMessages(EventSet.APE);
                            GUI.m_APE.WaitForMessages(EventSet.APE);
                            int Top = GUI.m_APE.GetValueFromMessage();
                            int Left = GUI.m_APE.GetValueFromMessage();
                            int Bottom = GUI.m_APE.GetValueFromMessage();
                            int Right = GUI.m_APE.GetValueFromMessage();

                            base.SingleClickInternal(Left + 3, Top + ((Bottom - Top) / 2), MouseButton.Left, MouseKeyModifier.None);

                            //Wait for it to be selected
                            bool Selected = false;
                            Stopwatch timer = Stopwatch.StartNew();
                            do
                            {
                                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Selected", MemberTypes.Property);
                                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
                                GUI.m_APE.SendMessages(EventSet.APE);
                                GUI.m_APE.WaitForMessages(EventSet.APE);
                                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                                Selected = GUI.m_APE.GetValueFromMessage();

                                if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                                {
                                    throw GUI.ApeException("Failed to select listview item");
                                }

                                Thread.Sleep(15);
                            }
                            while (!Selected);
                            timer.Stop();

                            return;
                        }
                    }
                }
            }
            else
            {
                //
                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Count", MemberTypes.Property);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store2);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                int Groups = GUI.m_APE.GetValueFromMessage();

                for (int Group = 0; Group < Groups; Group++)
                {
                    GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Header", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store4, "Items", MemberTypes.Property);
                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "Count", MemberTypes.Property);
                    GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
                    GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store5);
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                    // Get the value(s) returned MUST be done straight after the WaitForMessages call
                    string GroupHeader = GUI.m_APE.GetValueFromMessage();
                    int Items = GUI.m_APE.GetValueFromMessage();

                    if (GroupHeader == listViewGroup)
                    {
                        for (int Item = 0; Item < Items; Item++)
                        {
                            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Items", MemberTypes.Property);
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                            GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "Text", MemberTypes.Property);
                            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store5);
                            GUI.m_APE.SendMessages(EventSet.APE);
                            GUI.m_APE.WaitForMessages(EventSet.APE);
                            // Get the value(s) returned MUST be done straight after the WaitForMessages call
                            string ItemText = GUI.m_APE.GetValueFromMessage();

                            if (ItemText == listViewItem)
                            {
                                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Groups", MemberTypes.Property);
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Group));
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Items", MemberTypes.Property);
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Item));
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "EnsureVisible", MemberTypes.Method);
                                GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store6, "Index", MemberTypes.Property);
                                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store6);
                                GUI.m_APE.SendMessages(EventSet.APE);
                                GUI.m_APE.WaitForMessages(EventSet.APE);
                                // Get the value(s) returned MUST be done straight after the WaitForMessages call
                                int Index = GUI.m_APE.GetValueFromMessage();

                                GUI.m_APE.AddFirstMessageGetListViewItemRectangle(Identity.Handle, Index);
                                GUI.m_APE.SendMessages(EventSet.APE);
                                GUI.m_APE.WaitForMessages(EventSet.APE);
                                int Top = GUI.m_APE.GetValueFromMessage();
                                int Left = GUI.m_APE.GetValueFromMessage();
                                int Bottom = GUI.m_APE.GetValueFromMessage();
                                int Right = GUI.m_APE.GetValueFromMessage();

                                base.SingleClickInternal(Left + 3, Top + ((Bottom - Top) / 2), MouseButton.Left, MouseKeyModifier.None);

                                //Wait for it to be selected
                                bool Selected = false;
                                Stopwatch timer = Stopwatch.StartNew();
                                do
                                {
                                    GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Items", MemberTypes.Property);
                                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, Index));
                                    GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Selected", MemberTypes.Property);
                                    GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
                                    GUI.m_APE.SendMessages(EventSet.APE);
                                    GUI.m_APE.WaitForMessages(EventSet.APE);
                                    // Get the value(s) returned MUST be done straight after the WaitForMessages call
                                    Selected = GUI.m_APE.GetValueFromMessage();

                                    if (timer.ElapsedMilliseconds > GUI.m_APE.TimeOut)
                                    {
                                        throw GUI.ApeException("Failed to select listview item");
                                    }

                                    Thread.Sleep(15);
                                }
                                while (!Selected);
                                timer.Stop();

                                return;
                            }
                        }
                    }
                }
            }

            throw GUI.ApeException("Failed to select listview item");
        }
    }
}
