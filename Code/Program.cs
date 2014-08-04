using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Reflection;

namespace VHDMaker
{
    class Program
    {
        //Microsoft uses the “conectix” string to identify this file as a hard disk image
        const string _msCookie = "conectix";

        static void Main(string[] args)
        {
            if (args.Length != 4)
            {
                Console.WriteLine("Usage: VHDMaker <Filename> <Cylinders> <Heads> <Sectors>");
                return;
            }

            var file = args[0];

            var size = new System.IO.FileInfo(file).Length;

            var stream = new System.IO.FileStream(file, System.IO.FileMode.Open, System.IO.FileAccess.ReadWrite);
            stream.Seek(-512, System.IO.SeekOrigin.End);

            
            var footerBytes = new byte[512];
            stream.Read(footerBytes, 0, 512);

            var footer = DeserializeMsg<VHDFooter>(footerBytes);

            if (System.Text.Encoding.Default.GetString(footer.Cookie) == _msCookie)
            {
                //Already has footer
                stream.Seek(-512, System.IO.SeekOrigin.End);
                size -= 512;
            }
            else
            {
                //Add footer
                stream.Seek(0, System.IO.SeekOrigin.End);
            }

            footer = new VHDFooter()
            {
                Cookie = System.Text.Encoding.Default.GetBytes(_msCookie),
                //2=Reserved. This bit must always be set to 1.
                Features = 2,
                //For the current specification, this field must be initialized to 0x00010000
                FileFormatVersion = 0x10000,
                //For fixed disks, this field should be set to 0xFFFFFFFF
                DataOffset = -1,
                //seconds since January 1, 2000 12:00:00 AM
                TimeStamp = (int)((DateTime.Now - new DateTime(2000, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalSeconds),
                //Avertes VHD Maker => AvVM
                CreatorApplication = System.Text.Encoding.Default.GetBytes("AvVM"), 
                CreatorVersion = 0x10000,
                //Windows = Wi2k
                CreatorHostOS = System.Text.Encoding.Default.GetBytes("Wi2k"), 
                OriginalSize = size,
                CurrentSize = size,
                DiskGeometryCylinders = ushort.Parse(args[1]),
                DiskGeometryHeads = byte.Parse(args[2]),
                DiskGeometrySectors = byte.Parse(args[3]),
                //Fixed hard disk = 2
                DiskType = 2,
                UniqueId = new Guid(),
                SavedState = 0
            };

            footerBytes = SerializeMessage(footer);
            CalcCheckSum(footerBytes);
            stream.Write(footerBytes, 0, 512);
        }
        static void CalcCheckSum(byte[] VhdFooter)
        {
            int sum = -1;
            for (int i = 0; i < 512; i++)
                if (i < 64 || i >= 68) sum -= VhdFooter[i];

            VhdFooter[67] = (byte)(sum & 255);
            VhdFooter[66] = (byte)((sum >> 8) & 255);
            VhdFooter[65] = (byte)((sum >> 16) & 255);
            VhdFooter[64] = (byte)((sum >> 24) & 255);
        }
        public static Byte[] SerializeMessage<T>(T msg) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            Byte[] ret = new Byte[objsize];
            IntPtr buff = Marshal.AllocHGlobal(objsize);
            Marshal.StructureToPtr(FlipEndian(msg), buff, true);
            Marshal.Copy(buff, ret, 0, objsize);
            Marshal.FreeHGlobal(buff);
            return ret;
        }
        public static T DeserializeMsg<T>(Byte[] data) where T : struct
        {
            int objsize = Marshal.SizeOf(typeof(T));
            IntPtr buff = Marshal.AllocHGlobal(objsize);
            Marshal.Copy(data, 0, buff, objsize);
            T retStruct = (T)Marshal.PtrToStructure(buff, typeof(T));
            Marshal.FreeHGlobal(buff);
            return FlipEndian(retStruct);
        }

        private static T FlipEndian<T>(T data) where T : struct
        {
            System.Type t = data.GetType();
            FieldInfo[] fieldInfo = t.GetFields();
            foreach (FieldInfo fi in fieldInfo)
            {
                if (fi.FieldType == typeof(System.Int16))
                {
                    Int16 i16 = (Int16)fi.GetValue(data);
                    byte[] b16 = BitConverter.GetBytes(i16);
                    Array.Reverse(b16);
                    fi.SetValueDirect(__makeref(data), BitConverter.ToInt16(b16, 0));
                }
                else if (fi.FieldType == typeof(System.Int32))
                {
                    Int32 i32 = (Int32)fi.GetValue(data);
                    byte[] b32 = BitConverter.GetBytes(i32);
                    Array.Reverse(b32);
                    fi.SetValueDirect(__makeref(data), BitConverter.ToInt32(b32, 0));
                }
                else if (fi.FieldType == typeof(System.Int64))
                {
                    Int64 i64 = (Int64)fi.GetValue(data);
                    byte[] b64 = BitConverter.GetBytes(i64);
                    Array.Reverse(b64);
                    fi.SetValueDirect(__makeref(data), BitConverter.ToInt64(b64, 0));
                }
                else if (fi.FieldType == typeof(System.UInt16))
                {
                    UInt16 i16 = (UInt16)fi.GetValue(data);
                    byte[] b16 = BitConverter.GetBytes(i16);
                    Array.Reverse(b16);
                    fi.SetValueDirect(__makeref(data), BitConverter.ToUInt16(b16, 0));
                }
                else if (fi.FieldType == typeof(System.UInt32))
                {
                    UInt32 i32 = (UInt32)fi.GetValue(data);
                    byte[] b32 = BitConverter.GetBytes(i32);
                    Array.Reverse(b32);
                    fi.SetValueDirect(__makeref(data), BitConverter.ToUInt32(b32, 0));
                }
                else if (fi.FieldType == typeof(System.UInt64))
                {
                    // TODO
                    UInt64 i64 = (UInt64)fi.GetValue(data);
                    byte[] b64 = BitConverter.GetBytes(i64);
                    Array.Reverse(b64);
                    fi.SetValueDirect(__makeref(data), BitConverter.ToUInt64(b64, 0));
                }
            }
            return data;
        }
    }
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    struct VHDFooter
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public byte[] Cookie;
        public int Features;
        public int FileFormatVersion;
        public long DataOffset;
        public int TimeStamp;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] CreatorApplication;
        public int CreatorVersion;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] CreatorHostOS;
        public long OriginalSize;
        public long CurrentSize;
        public ushort DiskGeometryCylinders;
        public byte DiskGeometryHeads;
        public byte DiskGeometrySectors;
        public int DiskType;
        public int Checksum;
        public Guid UniqueId;
        public byte SavedState;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 427)]
        string Reserved;
    }
}
