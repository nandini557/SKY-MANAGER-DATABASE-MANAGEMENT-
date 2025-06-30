using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SkyReserve
{
    public partial class ThankYouForm1: Form
    {
        public ThankYouForm1()
        {
            InitializeComponent();
        }

        private void ThankYouForm1_Load(object sender, EventArgs e)
        {
            label1.Text = "🎉 Thank You for Booking with SkyReserve! ✈️";
            label1.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            label1.AutoSize = true;
            label1.TextAlign = ContentAlignment.MiddleCenter;
            label1.ForeColor = Color.DarkBlue;

            // Center label horizontally
            label1.Left = (this.ClientSize.Width - label1.Width) / 2;
            label1.Top = this.ClientSize.Height / 3;

            // Style the button
            button1.Text = "Return to Dashboard";
            button1.Font = new Font("Segoe UI", 10, FontStyle.Regular);
            button1.BackColor = Color.LightSteelBlue;
            button1.FlatStyle = FlatStyle.Flat;
            button1.FlatAppearance.BorderSize = 0;

            // Position the button below the label
            button1.Width = 160;
            button1.Height = 40;
            button1.Left = (this.ClientSize.Width - button1.Width) / 2;
            button1.Top = label1.Bottom + 30;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.WhiteSmoke;

        }

        private void button1_Click(object sender, EventArgs e)
        {
            UserDashboard dashboard = new UserDashboard();
            dashboard.Show();
            this.Close();
        }
    }
}
