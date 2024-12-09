using System;
using System.Diagnostics;
using System.Windows.Forms;

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
            AMLUtils.OpenURL("https://github.com/atenfyr/AstroModLoader/blob/master/LICENSE.md");
        }

        private void thirdPartyButton_Click(object sender, EventArgs e)
        {
            AMLUtils.OpenURL("https://github.com/atenfyr/AstroModLoader/blob/master/NOTICE.md");
        }
    }
}
