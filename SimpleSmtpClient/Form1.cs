using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

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

        private async void guiSendMail_Click(object sender, EventArgs e)
        {
            try
            {
                // Disable the button to prevent multiple clicks
                guiSendMail.Enabled = false;
                guiSendMail.Text = "Sending...";

                using (var client = new SmtpClient())
                {
                    // Configure server connection
                    string host = guiServerName.Text;
                    int port = Convert.ToInt32(guiPort.Text);

                    // Determine SSL/TLS options based on user selection
                    SecureSocketOptions socketOptions = SecureSocketOptions.None;
                    if (guiUseSsl.Checked)
                    {
                        // Get TLS version selection
                        int tlsVersionIndex = cmbSSLVersion.SelectedIndex;
                        
                        // MailKit handles TLS negotiation automatically, but we can specify preferences
                        // For STARTTLS (most common), use Auto which will negotiate the best available protocol
                        if (tlsVersionIndex == 0 || tlsVersionIndex == -1)
                        {
                            // Auto - MailKit will negotiate the best available TLS version
                            socketOptions = SecureSocketOptions.Auto;
                        }
                        else
                        {
                            // For explicit TLS versions, use StartTls
                            // MailKit will use the system's default TLS version, which in .NET 8
                            // defaults to TLS 1.2 or higher (including TLS 1.3 if available)
                            socketOptions = SecureSocketOptions.StartTls;
                        }
                    }

                    // Connect to the server
                    await client.ConnectAsync(host, port, socketOptions);

                    // Authenticate if credentials are provided
                    if (guiUseCredentials.Checked && !string.IsNullOrWhiteSpace(guiUser.Text))
                    {
                        await client.AuthenticateAsync(guiUser.Text, guiPassword.Text);
                    }

                    // Create and send the message
                    var message = CreateMailMessage();
                    await client.SendAsync(message);

                    // Disconnect
                    await client.DisconnectAsync(true);

                    MessageBox.Show("Email Sent.", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error sending email: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                // Re-enable the button
                guiSendMail.Enabled = true;
                guiSendMail.Text = "Send Mail";
            }
        }

        private MimeMessage CreateMailMessage()
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(string.Empty, guiEmailFrom.Text));
            message.To.Add(new MailboxAddress(string.Empty, guiEmailTo.Text));
            message.Subject = guiEmailSubject.Text;
            message.Body = new TextPart("plain")
            {
                Text = guiEmailBody.Text
            };
            return message;
        }
    }
}

