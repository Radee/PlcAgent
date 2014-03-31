﻿using System;
using System.Linq;
using System.Threading;
using Vector.vFlash.Automation;
using _3880_80_FlashStation.DataAquisition;

namespace _3880_80_FlashStation.Vector
{
    class VFlashHandler
    {
        #region Variables

        private readonly VFlashStationController _vFlashStationController;
        private readonly VFlashErrorCollector _vFlashErrorCollector;
        private readonly VFlashTypeBank _vFlashTypeBank;

        private readonly CommunicationInterfaceComposite _inputComposite;
        private readonly CommunicationInterfaceComposite _outputComposite;

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
        }

        #endregion

        #region Constructor

        public VFlashHandler(CommunicationInterfaceComposite inputComposite, CommunicationInterfaceComposite outputComposite, CallbackProgressDelegate updateProgressDelegate, CallbackStatusDelegate updateStatusDelegate)
        {
            _inputComposite = inputComposite;
            _outputComposite = outputComposite;

            _vFlashErrorCollector = new VFlashErrorCollector();
            _vFlashStationController = new VFlashStationController(ReportError, 0);
            _vFlashStationController.Add(new VFlashChannel(ReportError, "", 1, updateProgressDelegate, updateStatusDelegate));
            _vFlashStationController.Initialize();

            _vFlashTypeBank = new VFlashTypeBank();
            _vFlashErrorCollector = new VFlashErrorCollector();

            _vFlashThread = new Thread(VFlashPlcCommunicationThread);
            _vFlashThread.SetApartmentState(ApartmentState.STA);
            _vFlashThread.IsBackground = true;
            _vFlashThread.Start();
        }

        #endregion

        #region Methods

        public void LoadProject(uint chanId)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be loaded was not found");
            channelFound.ExecuteCommand("Load"); 
        }

        public void UnloadProject(uint chanId)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be unloaded was not found");
            channelFound.ExecuteCommand("Unload"); 
        }

        public void StartFlashing(uint chanId)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be flashed was not found");
            channelFound.ExecuteCommand("Start"); 
        }

        public void AbortFlashing(uint chanId)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be aborted was not found");
            channelFound.ExecuteCommand("Abort"); 
        }

        public void SetProjectPath(uint chanId, string projectPath)
        {
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == chanId);
            if (channelFound == null) throw new FlashHandlerException("Error: Channel to be aborted was not found");
            channelFound.FlashProjectPath = projectPath;
        }

        public VFlashChannel ReturnChannelSetup(uint chanId)
        {
            return (VFlashChannel)_vFlashStationController.ReturnChannelSetup(chanId);
        }

        public void Deinitialize()
        {
            _vFlashStationController.Deinitialize();
        }

        #endregion

        #region Background methods

        private void VFlashPlcCommunicationThread()
        {
            Int16 counter = 0;
            while (_vFlashThread.IsAlive)
            {
                var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == 1);
                var inputComposite = (CiInteger)_inputComposite.ReturnVariable("BEFEHL");

                if (channelFound != null)
                    switch (inputComposite.Value)
                    {
                        case 100:
                            if (_outputComposite != null) _outputComposite.ModifyValue("ANTWORT", (Int16)100);
                            break;
                        case 200:
                            channelFound.ExecuteCommand("Load");
                            if (_outputComposite != null && channelFound.Status == "Loaded") _outputComposite.ModifyValue("ANTWORT", (Int16)200);
                            break;
                        case 300:
                            channelFound.ExecuteCommand("Unload");
                            if (_outputComposite != null && channelFound.Status == "Unloaded") _outputComposite.ModifyValue("ANTWORT", (Int16)300);
                            break;
                        case 400:
                            channelFound.ExecuteCommand("Start");
                            if (_outputComposite != null && channelFound.Status == "Flashed") _outputComposite.ModifyValue("ANTWORT", (Int16)400);
                            break;
                        case 500:
                            channelFound.ExecuteCommand("Abort");
                            if (_outputComposite != null && channelFound.Status == "Loaded") _outputComposite.ModifyValue("ANTWORT", (Int16)500);
                            break;
                        default:
                            if (_outputComposite != null) _outputComposite.ModifyValue("ANTWORT", (Int16)0);
                            break;
                    }
                
                    Int16 statusInt = 0;
                if (channelFound != null)
                    switch (channelFound.Status)
                    {
                        case "Created":
                            statusInt = 100;
                            break;
                        case "Loading":
                            statusInt = 299;
                            break;
                        case "Loaded":
                            statusInt = 200;
                            break;
                        case "Unloading":
                            statusInt = 399;
                            break;
                        case "Unloaded":
                            statusInt = 300;
                            break;
                        case "Flashing":
                            statusInt = 499;
                            break;
                        case "Flashed":
                            statusInt = 400;
                            break;
                        case "Aborting":
                            statusInt = 599;
                            break;
                        case "Fault occured!":
                            statusInt = 999;
                            break;
                        default:
                            statusInt = 0;
                            break;
                    }
                if (_outputComposite != null)
                {
                    _outputComposite.ModifyValue("STATUS", statusInt);
                    _outputComposite.ModifyValue("LEBENSZAECHLER", counter);
                    counter++;
                }
                Thread.Sleep(50);
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
            var channelFound = (VFlashChannel)_vFlashStationController.Children.FirstOrDefault(channel => channel.ChannelId == channelId);
            if (channelFound != null)
            {
                channelFound.Command = "";
                channelFound.Status = "Fault occured!";
            }
        }

        #endregion
    }
}
