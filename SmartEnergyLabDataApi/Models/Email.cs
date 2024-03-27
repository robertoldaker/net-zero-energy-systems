using System.Net;
using System.Net.Mail;
using CommonInterfaces.Models;
using HaloSoft.EventLogger;

namespace SmartEnergyLabDataApi.Models;

public class Email
{
    public enum SystemEmailAddress { NoReply, Admin, Support, Sales };

    private static List<string> _allowedDevelopmentEmails = new List<string>()
    {
        "roldaker@gmail.com",
        "robertoldaker@clara.co.uk",
        "roberto@hypergraphics.co.uk",
        "rob@myfimusic.co.uk"
    };

    SmtpClient _smtp;
    MailAddress _from;
    MailAddress _sender;
    string _rootDomain = "net-zero-energy-systems.org";
    string _organisation = "Net Zero Energy Systems";

    // server params
    string _server = "127.0.0.1";
    int _port = 25;
    string _username = "";
    string _password = "";
    bool _enableSSL = false;



    public Email( SystemEmailAddress fromAddress)
    {
        _smtp = setSmtp(_server, _port, _username, _password, _enableSSL);
        //
        _from = GetMailAddress(fromAddress);
    }


    private SmtpClient setSmtp(string server, int port, string username, string password, bool enableSSL)
    {
        var smtp = new SmtpClient(server, port);
        smtp.EnableSsl = enableSSL;
        smtp.Credentials = new NetworkCredential(username, password);
        smtp.Timeout = 20000; // 20s
        return smtp;
    }

    public bool Send(MailAddress to, string subject, string message, IList<Attachment> attachments=null)
    {
        //
        if (to == null) {
            return false;
        }
        //
        if ( !isValidEmailAddress(to.Address) ) {
            Logger.Instance.LogInfoEvent($"Ignoring email to [{to.Address}] as its not on the development white list");
            return false;
        }
        //
        using (MailMessage m = new MailMessage())
        {
            m.To.Add(to);
            m.Subject = subject;
            m.From = _from;
            if (_sender != null) {
                m.Sender = _sender;
            }
            m.Body = message;
            m.IsBodyHtml = true;
            //
            if (attachments != null)
            {
                foreach (var a in attachments)
                {
                    m.Attachments.Add(a);
                }
            }
            //
            _smtp.Send(m);
            return true;
        }
    }

    private bool isValidEmailAddress(string to)
    {
        if ( AppEnvironment.Instance.Context == Context.Production) {
            return true;
        } else {
            // Ensure we do not send email addresses to clients when developing by only
            // allowing any emails in the list or an angelbooks address
            return (_allowedDevelopmentEmails.Contains(to)) || to.EndsWith($"@{_rootDomain}");
        }
    }

    public bool Send(string to, string subject, string message, IList<Attachment> attachments=null)
    {
        return Send(new MailAddress(to), subject, message, attachments);
    }

    public MailAddress From { 
        get {
            return _from;
        }
        set {
            _from = value;
        }
    }

    public MailAddress GetMailAddress( SystemEmailAddress emailAddress)
    {
        string rootDomain = $"@{_rootDomain}";
        if (emailAddress == SystemEmailAddress.Sales) {
            return new MailAddress("sales" + rootDomain, $"{_organisation} - Sales");
        } else if (emailAddress == SystemEmailAddress.Support) {
            return new MailAddress("support" + rootDomain, $"{_organisation} - Support");
        } else if (emailAddress == SystemEmailAddress.Admin) {
            return new MailAddress("admin" + rootDomain, $"{_organisation} - Site Admin");
        } else if (emailAddress == SystemEmailAddress.NoReply) {
            return new MailAddress("noreply" + rootDomain, $"{_organisation} - noreply");
        } else {
            throw new Exception("Unexpected SystemEmailAddress found: " + emailAddress.ToString());
        }
    }
}