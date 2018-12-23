using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;

namespace LOWRES_X4
{
    public partial class MainForm : Form
    {
        static readonly int MaxLineLength = 110;
        private string path_;
        private List<string> catFiles_;

        public MainForm()
        {
            InitializeComponent();
        }

        private void BtnOpen_Click(object sender, EventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                CheckFileExists = true,
                CheckPathExists = true,
                Filter = "X4 Foundations executable (X4.exe)|X4.exe"
            };

            if (dlg.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(dlg.FileName))
            {
                var dirInfo = new FileInfo(dlg.FileName).Directory;
                path_ = dirInfo.ToString();

                catFiles_ = dirInfo
                    .GetFiles("*.cat", SearchOption.TopDirectoryOnly)
                    .Select((finfo) => finfo.FullName).ToList();

                var filesFound = catFiles_.Count > 0;
                if (!filesFound)
                    MessageBox.Show("Selected folder does not contain .cat files", "Wrong folder selected", MessageBoxButtons.OK, MessageBoxIcon.Error);

                lblFolder.Text = path_;
                btnLowerQuality.Enabled = filesFound;
            }
        }

        private void BtnLowerQuality_Click(object sender, EventArgs e)
        {
            var lodLevels = new LODLevels
            {
                LvlCollectables = rbLODColl0.Checked ? 0 : rbLODColl1.Checked ? 1 : rbLODColl2.Checked ? 2 : 3,
                LvlEnvironment = rbLODEnv0.Checked ? 0 : rbLODEnv1.Checked ? 1 : rbLODEnv2.Checked ? 2 : 3,
                LvlShipExteriors = rbLODShipE0.Checked ? 0 : rbLODShipE1.Checked ? 1 : rbLODShipE2.Checked ? 2 : 3,
                LvlShipInteriors = rbLODShipI0.Checked ? 0 : rbLODShipI1.Checked ? 1 : rbLODShipI2.Checked ? 2 : 3,
                LvlStationExteriors = rbLODStationE0.Checked ? 0 : rbLODStationE1.Checked ? 1 : rbLODStationE2.Checked ? 2 : 3,
                LvlStationInteriors = rbLODStationI0.Checked ? 0 : rbLODStationI1.Checked ? 1 : rbLODStationI2.Checked ? 2 : 3
            };

            var texLevels = new TextureLevels
            {
                MinTextureSize = (int)numTexMinSize.Value,
                LvlFonts = rbTexFont0.Checked ? 0 : rbTexFont1.Checked ? 1 : rbTexFont2.Checked ? 2 : 3,
                LvlGUI = rbTexGUI0.Checked ? 0 : rbTexGUI1.Checked ? 1 : rbTexGUI2.Checked ? 2 : 3,
                LvlNPCs = rbTexNPC0.Checked ? 0 : rbTexNPC1.Checked ? 1 : rbTexNPC2.Checked ? 2 : 3,
                LvlFX = rbTexFX0.Checked ? 0 : rbTexFX1.Checked ? 1 : rbTexFX2.Checked ? 2 : 3,
                LvlEnvironments = rbTexEnv0.Checked ? 0 : rbTexEnv1.Checked ? 1 : rbTexEnv2.Checked ? 2 : 3,
                LvlStationExteriors = rbLODStationE0.Checked ? 0 : rbLODStationE1.Checked ? 1 : rbLODStationE2.Checked ? 2 : 3,
                LvlStationInteriors = rbLODStationI0.Checked ? 0 : rbLODStationI1.Checked ? 1 : rbLODStationI2.Checked ? 2 : 3,
                LvlShips = rbTexShips0.Checked ? 0 : rbTexShips1.Checked ? 1 : rbTexShips2.Checked ? 2 : 3,
                LvlMisc = rbTexMisc0.Checked ? 0 : rbTexMisc1.Checked ? 1 : rbTexMisc2.Checked ? 2 : 3
            };

            if (lodLevels.AllZero() && texLevels.AllZero())
            {
                MessageBox.Show(
                    "You set all quality settings to \"unchanged\", so no changes to the game's files will be made.\n" +
                    "Are you trying to restore those to a previous state right now ?\n" +
                    "Oh dear, i told you to back them up ..",
                    "Lowering quality",
                    MessageBoxButtons.OK, MessageBoxIcon.Question);
                return;
            }

            lbResult.Items.Clear();

            if (!cbSimulate.Checked)
            {
                if (MessageBox.Show(
                    string.Format(
                    "Did you make backups of all .cat-files AND .dat-files in\n\"{0}\" ?\n" +
                    "This little app currently can't restore any changes made to those files, " +
                    "so you would most likely have to reinstall X4 if you wanted to restore them.", path_),
                    "Last reminder",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Warning) == DialogResult.Cancel)
                    return;
            }

            if (!cbSimulate.Checked)
                AddLogLine("Worsening X4's graphics ...");
            else
                AddLogLine("[SIMULATION] Worsening X4's graphics ...");

            if (!texLevels.AllZero())
                AddLogLine(">> Modifying textures is enabled, this can take a couple of minutes.");
            
            AddLogLine();

            btnOpen.Enabled = false;
            btnLowerQuality.Enabled = false;
            gbMeshes.Enabled = false;
            gbTextures.Enabled = false;
            cbSimulate.Enabled = false;

            progressBar.Maximum = catFiles_.Count * 2;
            progressBar.Value = 0;

            var task = Task.Factory.StartNew(() =>
            {
                var totalRes = new CatIndex.Res();

                foreach (var file in catFiles_)
                {
                    progressBar.InvokeIfRequired(() => { ++progressBar.Value; });

                    var finfoCat = new FileInfo(file);
                    var finfoDat = new FileInfo(file.Substring(0, file.Length - 3) + "dat");

                    if (finfoCat.Exists && finfoDat.Exists)
                    {
                        var catIndex = new CatIndex();

                        try
                        {
                            catIndex.Load(finfoCat.FullName, finfoDat.FullName);

                            if (!lodLevels.AllZero())
                            {
                                var res = catIndex.ProcessMeshes(lodLevels);
                                if (res.Count > 0)
                                {
                                    totalRes.Count += res.Count;
                                    totalRes.RemovedBytes += res.RemovedBytes;

                                    AddLogLine(string.Format("{0}:", finfoCat.Name));
                                    AddLogLine(string.Format("\tMeshes: Modified {0} entries / removed {1:F3} MB", 
                                        res.Count, res.RemovedBytes / (1024.0 * 1024.0)));
                                    AddLogLine();
                                }
                            }

                            if (!texLevels.AllZero())
                            {
                                var res = catIndex.ProcessTextures(texLevels);
                                if (res.Count > 0)
                                {
                                    totalRes.Count += res.Count;
                                    totalRes.RemovedBytes += res.RemovedBytes;

                                    AddLogLine(string.Format("{0}:", finfoDat.Name));
                                    AddLogLine(string.Format("\tTextures: Modified {0} entries / removed {1:F3} MB (uncompressed)", 
                                        res.Count, res.RemovedBytes / (1024.0 * 1024.0)));
                                    AddLogLine();
                                }
                            }
                        }
                        catch (IOException)
                        {
                            AddLogLine(string.Format("{0}:", finfoCat.Name));
                            AddLogLine("\tCould not open file to write (check write permissions)");
                            AddLogLine();
                        }
                        catch (Exception ex)
                        {
                            totalRes.Count = 0;

                            AddLogLine(string.Format("{0}:", finfoCat.Name));
                            AddLogLine(string.Format("\tException occured: {0}", ex));
                            AddLogLine();
                            if (!cbSimulate.Checked)
                                AddLogLine("Canceled further processing (contact developer ?)");
                            else
                                AddLogLine("Canceled further processing, please restore using your backed up files (and contact developer ?)");

                            catIndex.Close(false);
                            break;
                        }

                        catIndex.Close(!cbSimulate.Checked);
                    }
                }

                lbResult.Invoke(new Action(() =>
                {
                    progressBar.Value = progressBar.Maximum;

                    AddLogLine();
                    AddLogLine(" **************************************************************** ");
                    AddLogLine("                             DONE.                                ");

                    if (totalRes.Count > 0)
                        AddLogLine(string.Format(
                            "       Total: Modified {0} entries / removed {1:F3} MB",
                            totalRes.Count, totalRes.RemovedBytes / (1024.0 * 1024.0)));
                    
                    AddLogLine(" **************************************************************** ");

                    if (!cbSimulate.Checked)
                    {
                        lblFolder.Text = "Please don't attempt to modify the same files again, restore them before that";
                        path_ = null;
                        catFiles_.Clear();
                    }
                    else
                        btnLowerQuality.Enabled = true;

                    gbMeshes.Enabled = true;
                    gbTextures.Enabled = true;
                    cbSimulate.Enabled = true;
                    btnOpen.Enabled = true;
                }));
            });
        }

        private void AddLogLine(string text = null)
        {
            lbResult.InvokeIfRequired(() =>
            {
                if (text == null)
                    text = string.Empty;

                if (text.Length > MaxLineLength)
                {
                    lbResult.Items.Add(text.Substring(0, MaxLineLength));
                    text = text.Substring(MaxLineLength, text.Length - MaxLineLength);

                    while (text.Length > MaxLineLength)
                    {
                        lbResult.Items.Add("\t" + text.Substring(0, MaxLineLength));
                        text = text.Substring(MaxLineLength, text.Length - MaxLineLength);
                    }

                    lbResult.Items.Add("\t" + text);
                }
                else
                    lbResult.Items.Add(text);

                lbResult.SelectedIndex = lbResult.Items.Count - 1;
            });
        }
    }
}
