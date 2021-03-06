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
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Reflection;
using System.Threading;
using NM = APE.Native.NativeMethods;

namespace APE.Communication
{
    public enum EventSet : byte
    {
        APE = 0,
        AUT = 1,
    }

    internal enum ApeTypeCode : int
    {
        Empty = 0,          // Null reference
        Object = 1,         // Instance that isn't a value
        DBNull = 2,         // Database null value
        Boolean = 3,        // Boolean
        Char = 4,           // Unicode character
        SByte = 5,          // Signed 8-bit integer
        Byte = 6,           // Unsigned 8-bit integer
        Int16 = 7,          // Signed 16-bit integer
        UInt16 = 8,         // Unsigned 16-bit integer
        Int32 = 9,          // Signed 32-bit integer
        UInt32 = 10,        // Unsigned 32-bit integer
        Int64 = 11,         // Signed 64-bit integer
        UInt64 = 12,        // Unsigned 64-bit integer
        Single = 13,        // IEEE 32-bit float
        Double = 14,        // IEEE 64-bit double
        Decimal = 15,       // Decimal
        DateTime = 16,      // DateTime
        IntPtr = 17,        // IntPtr
        String = 18,        // Unicode character string
        DataStore = 19,     // APE datastore
        Image = 20,         // Image
    }

    [Flags]
    internal enum MessageAction : int
    {
        None = 0,
        Find = 1,
        RefindByHandle = 2,
        ReflectGet = 3,
        GetResult = 4,
        GetListViewGroupRectangle = 5,
        GetListViewItemRectangle = 6,
        SetTimeOuts = 7,
        GetTitleBarItemRectangle = 8,
        AddMouseHook = 9,
        RemoveMouseHook = 10,
        WaitForMouseState = 11,
        GarbageCollect = 12,
        GetContextMenuStrip = 13,
        GetAppDomains = 14,
        GetRecognisedType = 15,
        GetApeTypeFromType = 16,
        GetApeTypeFromObject = 17,
        ReflectPoll = 18,
        ConvertType = 19,
        SentinelGridsGetUnderlyingGrid = 20,
        FlexgridGetCellRangeBackColourName = 21,
        FlexgridGetCellRangeForeColourName = 22,
        FlexgridGetCellRangeDataType = 23,
        FlexgridGetCellRangeCheckBox = 24,
        FlexgridGetCellRangeImage = 25,
        FlexgridGetCellRangeBackgroundImage = 26,
        FlexgridGetAllColumnsHidden = 27,
        FlexgridGetAllRowsHidden = 28,
        FlexgridGetAllColumnsWidth = 29,
        FlexgridGetAllRowsHeight = 30,
        ControlExists = 31,
        GridControlGetTitleRows = 32,
        GridControlGetTitleRowCount = 33,
        GridControlGetAllColumnsHidden = 34,
        GetDateTimePickerCheckboxRectangle = 35,
        GetDateTimePickerButtonRectangle = 36,
        ScrollControlIntoView = 37,
        PeakMessage = 38,
        SetFocus = 39,
        SetFocusAsync = 40,
        GridControlEnsureTitleCellVisible = 41,
        DictionaryContainsKey = 42,
        AddMouseClickHandler = 43,
        WaitForAndRemoveMouseClickHandler = 44,
        AddFlexgridCellChangedHandler = 45,
        WaitForAndRemoveFlexgridCellChangedHandler = 46,
        AddFlexgridAfterRowColChangeHandler = 47,
        WaitForAndRemoveFlexgridAfterRowColChangeHandler = 48,
        RemoveFlexgridAfterRowColChangeHandler = 49,
        AddGenericWalkerSelectedHandler = 50,
        WaitForAndRemoveGenericWalkerSelectedHandler = 51,
        VisualStyleSupported = 52,
        DataGridViewShowCell = 53,
        RemoveMouseClickHandler = 54,
        RemoveGenericWalkerSelectedHandler = 55,
        GetToolTip = 56,
        RefindByUniqueId = 57,
        DumpActiveX = 58,
        FlexgridGetNodeCollapsedState = 59,
        GetTypeInformationActiveX = 60,
        GetTabRect = 61,
        GetInvokeFormActiveX = 62,
        GetComboBoxExItemText = 63,
        WaitForMouseMove = 64,
        AddToolStripItemEnteredHandler = 65,
        WaitForAndRemoveToolStripItemEnteredHandler = 66,
        DumpControl = 67,
        FlexgridGetCellRangeTextDisplay = 68,
        FlexgridGetCellRangeFontStyle = 69,
        FlexgridGetCellBackgroundImage = 70,
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Message
    {
        public int MoreStringData;
        public int TotalStringDataLength;
        public MessageAction Action;
        public DataStores SourceStore;
        public DataStores DestinationStore;
        public int NameOffset;
        public int NameLength;
        public int NumberOfParameters;
        public MemberTypes MemberType;
        public long TypeCodeKey;
        public Parameters Parameter;
    }

    public partial class APEIPC
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct MessageStore
        {
            internal const int MaxMessages = 20;

            public Message Message0;
            public Message Message1;
            public Message Message2;
            public Message Message3;
            public Message Message4;
            public Message Message5;
            public Message Message6;
            public Message Message7;
            public Message Message8;
            public Message Message9;
            public Message Message10;
            public Message Message11;
            public Message Message12;
            public Message Message13;
            public Message Message14;
            public Message Message15;
            public Message Message16;
            public Message Message17;
            public Message Message18;
            public Message Message19;
            public byte NumberOfMessages;
            public EventSet LastWake;
        }

        /// <summary>
        /// When an instance of APEIPC is created in the AUT a new thread is created with this method as the thread start point
        /// </summary>
        /// <param name="APEPID">The PID of the APE process</param>
        /// <param name="AppDomainToLoadInto">The application domain to the load into</param>
        /// <param name="WPF">Whether the AUT contains WPF controls</param>
        [EditorBrowsable(EditorBrowsableState.Never)]
        unsafe public void ProcessMessages(int APEPID, string AppDomainToLoadInto)
        {
            m_ManagedThreadId = Thread.CurrentThread.ManagedThreadId;
            try
            {
                AUTProcess = Process.GetCurrentProcess();
                string AUTProcessId = AUTProcess.Id.ToString();
                string APEProcessId = APEPID.ToString();

                // Set the thread name so its easy to find in the debugger
                Thread.CurrentThread.Name = "APE_" + APEProcessId + "_" + AppDomainToLoadInto + "_" + AUTProcessId;

                m_HandleMemoryMappedFileStringStore = NM.CreateFileMapping((IntPtr)(NM.INVALID_HANDLE_VALUE), (IntPtr)0, NM.FileMapProtection.PageReadWrite, 0, StringSpaceBytes, APEProcessId + "_String_" + AppDomainToLoadInto + "_" + AUTProcessId);
                m_IntPtrMemoryMappedFileViewStringStore = NM.MapViewOfFile(m_HandleMemoryMappedFileStringStore, NM.FileMapAccess.FileMapAllAccess, 0, 0, (UIntPtr)StringSpaceBytes);
                m_HandleMemoryMappedFileMessageStore = NM.CreateFileMapping((IntPtr)(NM.INVALID_HANDLE_VALUE), (IntPtr)0, NM.FileMapProtection.PageReadWrite, 0, (uint)sizeof(MessageStore), APEProcessId + "_Message_" + AppDomainToLoadInto + "_" + AUTProcessId);
                m_IntPtrMemoryMappedFileViewMessageStore = NM.MapViewOfFile(m_HandleMemoryMappedFileMessageStore, NM.FileMapAccess.FileMapAllAccess, 0, 0, (UIntPtr)sizeof(MessageStore));
                m_PtrMessageStore = (MessageStore*)m_IntPtrMemoryMappedFileViewMessageStore.ToPointer();

                m_eventIPC = new EventWaitHandle(false, EventResetMode.AutoReset, APEProcessId + "_EventIPC_" + AppDomainToLoadInto + "_" + AUTProcessId);

                Side = EventSet.AUT;
                try
                {
                    ApeProcess = Process.GetProcessById(APEPID);
                }
                catch
                {
                }

                // Setup the hook procedures
                SetupmouseHelperHooks();
                EnumThreadProcedue = new NM.EnumWindow(EnumThreadCallback);

                // Setup the delegates
                m_GetWPFHandleAndNameAndTitleDelegater = new GetWPFHandleAndNameAndTitleDelegate(GetWPFHandleAndNameAndTitle);
                m_ConvertTypeDelegater = new ConvertTypeDelegate(Cast);
                m_GetTextDelegater = new GetTextDelegate(GetText);
                m_GetAccessibilityObjectNameDelegater = new GetAccessibilityObjectNameDelegate(GetAccessibilityObjectName);
                SetupSentinelGridsHelperDelegates();
                SetupFlexgridHelperDelegates();
                SetupComHelperDelegates();
                SetupGridControlHelperDelegates();
                SetupFormHelperDelegates();
                SetupDictionaryHelperDelegates();
                SetupDataGridViewHelperDelegates();

                //Process all the messages
                while (true)
                {
                    WaitForMessages(EventSet.AUT);
                    if (m_Abort)
                    {
                        RemoveFileMapping();
                        break;
                    }

                    string result = null;
                    int messageNumber = -1;
                    int numberOfMessages = -1;

                    try
                    {
                        numberOfMessages = m_PtrMessageStore->NumberOfMessages;
                        m_PtrMessageStore->NumberOfMessages = 0;
                        m_StringStoreOffset = 0;

                        for (messageNumber = 1; messageNumber <= numberOfMessages; messageNumber++)
                        {
                            Message* ptrMessage = (Message*)(m_IntPtrMemoryMappedFileViewMessageStore + ((messageNumber - 1) * m_SizeOfMessage));

                            DebugLogging.WriteLog("Processing message " + ptrMessage->Action.ToString());
                            //get the message action:
                            switch (ptrMessage->Action)
                            {
                                case MessageAction.GetListViewGroupRectangle:
                                    GetListViewGroupRectangle(messageNumber);
                                    break;
                                case MessageAction.GetListViewItemRectangle:
                                    GetListViewItemRectangle(messageNumber);
                                    break;
                                case MessageAction.ControlExists:
                                    string findText = Find(messageNumber, 0);
                                    if (findText != null)
                                    {
                                        ControlIdentifier newIdentifier = new ControlIdentifier();
                                        AddIdentifierMessage(newIdentifier);
                                    }
                                    break;
                                case MessageAction.Find:
                                    result = Find(messageNumber, m_TimeOut);
                                    break;
                                case MessageAction.RefindByHandle:
                                    RefindByHandle(messageNumber);
                                    break;
                                case MessageAction.RefindByUniqueId:
                                    RefindByUniqueId(messageNumber);
                                    break;
                                case MessageAction.ReflectGet:
                                    Reflect(messageNumber);
                                    break;
                                case MessageAction.ConvertType:
                                    ConvertType(messageNumber);
                                    break;
                                case MessageAction.SentinelGridsGetUnderlyingGrid:
                                    SentinelGridsGetUnderlyingGrid(ptrMessage);
                                    break;
                                case MessageAction.ReflectPoll:
                                    ReflectPoll(messageNumber);
                                    break;
                                case MessageAction.GetResult:
                                    GetResult(messageNumber);
                                    break;
                                case MessageAction.AddMouseHook:
                                    AddMouseHook(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.RemoveMouseHook:
                                    RemoveMouseHook(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.WaitForMouseState:
                                    WaitForMouseState(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.SetTimeOuts:
                                    SetTimeOuts(messageNumber);
                                    break;
                                case MessageAction.GetTitleBarItemRectangle:
                                    GetTitleBarItemRectangle(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.GarbageCollect:
                                    GarbageCollect(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.GetContextMenuStrip:
                                    GetContextMenuStrip(messageNumber);
                                    break;
                                case MessageAction.GetAppDomains:
                                    GetAppDomains(messageNumber);
                                    break;
                                case MessageAction.GetRecognisedType:
                                    GetRecognisedType(messageNumber);
                                    break;
                                case MessageAction.GetApeTypeFromType:
                                    GetApeTypeFromType(messageNumber);
                                    break;
                                case MessageAction.GetApeTypeFromObject:
                                    GetApeTypeFromObject(messageNumber);
                                    break;
                                // Flexgrid helper methods
                                case MessageAction.FlexgridGetCellRangeBackColourName:
                                    FlexgridGetCellRange(ptrMessage, CellProperty.BackColourName);
                                    break;
                                case MessageAction.FlexgridGetCellRangeForeColourName:
                                    FlexgridGetCellRange(ptrMessage, CellProperty.ForeColourName);
                                    break;
                                case MessageAction.FlexgridGetCellRangeDataType:
                                    FlexgridGetCellRange(ptrMessage, CellProperty.DataType);
                                    break;
                                case MessageAction.FlexgridGetCellRangeCheckBox:
                                    FlexgridGetCellRange(ptrMessage, CellProperty.CheckBox);
                                    break;
                                case MessageAction.FlexgridGetCellRangeImage:
                                    FlexgridGetCellRange(ptrMessage, CellProperty.Image);
                                    break;
                                case MessageAction.FlexgridGetCellRangeBackgroundImage:
                                    FlexgridGetCellRange(ptrMessage, CellProperty.BackgroundImage);
                                    break;
                                case MessageAction.FlexgridGetAllColumnsHidden:
                                    FlexgridGetAllColumnsHidden(ptrMessage);
                                    break;
                                case MessageAction.FlexgridGetAllRowsHidden:
                                    FlexgridGetAllRowsHidden(ptrMessage);
                                    break;
                                case MessageAction.FlexgridGetAllColumnsWidth:
                                    FlexgridGetAllColumnsWidth(ptrMessage);
                                    break;
                                case MessageAction.FlexgridGetAllRowsHeight:
                                    FlexgridGetAllRowsHeight(ptrMessage);
                                    break;
                                case MessageAction.GridControlGetTitleRows:
                                    GridControlGetTitleRows(ptrMessage);
                                    break;
                                case MessageAction.GridControlGetTitleRowCount:
                                    GridControlGetTitleRowCount(ptrMessage);
                                    break;
                                case MessageAction.GridControlGetAllColumnsHidden:
                                    GridControlGetAllColumnsVisible(ptrMessage);
                                    break;
                                case MessageAction.GetDateTimePickerCheckboxRectangle:
                                    GetDateTimePickerCheckboxRectangle(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.GetDateTimePickerButtonRectangle:
                                    GetDateTimePickerButtonRectangle(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.ScrollControlIntoView:
                                    ScrollControlIntoView(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.PeakMessage:
                                    PeakMessage(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.SetFocus:
                                    SetFocus(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.SetFocusAsync:
                                    SetFocusAsync(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.GridControlEnsureTitleCellVisible:
                                    GridControlEnsureTitleCellVisible(ptrMessage);
                                    break;
                                case MessageAction.DictionaryContainsKey:
                                    DictionaryContainsKey(ptrMessage);
                                    break;
                                case MessageAction.AddMouseClickHandler:
                                    AddMouseClickHandler(ptrMessage);
                                    break;
                                case MessageAction.WaitForAndRemoveMouseClickHandler:
                                    WaitForAndRemoveMouseClickHandler(ptrMessage);
                                    break;
                                case MessageAction.RemoveMouseClickHandler:
                                    RemoveMouseClickHandler(ptrMessage);
                                    break;
                                case MessageAction.AddFlexgridCellChangedHandler:
                                    AddFlexgridCellChangedHandler(ptrMessage);
                                    break;
                                case MessageAction.WaitForAndRemoveFlexgridCellChangedHandler:
                                    WaitForAndRemoveFlexgridCellChangedHandler(ptrMessage);
                                    break;
                                case MessageAction.AddFlexgridAfterRowColChangeHandler:
                                    AddFlexgridAfterRowColChangeHandler(ptrMessage);
                                    break;
                                case MessageAction.WaitForAndRemoveFlexgridAfterRowColChangeHandler:
                                    WaitForAndRemoveFlexgridAfterRowColChangeHandler(ptrMessage);
                                    break;
                                case MessageAction.RemoveFlexgridAfterRowColChangeHandler:
                                    RemoveFlexgridAfterRowColChangeHandler(ptrMessage);
                                    break;
                                case MessageAction.AddGenericWalkerSelectedHandler:
                                    AddGenericWalkerSelectedHandler(ptrMessage);
                                    break;
                                case MessageAction.WaitForAndRemoveGenericWalkerSelectedHandler:
                                    WaitForAndRemoveGenericWalkerSelectedHandler(ptrMessage);
                                    break;
                                case MessageAction.RemoveGenericWalkerSelectedHandler:
                                    RemoveGenericWalkerSelectedHandler(ptrMessage);
                                    break;
                                case MessageAction.VisualStyleSupported:
                                    VisualStyleSupported(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.DataGridViewShowCell:
                                    DataGridViewShowCell(ptrMessage);
                                    break;
                                case MessageAction.GetToolTip:
                                    GetToolTip(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.DumpActiveX:
                                    DumpActiveX(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.FlexgridGetNodeCollapsedState:
                                    FlexgridGetNodeCollapsedState(ptrMessage);
                                    break;
                                case MessageAction.GetTypeInformationActiveX:
                                    GetTypeInformationActiveX(ptrMessage);
                                    break;
                                case MessageAction.GetTabRect:
                                    GetTabRect(ptrMessage);
                                    break;
                                case MessageAction.GetInvokeFormActiveX:
                                    GetInvokeFormActiveX(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.GetComboBoxExItemText:
                                    GetComboBoxExItemText(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.WaitForMouseMove:
                                    WaitForMouseMove(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.AddToolStripItemEnteredHandler:
                                    AddToolStripItemEnteredHandler(ptrMessage);
                                    break;
                                case MessageAction.WaitForAndRemoveToolStripItemEnteredHandler:
                                    WaitForAndRemoveToolStripItemEnteredHandler(ptrMessage);
                                    break;
                                case MessageAction.DumpControl:
                                    DumpControl(ptrMessage, messageNumber);
                                    break;
                                case MessageAction.FlexgridGetCellRangeTextDisplay:
                                    FlexgridGetCellRange(ptrMessage, CellProperty.TextDisplay);
                                    break;
                                case MessageAction.FlexgridGetCellRangeFontStyle:
                                    FlexgridGetCellRange(ptrMessage, CellProperty.FontStyle);
                                    break;
                                case MessageAction.FlexgridGetCellBackgroundImage:
                                    FlexgridGetCellBackgroundImage(ptrMessage);
                                    break;
                                default:
                                    throw new Exception("Unknown action for message " + messageNumber.ToString() + " : " + ptrMessage->Action.ToString());
                            }

                            if (result != null)
                            {
                                break;
                            }
                        }

                        if (result == null)
                        {
                            AddResultMessage(MessageResult.Success);
                        }
                    }
                    catch (Exception ex)
                    {
                        result = "Message " + messageNumber.ToString() + " of " + numberOfMessages + " failed:\r\n";
                        result += ex.GetType().Name + " " + ex.Message + "\r\n" + ex.StackTrace;
                        if (ex.InnerException != null)
                        {
                            //TODO make this better?
                            result += "\r\n" + ex.InnerException.GetType().Name + " " + ex.InnerException.Message + "\r\n" + ex.InnerException.StackTrace;
                        }
                    }

                    if (result != null)
                    {
                        //clean up all the messages
                        for (messageNumber = 1; messageNumber <= MessageStore.MaxMessages; messageNumber++)
                        {
                            Message* ptrMessage = (Message*)(m_IntPtrMemoryMappedFileViewMessageStore + ((messageNumber - 1) * m_SizeOfMessage));
                            CleanUpMessage(ptrMessage);
                        }

                        m_PtrMessageStore->NumberOfMessages = 0;
                        m_StringStoreOffset = 0;

                        AddResultMessage(MessageResult.Failure, result);
                    }

                    //clear the data stores so we don't hold any references to objects in the AUT
                    //which would stop them being garbage collected
                    if (tempStore0ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore0);
                        tempStore0ReleaseComObject = false;
                    }
                    tempStore0 = null;
                    if (tempStore1ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore1);
                        tempStore1ReleaseComObject = false;
                    }
                    tempStore1 = null;
                    if (tempStore2ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore2);
                        tempStore2ReleaseComObject = false;
                    }
                    tempStore2 = null;
                    if (tempStore3ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore3);
                        tempStore3ReleaseComObject = false;
                    }
                    tempStore3 = null;
                    if (tempStore4ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore4);
                        tempStore4ReleaseComObject = false;
                    }
                    tempStore4 = null;
                    if (tempStore5ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore5);
                        tempStore5ReleaseComObject = false;
                    }
                    tempStore5 = null;
                    if (tempStore6ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore6);
                        tempStore6ReleaseComObject = false;
                    }
                    tempStore6 = null;
                    if (tempStore7ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore7);
                        tempStore7ReleaseComObject = false;
                    }
                    tempStore7 = null;
                    if (tempStore8ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore8);
                        tempStore8ReleaseComObject = false;
                    }
                    tempStore8 = null;
                    if (tempStore9ReleaseComObject)
                    {
                        Marshal.ReleaseComObject(tempStore9);
                        tempStore9ReleaseComObject = false;
                    }
                    tempStore9 = null;

                    //send back our response
                    SendMessages(EventSet.AUT);
                }
            }
            catch (Exception ex)
            {
                TextWriter log = File.AppendText(Environment.GetEnvironmentVariable("TEMP") + @"\critical.log");
                log.WriteLine(DateTime.Now.ToString() + "\t" + ex.Message);
                log.WriteLine(DateTime.Now.ToString() + "\t" + ex.StackTrace);
                log.Close();
                throw;
            }
        }

        /// <summary>
        /// Gets the object currently stored in the specified datastore number
        /// </summary>
        /// <param name="sourceStore">The datastore to get the object from</param>
        /// <returns>The object contained in the datastore</returns>
        private object GetObjectFromDatastore(DataStores sourceStore)
        {
            object sourceObject;

            switch (sourceStore)
            {
                case DataStores.Store0:
                    sourceObject = tempStore0;
                    break;
                case DataStores.Store1:
                    sourceObject = tempStore1;
                    break;
                case DataStores.Store2:
                    sourceObject = tempStore2;
                    break;
                case DataStores.Store3:
                    sourceObject = tempStore3;
                    break;
                case DataStores.Store4:
                    sourceObject = tempStore4;
                    break;
                case DataStores.Store5:
                    sourceObject = tempStore5;
                    break;
                case DataStores.Store6:
                    sourceObject = tempStore6;
                    break;
                case DataStores.Store7:
                    sourceObject = tempStore7;
                    break;
                case DataStores.Store8:
                    sourceObject = tempStore8;
                    break;
                case DataStores.Store9:
                    sourceObject = tempStore9;
                    break;
                default:
                    throw new Exception("Unsupported SourceStore " + sourceStore.ToString());
            }

            return sourceObject;
        }

        /// <summary>
        /// Given a datastore number and an object, puts the object in the correct datastore
        /// </summary>
        /// <param name="destinationStore">The datastore in which to store the object</param>
        /// <param name="destinationObject">The object to be stored</param>
        private void PutObjectInDatastore(DataStores destinationStore, object destinationObject)
        {
            switch (destinationStore)
            {
                case DataStores.Store0:
                    tempStore0 = destinationObject;
                    break;
                case DataStores.Store1:
                    tempStore1 = destinationObject;
                    break;
                case DataStores.Store2:
                    tempStore2 = destinationObject;
                    break;
                case DataStores.Store3:
                    tempStore3 = destinationObject;
                    break;
                case DataStores.Store4:
                    tempStore4 = destinationObject;
                    break;
                case DataStores.Store5:
                    tempStore5 = destinationObject;
                    break;
                case DataStores.Store6:
                    tempStore6 = destinationObject;
                    break;
                case DataStores.Store7:
                    tempStore7 = destinationObject;
                    break;
                case DataStores.Store8:
                    tempStore8 = destinationObject;
                    break;
                case DataStores.Store9:
                    tempStore9 = destinationObject;
                    break;
                default:
                    throw new Exception("Unsupported DestinationStore " + destinationStore.ToString());
            }
        }

        /// <summary>
        /// Resets the message parameters back to a fresh state
        /// </summary>
        /// <param name="ptrMessage">A pointer to the message</param>
        unsafe private void CleanUpMessage(Message* ptrMessage)
        {
            ptrMessage->TypeCodeKey = 0;
            ptrMessage->NumberOfParameters = 0;
            ptrMessage->NameOffset = 0;
            ptrMessage->NameLength = 0;
            ptrMessage->Action = MessageAction.None;
        }

        /// <summary>
        /// Gets a pointer to the next available message
        /// </summary>
        /// <returns>A pointer to the message</returns>
        unsafe internal Message* GetPointerToNextMessage()
        {
            if (m_PtrMessageStore->NumberOfMessages >= MessageStore.MaxMessages)
            {
                throw new Exception("Exceeded maximum number of messages in MessageStore");
            }
            return (Message*)(m_IntPtrMemoryMappedFileViewMessageStore + (m_PtrMessageStore->NumberOfMessages * m_SizeOfMessage));
        }

        /// <summary>
        /// Gets the specified parameter from the message, validating it is the correct type
        /// </summary>
        /// <param name="ptrMessage">A pointer to the message</param>
        /// <param name="parameter">The parameter number (0 based)</param>
        /// <returns>The value in the mssage for the specified parameter</returns>
        unsafe private int GetParameterInt32(Message* ptrMessage, int parameter)
        {
            if ((ptrMessage->Parameter.TypeCode[parameter]) == (int)ApeTypeCode.Int32)
            {
                return ptrMessage->Parameter.Int32[parameter];
            }
            else
            {
                throw new Exception("Expected ApeTypeCode.Int32 got ApeTypeCode." + (ptrMessage->Parameter.TypeCode[parameter]).ToString());
            }
        }

        /// <summary>
        /// Gets the specified parameter from the message, validating it is the correct type
        /// </summary>
        /// <param name="ptrMessage">A pointer to the message</param>
        /// <param name="parameter">The parameter number (0 based)</param>
        /// <returns>The value in the mssage for the specified parameter</returns>
        unsafe private float GetParameterSingle(Message* ptrMessage, int parameter)
        {
            if ((ptrMessage->Parameter.TypeCode[parameter]) == (int)ApeTypeCode.Single)
            {
                return ptrMessage->Parameter.Single[parameter];
            }
            else
            {
                throw new Exception("Expected ApeTypeCode.Single got ApeTypeCode." + (ptrMessage->Parameter.TypeCode[parameter]).ToString());
            }
        }

        /// <summary>
        /// Gets the specified parameter from the message, validating it is the correct type
        /// </summary>
        /// <param name="ptrMessage">A pointer to the message</param>
        /// <param name="parameter">The parameter number (0 based)</param>
        /// <returns>The value in the mssage for the specified parameter</returns>
        unsafe private IntPtr GetParameterIntPtr(Message* ptrMessage, int parameter)
        {
            if ((ptrMessage->Parameter.TypeCode[parameter]) == (int)ApeTypeCode.IntPtr)
            {
                return (IntPtr)(void*)ptrMessage->Parameter.IntPtr[parameter];
            }
            else
            {
                throw new Exception("Expected ApeTypeCode.IntPtr got ApeTypeCode." + (ptrMessage->Parameter.TypeCode[parameter]).ToString());
            }
        }

        /// <summary>
        /// Gets the specified parameter from the message, validating it is the correct type
        /// </summary>
        /// <param name="ptrMessage">A pointer to the message</param>
        /// <param name="parameter">The parameter number (0 based)</param>
        /// <returns>The value in the mssage for the specified parameter</returns>
        unsafe private bool GetParameterBoolean(Message* ptrMessage, int parameter)
        {
            if ((ptrMessage->Parameter.TypeCode[parameter]) == (int)ApeTypeCode.Boolean)
            {
                return ptrMessage->Parameter.Boolean[parameter];
            }
            else
            {
                throw new Exception("Expected ApeTypeCode.Boolean got ApeTypeCode." + (ptrMessage->Parameter.TypeCode[parameter]).ToString());
            }
        }

        /// <summary>
        /// Gets the specified parameter from the message, validating it is the correct type
        /// </summary>
        /// <param name="ptrMessage">A pointer to the message</param>
        /// <param name="parameter">The parameter number (0 based)</param>
        /// <returns>The value in the mssage for the specified parameter</returns>
        unsafe private string GetParameterString(Message* ptrMessage, int parameter)
        {
            if ((ptrMessage->Parameter.TypeCode[parameter]) == (int)ApeTypeCode.String)
            {
                if (ptrMessage->Parameter.StringLength[parameter] == -1)
                {
                    string Empty = null;
                    return Empty;
                }
                else
                {
                    return new string((char*)(m_IntPtrMemoryMappedFileViewStringStore + ptrMessage->Parameter.StringOffset[parameter]), 0, ptrMessage->Parameter.StringLength[parameter]);
                }
            }
            else
            {
                throw new Exception("Expected ApeTypeCode.String got ApeTypeCode." + (ptrMessage->Parameter.TypeCode[parameter]).ToString());
            }
        }
    }
}
