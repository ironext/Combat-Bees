using System;
using System.Diagnostics;
using Unity.Build.Common;

namespace Unity.Build.Desktop
{
    public abstract class DesktopRunInstance : IRunInstance
    {
        protected bool m_RedirectOutput;
        protected bool m_BatchMode;
        protected bool m_NoGraphics;
        protected string[] m_ExtraArguments;

        ProcessOutputHandler m_Stdout;
        ProcessOutputHandler m_Stderr;

        readonly Process m_Process;

        public bool IsRunning => !m_Process.HasExited;

        public void Kill()
        {
            m_Process.Kill();
        }

        protected virtual ProcessStartInfo GetStartInfo(RunContext context)
        {
            var args = String.Empty;

            // Prepend custom arguments
            args = string.Join(" ", m_ExtraArguments);

            if (m_BatchMode)
            {
                args += "-batchmode";
                if (m_NoGraphics)
                    args += " -nographics";
            }

            return new ProcessStartInfo
            {
                CreateNoWindow = true,
                UseShellExecute = !m_RedirectOutput,
                RedirectStandardOutput = m_RedirectOutput,
                RedirectStandardError = m_RedirectOutput,
                Arguments = args,
            };
        }

        public DesktopRunInstance(RunContext context)
        {
            var settings = context.GetComponentOrDefault<RunSettings>();
            m_RedirectOutput = settings.RedirectOutput;
            m_Stdout = settings.Stdout;
            m_Stderr = settings.Stderr;
            m_BatchMode = settings.BatchMode;
            m_NoGraphics = settings.NoGraphics;
            m_ExtraArguments = settings.ExtraArguments;

            if (m_NoGraphics && !m_BatchMode)
            {
                throw new System.Exception(
                    "RunSettings.NoGraphics can only be true if RunSettings.BatchMode is true");
            }

            m_Process = new Process()
            {
                StartInfo = GetStartInfo(context),
            };
        }

        public void Dispose()
        {
            m_Process.Dispose();
        }

        protected RunResult Start(RunContext context)
        {
            if (m_RedirectOutput)
            {
                m_Process.OutputDataReceived += (sender, line) =>
                {
                    m_Stdout?.Invoke(sender, line.Data);
                };

                m_Process.ErrorDataReceived += (sender, line) =>
                {
                    m_Stderr?.Invoke(sender, line.Data);
                };
            }

            if (!m_Process.Start())
            {
                return context.Failure($"Failed to start process at '{m_Process.StartInfo.FileName}'.");
            }

            if (m_RedirectOutput)
            {
                m_Process.BeginOutputReadLine();
                m_Process.BeginErrorReadLine();
            }

            return context.Success(this);
        }
    }
}
