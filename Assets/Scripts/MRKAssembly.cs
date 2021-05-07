using System.Runtime.InteropServices;

namespace MRK {
    [StructLayout(LayoutKind.Explicit)]
    public struct MRKAssemblyRegister {
        [FieldOffset(0)]
        public bool _1;

        [FieldOffset(0)]
        public char _8;

        [FieldOffset(0)]
        public short _16;

        [FieldOffset(0)]
        public int _32;

        [FieldOffset(0)]
        public long _64;
    }

    //max 16 regs
    public struct MRKAssemblyContext {
        public MRKAssemblyRegister m0,
            m1,
            m2,
            m3,
            m4,
            m5,
            m6,
            m7,
            m8,
            m9,
            m10,
            m11,
            m12,
            m13,
            m14,
            m15;
    }

    public class MRKAssembly {
        [DllImport("MRKAssembly", EntryPoint = "__mrkExecuteProxyAssembly", CallingConvention = CallingConvention.Cdecl)]
        static extern void ExecuteAsm([MarshalAs(UnmanagedType.LPStr)] string shellcode, out MRKAssemblyContext ctx);

        public static MRKAssemblyContext Execute(string shellcode) {
            MRKAssemblyContext ctx;
            ExecuteAsm(shellcode, out ctx);
            return ctx;
        }
    }
}
