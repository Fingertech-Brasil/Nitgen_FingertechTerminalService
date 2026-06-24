using System;
using System.Diagnostics;
using System.IO;
using NITGEN.SDK.NBioBSP;

namespace FingertechEnroll
{
    class Program
    {
        static void Main(string[] args)
        {
            string resultFile = @"C:\Windows\Temp\fingertech_enroll_result.txt";
            EventLog elog = new EventLog { Source = "service1" };

            try
            {
                elog.WriteEntry("FingertechEnroll: iniciando...", EventLogEntryType.Information);

                NBioAPI api = new NBioAPI();
                NBioAPI.Type.HFIR hFir = new NBioAPI.Type.HFIR();
                NBioAPI.Type.FIR_TEXTENCODE texto = new NBioAPI.Type.FIR_TEXTENCODE();
                NBioAPI.Type.FIR_PAYLOAD payload = new NBioAPI.Type.FIR_PAYLOAD();

                elog.WriteEntry("FingertechEnroll: OpenDevice...", EventLogEntryType.Information);
                api.OpenDevice(255);

                elog.WriteEntry("FingertechEnroll: Enroll (UI deve abrir)...", EventLogEntryType.Information);
                api.Enroll(out hFir, payload);

                elog.WriteEntry("FingertechEnroll: GetTextFIR...", EventLogEntryType.Information);
                api.GetTextFIRFromHandle(hFir, out texto, true);

                api.CloseDevice(255);

                elog.WriteEntry("FingertechEnroll: escrevendo resultado (" + texto.TextFIR.Length + " bytes)...", EventLogEntryType.Information);
                File.WriteAllText(resultFile, texto.TextFIR);

                elog.WriteEntry("FingertechEnroll: concluido com sucesso", EventLogEntryType.Information);
                Environment.Exit(0);
            }
            catch (Exception e)
            {
                elog.WriteEntry("FingertechEnroll ERRO: " + e.Message, EventLogEntryType.Error);
                try { File.WriteAllText(resultFile, "ERRO:" + e.Message); } catch { }
                Environment.Exit(1);
            }
        }
    }
}
