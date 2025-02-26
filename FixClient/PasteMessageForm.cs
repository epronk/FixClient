/////////////////////////////////////////////////
//
// FIX Client
//
// Copyright @ 2021 VIRTU Financial Inc.
// All rights reserved.
//
// Filename: PasteMessageForm.cs
// Author:   Gary Hughes
//
/////////////////////////////////////////////////

using System;
using System.Windows.Forms;

namespace FixClient
{
    public partial class PasteMessageForm : Form
    {
        public PasteMessageForm()
        {
            InitializeComponent();
        }

        public bool FilterEmptyFields
        {
            get { return filterEmptyFieldsCheckBox.Checked; }
            set { filterEmptyFieldsCheckBox.Checked = value; }
        }

        public bool DefineUnknownAsCustom
        {
            get { return defineUnknownAsCustomCheckBox.Checked; }
            set { defineUnknownAsCustomCheckBox.Checked = value; }
        }

        public bool ResetExistingMessage
        {
            get { return resetMessageCheckBox.Checked; }
            set { resetMessageCheckBox.Checked = value; }
        }
    }
}
