using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GeoTagNinja
{
    internal class lvwFunctions
    {
        public static void changeControlStatus(Control varControl, bool varState)
        {
            if (varControl.InvokeRequired)
            {
                varControl.BeginInvoke(new ControlStateChange(changeControlStatus), new object[] { varControl, varState });
            }
            else
            {
                varControl.Enabled = varState;
            }
        }
        public static void changeControlText(Control varControl, string varText)
        {
            if (varControl.InvokeRequired)
            {
                varControl.BeginInvoke(new ControlTextChange(changeControlText), new object[] { varControl, varText });
            }
            else
            {
                varControl.Text = varText;
            }
        }
        public static string readControlText(Control varControl)
        {
            if (varControl.InvokeRequired)
            {
                return (string)varControl.Invoke(new Func<String>(() => readControlText(varControl)));
            }
            else
            {
                string varText = varControl.Text;
                return varText;
            }
        }
        public static int listViewCountItems(ListView varControl)
        {
            if (varControl.InvokeRequired)
            {
                return (int)varControl.Invoke(new Func<int>(() => listViewCountItems(varControl)));
            }
            else
            {
                return varControl.Items.Count;
                //string varText = varControl.Text;
                //return varText;
            }
        }
        public static void comboBoxClearItems(ComboBox varControl)
        {
            if (varControl.InvokeRequired)
            {
                varControl.BeginInvoke(new MethodInvoker(() => comboBoxClearItems(varControl)));
            }
            else
            {
                varControl.Items.Clear();
            }
        }
        public static void listViewClearItems(ListView varListView)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new ListViewHandler(listViewClearItems), new object[] { varListView });
            }
            else
            {
                varListView.Items.Clear();
            }
        }
        public static void listViewClearColumns(ListView varListView)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new ListViewHandler(listViewClearColumns), new object[] { varListView });
            }
            else
            {
                varListView.Clear();
            }
        }
        public static void listViewAddItem(ListView varListView, ListViewItem item)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new MethodInvoker(() => listViewAddItem(varListView, item)));
            }
            else
            {
                varListView.Items.Add(item);
            }
        }
        public static void listViewEditItem(ListView varListView, int varRow, int varColumn, string varText)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new MethodInvoker(() => listViewEditItem(varListView, varRow, varColumn, varText)));
            }
            else
            {
                varListView.Items[varRow].SubItems[varColumn].Text = varText;
            }
        }
        public static void listViewEditItemColor(ListView varListView, int varRow, Color varColor)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new MethodInvoker(() => listViewEditItemColor(varListView, varRow, varColor)));
            }
            else
            {
                varListView.Items[varRow].BackColor = varColor;
            }
        }
        public static void listViewChangeBackColor(ListView varListView, ListViewItem item, Color varColor)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new MethodInvoker(() => listViewChangeBackColor(varListView, item, varColor)));
            }
            else
            {
                for (int i = 0; i < varListView.Columns.Count; i++)
                {
                    item.UseItemStyleForSubItems = false;
                    item.SubItems[i].BackColor = varColor;
                }
            }
        }
        public static void listViewChangeHeaderStyle(ListView varListView, ColumnHeaderStyle varColumnHeaderStyle)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new MethodInvoker(() => listViewChangeHeaderStyle(varListView, varColumnHeaderStyle)));
            }
            else
            {
                varListView.HeaderStyle = varColumnHeaderStyle;
            }
        }
        public static void listViewAddItemRange(ListView varListView, ListViewItem item)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new MethodInvoker(() => listViewAddItemRange(varListView, item)));
            }
            else
            {
                //varListView.Items.Add(item);
                varListView.Items.AddRange(new[] { item });
            }
        }
        public static void listViewAddColumn(ListView varListView, string varColumnName, int varColumnSize)
        {
            if (varListView.InvokeRequired)
            {
                varListView.BeginInvoke(new MethodInvoker(() => listViewAddColumn(varListView, varColumnName, varColumnSize)));
            }
            else
            {
                varListView.Columns.Add(varColumnName, varColumnSize, HorizontalAlignment.Left);
            }
        }
        #region Nested type: ControlStateChange
        private delegate void ControlStateChange(Control varControl, bool varState);
        #endregion
        #region Nested type: ControlTextChange
        private delegate void ControlTextChange(Control varControl, string varText);
        #endregion
        private delegate string ControlTextRead(Control varControl);
        #region Nested type: ListViewHandler
        private delegate void ListViewHandler(ListView varListView);
        #endregion
        #region Nested type: ListViewHandlerItem
        private delegate void ListViewHandlerItem(ListView varListView, ListViewItem item);
        #endregion
    }

}
