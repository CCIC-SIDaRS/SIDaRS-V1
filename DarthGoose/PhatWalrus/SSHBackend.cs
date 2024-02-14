using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Security;
using System.Security.Principal;
using System.Collections;
using CredentialManager;
using NetworkDeviceManager;
using System.Text.Json.Serialization;

namespace SSHBackend
{
    class SSHManager
    {
        public ManagementProtocol sshType { get; set; }
        private SshClient client { get; set; }
        private ShellStream? stream { get; set; }
        private TerminalManager.ReadCallback readCallback { get; set; }
        private Thread? readThread { get; set; }
        private bool connected { get; set; }

        // Can send a command and recieve a response to the command
        // returns a string with the response to the command -- either the error or the result
        public SSHManager(string hostaddress, Credentials credentials, TerminalManager.ReadCallback readCallback)
        {
            this.readCallback = readCallback;
            this.sshType = ManagementProtocol.SSH;
            connected = false;

            client = new SshClient(hostaddress, credentials.GetCreds()[0], credentials.GetCreds()[1]);
            Console.WriteLine(hostaddress);
        }
        private void ReadThreadMethod()
        {
            StreamReader reader = new StreamReader(stream);
            while (connected)
            {
                string output = ReadStream(reader);
                if (output != null && output != "" && output != "\n")
                {
                    readCallback(output);
                }
                Thread.Sleep(500);
            }
        }
        public void Connect()
        {
            try
            {
                client.Connect();
                if (sshType == ManagementProtocol.SSHNoExe)
                {
                    connected = true;
                    CreateShellStream();

                    // Initialize read thread
                    readThread = new Thread(ReadThreadMethod);
                    readThread.IsBackground = true;
                    readThread.Start();
                }  
            }
            catch (Exception ex)
            {
                throw new Exception(ex.ToString());
            }

        }

        public void CreateShellStream()
        {
            stream = client.CreateShellStream("customCommand", 80, 24, 800, 600, 1024);
        }

        public void ExecuteExecChannel(string command)
        {
            SshCommand _command = client.CreateCommand(command);
            _command.Execute();
            string result = _command.Result;
            if (_command.Error != "")
            {
                throw new Exception("SSH Command Error " + _command.Error);
            }
            readCallback(result);
        }

        public void ExecuteShellStream(string command)
        {
            if (stream == null)
            {
                throw new NullReferenceException(nameof(stream) + " Please run the create shell stream function before attempting to execute commands through the shell channel");
            }
            StreamWriter writer = new StreamWriter(stream);
            writer.AutoFlush = true;
            WriteStream(command, writer, stream);
        }

        public void Disconnect()
        {
            try
            {
                if (sshType == ManagementProtocol.SSHNoExe)
                {
                    connected = false;
                }
                client.Disconnect();
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