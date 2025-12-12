using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace SimpleSmtpClient;

public partial class MainForm : Form
{
    // Compiled regex for better performance (generated at compile time in .NET 7+)
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.IgnoreCase)]
    private static partial Regex EmailValidationRegex();

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
        var useCredentials = guiUseCredentials.Checked;
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

            using var client = new SmtpClient();

            // Configure server connection
            var host = guiServerName.Text.Trim();
            var port = int.Parse(guiPort.Text.Trim());

            // Determine SSL/TLS options based on user selection
            var socketOptions = GetSecureSocketOptions();

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

    private SecureSocketOptions GetSecureSocketOptions()
    {
        if (!guiUseSsl.Checked)
        {
            return SecureSocketOptions.None;
        }

        // MailKit handles TLS negotiation automatically
        // For STARTTLS (most common), use Auto which will negotiate the best available protocol
        return cmbSSLVersion.SelectedIndex is 0 or -1
            ? SecureSocketOptions.Auto
            : SecureSocketOptions.StartTls;
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

        if (!int.TryParse(guiPort.Text.Trim(), out var port) || port is < 1 or > 65535)
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

    private static bool IsValidEmail(string email) => EmailValidationRegex().IsMatch(email);

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

