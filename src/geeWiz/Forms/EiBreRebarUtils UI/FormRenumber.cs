// FormRenumber.cs
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Autodesk.Revit.DB;

namespace geeWiz.RebarUtils
{
    public partial class FormRenumber : System.Windows.Forms.Form
    {
        public FormRenumber(Document doc, string[] partitions, Dictionary<string, string[]> rebarNumbers, string selectedPartition = null, int? selectedRebarNumber = null)
        {
            InitializeComponent();

            this.doc = doc;
            this.rebarNumbers = rebarNumbers;

            comboPartition.Items.AddRange(partitions);

            if (!string.IsNullOrEmpty(selectedPartition) && partitions.Contains(selectedPartition))
            {
                comboPartition.SelectedItem = selectedPartition;
            }
            else
            {
                comboPartition.SelectedItem = partitions.FirstOrDefault();
            }

            if (comboPartition.SelectedItem != null)
            {
                string selectedPart = comboPartition.SelectedItem.ToString();
                comboRebarNumber.Items.Clear();
                comboRebarNumber.Items.AddRange(rebarNumbers[selectedPart].Distinct().ToArray());

                if (selectedRebarNumber.HasValue)
                {
                    string rebarStr = selectedRebarNumber.Value.ToString();
                    if (rebarNumbers[selectedPart].Contains(rebarStr))
                    {
                        comboRebarNumber.SelectedItem = rebarStr;
                    }
                }
            }
        }

        private Document doc { get; set; }
        private Dictionary<string, string[]> rebarNumbers { get; set; }

        private string _partition;
        public string partition
        {
            get => _partition;
            set
            {
                _partition = value;
                if (comboPartition.Items.Contains(value))
                {
                    comboPartition.SelectedItem = value;
                }
            }
        }

        private int _fromNumber;
        public int fromNumber
        {
            get => _fromNumber;
            set
            {
                _fromNumber = value;
                comboRebarNumber.Text = value.ToString();
            }
        }

        public int toNumber { get; set; }

        private void comboPartition_SelectedIndexChanged(object sender, EventArgs e)
        {
            string selectedPartition = comboPartition.Text;
            comboRebarNumber.Items.Clear();
            if (rebarNumbers.ContainsKey(selectedPartition))
            {
                comboRebarNumber.Items.AddRange(rebarNumbers[selectedPartition].Distinct().ToArray());
            }
        }

        private void buttonChange_Click(object sender, EventArgs e)
        {
            partition = comboPartition.Text;
            fromNumber = int.Parse(comboRebarNumber.Text);
            toNumber = int.Parse(textNewNumber.Text);

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
