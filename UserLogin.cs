using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Oracle.ManagedDataAccess.Client;

namespace SkyReserve
{
    public partial class UserLogin: Form
    {
        OracleConnection c;
        public static string currentUserID = "";
        public static string currentUserName = "";
        public UserLogin()
        {

            InitializeComponent();
        }

        public void connect()
        {
            c = new OracleConnection("User Id=system;Password=kushagra;Data Source=localhost:1521/xe;");

            try
            {
                c.Open();
                // Connection successful
            }
            catch (Exception ex)
            {
                MessageBox.Show("Database connection failed: " + ex.Message);
            }
        }

        private void UserLogin_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            string username = textBox1.Text;
            string password = textBox2.Text;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Please enter both username and password.");
                return;
            }

            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT UserID, Name FROM app_user WHERE Name = :username AND Password = :password";
            cmd.Parameters.Add(new OracleParameter("username", username));
            cmd.Parameters.Add(new OracleParameter("password", password));

            OracleDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                currentUserID = reader["UserID"].ToString();
                currentUserName = reader["Name"].ToString();

                

                UserDashboard dashboard = new UserDashboard();
                dashboard.Show();
                this.Hide();
            }
            else
            {
                MessageBox.Show("Invalid username or password. Please try again.");
            }

            reader.Close();
            c.Close();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Form1 form1 = new Form1();
            form1.Show();
            this.Hide();
        }
    }
}
