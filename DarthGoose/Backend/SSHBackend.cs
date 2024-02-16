﻿using Renci.SshNet;
using System.IO;
using Backend.CredentialManager;
using Backend.NetworkDeviceManager;
using System.Diagnostics;

namespace SSHBackend
{
    class SSHManager
    {
        public ManagementProtocol sshType { get; set; }
        private SshClient _client { get; set; }
        private ShellStream? _stream { get; set; }
        private TerminalManager.ReadCallback _readCallback { get; set; }
        private Thread? _readThread { get; set; }
        private bool _connected { get; set; }

        // Can send a command and recieve a response to the command
        // returns a string with the response to the command -- either the error or the result
        public SSHManager(string hostaddress, Credentials credentials, TerminalManager.ReadCallback readCallback)
        {
            this._readCallback = readCallback;
            this.sshType = ManagementProtocol.SSH;
            _connected = false;
            Debug.WriteLine(credentials.GetCreds()[1]);
            _client = new SshClient(hostaddress, credentials.GetCreds()[0], credentials.GetCreds()[1]);
            // Console.WriteLine(hostaddress);
        }
        private void ReadThreadMethod()
        {
            StreamReader reader = new StreamReader(_stream);
            while (_connected)
            {
                string output = ReadStream(reader);
                if (output != null && output != "" && output != "\n")
                {
                    _readCallback(output);
                }
                Thread.Sleep(500);
            }
        }
        public void Connect()
        {
            try
            {
                _client.Connect();
                if (sshType == ManagementProtocol.SSHNoExe)
                {
                    _connected = true;
                    CreateShellStream();

                    // Initialize read thread
                    _readThread = new Thread(ReadThreadMethod);
                    _readThread.IsBackground = true;
                    _readThread.Start();
                }  
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

        }

        public void CreateShellStream()
        {
            _stream = _client.CreateShellStream("customCommand", 80, 24, 800, 600, 1024);
        }

        public void ExecuteExecChannel(string command)
        {
            SshCommand _command = _client.CreateCommand(command);
            _command.Execute();
            string result = _command.Result;
            if (_command.Error != "")
            {
                throw new Exception("SSH Command Error " + _command.Error);
            }
            _readCallback(result);
        }

        public void ExecuteShellStream(string command)
        {
            if (_stream == null)
            {
                throw new NullReferenceException(nameof(_stream) + " Please run the create shell stream function before attempting to execute commands through the shell channel");
            }
            StreamWriter writer = new StreamWriter(_stream);
            writer.AutoFlush = true;
            WriteStream(command, writer, _stream);
        }

        public void Disconnect()
        {
            try
            {
                if (sshType == ManagementProtocol.SSHNoExe)
                {
                    _connected = false;
                }
                _client.Disconnect();
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

        }

        private static void WriteStream(string cmd, StreamWriter writer, ShellStream stream)
        {
            writer.WriteLine(cmd);
            while (stream.Length == 0)
            {
                Thread.Sleep(500);
            }
        }

        private static string ReadStream(StreamReader reader)
        {
            string result = "";
            string line = "";
            while (line != null)
            {
                line = reader.ReadLine();
                result += line + "\n";
            }
            return result;
        }
    }
}