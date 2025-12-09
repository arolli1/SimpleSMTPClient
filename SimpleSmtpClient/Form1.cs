using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Net.Mail;
using System.Net;

namespace SimpleSmtpClient
{
    public partial class mainForm : Form
    {
        public mainForm()
        {
            InitializeComponent();
        }

        private void guiUseCredentials_CheckedChanged(object sender, EventArgs e)
        {
            guiUser.ReadOnly = true;
            guiPassword.ReadOnly = true;
            if (guiUseCredentials.Checked)
            {
                guiUser.ReadOnly = false;
                guiPassword.ReadOnly = false;
            }
        }

        private void guiSendMail_Click(object sender, EventArgs e)
        {
            try
            {
                SmtpClient client = new SmtpClient();
                client.Host = guiServerName.Text;
                client.Port = Convert.ToInt32(guiPort.Text);
                if (guiUseCredentials.Checked)
                {
                    client.UseDefaultCredentials = false;
                    client.Credentials = new NetworkCredential(guiUser.Text, guiPassword.Text);
                }
                if (guiUseSsl.Checked)
                {
                    // Enable TLS/SSL encryption for SMTP connection
                    client.EnableSsl = true;

                    // Set the TLS protocol version based on user selection
                    int tlsVersionIndex = cmbSSLVersion.SelectedIndex;
                    if (tlsVersionIndex == 0 || tlsVersionIndex == -1)
                    {
                        // Auto/System Default - allows the system to negotiate the best available protocol
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.SystemDefault;
                    }
                    else if (tlsVersionIndex == 1)
                    {
                        // TLS 1.0
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls;
                    }
                    else if (tlsVersionIndex == 2)
                    {
                        // TLS 1.1
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls11;
                    }
                    else if (tlsVersionIndex == 3)
                    {
                        // TLS 1.2 (recommended for security)
                        System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    }
                    else if (tlsVersionIndex == 4)
                    {
                        // TLS 1.3 - Check if supported by the runtime
                        // Note: TLS 1.3 support depends on Windows version (Windows 11/Server 2022+) 
                        // and .NET Framework updates. If not available, fall back to TLS 1.2.
                        try
                        {
                            // TLS 1.3 protocol value: 0x3000 (12288)
                            // SecurityProtocolType.Tls13 enum is not available in .NET Framework 4.8,
                            // so we use the numeric value directly
                            const int TLS13_PROTOCOL_VALUE = 0x3000;
                            System.Net.ServicePointManager.SecurityProtocol = (SecurityProtocolType)TLS13_PROTOCOL_VALUE;
                        }
                        catch (Exception)
                        {
                            // Fall back to TLS 1.2 if TLS 1.3 is not supported by the system
                            System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                        }
                    }
                }
                MailMessage message = CreateMailMessage();
                client.Send(message);
                MessageBox.Show("Email Sent.");

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private MailMessage CreateMailMessage()
        {
            MailMessage mailMessage = new MailMessage();
            mailMessage.From = new MailAddress(guiEmailFrom.Text);
            mailMessage.To.Add(guiEmailTo.Text);
            mailMessage.Body = guiEmailBody.Text;
            mailMessage.Subject = guiEmailSubject.Text;
            return mailMessage;
        }
    }
}
