using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Npgsql;
using System.IO;
using System.Drawing.Imaging;

namespace StudentApplication
{
    public partial class Form4 : Form
    {
        string sql; byte[] key; int epican = 1; int balance; String unim; String name; string fakp;
        NpgsqlConnection conndb; string uidcard;
        NpgsqlCommand command;
        string connstring;
        private void tampil()
        {
            conndb.Open();
            sql = "select * from student";
            command = new NpgsqlCommand(sql, conndb);
            DataSet ds = new DataSet();
            NpgsqlDataAdapter da = new NpgsqlDataAdapter(command);
            da.Fill(ds, "student");
        }
        private void konek()
        {
            connstring = @"Server=localhost;Port=5433;Userid=postgres;Password=meimei123;Database=test_db";
            conndb = new NpgsqlConnection(connstring);
        }
        public Form4(String nama, String nim, String fp, String uid)
        {
            InitializeComponent();
            string foto; unim = ""+nim;
            textBox1.Text = " " + nama;
            textBox2.Text = " " + nim;
            textBox3.Text = " " + fp;
            fakp = fp;
            name = nama; uidcard = uid;
            konek(); tampil(); string source;
            NpgsqlCommand query = new NpgsqlCommand();
            query.Connection = conndb;
            query.CommandText = "select * from student where nim= '" + nim + "'";
            query.CommandType = CommandType.Text;
            NpgsqlDataReader eksekusi = query.ExecuteReader();
            NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(query.CommandText, conndb);
            if (eksekusi.HasRows == false)
            {
                MessageBox.Show("Sorry, your card is not registered. Try again or do registration first");
                conndb.Close();
            }
            else
            {
                while (eksekusi.Read())
                {
                    foto = eksekusi["foto"].ToString();
                    balance = int.Parse(eksekusi["balance"].ToString());
                    label2.Text = "Your Current Balance: Rp. " + balance;
                    source = "gambar_" + nim;
                    key = HexaStringToArrayOfByte(foto);
                    try
                    {
                        File.WriteAllBytes(source, key);
                    }
                    catch { }
                    pictureBox1.Image = new Bitmap(source);
                    pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;

                }
                conndb.Close();
            }

        }
        public static string TrimInput(string str)
        {
            string tmp = str.Replace(" ", "");
            tmp = tmp.Replace("-", "");
            return tmp;
        }
        public static byte[] HexaStringToArrayOfByte(string hexString)
        {
            byte[] buffer = null;
            string trimHexString = (TrimInput(hexString));
            buffer = new byte[trimHexString.Length / 2];
            for (int i = 0; i < trimHexString.Length; i += 2)
            {
                buffer[i / 2] = Convert.ToByte(trimHexString.Substring(i, 2), 16);
            }
            return buffer;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 frm1 = new Form1();
            frm1.Show();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                balance = balance + int.Parse(saldo.Text.ToString());
            }
            catch
            {
                MessageBox.Show("Top Up Amount must be an integer");
                return;
            }
            
            konek();
            tampil();
            try
            {
                sql = "update student SET balance ='" + balance + "' WHERE nim='" + unim + "'";
                command = new NpgsqlCommand(sql, conndb);
                command.ExecuteNonQuery();
                label2.Text = "Your Current Balance: Rp. " + balance;
                MessageBox.Show("Top-up success. Your balance now is Rp. "+balance);
            }
            catch
            {
                MessageBox.Show("Sorry, Top-up failed. Try again later");
            }
        }

        private void pictureBox2_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form5 frm5 = new Form5(unim, name, uidcard, fakp, balance);
            frm5.Show();
        }

       
    }
}
