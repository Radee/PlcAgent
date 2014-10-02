﻿using System;
using System.Linq;
using System.Threading;
using System.Windows;
using _PlcAgent.DataAquisition;
using _PlcAgent.General;
using _PlcAgent.Log;

namespace _PlcAgent.Vector
{
    public class VFlashHandler : ExecutiveModule
    {
        #region Variables

        private readonly VFlashStationController _vFlashStationController;
        private readonly VFlashErrorCollector _vFlashErrorCollector;
        private VFlashTypeBank _vFlashTypeBank;

        private readonly Thread _vFlashThread;

        #endregion


        #region Properties

        public VFlashErrorCollector ErrorCollector
        {
            get { return _vFlashErrorCollector; }
        }

        public VFlashTypeBank VFlashTypeBank
        {
            get { return _vFlashTypeBank; }
            set { _vFlashTypeBank = value; }
        }

        public CommunicationInterfaceHandler CommunicationInterfaceHandler { get; set; }
        public VFlashHandlerInterfaceAssignmentFile VFlashHandlerInterfaceAssignmentFile { get; set; }

        #endregion


        #region Constructors

        public VFlashHandler(uint id, string name, CommunicationInterfaceHandler communicationInterfaceHandler, VFlashTypeBank vFlashTypeBank, VFlashHandlerInterfaceAssignmentFile vFlashHandlerInterfaceAssignmentFile) : base(id, name)
        {
            CommunicationInterfaceHandler = communicationInterfaceHandler;
            VFlashHandlerInterfaceAssignmentFile = vFlashHandlerInterfaceAssignmentFile;

            _vFlashErrorCollector = new VFlashErrorCollector();
            _vFlashStationController = new VFlashStationController(ReportError, Header.Id);
            _vFlashStationController.Add(new VFlashChannel(ReportError, "", Header.Id));

            _vFlashTypeBank = vFlashTypeBank;
            _vFlashErrorCollector = new VFlashErrorCollector();

            _vFlashThread = new Thread(VFlashPlcCommunicationThread);
            _vFlashThread.SetApartmentState(ApartmentState.STA);
            _vFlashThread.IsBackground = true;

            CreateInterfaceAssignment(id, VFlashHandlerInterfaceAssignmentFile.Assignment);
        }

        #endregion


        #region Methods

        public void InitializeVFlash()
        {
            try
            {
                _vFlashStationController.Initialize();
            }
            catch (Exception)
            {
                MessageBox.Show("ID: " + Header.Id + " VFlash initialization failed", "VFlash Failed");
                throw new FlashHandlerException("VFlash initialization failed");
            }

            _vFlashThread.Start();
            Logger.Log("ID: " + Header.Id + " vFlash Initialized");
        }

        public override void Initialize()
        {
            
        }

        public override void Deinitialize()
        {
            _vFlashStationController.Deinitialize();
            _vFlashThread.Abort();

            Logger.Log("ID: " + Header.Id + " vFlash Deinitialized");
        }

        public void LoadProject(uint chanId)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be loaded was not found");
            channelFound.ExecuteCommand(VFlashStationComponent.VFlashCommand.Load); 
        }

        public void UnloadProject(uint chanId)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be unloaded was not found");
            channelFound.ExecuteCommand(VFlashStationComponent.VFlashCommand.Unload);

            if (CommunicationInterfaceHandler.WriteInterfaceComposite != null)
            {
                CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Program Type Active"), (Int16)0);
                CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Version"), "N/L ");
            }
        }

        public void StartFlashing(uint chanId)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be flashed was not found");
            channelFound.ExecuteCommand(VFlashStationComponent.VFlashCommand.Start); 
        }

        public void AbortFlashing(uint chanId)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be aborted was not found");
            channelFound.ExecuteCommand(VFlashStationComponent.VFlashCommand.Abort); 
        }

        public void SetProjectPath(uint chanId, string projectPath)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be set was not found");
            channelFound.FlashProjectPath = projectPath;
            Logger.Log("ID: " + Header.Id + " VFlash: Channel nr. " + chanId + " : New path assigned : \n" + channelFound.FlashProjectPath);

            if (CommunicationInterfaceHandler.WriteInterfaceComposite != null)
            {
                CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Program Type Active"), (Int16)0);
                CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Version"), "N/L ");
            }  
        }

        public VFlashChannel ReturnChannelSetup(uint chanId)
        {
            return (VFlashChannel)_vFlashStationController.ReturnChannelSetup(chanId);
        }

        #endregion


        #region Background methods

        private void VFlashPlcCommunicationThread()
        {
            Int16 counter = 0;
            Int16 antwort = 0;
            Int16 caseAuxiliary = 0;
            Int16 programActive = 0;

            var version = "N/L ";

            while (_vFlashThread.IsAlive)
            {
                var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == 1);
                PcControlModeChangeAllowed = false;

                if (channelFound != null && !PcControlMode && CheckInterface())
                {
                    var inputCompositeCommand = (Int16)CommunicationInterfaceHandler.ReadInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Command")).Value;
                    var inputCompositeProgrammTyp = (Int16)CommunicationInterfaceHandler.ReadInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Program Type")).Value;
                    var inputCompositeProgrammVersion = (string)CommunicationInterfaceHandler.ReadInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Program Version")).Value;

                    switch (inputCompositeCommand)
                    {
                        case 100:
                            if (caseAuxiliary != 100)
                            {
                                Logger.Log("ID: " + Header.Id + " VFlash: Channel nr. " + channelFound.ChannelId +
                                           " : Path change requested from PLC");
                                var returnedPath =
                                    _vFlashTypeBank.ReturnPath(inputCompositeProgrammVersion);
                                if (returnedPath != null)
                                {
                                    SetProjectPath(Header.Id,
                                        _vFlashTypeBank.ReturnPath(inputCompositeProgrammVersion));
                                    programActive = inputCompositeProgrammTyp;
                                    version = inputCompositeProgrammVersion;
                                    antwort = 100;
                                }
                                else
                                {
                                    Logger.Log("ID: " + Header.Id + " VFlash: Channel nr. " + channelFound.ChannelId +
                                               " : Path change requested failed");
                                    programActive = 0;
                                    version = "N/L ";
                                    antwort = 999;
                                }
                            }
                            caseAuxiliary = 100;
                            break;
                        case 200:
                            if (caseAuxiliary != 200)
                            {
                                Logger.Log("ID: " + Header.Id + " VFlash: Channel nr. " + channelFound.ChannelId +
                                           " : Path load requested from PLC");
                                channelFound.ExecuteCommand(VFlashStationComponent.VFlashCommand.Load);
                                Thread.Sleep(200);
                            }
                            if (CommunicationInterfaceHandler.WriteInterfaceComposite != null)
                            {
                                if (channelFound.Status == VFlashStationComponent.VFlashStatus.Loaded) antwort = 200;
                                if (channelFound.Status == VFlashStationComponent.VFlashStatus.Fault) antwort = 999;
                            }
                            caseAuxiliary = 200;
                            break;
                        case 300:
                            if (caseAuxiliary != 300)
                            {
                                Logger.Log("ID: " + Header.Id + " VFlash: Channel nr. " + channelFound.ChannelId +
                                           " : Path unload requested from PLC");
                                channelFound.ExecuteCommand(VFlashStationComponent.VFlashCommand.Unload);
                                Thread.Sleep(200);
                            }
                            if (CommunicationInterfaceHandler.WriteInterfaceComposite != null)
                            {
                                if (channelFound.Status == VFlashStationComponent.VFlashStatus.Unloaded)
                                {
                                    programActive = 0;
                                    version = "N/L ";
                                    antwort = 300;
                                }
                                if (channelFound.Status == VFlashStationComponent.VFlashStatus.Fault) antwort = 999;
                            }
                            caseAuxiliary = 300;
                            break;
                        case 400:
                            if (caseAuxiliary != 400)
                            {
                                Logger.Log("ID: " + Header.Id + " VFlash: Channel nr. " + channelFound.ChannelId +
                                           " : Flashing requested from PLC");
                                channelFound.ExecuteCommand(VFlashStationComponent.VFlashCommand.Start);
                                Thread.Sleep(200);
                            }
                            if (CommunicationInterfaceHandler.WriteInterfaceComposite != null)
                            {
                                if (channelFound.Status == VFlashStationComponent.VFlashStatus.Flashed) antwort = 400;
                                if (channelFound.Status == VFlashStationComponent.VFlashStatus.Fault) antwort = 999;
                            }
                            caseAuxiliary = 400;
                            break;
                        case 500:
                            if (caseAuxiliary != 500)
                            {
                                Logger.Log("ID: " + Header.Id + " VFlash: Channel nr. " + channelFound.ChannelId +
                                           " : Flashing abort requested from PLC");
                                channelFound.ExecuteCommand(VFlashStationComponent.VFlashCommand.Abort);
                                Thread.Sleep(200);
                            }
                            if (CommunicationInterfaceHandler.WriteInterfaceComposite != null)
                            {
                                if (channelFound.Status == VFlashStationComponent.VFlashStatus.Aborted) antwort = 500;
                                if (channelFound.Status == VFlashStationComponent.VFlashStatus.Fault) antwort = 999;
                            }
                            caseAuxiliary = 500;
                            break;
                        default:
                            antwort = 0;
                            caseAuxiliary = 0;
                            break;
                    }
                }
                else
                {
                    antwort = 999;
                    PcControlModeChangeAllowed = true;
                }

                Int16 statusInt = 0;
                Int16 programPercentage = 0;

                if (channelFound != null)
                    switch (channelFound.Status)
                    {
                        case VFlashStationComponent.VFlashStatus.Created:
                            statusInt = 100;
                            PcControlModeChangeAllowed = true;
                            break;
                        case VFlashStationComponent.VFlashStatus.Loading:
                            statusInt = 299;
                            break;
                        case VFlashStationComponent.VFlashStatus.Loaded:
                            statusInt = 200;
                            PcControlModeChangeAllowed = true;
                            break;
                        case VFlashStationComponent.VFlashStatus.Unloading:
                            statusInt = 399;
                            version = "N/L ";
                            break;
                        case VFlashStationComponent.VFlashStatus.Unloaded:
                            statusInt = 300;
                            PcControlModeChangeAllowed = true;
                            break;
                        case VFlashStationComponent.VFlashStatus.Flashing:
                            statusInt = 499;
                            programPercentage = Convert.ToInt16(channelFound.ProgressPercentage);
                            break;
                        case VFlashStationComponent.VFlashStatus.Flashed:
                            statusInt = 400;
                            programPercentage = 100;
                            PcControlModeChangeAllowed = true;
                            break;
                        case VFlashStationComponent.VFlashStatus.Aborting:
                            statusInt = 599;
                            break;
                        case VFlashStationComponent.VFlashStatus.Fault:
                            PcControlModeChangeAllowed = true;
                            statusInt = 999;
                            break;
                        default:
                            statusInt = 0;
                            PcControlModeChangeAllowed = true;
                            break;
                    }
                if (CommunicationInterfaceHandler.WriteInterfaceComposite != null && CheckInterface())
                {
                    CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Life Counter"), counter);
                    counter++;
                    CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Reply"), antwort);
                    CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Status"), statusInt);
                    CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Program Type Active"), programActive);
                    CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Version"), version);
                    CommunicationInterfaceHandler.WriteInterfaceComposite.ModifyValue(InterfaceAssignmentCollection.GetAssignment("Progress Percentage"), programPercentage);
                }
                Thread.Sleep(200);
            }
        }

        #endregion


        #region Auxiliaries

        public class FlashHandlerException : ApplicationException
        {
            public FlashHandlerException(string info) : base(info) { }
        }

        internal void ReportError(uint channelId, long handle, string errorMessage)
        {
            ErrorCollector.AddMessage(DateTime.Now + "Handle {0}: {1}", handle, errorMessage);
            Logger.Log("ID: " + Header.Id + " VFlash: Fault on Channel nr. " + channelId + " : " + errorMessage);
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == channelId);
            if (channelFound != null)
            {
                channelFound.Command = VFlashStationComponent.VFlashCommand.NoCommand;
                channelFound.Status = VFlashStationComponent.VFlashStatus.Fault;
            }
        }

        protected override Boolean CheckInterface()
        {
            CommunicationInterfaceComponent component = CommunicationInterfaceHandler.ReadInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Command"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.Integer)
                return false;
            component = CommunicationInterfaceHandler.ReadInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Program Type"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.Integer)
                return false;
            component = CommunicationInterfaceHandler.ReadInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Program Version"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.String)
                return false;
            component = CommunicationInterfaceHandler.WriteInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Life Counter"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.Integer)
                return false;
            component = CommunicationInterfaceHandler.WriteInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Reply"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.Integer)
                return false;
            component = CommunicationInterfaceHandler.WriteInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Status"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.Integer)
                return false;
            component = CommunicationInterfaceHandler.WriteInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Program Type Active"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.Integer)
                return false;
            component = CommunicationInterfaceHandler.WriteInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Version"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.String)
                return false;
            component = CommunicationInterfaceHandler.WriteInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Fault Code"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.Integer)
                return false;
            component = CommunicationInterfaceHandler.WriteInterfaceComposite.ReturnVariable(InterfaceAssignmentCollection.GetAssignment("Progress Percentage"));
            if (component == null || component.TypeOfVariable != CommunicationInterfaceComponent.VariableType.Integer)
                return false;

            return true;
        }

        protected override sealed void CreateInterfaceAssignment(uint id, string[][] assignment)
        {
            if (assignment[id].Length == 0) assignment[id] = new string[10];

            InterfaceAssignmentCollection = new InterfaceAssignmentCollection();
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.In,
                Name = "Command",
                Type = CommunicationInterfaceComponent.VariableType.Integer,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][0]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.In,
                Name = "Program Type",
                Type = CommunicationInterfaceComponent.VariableType.Integer,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][1]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.In,
                Name = "Program Version",
                Type = CommunicationInterfaceComponent.VariableType.String,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][2]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.Out,
                Name = "Life Counter",
                Type = CommunicationInterfaceComponent.VariableType.Integer,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][3]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.Out,
                Name = "Reply",
                Type = CommunicationInterfaceComponent.VariableType.Integer,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][4]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.Out,
                Name = "Status",
                Type = CommunicationInterfaceComponent.VariableType.Integer,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][5]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.Out,
                Name = "Program Type Active",
                Type = CommunicationInterfaceComponent.VariableType.Integer,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][6]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.Out,
                Name = "Version",
                Type = CommunicationInterfaceComponent.VariableType.String,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][7]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.Out,
                Name = "Fault Code",
                Type = CommunicationInterfaceComponent.VariableType.Integer,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][8]
            });
            InterfaceAssignmentCollection.Children.Add(new InterfaceAssignment
            {
                VariableDirection = InterfaceAssignment.Direction.Out,
                Name = "Progress Percentage",
                Type = CommunicationInterfaceComponent.VariableType.Integer,
                Assignment = VFlashHandlerInterfaceAssignmentFile.Assignment[id][9]
            });
        }

        public override void UpdateAssignment()
        {
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][0] =
                InterfaceAssignmentCollection.GetAssignment("Command");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][1] =
                InterfaceAssignmentCollection.GetAssignment("Program Type");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][2] =
                InterfaceAssignmentCollection.GetAssignment("Program Version");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][3] =
                InterfaceAssignmentCollection.GetAssignment("Life Counter");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][4] =
                InterfaceAssignmentCollection.GetAssignment("Reply");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][5] =
                InterfaceAssignmentCollection.GetAssignment("Status");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][6] =
                InterfaceAssignmentCollection.GetAssignment("Program Type Active");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][7] =
                InterfaceAssignmentCollection.GetAssignment("Version");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][8] =
                InterfaceAssignmentCollection.GetAssignment("Fault Code");
            VFlashHandlerInterfaceAssignmentFile.Assignment[Header.Id][9] =
                InterfaceAssignmentCollection.GetAssignment("Progress Percentage");
            VFlashHandlerInterfaceAssignmentFile.Save();
        }

        #endregion

    }
}
