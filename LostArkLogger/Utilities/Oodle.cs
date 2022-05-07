﻿using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;

namespace LostArkLogger
{
    public enum OodleInitStatus {
        OODLE_OK,
        OODLE_DLL_FAIL
    }

    public class Oodle
    {
        [DllImport("oo2net_9_win64")] static extern bool OodleNetwork1UDP_Decode(byte[] state, byte[] shared, byte[] comp, int compLen, byte[] raw, int rawLen);
        [DllImport("oo2net_9_win64")] static extern bool OodleNetwork1UDP_State_Uncompact(byte[] state, byte[] compressorState);
        [DllImport("oo2net_9_win64")] static extern void OodleNetwork1_Shared_SetWindow(byte[] data, int length, byte[] data2, int length2);
        [DllImport("oo2net_9_win64")] static extern int OodleNetwork1UDP_State_Size();
        [DllImport("oo2net_9_win64")] static extern int OodleNetwork1_Shared_Size(int bits);
        static Byte[] oodleState;
        static Byte[] oodleSharedDict;
        static Byte[] initDict;
        const string oodleDll = "oo2net_9_win64.dll";
        public static OodleInitStatus Init()

        {
            while (!File.Exists(oodleDll))
            {
                if (File.Exists(@"C:\Program Files (x86)\Steam\steamapps\common\Lost Ark\Binaries\Win64\" + oodleDll))
                {
                    File.Copy(@"C:\Program Files (x86)\Steam\steamapps\common\Lost Ark\Binaries\Win64\" + oodleDll, oodleDll);
                    continue;
                }
                var installLocation = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 1599340")?.GetValue("InstallLocation");
                if (installLocation != null)
                {
                    var fullOodleDll = Path.Combine(installLocation.ToString(), "Binaries", "Win64", oodleDll);
                    if (File.Exists(fullOodleDll))
                    {
                        File.Copy(fullOodleDll, oodleDll);
                        continue;
                    }
                }
                return OodleInitStatus.OODLE_DLL_FAIL;
            }
            var payload = ObjectSerialize.Decompress(Properties.Resources.oodle_state);
            initDict = payload.Skip(0x20).Take(0x800000).ToArray();
            var compressorSize = BitConverter.ToInt32(payload, 0x18);
            var compressorState = payload.Skip(0x20).Skip(0x800000).Take(compressorSize).ToArray();
            var stateSize = OodleNetwork1UDP_State_Size();
            oodleState = new Byte[stateSize];
            if (!OodleNetwork1UDP_State_Uncompact(oodleState, compressorState)) throw new Exception("oodle init fail");
            oodleSharedDict = new Byte[OodleNetwork1_Shared_Size(0x13)];
            OodleNetwork1_Shared_SetWindow(oodleSharedDict, 0x13, initDict, 0x800000);
            return OodleInitStatus.OODLE_OK;

        }
        public static Byte[] Decompress(Byte[] decompressed)
        {
            var oodleSize = BitConverter.ToInt32(decompressed, 0);
            var payload = decompressed.Skip(4).ToArray();
            var tempPayload = new Byte[oodleSize];
            try
            {
                if (!OodleNetwork1UDP_Decode(oodleState, oodleSharedDict, payload, payload.Length, tempPayload, oodleSize))
                    throw new Exception("oodle decompress fail");
            }
            catch (Exception e)
            {
                Console.WriteLine("access excepted");
            }
            return tempPayload;
        }
    }
}
