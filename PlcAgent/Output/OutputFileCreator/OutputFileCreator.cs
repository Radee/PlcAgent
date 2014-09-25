﻿using System;
using System.Threading;
using _PlcAgent.DataAquisition;
using _PlcAgent.General;
using _PlcAgent.Log;

namespace _PlcAgent.Output.OutputFileCreator
{
    class OutputFileCreator : OutputModule
    {
        #region Variables

        private readonly Thread _communicationThread;

        #endregion


        #region Properties

        public OutputWriter OutputWriter { get; set; }

        public OutputHandlerFile OutputHandlerFile { get; set; }
        public OutputHandlerInterfaceAssignmentFile OutputHandlerInterfaceAssignmentFile { get; set; }

        #endregion


        #region Constructors

        public OutputFileCreator(uint id, string name, CommunicationInterfaceHandler communicationInterfaceHandler)
            : base(id, name, communicationInterfaceHandler)
        {
            OutputWriter = new OutputXmlWriter();

            _communicationThread = new Thread(OutputCommunicationThread);
            _communicationThread.SetApartmentState(ApartmentState.STA);
            _communicationThread.IsBackground = true;
        }

        #endregion


        #region Background methods

        private void OutputCommunicationThread()
        {
        }

        #endregion


        #region Auxiliaries

        public class OutputFileCreatorException : ApplicationException
        {
            public OutputFileCreatorException(string info) : base(info) { }
        }

        public override void Initialize()
        {
            _communicationThread.Start();
            Logger.Log("ID: " + Header.Id + " Output File Creator Initialized");
        }

        public override void Deinitialize()
        {
            _communicationThread.Abort();
            Logger.Log("ID: " + Header.Id + " Output File Creator Deinitialized");
        }

        public override void UpdateAssignment()
        {
            throw new NotImplementedException();
        }

        protected override bool CheckInterface()
        {
            throw new NotImplementedException();
        }

        protected override void CreateInterfaceAssignment(uint id, string[][] assignment)
        {
            throw new NotImplementedException();
        }

        #endregion

    }
}