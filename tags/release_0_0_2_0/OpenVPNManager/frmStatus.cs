﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using OpenVPN;

namespace OpenVPNManager
{
    /// <summary>
    /// displays the state of the connection, shows log, etc.
    /// </summary>
    public partial class frmStatus : Form
    {
        /// <summary>
        /// represents a colored listbox entry
        /// </summary>
        private class ColoredListBoxItem
        {
            /// <summary>
            /// color of the entry
            /// </summary>
            public enum rowColor
            {
                /// <summary>
                /// red
                /// </summary>
                RED,

                /// <summary>
                /// blue
                /// </summary>
                BLUE,

                /// <summary>
                /// darkblue
                /// </summary>
                DARKBLUE,

                /// <summary>
                /// green
                /// </summary>
                GREEN,

                /// <summary>
                /// black
                /// </summary>
                BLACK
            }

            /// <summary>
            /// the text of the entry
            /// </summary>
            private string m_text;

            /// <summary>
            /// the prefix of the entry
            /// </summary>
            private string m_prefix;

            /// <summary>
            /// the color of the entry
            /// </summary>
            private rowColor m_color;

            /// <summary>
            /// creates a new ColoredListBoxItem
            /// </summary>
            /// <param name="prefix">the prefix which will be used</param>
            /// <param name="text">the real message</param>
            /// <param name="color">the color of both</param>
            public ColoredListBoxItem(string prefix, string text,
                rowColor color)
            {
                m_text = text;
                m_prefix = prefix;
                m_color = color;
            }

            /// <summary>
            /// the prefix of the text
            /// </summary>
            public string prefix
            {
                get { return m_prefix; }
            }

            /// <summary>
            /// the real message
            /// </summary>
            public string text
            {
                get { return m_text;  }
            }

            /// <summary>
            /// the color of the message
            /// </summary>
            public rowColor color
            {
                get { return m_color;  }
            }
        }

        /// <summary>
        /// the config which belongs to the control
        /// </summary>
        private VPNConfig m_config;

        /// <summary>
        /// creates a new form
        /// </summary>
        /// <param name="config">parent config</param>
        public frmStatus(VPNConfig config)
        {
            InitializeComponent();
            m_config = config;
        }
        
        /// <summary>
        /// (re)initialize the form;
        /// this is needed if the vpn changes
        /// </summary>
        public void init()
        {
            lstLog.Items.Clear();

            // register the event
            m_config.vpn.stateChanged += new EventHandler(m_vpn_stateChanged);

            // refresh button state
            m_vpn_stateChanged(null, null);

            this.Text = "OpenVPN Manager [ " + m_config.name + " ]";
            
        }

        /// <summary>
        /// user wants to (dis)connect
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">ignored</param>
        private void btnConnect_Click(object sender, EventArgs e)
        {
            // connect only if we are disconnected, clear the list
            if (m_config.vpn.state == OVPN.OVPNState.STOPPED)
            {
                lstLog.Items.Clear();
                m_config.connect();
            }

            // disconnect only if we are connected
            else if (m_config.vpn.state == OVPN.OVPNState.INITIALIZING ||
                m_config.vpn.state == OVPN.OVPNState.RUNNING)
            {
                m_config.disconnect();
            }
        }

        /// <summary>
        /// vpn state has changed, refresh all buttons/texts
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">ignored</param>
        public void m_vpn_stateChanged(object sender, EventArgs e)
        {
            // wrong thread? invoke!
            if (this.InvokeRequired)
            {
                try
                {
                    this.Invoke(new EventHandler(m_vpn_stateChanged), sender, e);
                }
                catch (ObjectDisposedException)
                { }
                return;
            }
            
            // refresh all controls
            switch (m_config.vpn.state)
            {
                case OVPN.OVPNState.INITIALIZING:
                    lstLog.Items.Clear();
                    lblState.Text = Program.res.GetString("STATE_Initializing");
                    pbStatus.Image = Properties.Resources.STATE_Initializing;
                    toolTip.SetToolTip(btnConnect, 
                        Program.res.GetString("QUICKINFO_Disconnect"));
                    btnConnect.Image = Properties.Resources.BUTTON_Disconnect;
                    btnConnect.Enabled = true;
                    break;
                case OVPN.OVPNState.RUNNING:
                    lblState.Text = Program.res.GetString("STATE_Connected");
                    pbStatus.Image = Properties.Resources.STATE_Running;
                    toolTip.SetToolTip(btnConnect,
                        Program.res.GetString("QUICKINFO_Disconnect"));
                    btnConnect.Image = Properties.Resources.BUTTON_Disconnect;
                    btnConnect.Enabled = true;
                    break;
                case OVPN.OVPNState.STOPPED:
                    lblState.Text = Program.res.GetString("STATE_Stopped");
                    pbStatus.Image = Properties.Resources.STATE_Stopped;
                    toolTip.SetToolTip(btnConnect,
                        Program.res.GetString("QUICKINFO_Connect"));
                    btnConnect.Image = Properties.Resources.BUTTON_Connect;
                    btnConnect.Enabled = true;
                    break;
                case OVPN.OVPNState.STOPPING:
                    lblState.Text = Program.res.GetString("STATE_Stopping");
                    pbStatus.Image = Properties.Resources.STATE_Stopping;
                    toolTip.SetToolTip(btnConnect,
                        Program.res.GetString("QUICKINFO_Connect"));
                    btnConnect.Image = Properties.Resources.BUTTON_Connect;
                    btnConnect.Enabled = false;
                    break;
            }
        }

        /// <summary>
        /// Delegate to addLog.
        /// </summary>
        /// <param name="p">type of log event</param>
        /// <param name="m">the message</param>
        private delegate void addLogDelegate(OVPNLogEventArgs.LogType p, string m);

        /// <summary>
        /// Add a log entry.
        /// </summary>
        /// <param name="prefix">type of log event</param>
        /// <param name="text">the message</param>
        private void addLog(OVPNLogEventArgs.LogType prefix, string text)
        {
            // wrong thread? invoke!
            if (lstLog.InvokeRequired)
            {
                try
                {
                    lstLog.BeginInvoke(new addLogDelegate(addLog), prefix, text);
                }
                catch (ObjectDisposedException)
                {
                }
                return;
            }

            // get the color
            ColoredListBoxItem.rowColor rc  = ColoredListBoxItem.rowColor.BLACK;
            switch (prefix)
            {
                case OVPNLogEventArgs.LogType.MGNMT:
                    rc = ColoredListBoxItem.rowColor.GREEN;
                    break;

                case OVPNLogEventArgs.LogType.STDERR:
                    rc = ColoredListBoxItem.rowColor.RED;
                    break;

                case OVPNLogEventArgs.LogType.STDOUT:
                    rc = ColoredListBoxItem.rowColor.BLUE;
                    break;

                case OVPNLogEventArgs.LogType.LOG:
                    rc = ColoredListBoxItem.rowColor.DARKBLUE;
                    break;
            }

            // delete the oldes entry, if needed
            if (lstLog.Items.Count == 2048)
                lstLog.Items.RemoveAt(0);

            // add the log entry
            lstLog.Items.Add(new ColoredListBoxItem(prefix.ToString(),
               text, rc));
            
            //lstLog.SelectedIndex = lstLog.Items.Count - 1;

            int h = lstLog.ClientSize.Height - lstLog.Margin.Vertical;
            int i = lstLog.Items.Count - 1;
            while (h >= 0 && i > 0)
            {
                int nh = lstLog.GetItemHeight(i);

                if (nh > h)
                    break;
                else
                {
                    h -= nh;
                    i--;
                }
            }

            lstLog.TopIndex = i;
        }

        /// <summary>
        /// a listitem was double clicked, show text in message box
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">ignored</param>
        private void lstLog_DoubleClick(object sender, EventArgs e)
        {
            // show the selected item
            if (lstLog.SelectedItem != null)
                MessageBox.Show(
                    ((ColoredListBoxItem) lstLog.SelectedItem).text,
                    "OpenVPN Manager", MessageBoxButtons.OK,
                    MessageBoxIcon.Asterisk);
        }

        /// <summary>
        /// user wants to close the form;
        /// this is "transformed" to hide
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">EventArguments used to prevent closing</param>
        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // if the user closes the form via "x"...
            if (e.CloseReason == CloseReason.UserClosing)
            {
                // cancel and hide
                e.Cancel = true;
                this.Hide();
                return;
            }
        }

        /// <summary>
        /// OVPN wants to log an event
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">information about the event</param>
        public void logs_LogEvent(object sender, OVPNLogEventArgs e)
        {
            addLog(e.type, e.message);
        }

        /// <summary>
        /// use wants to hide the form
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">ignored</param>
        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        /// <summary>
        /// listbox redraws an item
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">information about what should be drawed and where</param>
        private void lstLog_DrawItem(object sender, DrawItemEventArgs e)
        {
            // just in case the list is empty...
            if (e.Index == -1)
                return;

            Brush br;

            // prefixes are drawed bold
            Font f = new Font(e.Font, FontStyle.Bold);

            ColoredListBoxItem li = (ColoredListBoxItem)
                ((ListBox)sender).Items[e.Index];

            // prepare the prefix
            string prefix = "[" + li.prefix + "] ";

            // chose the color
            switch (li.color)
            {
                case ColoredListBoxItem.rowColor.RED:
                    br = Brushes.Red;
                    break;
                case ColoredListBoxItem.rowColor.BLUE:
                    br = Brushes.Blue;
                    break;
                case ColoredListBoxItem.rowColor.GREEN:
                    br = Brushes.Green;
                    break;
                case ColoredListBoxItem.rowColor.DARKBLUE:
                    br = Brushes.DarkBlue;
                    break;
                case ColoredListBoxItem.rowColor.BLACK:
                default:
                    br = Brushes.Black;
                    break;
            }

            e.DrawBackground();

            // draw the prefix
            e.Graphics.DrawString(prefix, f, br, e.Bounds,
                StringFormat.GenericDefault);

            // calculate the width of the longest prefix
            int w = (int)
                e.Graphics.MeasureString("[XXXXXXX] ", e.Font, e.Bounds.Width,
                StringFormat.GenericDefault).Width;

            // calculate the new rectangle
            Rectangle newBounds = new Rectangle(e.Bounds.Location, 
                e.Bounds.Size);
            newBounds.X += w;
            newBounds.Width -= w;

            // draw the text
            e.Graphics.DrawString(
                li.text, e.Font, br, newBounds,
                StringFormat.GenericDefault);

            // draw the focus
            e.DrawFocusRectangle();
        }

        /// <summary>
        /// user wants to edit the configuration
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">ignored</param>
        private void btnEdit_Click(object sender, EventArgs e)
        {
            m_config.edit();
        }

        /// <summary>
        /// A button was pressed, perform/simulate the clicks.
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">the pressed key</param>
        private void frmStatus_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Alt)
            {
                e.Handled = true;
                switch (e.KeyCode)
                {
                    case Keys.C:
                        btnClose_Click(null, null);
                        break;
                    case Keys.E:
                        btnEdit_Click(null, null);
                        break;
                    case Keys.O:
                        btnClose_Click(null, null);
                        break;
                    default:
                        e.Handled = false;
                        break;
                }
            }
        }

        /// <summary>
        /// The form was resized.
        /// Make shure, the list displays proper data.
        /// </summary>
        /// <param name="sender">ignored</param>
        /// <param name="e">ignored</param>
        private void frmStatus_ResizeEnd(object sender, EventArgs e)
        {
            lstLog.Refresh();
        }
    }
}