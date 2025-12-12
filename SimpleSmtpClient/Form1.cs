using System;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SimpleSmtpClient
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            // Initialize control states on form load
            UpdateCredentialFieldsState();
            UpdateTlsVersionDropdownState();
        }

        private void guiUseCredentials_CheckedChanged(object sender, EventArgs e)
        {
            UpdateCredentialFieldsState();
        }

        private void guiUseSsl_CheckedChanged(object sender, EventArgs e)
        {
            UpdateTlsVersionDropdownState();
        }

        private void UpdateCredentialFieldsState()
        {
            bool useCredentials = guiUseCredentials.Checked;
            guiUser.ReadOnly = !useCredentials;
            guiPassword.ReadOnly = !useCredentials;
            
            // Clear fields when disabling credentials
            if (!useCredentials)
            {
                guiUser.Text = string.Empty;
                guiPassword.Text = string.Empty;
            }
        }

        private void UpdateTlsVersionDropdownState()
        {
            cmbSSLVersion.Enabled = guiUseSsl.Checked;
            lblSSLVersion.Enabled = guiUseSsl.Checked;
        }

        private async void guiSendMail_Click(object sender, EventArgs e)
        {
            // Validate inputs before sending
            if (!ValidateInputs())
            {
                return;
            }

            try
            {
                // Disable the button to prevent multiple clicks
                guiSendMail.Enabled = false;
                guiSendMail.Text = "Sending...";

                using (var client = new SmtpClient())
                {
                    // Configure server connection
                    string host = guiServerName.Text.Trim();
                    int port = int.Parse(guiPort.Text.Trim());

                    // Determine SSL/TLS options based on user selection
                    SecureSocketOptions socketOptions = SecureSocketOptions.None;
                    if (guiUseSsl.Checked)
                    {
                        // Get TLS version selection
                        int tlsVersionIndex = cmbSSLVersion.SelectedIndex;
                        
                        // MailKit handles TLS negotiation automatically
                        // For STARTTLS (most common), use Auto which will negotiate the best available protocol
                        if (tlsVersionIndex == 0 || tlsVersionIndex == -1)
                        {
                            // Auto - MailKit will negotiate the best available TLS version
                            socketOptions = SecureSocketOptions.Auto;
                        }
                        else
                        {
                            // For explicit TLS versions, use StartTls
                            // MailKit will use the system's default TLS version
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

        private bool ValidateInputs()
        {
            // Validate server name
            if (string.IsNullOrWhiteSpace(guiServerName.Text))
            {
                ShowValidationError("Please enter the SMTP server name.", guiServerName);
                return false;
            }

            // Validate port
            if (string.IsNullOrWhiteSpace(guiPort.Text))
            {
                ShowValidationError("Please enter the SMTP port number.", guiPort);
                return false;
            }

            if (!int.TryParse(guiPort.Text.Trim(), out int port) || port < 1 || port > 65535)
            {
                ShowValidationError("Please enter a valid port number (1-65535).", guiPort);
                return false;
            }

            // Validate credentials if authentication is enabled
            if (guiUseCredentials.Checked && string.IsNullOrWhiteSpace(guiUser.Text))
            {
                ShowValidationError("Please enter a username for authentication.", guiUser);
                return false;
            }

            // Validate email addresses
            if (string.IsNullOrWhiteSpace(guiEmailFrom.Text))
            {
                ShowValidationError("Please enter a sender email address.", guiEmailFrom);
                return false;
            }

            if (!IsValidEmail(guiEmailFrom.Text.Trim()))
            {
                ShowValidationError("Please enter a valid sender email address.", guiEmailFrom);
                return false;
            }

            if (string.IsNullOrWhiteSpace(guiEmailTo.Text))
            {
                ShowValidationError("Please enter a recipient email address.", guiEmailTo);
                return false;
            }

            if (!IsValidEmail(guiEmailTo.Text.Trim()))
            {
                ShowValidationError("Please enter a valid recipient email address.", guiEmailTo);
                return false;
            }

            return true;
        }

        private static bool IsValidEmail(string email)
        {
            // Simple email validation pattern
            string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, pattern, RegexOptions.IgnoreCase);
        }

        private void ShowValidationError(string message, Control control)
        {
            MessageBox.Show(message, "Validation Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            control.Focus();
        }

        private MimeMessage CreateMailMessage()
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(string.Empty, guiEmailFrom.Text.Trim()));
            message.To.Add(new MailboxAddress(string.Empty, guiEmailTo.Text.Trim()));
            message.Subject = guiEmailSubject.Text;
            message.Body = new TextPart("plain")
            {
                Text = guiEmailBody.Text
            };
            return message;
        }
    }
}

