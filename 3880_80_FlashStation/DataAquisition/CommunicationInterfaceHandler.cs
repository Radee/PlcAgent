﻿using System;
using _3880_80_FlashStation.PLC;

namespace _3880_80_FlashStation.DataAquisition
{
    public class CommunicationInterfaceHandler
    {
        private CommunicationInterfaceComposite _readInterfaceComposite;
        private CommunicationInterfaceComposite _writeInterfaceComposite;

        public CommunicationInterfaceComposite ReadInterfaceComposite
        {
            get { return _readInterfaceComposite; }
        }

        public CommunicationInterfaceComposite WriteInterfaceComposite
        {
            get { return _writeInterfaceComposite; }
            set { _writeInterfaceComposite = value; }
        }

        public void Initialize()
        {
            _readInterfaceComposite = CommunicationInterfaceBuilder.InitializeInterface("readInterface");
            _writeInterfaceComposite = CommunicationInterfaceBuilder.InitializeInterface("writeInterface");
        }

        public void MaintainConnection(PlcCommunicator communication)
        {
            if (_readInterfaceComposite != null && _writeInterfaceComposite != null &&
                communication.ConnectionStatus == 1)
            {
                _readInterfaceComposite.ReadValue(communication.ReadBytes);
                _writeInterfaceComposite.WriteValue(communication.WriteBytes);
            }
            else { throw new InitializerException("Error: Connection can not be maintained."); }
        }

        #region Auxiliaries

        public class InitializerException : ApplicationException
        { public InitializerException(string info) : base(info) { }}

        #endregion
    }
}
