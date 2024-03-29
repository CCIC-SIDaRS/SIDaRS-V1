﻿using Renci.SshNet;
using System.IO;
using Backend.CredentialManager;
using Backend.NetworkDeviceManager;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace SSHBackend
{
    class SSHManager
    {
        public ManagementProtocol sshType { get; set; }
        public bool _connected { get; private set; }
        private SshClient _client { get; set; }
        private ShellStream? _stream { get; set; }
        private TerminalManager.ReadCallback _readCallback { get; set; }
        private Thread? _readThread { get; set; }
        private TerminalBuffer _buff { get; set; }

        // Can send a command and recieve a response to the command
        // returns a string with the response to the command -- either the error or the result
        public SSHManager(string hostaddress, Credentials credentials, TerminalManager.ReadCallback readCallback)
        {
            this._readCallback = readCallback;
            this.sshType = ManagementProtocol.SSH;
            _connected = false;
            // Debug.WriteLine(credentials.GetCreds()[1]);
            _client = new SshClient(hostaddress, credentials.GetCreds()[0], credentials.GetCreds()[1]);
            this._buff = new TerminalBuffer(readCallback);
            // Console.WriteLine(hostaddress);
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

        private void ReadThreadMethod()
        {
            StreamReader reader = new StreamReader(_stream);
            while (_connected)
            {
                ReadStream(reader);
                _buff.Flush();
                Thread.Sleep(250);
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
                if (sshType == ManagementProtocol.SSHNoExe && _connected)
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
        }

        private void ReadStream(StreamReader reader)
        {
            string line = "";
            while (line != null)
            {
                line = reader.ReadLine();
                //Debug.WriteLine(line);
                _buff.Push(line + "\n");
            }
        }
    }

    class TerminalBuffer
    {
        public string terminalMessage { get; private set; }
        private TerminalManager.ReadCallback _readCallback { get; set; }

        public TerminalBuffer(TerminalManager.ReadCallback readCallBack) 
        {
            this._readCallback = readCallBack;
        }

        public void Push(string line)
        {
            //Debug.WriteLine(terminalMessage);
            terminalMessage += line;
        }

        static string FilterSSHCursorCodes(string text)
        {
            // Regular expression pattern to match SSH cursor codes
            string cursorPattern = @"\x1b\[[0-9;]*[a-zA-Z]";

            // Replace all occurrences of the cursor codes with an empty string
            string filteredText = Regex.Replace(text, cursorPattern, string.Empty);

            return filteredText;
        }

        public void Flush()
        {
            if(terminalMessage != null && terminalMessage != "" && terminalMessage != "\n")
            {
                /*List<string> testing = terminalMessage.Split("[").ToList();
                int max = testing.Count;
                Debug.WriteLine(string.Join("    |||||    ", testing));
                for (int i = 0; i < max; ++i)
                {
                    if ((testing[i].Length > 2 && testing[i][2] == 'X') || testing[i].StartsWith("2J") || testing[i].StartsWith("25h") || testing[i].StartsWith("]"))
                    {
                        testing.Remove(testing[i]);
                        --max;
                    }
                }*/
                //Debug.WriteLine(terminalMessage);

                terminalMessage = FilterSSHCursorCodes(terminalMessage);

                //while (true)
                //{
                //    int start = terminalMessage.IndexOf("]0;");
                //    if (start)
                //}

                string[] middleman = terminalMessage.Split("\n");//string.Join("", testing).Split("\n");
                middleman = middleman.Distinct().ToArray();
                _readCallback(string.Join("\n", middleman));
                terminalMessage = "";
            }
        }
    }
}