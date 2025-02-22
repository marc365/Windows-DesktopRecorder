#region MIT license
// 
// MIT license
//
// Copyright (c) 2017-2025 Marc Williams
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
// 
#endregion
using Microsoft.Win32;
using System;
using System.IO;
using System.IO.Pipes;
using System.Windows.Forms;

namespace DesktopRecorder
{
    static class Program
    {
        public static bool AutoStart = false;

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
            if (args.Length > 0)
            {
                RegistryKey registry = null;
                string arg = null;
                for (int i = 0; i < args.Length; i++) switch (args[i])
                {
                    case Arg.Start:
                    case Arg.Stop:
                    case Arg.Quit:
                        arg = args[i];
                        break;
                    case Arg.File:
                        i++;
                        if (i < args.Length)
                        {
                            if (string.IsNullOrEmpty(Path.GetExtension(args[i]))) args[i] = args[i] + Def.Wav;

                            registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
                            registry.SetValue(Reg.File, args[i]);
                            registry.SetValue(Reg.HotSwap, Def.True);
                            registry.SetValue(Reg.Output, 0);
                        }
                        break;
                    case Arg.Stream:
                        i++;
                        if (i < args.Length)
                        {
                            registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
                            registry.SetValue(Reg.Stream, args[i]);
                            registry.SetValue(Reg.Output, 1);
                        }
                        break;
                    case Arg.Mode:
                        i++;
                        if (i < args.Length)
                        {
                            int mode;
                            if (int.TryParse(args[i], out mode))
                            {
                                registry = Registry.CurrentUser.CreateSubKey(Reg.KEY);
                                registry.SetValue(Reg.Mode, mode);
                            }
                        }
                        break;
                    default:
                        MessageBox.Show(args[i], Error.Argument, MessageBoxButtons.OK);
                        return;
                }
                if (registry != null) registry.Close();
                TryPipe(arg);
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        /// <summary>
        /// Find an existing desktop recorder instance
        /// </summary>
        private static void TryPipe(string arg)
        {
            using (NamedPipeClientStream pipe = new NamedPipeClientStream(Def.PipeName))
            {
                try
                {
                    pipe.Connect(0);
                }
                catch (TimeoutException) //nothing found 
                {
                    switch (arg)
                    {
                        case Arg.Start:
                            AutoStart = true;
                            break;
                    }
                    return;
                }
                //command line remote control
                if (pipe.ReadByte() == 1)
                {
                    switch (arg)
                    {
                        case Arg.Start:
                            pipe.WriteByte(2);
                            break;
                        case Arg.Stop:
                            pipe.WriteByte(3);
                            break;
                        case Arg.Quit:
                            pipe.WriteByte(4);
                            break;
                        default:
                            pipe.WriteByte(1); //handshake
                            break;
                    }
                }
                Environment.Exit(0);
            }
        }
    }
}
