using Oracle.ManagedDataAccess.Client;
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
    public partial class BookingDetails: Form
    {
        OracleConnection c;
        public string flightID;
        public string sourceAirport;
        public string destAirport;
        public string departureTime;
        public string arrivalTime;
        public static string seatClass;
        public static string mealOption;
        public static bool extraLuggage;
        public static decimal totalAmount;
        public BookingDetails()
        {
            InitializeComponent();
            flightID = FlightResults.selectedFlightID;
        }

        private void label3_Click(object sender, EventArgs e)
        {

        }

        private void BookingDetails_Load(object sender, EventArgs e)
        {
            LoadFlightDetails();
            LoadPassengerDetails();
            LoadMealOptions();
            comboBox1.Items.Clear();
            comboBox1.Items.Add("Economy");
            comboBox1.Items.Add("Business");
            comboBox1.Items.Add("First Class");

            // Set defaults

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
        private void LoadFlightDetails()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT f.FlightID, " +
                             "(SELECT Name FROM Airport WHERE AirportID = f.SourceAirportID) as DepartureAirport, " +
                             "(SELECT Name FROM Airport WHERE AirportID = f.DestinationAirportID) as ArrivalAirport, " +
                             "TO_CHAR(f.DepartureTime, 'DD-MON-YYYY HH24:MI') as DepartureTime, " +
                             "TO_CHAR(f.ArrivalTime, 'DD-MON-YYYY HH24:MI') as ArrivalTime " +
                             "FROM Flight f WHERE f.FlightID = :flightID";
            cmd.Parameters.Add(new OracleParameter("flightID", flightID));

            OracleDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                sourceAirport = reader["DepartureAirport"].ToString();
                destAirport = reader["ArrivalAirport"].ToString();
                departureTime = reader["DepartureTime"].ToString();
                arrivalTime = reader["ArrivalTime"].ToString();

                label5.Text = flightID;
                label6.Text = sourceAirport;
                label7.Text = destAirport;
                label8.Text = departureTime;
                label9.Text = arrivalTime;
            }

            reader.Close();
            c.Close();
        }
        private void LoadPassengerDetails()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT Name, Email FROM app_user WHERE UserID = :userID";
            cmd.Parameters.Add(new OracleParameter("userID", UserLogin.currentUserID));

            OracleDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                label10.Text = reader["Name"].ToString();
                label11.Text = reader["Email"].ToString();
            }

            reader.Close();
            c.Close();
        }
        private void LoadMealOptions()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT Name, Price FROM Meal WHERE FlightID = :flightID";
            cmd.Parameters.Add(new OracleParameter("flightID", flightID));

            OracleDataAdapter da = new OracleDataAdapter(cmd);
            DataTable dt = new DataTable();
            da.Fill(dt);

            comboBox2.DisplayMember = "Name";
            comboBox2.ValueMember = "Price";
            comboBox2.DataSource = dt;

            c.Close();
        }
        private void CalculateTotal()
        {
            // Base price depending on route - simplified for demo
            decimal basePrice = 5000.00m;

            // Multiplier based on seat class
            decimal classMultiplier = 1.0m;
            switch (comboBox1.SelectedItem.ToString())
            {
                case "Economy":
                    classMultiplier = 1.0m;
                    break;
                case "Business":
                    classMultiplier = 2.5m;
                    break;
                case "First Class":
                    classMultiplier = 4.0m;
                    break;
            }

            // Add meal price
            decimal mealPrice = 0;
            if (comboBox2.SelectedValue != null)
            {
                mealPrice = Convert.ToDecimal(comboBox2.SelectedValue);
            }

            // Extra luggage
            decimal luggagePrice = checkBox1.Checked ? 1500.00m : 0.00m;

            // Calculate total
            totalAmount = (basePrice * classMultiplier) + mealPrice + luggagePrice;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (comboBox1.SelectedIndex == -1 || comboBox2.SelectedIndex == -1)
            {
                MessageBox.Show("Please select seat class and meal option.");
                return;
            }
            // Save selections
            seatClass = comboBox1.SelectedItem.ToString();
            mealOption = comboBox2.Text;
            extraLuggage = checkBox1.Checked;

            CalculateTotal();

            // Proceed to payment
            PaymentForm paymentForm = new PaymentForm();
            paymentForm.WindowState = FormWindowState.Maximized;
            paymentForm.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Close();
            FlightResults flightResults = new FlightResults();
            flightResults.WindowState = FormWindowState.Maximized;
            flightResults.Show();
        }
    }
}
