  //\\   OmenMon: Hardware Monitoring & Control Utility
 //  \\  Copyright © 2023 Piotr Szczepański * License: GPL3
     //  https://omenmon.github.io/

using System;
using System.Runtime.InteropServices;

namespace OmenMon.External {

    // NVIDIA Management Library (nvml.dll) Resources
    // Used for GPU power limit queries
    public class Nvml {

#region NVML Data
        public const int NVML_SUCCESS = 0;
#endregion

#region NVML Imports
        public const string DllName = "nvml.dll";

        [DllImportAttribute(DllName, CallingConvention = CallingConvention.Winapi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int nvmlInit_v2();

        [DllImportAttribute(DllName, CallingConvention = CallingConvention.Winapi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int nvmlDeviceGetHandleByIndex_v2(uint index, out IntPtr device);

        [DllImportAttribute(DllName, CallingConvention = CallingConvention.Winapi)]
        [DefaultDllImportSearchPaths(DllImportSearchPath.System32)]
        public static extern int nvmlDeviceGetEnforcedPowerLimit(IntPtr device, out uint limit);
#endregion

#region State and Logic
        private static bool initialized;
        private static bool isAvailable;
        private static IntPtr deviceHandle;
        private static readonly object initLock = new object();

        // Returns the currently enforced GPU power limit in milliwatts,
        // or null when NVML or a discrete GPU is unavailable.
        public static uint? GetEnforcedPowerLimitMilliwatts() {
            try {
                if (!initialized) {
                    lock (initLock) {
                        if (!initialized) {
                            try {
                                if (nvmlInit_v2() == NVML_SUCCESS &&
                                    nvmlDeviceGetHandleByIndex_v2(0, out deviceHandle) == NVML_SUCCESS &&
                                    deviceHandle != IntPtr.Zero) {
                                    isAvailable = true;
                                }
                            }
                            catch {
                                isAvailable = false;
                            }
                            initialized = true;
                        }
                    }
                }

                if (!isAvailable) {
                    return null;
                }

                if (nvmlDeviceGetEnforcedPowerLimit(deviceHandle, out uint limit) == NVML_SUCCESS) {
                    return limit;
                }

                return null;
            }
            catch {
                return null;
            }
        }
#endregion

    }

}
