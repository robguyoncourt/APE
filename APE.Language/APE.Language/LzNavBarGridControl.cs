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
using APE.Communication;
using System;
using System.Reflection;
using System.Diagnostics;

namespace APE.Language
{
    /// <summary>
    /// Automation class used to automate controls derived from the following:
    /// LzNavBarControls.LzNavBarGridControl
    /// </summary>
    public sealed class GUILzNavBarGridControl : GUIFocusableObject
    {
        /// <summary>
        /// Constructor used for non-form controls
        /// </summary>
        /// <param name="parentForm">The top level form the control belongs to</param>
        /// <param name="descriptionOfControl">A description of the control which would make sense to a human.
        /// <para/>This text is used in the logging method.  For example: OK button</param>
        /// <param name="identParams">One or more identifier object(s) used to locate the control.
        /// <para/>Normally you would just use the name identifier</param>
        public GUILzNavBarGridControl(GUIForm parentForm, string descriptionOfControl, params Identifier[] identParams)
            : base(parentForm, descriptionOfControl, identParams)
        {
        }

        /// <summary>
        /// Selects the specified node by scrolling it into view and clicking on it
        /// </summary>
        /// <param name="nodeIndex">The index of the node to click on</param>
        /// <param name="button">The button with which to click</param>
        /// <param name="locationInCell">The location in the cell to click</param>
        public void SingleClickItem(int nodeIndex, MouseButton button, CellClickLocation locationInCell)
        {
            string nodeText = GetNodePath(nodeIndex);
            if (nodeText == null)
            {
                GUI.Log("Failed to find node index " + nodeIndex.ToString() + " in the " + Description, LogItemType.Information);
                throw GUI.ApeException("Failed to find node index in the " + Description);
            }
            SingleClickItem(nodeText, button, locationInCell);
        }

        /// <summary>
        /// Selects the specified node by scrolling it into view and clicking on it
        /// </summary>
        /// <param name="nodeText">The node to look for delimited by -> for example Order -> Id</param>
        /// <param name="button">The button with which to click</param>
        /// <param name="locationInCell">The location in the cell to click</param>
        public void SingleClickItem(string nodeText, MouseButton button, CellClickLocation locationInCell)
        {
            GUIFlexgrid grid = new GUIFlexgrid(ParentForm, Identity.Description + " grid", new Identifier(Identifiers.Name, "Grid"), new Identifier(Identifiers.ChildOf, this));
            string uid = FindNodeUid(nodeText);
            if (uid == null)
            {
                GUI.Log("Failed to find node " + nodeText + " in the " + Description, LogItemType.Information);
                throw GUI.ApeException("Failed to find node in the " + Description);
            }

            int row = grid.FindRow(uid, 1);     // hidden column 1 contains the uid for each row
            if (row == -1)
            {
                GUI.Log("Failed to find uid " + uid + " in the " + Description, LogItemType.Information);
                throw GUI.ApeException("Failed to find uid in the " + Description);
            }
            GUI.Log("Single " + button.ToString() + " click on the " + Identity.Description + " node " + nodeText, LogItemType.Action);
            grid.SingleClickCellInternal(row, 0, button, locationInCell, MouseKeyModifier.None);
        }

        /// <summary>
        /// Returns true if the specified node item exists in the grid
        /// </summary>
        /// <param name="nodeText">The node to look for delimited by -> for example Order -> Id</param>
        /// <returns>True or False</returns>
        public bool ItemExists(string nodeText)
        {
            if (FindNodeUid(nodeText) == null)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Gets the index of the specified node
        /// </summary>
        /// <param name="nodeText">The node to look for delimited by -> for example Order -> Id</param>
        /// <returns>The index of the node</returns>
        public int ItemIndex(string nodeText)
        {
            string uid = FindNodeUid(nodeText);
            if (uid == null)
            {
                GUI.Log("Failed to find node " + nodeText + " in the " + Description, LogItemType.Information);
                throw GUI.ApeException("Failed to find node in the " + Description);
            }
            int row = GetNodeIndexForUid(uid);
            return row;
        }

        /// <summary>
        /// Gets the text of the specified node index
        /// </summary>
        /// <param name="nodeIndex">The index of the node to click on</param>
        /// <returns>The text of the node</returns>
        public string ItemText(int nodeIndex)
        {
            return GetNodePath(nodeIndex);
        }

        private string FindNodeUid(string nodeText)
        {
            int nodes = GetNodeCount();
            for (int nodeIndex = 0; nodeIndex < nodes; nodeIndex++)
            {
                string currentNodePath = GetNodePath(nodeIndex);
                if (currentNodePath == nodeText)
                {
                    string uid = GetNodeUid(nodeIndex);
                    return uid;
                }
            }

            return null;
        }

        private string GetNodePath(int nodeIndex)
        {
            string nodePath = null;
            string parentUid;
            int currentNodeIndex = nodeIndex;

            do
            {
                string nodeType = GetNodeType(currentNodeIndex);
                if (nodeType == "LzNavBarData.NBNode" || nodeType == "LzNavBarData._NBNode")
                {
                    string nodeCaption = GetNodeCaption(currentNodeIndex);

                    if (string.IsNullOrEmpty(nodePath))
                    {
                        nodePath = nodeCaption;
                    }
                    else
                    {
                        nodePath = nodeCaption + " -> " + nodePath;
                    }

                    parentUid = GetNodeParentUid(nodeIndex);
                    if (!string.IsNullOrEmpty(parentUid))
                    {
                        currentNodeIndex = GetNodeIndexForUid(parentUid);
                    }
                }
                else
                {
                    parentUid = null;
                }
            }
            while (parentUid != null);
            
            return nodePath;
        }

        private string GetNodeType(int nodeIndex)
        {
            if (Identity.TechnologyType == "Windows ActiveX")
            {
                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Group", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Nodes", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store4, "Item", MemberTypes.Property, new Parameter(GUI.m_APE, nodeIndex + 1));
                GUI.m_APE.AddQueryMessageGetTypeInformationActiveX(DataStores.Store4, DataStores.Store5);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store5);
                try
                {
                    GUI.m_APE.SendMessages(EventSet.APE);
                    GUI.m_APE.WaitForMessages(EventSet.APE);
                }
                catch
                {
                    return null;
                }
                //Get the value(s) returned MUST be done straight after the WaitForMessages call;
                string typeName = GUI.m_APE.GetValueFromMessage();
                return typeName;
            }
            else
            {
                GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Group", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Nodes", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "m_col", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, nodeIndex));
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store6, "GetType", MemberTypes.Method);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store6, DataStores.Store7, "Namespace", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store6, DataStores.Store8, "Name", MemberTypes.Property);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store7);
                GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store8);
                GUI.m_APE.SendMessages(EventSet.APE);
                GUI.m_APE.WaitForMessages(EventSet.APE);
                //Get the value(s) returned MUST be done straight after the WaitForMessages call;
                string Namespace = GUI.m_APE.GetValueFromMessage();
                string Name = GUI.m_APE.GetValueFromMessage();
                return Namespace + "." + Name;
            }
        }

        private string GetNodeCaption(int nodeIndex)
        {
            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Group", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Nodes", MemberTypes.Property);
            if (Identity.TechnologyType == "Windows ActiveX")
            {
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store4, "Item", MemberTypes.Property, new Parameter(GUI.m_APE, nodeIndex + 1));
            }
            else
            {
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "m_col", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, nodeIndex));
            }
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "Caption", MemberTypes.Property);
            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store5);
            GUI.m_APE.SendMessages(EventSet.APE);
            GUI.m_APE.WaitForMessages(EventSet.APE);
            //Get the value(s) returned MUST be done straight after the WaitForMessages call
            string caption = GUI.m_APE.GetValueFromMessage();
            return caption;
        }

        private string GetNodeUid(int nodeIndex)
        {
            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Group", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Nodes", MemberTypes.Property);
            if (Identity.TechnologyType == "Windows ActiveX")
            {
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store4, "Item", MemberTypes.Property, new Parameter(GUI.m_APE, nodeIndex + 1));
            }
            else
            {
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "m_col", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, nodeIndex));
            }
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "UID", MemberTypes.Property);
            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store5);
            GUI.m_APE.SendMessages(EventSet.APE);
            GUI.m_APE.WaitForMessages(EventSet.APE);
            //Get the value(s) returned MUST be done straight after the WaitForMessages call
            string uid = GUI.m_APE.GetValueFromMessage();
            return uid;
        }

        private string GetNodeParentUid(int nodeIndex)
        {
            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Group", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Nodes", MemberTypes.Property);
            if (Identity.TechnologyType == "Windows ActiveX")
            {
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store4, "Item", MemberTypes.Property, new Parameter(GUI.m_APE, nodeIndex + 1));   
            }
            else
            {
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "m_col", MemberTypes.Property);
                GUI.m_APE.AddQueryMessageReflect(DataStores.Store3, DataStores.Store4, "<Indexer>", MemberTypes.Property, new Parameter(GUI.m_APE, nodeIndex));
            }
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store4, DataStores.Store5, "Parent", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store5, DataStores.Store6, "UID", MemberTypes.Property);
            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store6);
            GUI.m_APE.SendMessages(EventSet.APE);
            GUI.m_APE.WaitForMessages(EventSet.APE);
            //Get the value(s) returned MUST be done straight after the WaitForMessages call
            string uid = GUI.m_APE.GetValueFromMessage();
            return uid;
        }

        private int GetNodeCount()
        {
            GUI.m_APE.AddFirstMessageFindByHandle(DataStores.Store0, Identity.ParentHandle, Identity.Handle);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store0, DataStores.Store1, "Group", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store1, DataStores.Store2, "Nodes", MemberTypes.Property);
            GUI.m_APE.AddQueryMessageReflect(DataStores.Store2, DataStores.Store3, "Count", MemberTypes.Property);
            GUI.m_APE.AddRetrieveMessageGetValue(DataStores.Store3);
            GUI.m_APE.SendMessages(EventSet.APE);
            GUI.m_APE.WaitForMessages(EventSet.APE);
            //Get the value(s) returned MUST be done straight after the WaitForMessages call
            int count = GUI.m_APE.GetValueFromMessage();
            return count;
        }

        private int GetNodeIndexForUid(string uid)
        {
            int nodes = GetNodeCount();
            for (int nodeIndex = 0; nodeIndex < nodes; nodeIndex++)
            {
                string nodeType = GetNodeType(nodeIndex);
                if (nodeType == "LzNavBarData.NBNode" || nodeType == "LzNavBarData._NBNode")
                {
                    string currentNodeUid = GetNodeUid(nodeIndex);
                    if (currentNodeUid == uid)
                    {
                        return nodeIndex;
                    }
                }
            }

            GUI.Log("Failed to find node index for uid " + uid + " in the " + Description, LogItemType.Information);
            throw GUI.ApeException("Failed to find node index from uid in the " + Description);
        }
    }
}
