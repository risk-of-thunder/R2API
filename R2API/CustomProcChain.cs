using RoR2;
using System;
using System.Collections.Generic;

namespace R2API.Utils {

    class ModProc {
        public Dictionary<string, bool> ProcList = new Dictionary<string, bool>();

        public ModProc() {
            foreach (ProcType proc in (ProcType[])Enum.GetValues(typeof(ProcType))) {
                ProcList.Add(proc.ToString(), false);
            }
            foreach (string proc in ModProcManager.CustomProcList) {
                ProcList.Add(proc, false);
            }
        }

        public void ChangeProcState(string Proc, bool value) {
            if (ProcList.ContainsKey(Proc))
                ProcList[Proc] = value;
        }
    }

    static class ProcChainMaskExtention {
        public static void SetProcValue(this ProcChainMask procMask, string ProcName, bool value) {
            ModProcManager.SetProcValue(procMask, ProcName, value);
        }
        public static void SetProcValue(this ProcChainMask procMask, ProcType Proc, bool value) {
            ModProcManager.SetProcValue(procMask, Proc.ToString(), value);
        }
        public static bool GetProcValue(this ProcChainMask procMask, string ProcName) {
            return ModProcManager.GetProcValue(procMask, ProcName);
        }
        public static bool GetProcValue(this ProcChainMask procMask, ProcType Proc) {
            return ModProcManager.GetProcValue(procMask, Proc.ToString());
        }
        public static void LinkToManager(this ProcChainMask procMask) {
            ModProcManager.AddLink(procMask, new ModProc());
        }
        public static void UnlinkToManager(this ProcChainMask procMask) {
            ModProcManager.RemoveLink(procMask);
        }
    }

    class ModProcManager {

        public static List<string> CustomProcList = new List<string>();

        /// <summary>
        /// Used to decalre new Proc Type in addition to existing one
        /// </summary>
        /// <param name="ProcName"></param>
        public static void DeclareNewProc(string ProcName) {
            if (!CustomProcList.Contains(ProcName)) {
                CustomProcList.Add(ProcName);
            }
            else {
                throw new Exception("Mod Proc Manager : Trying to declare an existing Proc : " + ProcName);
            }
        }

        public static Dictionary<ProcChainMask, ModProc> ProcChainLinker = new Dictionary<ProcChainMask, ModProc>();

        public static void SetProcValue(ProcChainMask chain, string ProcName, bool value) {
            if (ProcChainLinker.ContainsKey(chain)) {
                ProcChainLinker[chain].ChangeProcState(ProcName, value);
            }
        }
        public static bool GetProcValue(ProcChainMask chain, string ProcName) {
            if (ProcChainLinker.ContainsKey(chain)) {
                if (ProcChainLinker[chain].ProcList.ContainsKey(ProcName))
                    return ProcChainLinker[chain].ProcList[ProcName];
            }
            return false;
        }

        public static void AddLink(ProcChainMask chain, ModProc modproc) {
            if (!ProcChainLinker.ContainsKey(chain)) {
                ProcChainLinker.Add(chain, modproc);
            }
        }

        public static void RemoveLink(ProcChainMask chain) {
            if (ProcChainLinker.ContainsKey(chain)) {
                ProcChainLinker.Remove(chain);
            }
        }

        public static void RemoveAllLink() {
            ProcChainLinker = new Dictionary<ProcChainMask, ModProc>();
        }


    }


}
