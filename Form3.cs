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
using System.Runtime.InteropServices;

namespace StudentApplication
{
    public partial class Form3 : Form
    {
        public Form3()
        {
            InitializeComponent();
            ProgressBar pBar = new ProgressBar();
            pBar.Location = new System.Drawing.Point(20, 20);
            pBar.Name = "progressBar1";
            pBar.Width = 400;
            pBar.Height = 30;
            Controls.Add(pBar);
            pBar.Style = ProgressBarStyle.Marquee;
            pBar.MarqueeAnimationSpeed = 10;
            pBar.Dock = DockStyle.Bottom;
            pBar.Minimum = 0;
            pBar.Maximum = 100;
            pBar.Value = 99;
            konek();
            tampil();
            timer1.Enabled = true;
        }
        string sql;
        NpgsqlConnection conndb;
        NpgsqlCommand command;
        string connstring;

        public int retCode, hContext, hCard, Protocol;
        public bool connActive = false; string uid;
        public bool autoDet;
        public byte[] SendBuff = new byte[263];
        public byte[] RecvBuff = new byte[263];
        string nama = "", nim="", fp="";

        public int SendLen, RecvLen, nBytesRet, reqType, Aprotocol, dwProtocol, cbPciLength;
        public ModWinsCard.SCARD_READERSTATE RdrState;
        public ModWinsCard.SCARD_IO_REQUEST pioSendRequest;

        private void displayOut(int errType, int retVal, string PrintText)
        {
            switch (errType)
            {
                case 0:
                    break;
                case 1:
                    PrintText = ModWinsCard.GetScardErrMsg(retVal);
                    break;
                case 2:
                    PrintText = "<" + PrintText;
                    break;
                case 3:
                    PrintText = "-->" + PrintText;
                    break;
            }
            pesan.Items.Add(PrintText);
        }
        private void ClearBuffers()
        {
            long indx;

            for (indx = 0; indx <= 262; indx++)
            {
                RecvBuff[indx] = 0;
                SendBuff[indx] = 0;
            }

        }
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
        private int SendAPDUandDisplay(int reqType)
        {
            int indx;
            string tmpStr;

            pioSendRequest.dwProtocol = Aprotocol;
            pioSendRequest.cbPciLength = 8;

            // Display Apdu In
            tmpStr = "";
            for (indx = 0; indx <= SendLen - 1; indx++)
            {

                tmpStr = tmpStr + " " + string.Format("{0:X2}", SendBuff[indx]);

            }

            displayOut(2, 0, tmpStr);
            retCode = ModWinsCard.SCardTransmit(hCard, ref pioSendRequest, ref SendBuff[0], SendLen, ref pioSendRequest, ref RecvBuff[0], ref RecvLen);

            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                displayOut(1, retCode, "");
                return retCode;
            }

            else
            {
                tmpStr = "";
                switch (reqType)
                {

                    case 0:
                        for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                        {

                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }


                        if ((tmpStr).Trim() != "90 00")
                        {
                            displayOut(4, 0, "Return bytes are not acceptable.");
                        }
                        break;

                    case 1:

                        for (indx = (RecvLen - 2); indx <= (RecvLen - 1); indx++)
                        {
                            tmpStr = tmpStr + string.Format("{0:X2}", RecvBuff[indx]);
                        }


                        if (tmpStr.Trim() != "90 00")
                        {

                            tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);
                        }

                        else
                        {

                            tmpStr = "ATR : ";
                            for (indx = 0; indx <= (RecvLen - 3); indx++)
                            {

                                tmpStr = tmpStr + " " + string.Format("{0:X2}", RecvBuff[indx]);

                            }

                        }

                        break;

                    case 2:

                        for (indx = 0; indx <= (RecvLen - 1); indx++)
                        {

                            tmpStr = tmpStr + "" + string.Format("{0:X2}", RecvBuff[indx]);

                        }
                        uid = tmpStr;
                        break;

                }

                displayOut(3, 0, tmpStr.Trim());

            }
            return retCode;
        }
        private void auten(int a)
        {
            ClearBuffers();
            SendBuff[0] = 0xFF;                      // CLA
            SendBuff[1] = 0x82;                      // INS
            SendBuff[2] = 0x20;                 // P1 : Non volatile memory
            SendBuff[3] = byte.Parse("1", System.Globalization.NumberStyles.HexNumber);    // P2 : Memory location
            SendBuff[4] = 0x06;                     // P3
            for (int k = 5; k < 11; k++)
            {
                SendBuff[k] = byte.Parse("ff", System.Globalization.NumberStyles.HexNumber); // Key 1-6 value
            }

            SendLen = 11;
            RecvLen = 2;
            retCode = SendAPDUandDisplay(0);
            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                return;
            }

            ClearBuffers();
            SendBuff[0] = 0xFF;                                       // CLA
            SendBuff[2] = 0x00;                                       // P1: same for all source types
            SendBuff[1] = 0x86;                                       // INS: for stored key input
            SendBuff[3] = 0x00;                                       // P2: for stored key input
            SendBuff[4] = 0x05;                                       // P3: for stored key input
            SendBuff[5] = 0x01;                                       // Byte 1: version number
            SendBuff[6] = 0x00;                                       // Byte 2
            SendBuff[7] = (byte)a;  //block number : 0-63             // Byte 3: sectore no. for stored key input
            SendBuff[8] = 0x60;                                       // Byte 4 : Key B for stored key input
            SendBuff[9] = (byte)1;  //index key reader            // Byte 5 : Session key for non-volatile memory
            SendLen = 0x0A;
            RecvLen = 0x02;
            retCode = SendAPDUandDisplay(0);
            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                return;
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            this.Hide();
            Form1 frm1 = new Form1();
            frm1.Show();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            init();
        }
        public int epican = 0;
        private void init()
        {
            timer1.Stop();
            int indx;
            int pcchReaders = 0;
            string rName = "";

            // 1. Establish Context
            retCode = ModWinsCard.SCardEstablishContext(ModWinsCard.SCARD_SCOPE_USER, 0, 0, ref hContext);

            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                displayOut(1, retCode, "");
                pesan.Items.Add("GAGAL ESTABLISH");
                return;

            }
            else
            {
                pesan.Items.Add("Establish Context (0)");
            }

            // 2. List PC/SC card readers installed in the system
            retCode = ModWinsCard.SCardListReaders(this.hContext, null, null, ref pcchReaders);
            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                pesan.Items.Add("GAGAL LIST CARD READER");
                displayOut(1, retCode, "");
                return;
            }

            byte[] ReadersList = new byte[pcchReaders];

            // Fill reader list
            retCode = ModWinsCard.SCardListReaders(this.hContext, null, ReadersList, ref pcchReaders);

            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                pesan.Items.Add("SCardListReaders Error: " + ModWinsCard.GetScardErrMsg(retCode));
                pesan.SelectedIndex = pesan.Items.Count - 1;
                return;
            }
            else
            {
                displayOut(0, 0, " ");
            }

            rName = "";
            indx = 0;

            // Convert reader buffer to string
            while (ReadersList[indx] != 0)
            {
                while (ReadersList[indx] != 0)
                {
                    rName = rName + (char)ReadersList[indx];
                    indx = indx + 1;
                }
                // Add reader name to list
                reader.Items.Add(rName);
                rName = "";
                indx = indx + 1;
            }

            if (reader.Items.Count > 0)
            {
                reader.SelectedIndex = 1;
            }

            indx = 1;
            conn();
            reader.Items.Clear();
            return;
        }

        private void conn()
        {
            if (connActive)
            {
                retCode = ModWinsCard.SCardDisconnect(hCard, ModWinsCard.SCARD_UNPOWER_CARD);
                if (retCode == ModWinsCard.SCARD_S_SUCCESS)
                {
                    pesan.Items.Add("Disconnect (0)");
                }
                else
                {
                    pesan.Items.Add("Disconnect (-1)");
                }

            }

            //Connect
            retCode = ModWinsCard.SCardConnect(hContext, reader.Text, ModWinsCard.SCARD_SHARE_EXCLUSIVE, ModWinsCard.SCARD_PROTOCOL_T1, ref hCard, ref Protocol);
            if (retCode != ModWinsCard.SCARD_S_SUCCESS)
            {
                pesan.Items.Add("SCardConnect Error: " + ModWinsCard.GetScardErrMsg(retCode));
                displayOut(0, 0, "Connection failed. Further communication is disabled");
                rid = 0;
                timer1.Start();
            }

            else
            {
                displayOut(0, 0, "Successfully connection to " + reader.Text);
                GetUID();
                uid = uid.Substring(0, 8).Trim();
                cekuid(uid);
            }
            connActive = true;
        }
        private void cekuid(string uid)
        {
            string kart = "";
            try
            {
                conndb.Open();
            }
            catch
            {
            }
            NpgsqlCommand query = new NpgsqlCommand();
            query.Connection = conndb;
            query.CommandText = "select * from student where uid= '" + uid + "'";
            query.CommandType = CommandType.Text;
            NpgsqlDataReader eksekusi = query.ExecuteReader();
            NpgsqlDataAdapter adapter = new NpgsqlDataAdapter(query.CommandText, conndb);
            if (eksekusi.HasRows == false)
            {
                MessageBox.Show("Sorry, your card is not registered. Try again or do registration first");
                conndb.Close();
                timer1.Start();
            }
            else
            {
                while (eksekusi.Read())
                {
                    kart = eksekusi["uid"].ToString();
                    baca();
                }
            }
            conndb.Close();
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
        public int rid = 0;
        public int f = 0; string source = "";
        private void GetUID()
        {
            //Command APDU = CLA INS P1 P2 Le
            //               FF  CA  00 00 00
            SendBuff[0] = 0xFF;//cla
            SendBuff[1] = 0xCA;//ins
            SendBuff[2] = 0x00;
            SendBuff[3] = 0x00;
            SendBuff[4] = 0x00;

            SendLen = 5;
            RecvLen = 255;
            SendAPDUandDisplay(2);
        }
        private void baca()
        {
            string tmpStr = ""; string str = "";

            // Validate Inputs

            int temp = 8;
            int Flag = 1; int i = 0;
            while (Flag == 1)
            {
                if ((temp + 1) % 4 == 0)
                {
                    temp += 1;
                }
                if (temp % 4 == 0)
                {
                    auten(temp);
                }

                ClearBuffers();
                SendBuff[0] = 0xFF;
                SendBuff[1] = 0xB0;
                SendBuff[2] = 0x00;
                SendBuff[3] = (byte)temp;
                SendBuff[4] = (byte)16;
                SendLen = 5;
                RecvLen = SendBuff[4] + 2;

                retCode = SendAPDUandDisplay(2);
                if (retCode != ModWinsCard.SCARD_S_SUCCESS)
                {
                    timer1.Start();
                }

                // Display data in text format
                tmpStr = "";
                for (int indx = 0; indx < RecvLen - 2; indx++)
                {
                    tmpStr = tmpStr + Convert.ToChar(RecvBuff[indx]);
                }
                str = str + "" + tmpStr;
                //data[i] = tmpStr;
                temp += 1;
                i += 1;
                if (tmpStr.Contains('*'))
                {
                    Flag = 0;
                }
            }
            string[] pisah = str.Split('#', '*');
            try
            {
                nama = pisah[0];
                nim = pisah[1];
                fp = pisah[2];
            }
            catch
            {
            }
           
            epican += 1;
            rid = 1;

            this.Hide();
            Form4 frm4 = new Form4(nama, nim, fp, uid);
            frm4.Show();
        }
        
    }
}
