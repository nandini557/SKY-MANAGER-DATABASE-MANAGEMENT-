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
    public partial class AdminDashboard: Form
    {
        OracleConnection c;
        public string adminID;
        public string adminName;
        public AdminDashboard()
        {
            InitializeComponent();
            adminID = AdminLogin.currentAdminID;
            adminName = AdminLogin.currentAdminName;
            this.Text = "SkyReserve Admin Dashboard - " + adminName;
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
                return;
            }
        }

        private void AdminDashboard_Load(object sender, EventArgs e)
        {
            LoadEmergencies();
            LoadNotifications();
            LoadUsers();
            // Base appearance
            dataGridView1.BorderStyle = BorderStyle.None;
            dataGridView1.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            dataGridView1.BackgroundColor = Color.White;

            // Header styling
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(50, 130, 184); // soft blue
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 11, FontStyle.Bold);
            dataGridView1.ColumnHeadersHeight = 35;

            // Default cell styling
            dataGridView1.DefaultCellStyle.BackColor = Color.White;
            dataGridView1.DefaultCellStyle.ForeColor = Color.Black;
            dataGridView1.DefaultCellStyle.Font = new Font("Segoe UI", 10);
            dataGridView1.DefaultCellStyle.SelectionBackColor = Color.FromArgb(30, 144, 255); // DodgerBlue
            dataGridView1.DefaultCellStyle.SelectionForeColor = Color.White;

            // Alternating row color
            dataGridView1.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(245, 250, 255); // very light blue

            // Row settings
            dataGridView1.RowTemplate.Height = 30;
            dataGridView1.RowHeadersVisible = false;
            dataGridView1.GridColor = Color.LightGray;

            // Other visual settings
            dataGridView1.AllowUserToAddRows = false;
            dataGridView1.ReadOnly = true;
            dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        }
        private void LoadEmergencies()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT e.EmergencyID, e.FlightID, e.Type, e.Description, " +
                              "TO_CHAR(e.ReportedTime, 'DD-MON-YYYY HH24:MI') as ReportedTime, " +
                              "(SELECT f.SourceAirportID || '-' || f.DestinationAirportID FROM Flight f WHERE f.FlightID = e.FlightID) as Route " +
                              "FROM Emergency e ORDER BY e.ReportedTime DESC";

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds, "Emergencies");
            dataGridView1.DataSource = ds.Tables["Emergencies"];

            c.Close();
        }
        private void LoadNotifications()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT n.NotificationID, " +
                              "(CASE WHEN n.UserID IS NULL THEN 'All Users' " +
                              "      ELSE (SELECT Name || ' ' || Surname FROM App_User WHERE UserID = n.UserID) END) as Recipient, " +
                              "n.Message, TO_CHAR(n.Timestamp, 'DD-MON-YYYY HH24:MI') as SendTime, " +
                              "n.ReadStatus " +
                              "FROM Notification n " +
                              "ORDER BY n.Timestamp DESC";

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds, "Notifications");
            dataGridView3.DataSource = ds.Tables["Notifications"];

            c.Close();
        }
        private void LoadUsers()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT UserID, Name || ' ' || Surname as FullName FROM App_User ORDER BY Name";

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            // Add an "All Users" option
            DataRow allUsersRow = dt.NewRow();
            allUsersRow["UserID"] = DBNull.Value;
            allUsersRow["FullName"] = "All Users";
            dt.Rows.InsertAt(allUsersRow, 0);

            comboBox1.DisplayMember = "FullName";
            comboBox1.ValueMember = "UserID";
            comboBox1.DataSource = dt;

            c.Close();
        }
        private void tabPage3_Click(object sender, EventArgs e)
        {

        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox2.Text))
            {
                MessageBox.Show("Please enter an SQL query.");
                return;
            }

            try
            {
                connect();
                OracleCommand cmd = c.CreateCommand();
                cmd.CommandType = CommandType.Text;
                cmd.CommandText = textBox2.Text;

                if (textBox2.Text.Trim().ToUpper().StartsWith("SELECT"))
                {
                    // Query returns data
                    OracleDataAdapter da = new OracleDataAdapter(cmd);
                    DataSet ds = new DataSet();
                    da.Fill(ds, "Results");
                    dataGridView2.DataSource = ds.Tables["Results"];
                    MessageBox.Show("Query executed successfully.");
                }
                else
                {
                    // Update/Insert/Delete query
                    int rowsAffected = cmd.ExecuteNonQuery();
                    MessageBox.Show(rowsAffected + " row(s) affected.");

                    // Clear previous results
                    dataGridView2.DataSource = null;
                }

                c.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error executing query: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            LoadEmergencies();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(textBox1.Text))
            {
                MessageBox.Show("Please enter a notification message.");
                return;
            }

            connect();

            // Get next notification ID
            OracleCommand idCmd = c.CreateCommand();
            idCmd.CommandType = CommandType.Text;
            idCmd.CommandText = "SELECT MAX(NotificationID) + 1 FROM Notification";
            object result = idCmd.ExecuteScalar();
            int notificationID = result != DBNull.Value ? Convert.ToInt32(result) : 4004;

            // Create the notification
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "INSERT INTO Notification (NotificationID, UserID, AdminID, Message, Timestamp, ReadStatus) " +
                             "VALUES (:notificationID, :userID, :adminID, :message, SYSDATE, 'N')";

            cmd.Parameters.Add(new OracleParameter("notificationID", notificationID));

            // Check if targeting specific user or all users
            if (comboBox1.SelectedIndex > 0 && comboBox1.SelectedValue != DBNull.Value)
            {
                cmd.Parameters.Add(new OracleParameter("userID", comboBox1.SelectedValue));
            }
            else
            {
                cmd.Parameters.Add(new OracleParameter("userID", DBNull.Value));
            }

            cmd.Parameters.Add(new OracleParameter("adminID", adminID));
            cmd.Parameters.Add(new OracleParameter("message", textBox1.Text));

            cmd.ExecuteNonQuery();

            c.Close();

            textBox1.Clear();
            LoadNotifications();
        }
        private void LoadFlights()
        {
            connect();

            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT \r\n    f.FlightID,\r\n    sa.Name AS \"Source\",\r\n    da.Name AS \"Destination\",\r\n    TO_CHAR(f.DepartureTime, 'DD-MON-YYYY HH24:MI') AS \"Departure\",\r\n    TO_CHAR(f.ArrivalTime, 'DD-MON-YYYY HH24:MI') AS \"Arrival\"\r\nFROM Flight f\r\nJOIN Airport sa ON f.SourceAirportID = sa.AirportID\r\nJOIN Airport da ON f.DestinationAirportID = da.AirportID\r\n";

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataSet ds = new DataSet();
            da.Fill(ds, "Flights");
            dataGridView4.DataSource = ds.Tables["Flights"];

            c.Close();
        }
        private void button5_Click(object sender, EventArgs e)
        {
            LoadFlights();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            AdminLogin.currentAdminID = "";
            AdminLogin.currentAdminName = "";
            Form1 form1 = new Form1();
            form1.Show();
            this.Close();
        }
    }
}
