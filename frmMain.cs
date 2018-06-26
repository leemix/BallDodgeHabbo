using Sulakore.Habbo;
using Sulakore.Modules;
using System;
using System.Windows.Forms;
using Tangine;
using Sulakore.Communication;
using System.Reflection;
using System.Net;
using System.Runtime.InteropServices;

namespace BallDodge
{
    [Module("BallDodge", "Allow you to automatically dodge the balls on habbo.")]
    [Author("Riko66", HabboName = "Iterator", Hotel = HHotel.ComBr)]
    public partial class frmMain : ExtensionForm
    {
        public override bool IsRemoteModule => true;
        int iDistance = 2;
        int iWalk = 1;

        [DllImport("User32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        private static readonly int VK_F4 = 0x73; //This is the print-screen key.

        //Assume the timer is setup with Interval = 16 (corresponds to ~60FPS).
        private System.Windows.Forms.Timer timer1 = new System.Windows.Forms.Timer();

        public frmMain()
        {
			InitializeComponent();
			timer1.Interval = 80;
			//timer1.Enabled = true;
			timer1.Tick += new EventHandler(timer1_Tick);
			KeyPreview = true;
			KeyDown += new KeyEventHandler(Form_KeyDown);
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            short keyState = GetAsyncKeyState(VK_F4);

            bool f2 = ((keyState >> 15) & 0x0001) == 0x0001;
            bool unprocessedPress = ((keyState >> 0) & 0x0001) == 0x0001;

            if (f2)
            {
                checkBox1.Checked = !checkBox1.Checked;
            }
        }

        private bool RemoteFileExists(string url)
        {
            try
            {
                HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
                request.Method = "HEAD";
                HttpWebResponse response = request.GetResponse() as HttpWebResponse;
                response.Close();
                return (response.StatusCode == HttpStatusCode.OK);
            }
            catch
            {
                return false;
            }
        }

        private struct PlayerBase
        {
            public int x, y;

            public PlayerBase(int posX, int posY)
            {
                x = posX;
                y = posY;
            }
        }

        int pPosX = 0;
        int pPosY = 0;

        private void frmMain_Load(object sender, EventArgs e)
        {
        }

        void Form_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F3)
            {
                checkBox1.Checked = !checkBox1.Checked;
            }
        }

        public void GetCurrentPos(DataInterceptedEventArgs e)
        {
            PlayerBase pPlayer;

            e.Packet.ReadBytes(11);
            int pY = (int)e.Packet.ReadBytes(1)[0];
            e.Packet.ReadBytes(3);
            int pX = (int)e.Packet.ReadBytes(1)[0];            

            pPlayer.y = pY;
            pPlayer.x = pX;

            pPosX = pPlayer.x;
            pPosY = pPlayer.y;

            if (checkBox2.Checked)
            {
                label2.Text = pPosX.ToString();
                label3.Text = pPosY.ToString();
            }
        }

        public void SetCurrentPos(DataInterceptedEventArgs e)
        {
            PlayerBase pPlayer;
            pPlayer.y = e.Packet.ReadInteger();
            pPlayer.x = e.Packet.ReadInteger();
            pPosX = pPlayer.x;
            pPosY = pPlayer.y;

            if (checkBox2.Checked)
            {
                label2.Text = pPosX.ToString();
                label3.Text = pPosY.ToString();
            }
        }

        private bool isItemNear(int fX, int fY, int iFactor)
        {
            if (fY + iFactor == pPosY || fY - iFactor == pPosY || fX + iFactor+1 == pPosX || fX - iFactor == pPosX)
            {                
                return true;
            }

            return true;
        
        }

        private void DodgeBall(int fX, int fY, int iDistance_)
        {
            int iBallY = fY;
            int iBallX = fX;
            bool bUpdate = false;

            if (isItemNear(iBallX, iBallY, 2) || isItemNear(iBallX, iBallY, 1))
            {
                if (pPosY - iDistance_ == iBallY || pPosY - 1 == iBallY)
                {
                    pPosY += iWalk;
                    bUpdate = true;
                }

                if (pPosY + iDistance_ == iBallY || pPosY + 1 == iBallY)
                {
                    pPosY -= iWalk;
                    bUpdate = true;
                }

                if (pPosX == iBallX + iDistance_ + 1 || pPosX == iBallX + 2)
                {
                    pPosX += iWalk;
                    bUpdate = true;
                }

                if (pPosX + iDistance_ + 1 == iBallX || pPosX + 2 == iBallX)
                {
                    pPosX -= iWalk;
                    bUpdate = true;
                }

                if (checkBox2.Checked)
                {
                    label2.Text = pPosX.ToString();
                    label3.Text = pPosY.ToString();
                }
            }

            if (bUpdate)
                Connection.SendToServerAsync((ushort)676, pPosY, pPosX);
        }

        private void floorItemUpdate(DataInterceptedEventArgs e)
        {
            e.Packet.ReadBytes(7);
            int furniX = (int)e.Packet.ReadBytes(1)[0];
            e.Packet.ReadBytes(3);
            int furniY = (int)e.Packet.ReadBytes(1)[0];

            if (checkBox2.Checked)
            {
                label9.Text = furniX.ToString();
                label7.Text = furniY.ToString();
            }

            DodgeBall(furniX, furniY, iDistance);
        }

        private void chkJsonOnly_CheckedChanged(object sender, EventArgs e)
        {
        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBox1.Checked)
            {
                if (checkBox3.Checked)
                {
                    Triggers.OutAttach((ushort)676, SetCurrentPos);
                }
                else
                {
                    Triggers.InAttach((ushort)1662, GetCurrentPos);
                }

                Triggers.InAttach((ushort)2978, floorItemUpdate);

            }
            else
            {
                if (checkBox3.Checked)
                {
                    Triggers.OutDetach((ushort)676);
                }
                else
                {
                    Triggers.InDetach((ushort)1662);
                }

                Triggers.InDetach((ushort)2978);
            }
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            iDistance = Convert.ToInt32(textBox1.Text);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            iWalk = Convert.ToInt32(textBox2.Text);
        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
}
