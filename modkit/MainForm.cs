// SPDX-License-Identifier: GPL-2.0-or-later
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BlackModKit
{
    public sealed class MainForm : Form
    {
        private readonly string _settingsPath;
        private ModManager _manager;
        private List<ModInfo> _mods = new List<ModInfo>();
        private List<ModConflict> _conflicts = new List<ModConflict>();
        private ModInfo _selected;
        private bool _loading;

        private TextBox _portBox;
        private ListView _modList;
        private TextBox _nameBox;
        private TextBox _folderBox;
        private TextBox _versionBox;
        private TextBox _authorBox;
        private TextBox _descriptionBox;
        private ComboBox _langBox;
        private NumericUpDown _priorityBox;
        private CheckBox _onBox;
        private ListBox _filesList;
        private TextBox _validationBox;
        private ListView _conflictsList;
        private Label _conflictInfo;
        private ToolStripStatusLabel _status;
        private SplitContainer _split;

        public MainForm()
        {
            Text = "BLACK PC ModKit - criação de mods";
            StartPosition = FormStartPosition.CenterScreen;
            MinimumSize = new Size(980, 640);
            ClientSize = new Size(1120, 720);
            Font = new Font("Segoe UI", 9f);
            _settingsPath = SettingsPath();

            BuildUi();
            Load += OnLoad;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            try
            {
                _split.Panel1MinSize = 350;
                _split.Panel2MinSize = 400;
                _split.SplitterDistance = Math.Max(350, Math.Min(470, _split.Width - 400));
            }
            catch
            {
                // The splitter is cosmetic; never let it stop the window from opening.
            }
        }

        private static string SettingsPath()
        {
            try
            {
                string folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlackModKit");
                Directory.CreateDirectory(folder);
                return Path.Combine(folder, "modkit.ini");
            }
            catch
            {
                return Path.Combine(Application.StartupPath, "modkit.ini");
            }
        }

        private void BuildUi()
        {
            var root = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3
            };
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 24));

            var top = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 5,
                Padding = new Padding(8, 8, 8, 0)
            };
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
            top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));

            top.Controls.Add(new Label
            {
                Text = "Pasta do port:",
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            }, 0, 0);

            _portBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3) };
            _portBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode != Keys.Enter) return;
                e.SuppressKeyPress = true;
                SetPort(_portBox.Text.Trim(), true);
            };
            top.Controls.Add(_portBox, 1, 0);

            var browse = new Button { Text = "Procurar...", Dock = DockStyle.Fill, Margin = new Padding(3) };
            browse.Click += (s, e) => BrowsePort();
            top.Controls.Add(browse, 2, 0);

            var refresh = new Button { Text = "Atualizar", Dock = DockStyle.Fill, Margin = new Padding(3) };
            refresh.Click += (s, e) => RefreshMods();
            top.Controls.Add(refresh, 3, 0);

            var play = new Button { Text = "Abrir jogo", Dock = DockStyle.Fill, Margin = new Padding(3) };
            play.Click += (s, e) => OpenGame();
            top.Controls.Add(play, 4, 0);
            root.Controls.Add(top, 0, 0);

            _split = new SplitContainer
            {
                Dock = DockStyle.Fill,
                FixedPanel = FixedPanel.Panel1
            };
            _split.Panel1.Controls.Add(BuildLeftPanel());
            _split.Panel2.Controls.Add(BuildRightTabs());
            root.Controls.Add(_split, 0, 1);

            var strip = new StatusStrip { Dock = DockStyle.Fill, SizingGrip = false };
            _status = new ToolStripStatusLabel("Escolha a pasta do port para começar.");
            strip.Items.Add(_status);
            root.Controls.Add(strip, 0, 2);

            Controls.Add(root);
        }

        private Control BuildLeftPanel()
        {
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
                ColumnCount = 1,
                RowCount = 2
            };
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            panel.RowStyles.Add(new RowStyle(SizeType.Absolute, 78));

            _modList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false
            };
            _modList.Columns.Add("Nome", 145);
            _modList.Columns.Add("Pasta", 105);
            _modList.Columns.Add("Versão", 55);
            _modList.Columns.Add("Prio", 42);
            _modList.Columns.Add("Idioma", 58);
            _modList.Columns.Add("Estado", 78);
            _modList.Columns.Add("Arq.", 42);
            _modList.SelectedIndexChanged += (s, e) => SelectCurrentMod();
            panel.Controls.Add(_modList, 0, 0);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(0, 4, 0, 0)
            };
            AddButton(buttons, "Novo mod", 130, (s, e) => NewMod());
            AddButton(buttons, "Importar .bmod", 130, (s, e) => ImportPackageDialog());
            AddButton(buttons, "Importar pasta", 130, (s, e) => ImportPackageFolder());
            AddButton(buttons, "Abrir mods", 130, (s, e) => OpenFolder(_manager == null ? "" : _manager.ModsDir));
            panel.Controls.Add(buttons, 0, 1);

            return panel;
        }

        private void AddButton(FlowLayoutPanel panel, string text, int width, EventHandler handler)
        {
            var button = new Button { Text = text, Width = width, Height = 30, Margin = new Padding(0, 0, 6, 4) };
            button.Click += handler;
            panel.Controls.Add(button);
        }

        private Control BuildRightTabs()
        {
            var tabs = new TabControl { Dock = DockStyle.Fill };
            tabs.TabPages.Add(BuildModTab());
            tabs.TabPages.Add(BuildConflictsTab());
            tabs.TabPages.Add(BuildCommunityTab());
            return tabs;
        }

        private TabPage BuildModTab()
        {
            var tab = new TabPage("Mod");
            var table = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(8),
                ColumnCount = 4,
                RowCount = 10
            };
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 78));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 75));
            table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 94));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 22));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            _nameBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3) };
            _versionBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3) };
            _folderBox = new TextBox { Dock = DockStyle.Fill, ReadOnly = true, Margin = new Padding(3) };
            _authorBox = new TextBox { Dock = DockStyle.Fill, Margin = new Padding(3) };
            _priorityBox = new NumericUpDown
            {
                Dock = DockStyle.Left,
                Width = 105,
                Minimum = -9999,
                Maximum = 9999,
                Margin = new Padding(3)
            };
            _langBox = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDown, Margin = new Padding(3) };
            _langBox.Items.AddRange(new object[] { "", "en", "ptbr", "es" });
            _descriptionBox = new TextBox { Dock = DockStyle.Fill, Multiline = true, ScrollBars = ScrollBars.Vertical, Margin = new Padding(3) };
            _onBox = new CheckBox { Text = "Ligado (sem .off)", AutoSize = true, Anchor = AnchorStyles.Left, Margin = new Padding(3) };

            AddField(table, 0, 0, "Nome:", _nameBox);
            AddField(table, 0, 2, "Versão:", _versionBox);
            AddField(table, 1, 0, "Pasta:", _folderBox);
            AddField(table, 1, 2, "Autor:", _authorBox);
            AddField(table, 2, 0, "Prioridade:", _priorityBox);
            AddField(table, 2, 2, "Idioma:", _langBox);

            table.Controls.Add(new Label { Text = "Descrição:", TextAlign = ContentAlignment.MiddleLeft, Dock = DockStyle.Fill }, 0, 3);
            table.Controls.Add(_descriptionBox, 1, 3);
            table.SetColumnSpan(_descriptionBox, 3);

            table.Controls.Add(_onBox, 1, 4);
            table.SetColumnSpan(_onBox, 3);

            var buttons = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Margin = new Padding(0, 4, 0, 0)
            };
            AddButton(buttons, "Aplicar", 82, (s, e) => ApplySelected(true));
            AddButton(buttons, "Abrir pasta", 95, (s, e) => OpenSelectedFolder());
            AddButton(buttons, "Adicionar pasta...", 135, (s, e) => AddFolderToSelected());
            AddButton(buttons, "Validar", 82, (s, e) => ValidateSelected());
            AddButton(buttons, "Exportar .bmod", 125, (s, e) => ExportSelected());
            table.Controls.Add(buttons, 0, 5);
            table.SetColumnSpan(buttons, 4);

            var filesLabel = new Label { Text = "Arquivos de jogo (caminho relativo):", Dock = DockStyle.Fill };
            table.Controls.Add(filesLabel, 0, 6);
            table.SetColumnSpan(filesLabel, 4);

            _filesList = new ListBox { Dock = DockStyle.Fill, IntegralHeight = false, HorizontalScrollbar = true, Margin = new Padding(3) };
            table.Controls.Add(_filesList, 0, 7);
            table.SetColumnSpan(_filesList, 4);

            var validationLabel = new Label { Text = "Validação:", Dock = DockStyle.Fill };
            table.Controls.Add(validationLabel, 0, 8);
            table.SetColumnSpan(validationLabel, 4);

            _validationBox = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Both,
                WordWrap = false,
                Font = new Font("Consolas", 8.5f),
                BackColor = SystemColors.Window,
                Margin = new Padding(3)
            };
            table.Controls.Add(_validationBox, 0, 9);
            table.SetColumnSpan(_validationBox, 4);

            tab.Controls.Add(table);
            return tab;
        }

        private static void AddField(TableLayoutPanel table, int row, int labelColumn, string label, Control control)
        {
            table.Controls.Add(new Label
            {
                Text = label,
                TextAlign = ContentAlignment.MiddleLeft,
                Dock = DockStyle.Fill
            }, labelColumn, row);
            table.Controls.Add(control, labelColumn + 1, row);
        }

        private TabPage BuildConflictsTab()
        {
            var tab = new TabPage("Conflitos");
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Padding = new Padding(8) };
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 44));

            _conflictsList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false
            };
            _conflictsList.Columns.Add("Arquivo", 300);
            _conflictsList.Columns.Add("Mods (prioridade)", 350);
            _conflictsList.Columns.Add("Vence", 150);
            _conflictsList.Columns.Add("Conteúdo", 110);
            table.Controls.Add(_conflictsList, 0, 0);

            var bottom = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1 };
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));
            _conflictInfo = new Label
            {
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleLeft,
                Text = "Nenhum conflito."
            };
            bottom.Controls.Add(_conflictInfo, 0, 0);
            var revalidate = new Button { Text = "Revalidar", Dock = DockStyle.Fill, Margin = new Padding(3) };
            revalidate.Click += (s, e) => ValidateSelected();
            bottom.Controls.Add(revalidate, 1, 0);
            table.Controls.Add(bottom, 0, 1);

            tab.Controls.Add(table);
            return tab;
        }

        private TabPage BuildCommunityTab()
        {
            var tab = new TabPage("Comunidade");
            var table = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3, Padding = new Padding(8) };
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 170));
            table.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
            table.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            var text = new TextBox
            {
                Dock = DockStyle.Fill,
                Multiline = true,
                ReadOnly = true,
                ScrollBars = ScrollBars.Vertical,
                BackColor = SystemColors.Window,
                Text =
                    "Receber mods da comunidade\r\n" +
                    "1. Exporte seu mod como .bmod pelo botão da aba Mod.\r\n" +
                    "2. Envie o pacote pelo canal de mods do projeto (issue ou pasta comunidade/pacotes).\r\n" +
                    "3. O pacote é validado antes de entrar no modloader.\r\n\r\n" +
                    "Importar um .bmod instala o mod em mods\\<id>. Se já existir, o anterior é guardado em mods\\.backup.\r\n" +
                    "Quando houver conflito de arquivo, o launcher mostra quem vence e permite desligar o mod.\r\n" +
                    "Use \"Importar pasta\" para receber vários pacotes de uma vez; eles entram desligados para revisão."
            };
            table.Controls.Add(text, 0, 0);

            var buttons = new FlowLayoutPanel { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
            AddButton(buttons, "Importar .bmod...", 140, (s, e) => ImportPackageDialog());
            AddButton(buttons, "Importar pasta de pacotes...", 185, (s, e) => ImportPackageFolder());
            AddButton(buttons, "Abrir pasta mods", 130, (s, e) => OpenFolder(_manager == null ? "" : _manager.ModsDir));
            table.Controls.Add(buttons, 0, 1);

            var hint = new Label
            {
                Dock = DockStyle.Fill,
                Text = "O formato do pacote está em docs/MODS.md e comunidade/COMO-ENVIAR.md no kit público.",
                ForeColor = Color.DimGray
            };
            table.Controls.Add(hint, 0, 2);
            tab.Controls.Add(table);
            return tab;
        }

        // ------------------------------------------------------------------ lifecycle ---

        private void OnLoad(object sender, EventArgs e)
        {
            string saved = ReadSetting("port");
            if (!string.IsNullOrWhiteSpace(saved) && Directory.Exists(saved))
            {
                SetPort(saved, false);
                return;
            }

            if (ModManager.LooksLikePort(Application.StartupPath))
            {
                SetPort(Application.StartupPath, false);
                return;
            }
            if (ModManager.LooksLikePort(Directory.GetCurrentDirectory()))
            {
                SetPort(Directory.GetCurrentDirectory(), false);
                return;
            }
            SetStatus("Escolha a pasta do port (onde ficam black.exe e game\\default.xbe).");
        }

        private void BrowsePort()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Escolha a pasta do BLACK PC (onde ficam black.exe e game\\default.xbe)";
                if (Directory.Exists(_portBox.Text)) dialog.SelectedPath = _portBox.Text;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;

                string dir = dialog.SelectedPath;
                if (!ModManager.LooksLikePort(dir))
                {
                    DialogResult answer = MessageBox.Show(this,
                        "Essa pasta não tem black.exe e game\\default.xbe. Continuar mesmo assim?",
                        "Pasta do port", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                    if (answer != DialogResult.Yes) return;
                }
                SetPort(dir, true);
            }
        }

        private void SetPort(string dir, bool showMessage)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                    throw new DirectoryNotFoundException("A pasta não existe.");
                _manager = new ModManager(dir);
                _portBox.Text = _manager.PortDir;
                SaveSetting("port", _manager.PortDir);
                RefreshMods();
                if (showMessage) SetStatus("Port carregado: " + _manager.PortDir);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Não foi possível abrir a pasta:\n" + ex.Message, "Port",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshMods()
        {
            string typed = (_portBox.Text ?? "").Trim();
            if (!string.IsNullOrEmpty(typed) && Directory.Exists(typed) &&
                (_manager == null || !string.Equals(Path.GetFullPath(typed), _manager.PortDir, StringComparison.OrdinalIgnoreCase)))
            {
                SetPort(typed, true);
                return;
            }
            if (_manager == null)
            {
                MessageBox.Show(this, "Escolha a pasta do port primeiro (Procurar...).", "Port",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string selectedFolder = _selected == null ? "" : _selected.Folder;
            try
            {
                _mods = _manager.Scan();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Não foi possível ler a pasta mods:\n" + ex.Message, "Mods",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _loading = true;
            _modList.BeginUpdate();
            _modList.Items.Clear();
            foreach (ModInfo mod in _mods)
            {
                var item = new ListViewItem(mod.Title);
                item.SubItems.Add(mod.Folder);
                item.SubItems.Add(mod.Version);
                item.SubItems.Add(mod.PriorityText);
                item.SubItems.Add(mod.Lang);
                item.SubItems.Add(mod.StateText);
                item.SubItems.Add(mod.FileCountText);
                item.Tag = mod;
                if (mod.Blocked) item.ForeColor = Color.DimGray;
                _modList.Items.Add(item);
            }
            _modList.EndUpdate();
            _loading = false;

            if (!string.IsNullOrEmpty(selectedFolder))
                SelectFolder(selectedFolder);
            else if (_modList.Items.Count > 0)
                _modList.Items[0].Selected = true;
            else
            {
                _selected = null;
                ClearDetails("Nenhum mod encontrado. Use \"Novo mod\" ou importe um .bmod.");
            }

            UpdateConflicts();
            SetStatus(_mods.Count + " mod(s) em " + _manager.ModsDir);
        }

        private void SelectFolder(string folder)
        {
            foreach (ListViewItem item in _modList.Items)
            {
                var mod = item.Tag as ModInfo;
                if (mod != null && string.Equals(mod.Folder, folder, StringComparison.OrdinalIgnoreCase))
                {
                    item.Selected = true;
                    item.Focused = true;
                    item.EnsureVisible();
                    return;
                }
            }
        }

        private void SelectCurrentMod()
        {
            if (_loading) return;
            if (_modList.SelectedItems.Count == 0) return;
            var mod = _modList.SelectedItems[0].Tag as ModInfo;
            if (mod == null) return;
            _selected = mod;

            _loading = true;
            _nameBox.Text = mod.Title;
            _folderBox.Text = mod.Folder;
            _versionBox.Text = mod.Version;
            _authorBox.Text = mod.Author;
            _descriptionBox.Text = mod.Description;
            _priorityBox.Value = Math.Max(_priorityBox.Minimum, Math.Min(_priorityBox.Maximum, mod.Priority));
            _langBox.Text = mod.Lang;
            _onBox.Checked = mod.On;

            _filesList.BeginUpdate();
            _filesList.Items.Clear();
            foreach (string file in mod.Files) _filesList.Items.Add(file);
            _filesList.EndUpdate();
            _validationBox.Text = _manager.Validate(mod, _mods).Text;
            _loading = false;
        }

        private void ClearDetails(string message)
        {
            _nameBox.Text = "";
            _folderBox.Text = "";
            _versionBox.Text = "";
            _authorBox.Text = "";
            _descriptionBox.Text = "";
            _priorityBox.Value = 0;
            _langBox.Text = "";
            _onBox.Checked = false;
            _filesList.Items.Clear();
            _validationBox.Text = message;
        }

        // ------------------------------------------------------------------ actions ---

        private void NewMod()
        {
            if (_manager == null)
            {
                MessageBox.Show(this, "Escolha a pasta do port primeiro.", "Novo mod",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dialog = new NewModForm())
            {
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    ModInfo created = _manager.CreateMod(dialog.ModName, dialog.Folder, dialog.Priority, dialog.Lang, dialog.StartOff);
                    RefreshMods();
                    if (created != null) SelectFolder(created.Folder);
                    SetStatus("Mod criado em " + (created == null ? dialog.Folder : created.BasePath));
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Novo mod", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ApplySelected(bool showMessage)
        {
            if (_selected == null || _manager == null) return;
            try
            {
                _manager.UpdateMod(_selected, _nameBox.Text, (int)_priorityBox.Value, _langBox.Text,
                    _versionBox.Text, _authorBox.Text, _descriptionBox.Text, _onBox.Checked);
                RefreshMods();
                SelectFolder(_selected.Folder);
                if (showMessage) SetStatus("Mod atualizado.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Aplicar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void AddFolderToSelected()
        {
            if (_selected == null)
            {
                MessageBox.Show(this, "Escolha um mod primeiro.", "Adicionar pasta",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Escolha a pasta com os arquivos do mod. Se ela tiver uma subpasta game, o conteúdo dela será usado.";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    try
                    {
                        _manager.AddFolder(_selected, dialog.SelectedPath, false);
                    }
                    catch (InvalidOperationException ex)
                    {
                        if (!ex.Message.Contains("já existe")) throw;
                        DialogResult answer = MessageBox.Show(this, ex.Message + "\n\nSubstituir os arquivos?",
                            "Adicionar pasta", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                        if (answer != DialogResult.Yes) return;
                        _manager.AddFolder(_selected, dialog.SelectedPath, true);
                    }

                    string folder = _selected.Folder;
                    _manager.UpdateMod(_selected, _nameBox.Text, (int)_priorityBox.Value, _langBox.Text,
                        _versionBox.Text, _authorBox.Text, _descriptionBox.Text, _onBox.Checked);
                    RefreshMods();
                    SelectFolder(folder);
                    SetStatus("Arquivos adicionados ao mod " + folder + ".");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Adicionar pasta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ValidateSelected()
        {
            if (_selected == null)
            {
                UpdateConflicts();
                return;
            }
            try
            {
                ValidationResult result = _manager.Validate(_selected, _mods);
                _validationBox.Text = result.Text;
                UpdateConflicts();
                SetStatus(result.Ok ? "Validação sem erros." : "Validação encontrou erro(s).");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Validar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void UpdateConflicts()
        {
            if (_manager == null)
            {
                _conflicts = new List<ModConflict>();
                return;
            }
            try
            {
                _conflicts = _manager.Conflicts(_mods);
            }
            catch
            {
                _conflicts = new List<ModConflict>();
            }

            _conflictsList.BeginUpdate();
            _conflictsList.Items.Clear();
            foreach (ModConflict conflict in _conflicts)
            {
                string mods = string.Join("; ", conflict.Mods
                    .Select(m => m.Title + " (" + m.Priority + ")")
                    .ToArray());
                var item = new ListViewItem(conflict.Path);
                item.SubItems.Add(mods);
                item.SubItems.Add(conflict.Winner == null ? "" : conflict.Winner.Title);
                item.SubItems.Add(conflict.SameContent ? "mesmo conteúdo" : "diferente");
                _conflictsList.Items.Add(item);
            }
            _conflictsList.EndUpdate();

            var meaningful = _conflicts.Count(c => !c.SameContent);
            _conflictInfo.Text = meaningful == 0
                ? "Nenhum conflito de conteúdo."
                : meaningful + " conflito(s). O vencedor é o de maior prioridade; empate decide pela pasta.";
        }

        private void ExportSelected()
        {
            if (_selected == null)
            {
                MessageBox.Show(this, "Escolha um mod primeiro.", "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string folder = _selected.Folder;
            try
            {
                // Export what is on screen, not the values from before the last Apply.
                _manager.UpdateMod(_selected, _nameBox.Text, (int)_priorityBox.Value, _langBox.Text,
                    _versionBox.Text, _authorBox.Text, _descriptionBox.Text, _onBox.Checked);
                RefreshMods();
                SelectFolder(folder);
                _selected = _mods.FirstOrDefault(m => string.Equals(m.Folder, folder, StringComparison.OrdinalIgnoreCase));
                if (_selected == null) return;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var dialog = new SaveFileDialog())
            {
                dialog.Title = "Exportar mod para a comunidade";
                dialog.Filter = "Pacote de mod (*.bmod)|*.bmod|Todos os arquivos (*.*)|*.*";
                string id = ModManager.Slug(_selected.Folder);
                string version = ModManager.Slug(_selected.Version);
                dialog.FileName = id + (version.Length == 0 ? "" : "-" + version) + ".bmod";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                try
                {
                    string path = _manager.ExportPackage(_selected, dialog.FileName);
                    RefreshMods();
                    SelectFolder(folder);
                    SetStatus("Pacote criado: " + path);
                    MessageBox.Show(this, "Pacote criado:\n" + path +
                        "\n\nEnvie esse .bmod pelo canal de mods do projeto. Ele contém mod.json com os hashes dos arquivos.",
                        "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Exportar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }

        private void ImportPackageDialog()
        {
            if (_manager == null)
            {
                MessageBox.Show(this, "Escolha a pasta do port primeiro.", "Importar",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dialog = new OpenFileDialog())
            {
                dialog.Title = "Importar pacote de mod";
                dialog.Filter = "Pacote de mod (*.bmod)|*.bmod|Arquivo ZIP (*.zip)|*.zip|Todos os arquivos (*.*)|*.*";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                ImportOne(dialog.FileName);
            }
        }

        private void ImportOne(string path)
        {
            PackageInfo info = _manager.ReadPackage(path);
            if (!info.Ok)
            {
                MessageBox.Show(this, "Pacote inválido:\n" + info.Error, "Importar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string id = ModManager.Slug(info.Manifest.Id);
            string target = Path.Combine(_manager.ModsDir, id);
            bool overwrite = false;
            if (Directory.Exists(target))
            {
                DialogResult answer = MessageBox.Show(this,
                    "Já existe o mod \"" + id + "\" em mods.\n\nSubstituir? O mod atual será guardado em mods\\.backup.",
                    "Importar", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                if (answer != DialogResult.Yes) return;
                overwrite = true;
            }

            ImportResult result = _manager.ImportPackage(path, overwrite, false);
            if (!result.Ok)
            {
                MessageBox.Show(this, "Não foi possível importar:\n" + result.Error, "Importar",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshMods();
                return;
            }

            RefreshMods();
            SelectFolder(id);
            if (result.Warnings != null && result.Warnings.Count > 0)
            {
                MessageBox.Show(this, string.Join("\n", result.Warnings.ToArray()), "Importar",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            var meaningful = result.Conflicts == null ? new List<ModConflict>() : result.Conflicts.Where(c => !c.SameContent).ToList();
            if (meaningful.Count > 0)
            {
                string list = string.Join("\n", meaningful.Select(c => c.Path + " -> " + (c.Winner == null ? "" : c.Winner.Title)).ToArray());
                DialogResult answer = MessageBox.Show(this,
                    "O mod foi instalado, mas disputa estes arquivos com outro mod:\n\n" + list +
                    "\n\nO vencedor atual é o de maior prioridade. Deseja desligar o mod importado para revisar depois?",
                    "Conflito de arquivos", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (answer == DialogResult.Yes && result.Mod != null)
                {
                    _manager.SetOn(result.Mod, false);
                    RefreshMods();
                    SelectFolder(id);
                }
            }
            SetStatus("Mod importado: " + id + ". Confira conflitos na aba Conflitos.");
        }

        private void ImportPackageFolder()
        {
            if (_manager == null)
            {
                MessageBox.Show(this, "Escolha a pasta do port primeiro.", "Importar",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Escolha a pasta com arquivos .bmod";
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                string[] packages;
                try
                {
                    packages = Directory.GetFiles(dialog.SelectedPath, "*.bmod", SearchOption.TopDirectoryOnly);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, ex.Message, "Importar", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                if (packages.Length == 0)
                {
                    MessageBox.Show(this, "Nenhum arquivo .bmod na pasta.", "Importar",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                var report = new StringBuilder();
                int installed = 0;
                int failed = 0;
                foreach (string package in packages.OrderBy(p => p, StringComparer.OrdinalIgnoreCase))
                {
                    PackageInfo info = _manager.ReadPackage(package);
                    if (!info.Ok)
                    {
                        report.AppendLine("FALHOU " + Path.GetFileName(package) + ": " + info.Error);
                        failed++;
                        continue;
                    }
                    string id = ModManager.Slug(info.Manifest.Id);
                    if (Directory.Exists(Path.Combine(_manager.ModsDir, id)))
                    {
                        report.AppendLine("PULADO " + Path.GetFileName(package) + ": já existe mods\\" + id);
                        failed++;
                        continue;
                    }
                    ImportResult result = _manager.ImportPackage(package, false, true);
                    if (result.Ok)
                    {
                        installed++;
                        report.AppendLine("OK " + Path.GetFileName(package) + " -> mods\\" + id + " (desligado)");
                    }
                    else
                    {
                        failed++;
                        report.AppendLine("FALHOU " + Path.GetFileName(package) + ": " + result.Error);
                    }
                }

                RefreshMods();
                MessageBox.Show(this,
                    "Importação em lote concluída.\n\nInstalados: " + installed + "\nFalhas/pulados: " + failed +
                    "\n\n" + report.ToString() +
                    "\nOs mods do lote entram desligados. Ligue um por vez na aba Mod e revise os conflitos.",
                    "Importar pasta", MessageBoxButtons.OK, failed > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
            }
        }

        private void OpenGame()
        {
            if (_manager == null)
            {
                MessageBox.Show(this, "Escolha a pasta do port primeiro.", "Abrir jogo",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            string exe = Path.Combine(_manager.PortDir, "black.exe");
            if (!File.Exists(exe))
            {
                MessageBox.Show(this, "black.exe não encontrado em:\n" + _manager.PortDir, "Abrir jogo",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = exe,
                    WorkingDirectory = _manager.PortDir,
                    UseShellExecute = true
                });
                SetStatus("Jogo aberto. Feche o jogo e use Atualizar ao voltar.");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Abrir jogo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OpenSelectedFolder()
        {
            if (_selected == null) return;
            OpenFolder(_selected.BasePath);
        }

        private void OpenFolder(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                MessageBox.Show(this, "Escolha a pasta do port primeiro.", "Abrir pasta",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            try
            {
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                Process.Start("explorer.exe", "\"" + path + "\"");
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Abrir pasta", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SetStatus(string text)
        {
            if (_status != null) _status.Text = text;
        }

        // ------------------------------------------------------------------ settings ---

        private string ReadSetting(string key)
        {
            try
            {
                if (!File.Exists(_settingsPath)) return "";
                foreach (string raw in File.ReadAllLines(_settingsPath))
                {
                    string line = raw.Trim().TrimStart('\uFEFF');
                    int equal = line.IndexOf('=');
                    if (equal < 0) continue;
                    if (string.Equals(line.Substring(0, equal).Trim(), key, StringComparison.OrdinalIgnoreCase))
                        return line.Substring(equal + 1).Trim();
                }
            }
            catch { }
            return "";
        }

        private void SaveSetting(string key, string value)
        {
            try
            {
                var lines = new List<string>();
                if (File.Exists(_settingsPath))
                {
                    foreach (string raw in File.ReadAllLines(_settingsPath))
                    {
                        string line = raw.Trim().TrimStart('\uFEFF');
                        int equal = line.IndexOf('=');
                        if (equal >= 0 && string.Equals(line.Substring(0, equal).Trim(), key, StringComparison.OrdinalIgnoreCase))
                            continue;
                        if (line.Length > 0) lines.Add(line);
                    }
                }
                lines.Add(key + "=" + (value ?? "").Trim());
                File.WriteAllText(_settingsPath, string.Join("\r\n", lines.ToArray()) + "\r\n");
            }
            catch { }
        }
    }
}
