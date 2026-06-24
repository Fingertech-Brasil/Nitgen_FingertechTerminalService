using NITGEN.SDK.NBioBSP;
using System;
using System.Diagnostics;
using System.Threading;

namespace TsService
{
    class utilsNitgen
    {
        private readonly NBioAPI m_NBioAPI;
        private readonly object _deviceLock = new object();
        private readonly EventLog m_eventLog;

        public utilsNitgen()
        {
            m_NBioAPI = new NBioAPI();
            m_eventLog = new EventLog { Source = "service1" };
        }

        public string Capturar()
        {
            lock (_deviceLock)
            {
                try
                {
                    NBioAPI.Type.HFIR hCapturedFIR = new NBioAPI.Type.HFIR();
                    NBioAPI.Type.FIR_TEXTENCODE texto = new NBioAPI.Type.FIR_TEXTENCODE();

                    m_NBioAPI.OpenDevice(255);
                    m_NBioAPI.Capture(out hCapturedFIR);
                    m_NBioAPI.GetTextFIRFromHandle(hCapturedFIR, out texto, true);

                    return texto.TextFIR;
                }
                catch (Exception e)
                {
                    m_eventLog.WriteEntry("Erro na captura: " + e.Message, EventLogEntryType.Error);
                    return null;
                }
                finally
                {
                    try { m_NBioAPI.CloseDevice(255); } catch { }
                }
            }
        }

        public string Enroll()
        {
            lock (_deviceLock)
            {
                try
                {
                    NBioAPI.Type.HFIR hCapturedFIR = new NBioAPI.Type.HFIR();
                    NBioAPI.Type.FIR_TEXTENCODE texto = new NBioAPI.Type.FIR_TEXTENCODE();

                    NBioAPI.Type.FIR_PAYLOAD payload = new NBioAPI.Type.FIR_PAYLOAD();
                    m_NBioAPI.OpenDevice(255);
                    m_NBioAPI.Enroll(out hCapturedFIR, payload);
                    m_NBioAPI.GetTextFIRFromHandle(hCapturedFIR, out texto, true);

                    return texto.TextFIR;
                }
                catch (Exception e)
                {
                    m_eventLog.WriteEntry("Erro no enroll: " + e.Message, EventLogEntryType.Error);
                    return null;
                }
                finally
                {
                    try { m_NBioAPI.CloseDevice(255); } catch { }
                }
            }
        }
    }
}
