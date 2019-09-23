using System;
using System.Drawing;
using System.Windows.Forms;

namespace Diffology.Addin
{
    public partial class UnhandledErrorDialog : Form
    {
        internal UnhandledErrorDialog()
        {
            InitializeComponent();
            errorPictureBox.Image = SystemIcons.Error.ToBitmap();  // 32x32
        }

        internal UnhandledErrorDialog(Exception e) : this()
        {
            exceptionTextBox.Text = e.ToString();
        }

        private void ShowMoreInformationButton_Click(object sender, EventArgs e)
        {
            Height = 690;
            showMoreInformationButton.Hide();
        }
    }
}
