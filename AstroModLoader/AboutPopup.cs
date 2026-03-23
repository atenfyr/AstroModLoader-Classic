using System;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using UAssetGUI;

namespace AstroModLoader
{
    public partial class AboutPopup : Form
    {
        public AboutPopup()
        {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void licenseButton_Click(object sender, EventArgs e)
        {
            AMLUtils.InvokeUI(() =>
            {
                string rawMarkdownText = string.Empty;
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AstroModLoader.LICENSE.md"))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            if (reader != null) rawMarkdownText = reader.ReadToEnd().Trim();
                        }
                    }
                }

                if (string.IsNullOrEmpty(rawMarkdownText))
                {
                    AMLUtils.OpenURL("https://github.com/atenfyr/AstroModLoader-Classic/blob/master/LICENSE.md");
                    return;
                }

                var formPopup = new MarkdownViewer();
                formPopup.MarkdownToDisplay = "```\n" + rawMarkdownText + "\n```";
                formPopup.Text = "License";
                formPopup.StartPosition = FormStartPosition.CenterParent;
                formPopup.ShowDialog(this);
            });
        }

        private void thirdPartyButton_Click(object sender, EventArgs e)
        {
            AMLUtils.InvokeUI(() =>
            {
                string rawMarkdownText = string.Empty;
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("AstroModLoader.NOTICE.md"))
                {
                    if (stream != null)
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            if (reader != null) rawMarkdownText = reader.ReadToEnd().Trim();
                        }
                    }
                }

                if (string.IsNullOrEmpty(rawMarkdownText))
                {
                    AMLUtils.OpenURL("https://github.com/atenfyr/AstroModLoader-Classic/blob/master/NOTICE.md");
                    return;
                }

                var formPopup = new MarkdownViewer();
                formPopup.MarkdownToDisplay = rawMarkdownText;
                formPopup.Text = "List of 3rd-party software";
                formPopup.StartPosition = FormStartPosition.CenterParent;
                formPopup.ShowDialog(this);
            });
        }
    }
}
