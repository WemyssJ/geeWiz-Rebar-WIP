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
        // Constructor with optional preselected values
        public FormRenumber(Document doc, string[] partitions, Dictionary<string, string[]> rebarNumbers, string selectedPartition = null, int? selectedRebarNumber = null)
        {
            InitializeComponent();

            this.doc = doc;
            this.rebarNumbers = rebarNumbers;

            comboPartition.Items.AddRange(partitions);

            // Select initial partition
            if (!string.IsNullOrEmpty(selectedPartition) && partitions.Contains(selectedPartition))
            {
                comboPartition.SelectedItem = selectedPartition;
            }
            else
            {
                comboPartition.SelectedItem = partitions.FirstOrDefault();
            }

            // Populate rebar numbers for selected partition
            if (comboPartition.SelectedItem != null)
            {
                string selectedPart = comboPartition.SelectedItem.ToString();
                comboRebarNumber.Items.AddRange(rebarNumbers[selectedPart]);

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

        // Properties
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

        // Events
        private void comboPartition_SelectedIndexChanged(object sender, EventArgs e)
        {
            try
            {
                string selectedPartition = comboPartition.Text;
                comboRebarNumber.Items.Clear();
                if (rebarNumbers.ContainsKey(selectedPartition))
                {
                    comboRebarNumber.Items.AddRange(rebarNumbers[selectedPartition]);
                }
            }
            catch
            {
                comboRebarNumber.Items.Clear();
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
