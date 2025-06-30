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
    public partial class BookingConfirmation: Form
    {
        OracleConnection c;
        public int BookingID { get; set; }
        public BookingConfirmation()
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

        private void BookingConfirmation_Load(object sender, EventArgs e)
        {
            LoadConfirmationDetails();
        }

        private void LoadConfirmationDetails()
        {
            connect();
            OracleCommand cmd = c.CreateCommand();
            cmd.CommandType = CommandType.Text;
            cmd.CommandText = "SELECT b.BookingID, " +
                              "(SELECT Name FROM app_user WHERE UserID = b.UserID) as PassengerName, " +
                              "f.FlightID, " +
                              "(SELECT Code FROM Airport WHERE AirportID = f.SourceAirportID) || ' - ' || " +
                              "(SELECT Code FROM Airport WHERE AirportID = f.DestinationAirportID) as Route, " +
                              "TO_CHAR(f.DepartureTime, 'DD-MON-YYYY') as TravelDate, " +
                              "t.TicketNumber " +
                              "FROM Booking b " +
                              "JOIN Flight f ON b.FlightID = f.FlightID " +
                              "JOIN Ticket t ON b.BookingID = t.BookingID " +
                              "WHERE b.BookingID = :bookingID";
            cmd.Parameters.Add(new OracleParameter("bookingID", BookingID));

            OracleDataReader reader = cmd.ExecuteReader();
            if (reader.Read())
            {
                label1.Text = reader["BookingID"].ToString();
                label2.Text = reader["FlightID"].ToString() + " (" + reader["Route"].ToString() + ")";
                label3.Text = reader["PassengerName"].ToString();
                label4.Text = reader["TravelDate"].ToString();
                label5.Text = BookingDetails.seatClass;
                label6.Text = reader["TicketNumber"].ToString();
            }


            reader.Close();
            c.Close();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ThankYouForm1 thankYou = new ThankYouForm1();
            thankYou.Show();
            this.Close();
        }
    }
}
