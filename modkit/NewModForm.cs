// SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.Drawing;
using System.Windows.Forms;

namespace BlackModKit
{
    public sealed class NewModForm : Form
    {
        private readonly TextBox _name;
        private readonly TextBox _folder;
        private readonly TextBox _lang;
        private readonly NumericUpDown _priority;
        private readonly CheckBox _startOff;
        private readonly Label _hint;
        private bool _loading = true;

        public string ModName { get; private set; }
        public string Folder { get; private set; }
        public int Priority { get; private set; }
        public string Lang { get; private set; }
        public bool StartOff { get; private set; }

        public NewModForm()
        {
            Text = "Novo mod";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            ClientSize = new Size(510, 285);
            Font = new Font("Segoe UI", 9f);

            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(12),
                ColumnCount = 2,
                RowCount = 7
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _name = new TextBox { Dock = DockStyle.Fill, Text = "MEU MOD" };
            _folder = new TextBox { Dock = DockStyle.Fill, Text = "meu-mod" };
            _lang = new TextBox { Dock = DockStyle.Fill };
            _priority = new NumericUpDown { Dock = DockStyle.Left, Width = 100, Minimum = -9999, Maximum = 9999 };
            _startOff = new CheckBox { Text = "Começar desligado (.off)", AutoSize = true };
            _hint = new Label { AutoSize = false, Dock = DockStyle.Fill, ForeColor = Color.DimGray };

            AddRow(table, 0, "Nome:", _name);
            AddRow(table, 1, "Pasta:", _folder);
            AddRow(table, 2, "Prioridade:", _priority);
            AddRow(table, 3, "Idioma:", _lang);
            table.Controls.Add(_startOff, 1, 4);
            table.Controls.Add(_hint, 0, 6);
            table.SetColumnSpan(_hint, 2);
            _hint.Text = "A pasta será criada dentro de mods\\.";
            _hint.Padding = new Padding(0, 10, 0, 0);

            _name.TextChanged += (s, e) =>
            {
                if (_loading) return;
                _folder.Text = ModManager.Slug(_name.Text);
                UpdateHint();
            };
            _folder.TextChanged += (s, e) => UpdateHint();
            _lang.TextChanged += (s, e) => UpdateHint();

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                Height = 46,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(8)
            };
            var cancel = new Button { Text = "Cancelar", DialogResult = DialogResult.Cancel, Width = 90 };
            var ok = new Button { Text = "Criar", Width = 90 };
            ok.Click += (s, e) => Confirm();
            buttons.Controls.Add(cancel);
            buttons.Controls.Add(ok);

            Controls.Add(table);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
            _loading = false;
            UpdateHint();
        }

        private static void AddRow(TableLayoutPanel table, int row, string label, Control control)
        {
            table.Controls.Add(new Label { Text = label, TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, row);
            table.Controls.Add(control, 1, row);
        }

        private void UpdateHint()
        {
            string folder = ModManager.Slug(_folder.Text);
            _hint.Text = folder.Length == 0
                ? "A pasta não pode ficar vazia."
                : "Será criada a pasta mods\\" + folder + ".";
        }

        private void Confirm()
        {
            string name = (_name.Text ?? "").Trim();
            string folder = ModManager.Slug(_folder.Text);
            if (name.Length == 0)
            {
                MessageBox.Show(this, "Digite um nome para o mod.", "Nome", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _name.Focus();
                return;
            }
            if (folder.Length == 0)
            {
                MessageBox.Show(this, "Digite uma pasta para o mod.", "Pasta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _folder.Focus();
                return;
            }

            ModName = name;
            Folder = folder;
            Priority = (int)_priority.Value;
            Lang = (_lang.Text ?? "").Trim().ToLowerInvariant();
            StartOff = _startOff.Checked;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
