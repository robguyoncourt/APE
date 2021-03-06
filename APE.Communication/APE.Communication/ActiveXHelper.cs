﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using APE.Bridge;
using NM = APE.Native.NativeMethods;
using WF = System.Windows.Forms;

namespace APE.Communication
{
    public partial class APEIPC
    {
        //
        //  DumpActiveX
        //

        unsafe public void AddFirstMessageDumpActiveX()
        {
            FirstMessageInitialise();

            Message* ptrMessage = GetPointerToNextMessage();

            ptrMessage->Action = MessageAction.DumpActiveX;

            m_PtrMessageStore->NumberOfMessages++;
            m_DoneFind = true;
            m_DoneQuery = true;
            m_DoneGet = true;
        }

        private unsafe void DumpActiveX(Message* ptrMessage, int messageNumber)
        {
            //must be first message
            if (messageNumber != 1)
            {
                throw new Exception("DumpActiveX must be first message");
            }

            CleanUpMessage(ptrMessage);

            StringBuilder dump = new StringBuilder();
            if (Ax.Items.Count > 0)
            {
                lock (Ax.AxItemsLock)
                {
                    int items = Ax.Items.Count;
                    for (int item = 0; item < items; item++)
                    {
                        dump.Append("Name: " + Ax.Items[item].Name);
                        dump.Append(" TypeName: " + Ax.Items[item].TypeName);
                        dump.Append(" TypeNameSpace: " + Ax.Items[item].TypeNameSpace);
                        dump.Append(" UniqueId: " + Ax.Items[item].UniqueId);
                        dump.Append(" Handle: " + Ax.Items[item].Handle.ToString());
                        dump.Append(" Parent: " + Ax.Items[item].ParentHandle.ToString());
                        dump.Append(" ContainerUniqueId: " + Ax.Items[item].ContainerUniqueId);
                        dump.Append(" Rendered: " + Ax.Items[item].Rendered.ToString());
                        dump.AppendLine();
                    }
                }
            }

            AddReturnValue(new Parameter(this, dump.ToString()));
        }

        unsafe public void AddFirstMessageGetInvokeFormActiveX(DataStores destinationStore)
        {
            FirstMessageInitialise();

            Message* ptrMessage = GetPointerToNextMessage();
            ptrMessage->DestinationStore = destinationStore;
            ptrMessage->Action = MessageAction.GetInvokeFormActiveX;

            m_PtrMessageStore->NumberOfMessages++;
            m_DoneFind = true;
        }

        private unsafe void GetInvokeFormActiveX(Message* ptrMessage, int messageNumber)
        {
            //must be first message
            if (messageNumber != 1)
            {
                throw new Exception("GetInvokeFormActiveX must be first message");
            }

            PutObjectInDatastore(ptrMessage->DestinationStore, Ax.InvokeForm);
            CleanUpMessage(ptrMessage);
        }

        private object FindByHandleActiveX(IntPtr handle, out string name, out string typeNameSpace, out string typeName, out string uniqueId)
        {
            if (Ax.Items.Count > 0)
            {
                lock (Ax.AxItemsLock)
                {
                    int items = Ax.Items.Count;
                    for (int item = 0; item < items; item++)
                    {
                        if (Ax.Items[item].Handle == handle)
                        {
                            name = Ax.Items[item].Name;
                            typeName = Ax.Items[item].TypeName;
                            typeNameSpace = Ax.Items[item].TypeNameSpace;
                            uniqueId = Ax.Items[item].UniqueId;
                            return Ax.Items[item].Control;
                        }
                    }
                }
            }
            name = null;
            typeName = null;
            typeNameSpace = null;
            uniqueId = null;
            return null;
        }

        private object FindByUniqueIdActiveX(string uniqueId, out string name, out string typeNameSpace, out string typeName, out IntPtr handle)
        {
            if (Ax.Items.Count > 0)
            {
                lock (Ax.AxItemsLock)
                {
                    int items = Ax.Items.Count;
                    for (int item = 0; item < items; item++)
                    {
                        if (Ax.Items[item].UniqueId == uniqueId)
                        {
                            name = Ax.Items[item].Name;
                            typeName = Ax.Items[item].TypeName;
                            typeNameSpace = Ax.Items[item].TypeNameSpace;
                            handle = Ax.Items[item].Handle;
                            return Ax.Items[item].Control;
                        }
                    }
                }
            }
            name = null;
            typeName = null;
            typeNameSpace = null;
            handle = IntPtr.Zero;
            return null;
        }

        private void FindByIdentifierActiveX(ControlIdentifier identifier, ref IntPtr handle, ref string name, ref string theText, ref string typeNameSpace, ref string typeName, ref int currentIndex, ref string uniqueId, ref bool foundControl)
        {
            bool found = false;
            IntPtr parentHandle = IntPtr.Zero;

            if (Ax.Items.Count > 0)
            {
                lock (Ax.AxItemsLock)
                {
                    object control = null;
                    int item;
                    int items = Ax.Items.Count;
                    for (item = 0; item < items; item++)
                    {
                        if (Ax.Items[item].Handle == handle)
                        {
                            parentHandle = Ax.Items[item].ParentHandle;
                            name = Ax.Items[item].Name;
                            uniqueId = Ax.Items[item].UniqueId;
                            control = Ax.Items[item].Control;
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        return;
                    }

                    if (identifier.ParentHandle == parentHandle || (identifier.ParentHandle == IntPtr.Zero && parentHandle == handle))
                    {
                    }
                    else
                    {
                        return;
                    }

                    if (identifier.Name != null)
                    {
                        if (name != identifier.Name)
                        {
                            return;
                        }
                    }

                    if (identifier.TechnologyType != null)
                    {
                        if ("Windows ActiveX" != identifier.TechnologyType)
                        {
                            return;
                        }
                    }

                    if (identifier.TypeNameSpace != null)
                    {
                        typeNameSpace = Ax.Items[item].TypeNameSpace;   //Lazy load it only if we need it
                        if (typeNameSpace == null)
                        {
                            return;
                        }
                        else
                        {
                            if (!Regex.IsMatch(typeNameSpace, identifier.TypeNameSpace))
                            {
                                return;
                            }
                        }
                    }

                    if (identifier.TypeName != null)
                    {
                        typeName = Ax.Items[item].TypeName; //Lazy load it only if we need it
                        if (typeName == null)
                        {
                            return;
                        }
                        else
                        {
                            if (!Regex.IsMatch(typeName, identifier.TypeName))
                            {
                                return;
                            }
                        }
                    }

                    if (identifier.ModuleName != null)
                    {
                        if (Path.GetFileName(NM.GetWindowModuleFileName(handle)) != identifier.ModuleName)
                        {
                            return;
                        }
                    }

                    if (identifier.AssemblyName != null)
                    {
                        return;
                    }

                    if (identifier.ChildOf != IntPtr.Zero)
                    {
                        if (!NM.IsChild(identifier.ChildOf, handle))
                        {
                            return;
                        }
                    }

                    if (identifier.SiblingOf != IntPtr.Zero)
                    {
                        if (!NM.IsSibling(identifier.SiblingOf, handle))
                        {
                            return;
                        }
                    }

                    if (identifier.ParentOf != IntPtr.Zero)
                    {
                        if (!NM.IsChild(handle, identifier.ParentOf))
                        {
                            return;
                        }
                    }

                    theText = GetWindowTextViaWindowMessage(handle);
                    if (identifier.Text != null)
                    {
                        if (theText == null)
                        {
                            return;
                        }
                        else
                        {
                            if (!Regex.IsMatch(theText, identifier.Text))
                            {
                                return;
                            }
                        }
                    }

                    if (identifier.AccessibilityObjectName != null)
                    {
                        return;
                    }

                    currentIndex++;
                    if (identifier.Index > 0)
                    {
                        if (currentIndex != identifier.Index)
                        {
                            return;
                        }
                    }

                    //Make sure the type name space and type name are populated
                    typeName = Ax.Items[item].TypeName;
                    typeNameSpace = Ax.Items[item].TypeNameSpace;

                    //we have a match
                    foundControl = true;
                }
            }
        }

        private void FindByIdentifierRenderedActiveX(ControlIdentifier identifier, ref IntPtr handle, ref string name, ref string theText, ref string typeNameSpace, ref string typeName, ref string technologyType, ref string uniqueId, ref bool foundControl)
        {
            IntPtr parentHandle = IntPtr.Zero;
            int currentIndex = 0;

            if (Ax.Items.Count > 0)
            {
                lock (Ax.AxItemsLock)
                {
                    object control = null;
                    int item;
                    int items = Ax.Items.Count;
                    for (item = 0; item < items; item++)
                    {
                        if (Ax.Items[item].Control == null)
                        {
                            continue;
                        }

                        handle = Ax.Items[item].Handle;
                        parentHandle = Ax.Items[item].ParentHandle;
                        name = Ax.Items[item].Name;
                        uniqueId = Ax.Items[item].UniqueId;
                        control = Ax.Items[item].Control;

                        if (identifier.ParentHandle == parentHandle || NM.GetAncestor(identifier.ParentHandle, NM.GetAncestorFlags.GetRoot) == parentHandle || (identifier.ParentHandle == IntPtr.Zero && parentHandle == handle))
                        {
                        }
                        else
                        {
                            continue;
                        }

                        if (!NM.IsWindowVisible(handle))
                        {
                            continue;
                        }

                        //if (!Ax.Items[item].Rendered)
                        //{
                        //    continue;
                        //}

                        if (identifier.Name != null)
                        {
                            if (name != identifier.Name)
                            {
                                continue;
                            }
                        }

                        if (identifier.TechnologyType != null)
                        {
                            if ("Windows ActiveX" != identifier.TechnologyType)
                            {
                                continue;
                            }
                        }

                        if (identifier.TypeNameSpace != null)
                        {
                            typeNameSpace = Ax.Items[item].TypeNameSpace;   //Lazy load it only if we need it
                            if (typeNameSpace == null)
                            {
                                continue;
                            }
                            else
                            {
                                if (!Regex.IsMatch(typeNameSpace, identifier.TypeNameSpace))
                                {
                                    continue;
                                }
                            }
                        }

                        if (identifier.TypeName != null)
                        {
                            typeName = Ax.Items[item].TypeName; //Lazy load it only if we need it
                            if (typeName == null)
                            {
                                continue;
                            }
                            else
                            {
                                if (!Regex.IsMatch(typeName, identifier.TypeName))
                                {
                                    continue;
                                }
                            }
                        }

                        if (identifier.ModuleName != null)
                        {
                            if (Path.GetFileName(NM.GetWindowModuleFileName(handle)) != identifier.ModuleName)
                            {
                                continue;
                            }
                        }

                        if (identifier.AssemblyName != null)
                        {
                            continue;
                        }

                        if (identifier.ChildOf != IntPtr.Zero)
                        {
                            if (identifier.ChildOf == handle || NM.IsChild(identifier.ChildOf, handle))
                            {
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (identifier.SiblingOf != IntPtr.Zero)
                        {
                            continue;   
                        }

                        if (identifier.ParentOf != IntPtr.Zero)
                        {
                            continue;
                        }

                        //Make sure the type name space and type name are populated
                        typeName = Ax.Items[item].TypeName;
                        typeNameSpace = Ax.Items[item].TypeNameSpace;

                        if (typeName == "Label" && string.IsNullOrEmpty(typeNameSpace))
                        {
                            theText = (string)m_ComReflectDelegater.Invoke("Caption", Ax.Items[item].Control, null);
                        }
                        if (identifier.Text != null)
                        {
                            if (theText == null)
                            {
                                continue;
                            }
                            else
                            {
                                if (!Regex.IsMatch(theText, identifier.Text))
                                {
                                    continue;
                                }
                            }
                        }

                        if (identifier.AccessibilityObjectName != null)
                        {
                            continue;
                        }

                        currentIndex++;

                        if (identifier.Index > 0)
                        {
                            if (currentIndex != identifier.Index)
                            {
                                continue;
                            }
                        }

                        technologyType = "Windows ActiveX";
                        //WF.MessageBox.Show("Found handle: " + name + " " + handle.ToString());
                        foundControl = true;
                        return;
                    }
                }
            }
        }
    }
}
