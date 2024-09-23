﻿using BeatSaberModdingTools.Commands;
using BeatSaberModdingTools.Menus;
using BeatSaberModdingTools.Models;
using BeatSaberModdingTools.Utilities;
using EnvDTE80;
using Microsoft.Build.Evaluation;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Linq;
using static BeatSaberModdingTools.Utilities.EnvUtils;

namespace BeatSaberModdingTools
{
    // From https://stackoverflow.com/a/57166903
    public class CommandFilter : IOleCommandTarget
    {
        private AsyncPackage package;
        public CommandFilter(IOleCommandTarget nextTarget, AsyncPackage asyncPackage)
        {
            NextTarget = nextTarget;
            package = asyncPackage;
        }

        public IOleCommandTarget NextTarget { get; set; }
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var cmdId = prgCmds[0].cmdID;
            if (pguidCmdGroup.Equals(CommandSetGuids.ReferencesContextCmdSet))
            {
                if (cmdId == AddProjectReference.CommandId)
                {
                    bool visible = false;
                    CommandStatus status = 0;
                    if (TryGetSelectedProject(package, out ProjectModel proj) && proj.IsBSIPAProject)
                    {
                        status |= CommandStatus.Supported;
                        status |= CommandStatus.Enabled;
                        visible = true;
                    }
                    if (visible)
                        status &= ~CommandStatus.Invisible;
                    else
                        status |= CommandStatus.Invisible;
                    prgCmds[0].cmdf = (uint)GetVsStatus(status);
                }
            }
            else if (pguidCmdGroup.Equals(CommandSetGuids.ProjectContextCmdSet))
            {
                if (cmdId == ProjectContextSubmenu.CommandId)
                {
                    bool visible = false;
                    CommandStatus status = 0;
                    if (TryGetSelectedProject(package, out ProjectModel proj) && proj.IsBSIPAProject)
                    {
                        status |= CommandStatus.Supported;
                        status |= CommandStatus.Enabled;
                        visible = true;
                    }
                    if (visible)
                        status &= ~CommandStatus.Invisible;
                    else
                        status |= CommandStatus.Invisible;
                    prgCmds[0].cmdf = (uint)GetVsStatus(status);
                }
                else if (cmdId == SetBeatSaberDirCommand.CommandId)
                {
                    bool visible = false;
                    CommandStatus status = 0;
                    if (TryGetSelectedProject(package, out ProjectModel projectModel, out var project, out _) && projectModel.IsBSIPAProject)
                    {
                        status |= CommandStatus.Supported;
                        string prop = project.GetProperty("BeatSaberDir")?.UnevaluatedValue;
                        if (prop != BSMTSettingsManager.Instance.CurrentSettings.ChosenInstallPath)
                            status |= CommandStatus.Enabled;
                        visible = true;
                    }
                    if (visible)
                        status &= ~CommandStatus.Invisible;
                    else
                        status |= CommandStatus.Invisible;
                    prgCmds[0].cmdf = (uint)GetVsStatus(status);
                }
                else if (cmdId == AddProjectReferencePaths.CommandId)
                {
                    bool visible = false;
                    CommandStatus status = 0;
                    if (TryGetSelectedProject(package, out ProjectModel projectModel, out var project, out _)
                        && projectModel.IsBSIPAProject)
                    {
                        status |= CommandStatus.Supported;
                        string prop = project.GetProperty("ReferencePath")?.UnevaluatedValue;
                        if (prop != EnvUtils.GetReferencePathString(BSMTSettingsManager.Instance.CurrentSettings.ChosenInstallPath))
                            status |= CommandStatus.Enabled;
                        visible = true;
                    }
                    if (visible)
                        status &= ~CommandStatus.Invisible;
                    else
                        status |= CommandStatus.Invisible;
                    prgCmds[0].cmdf = (uint)GetVsStatus(status);
                }
            }
            else
            {
                return NextTarget.QueryStatus(pguidCmdGroup, cmdId, prgCmds, pCmdText);
            }
            return VSConstants.S_OK;
        }


        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            return NextTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        private OLECMDF GetVsStatus(CommandStatus commandStatus)
        {
            OLECMDF ret = 0;
            if (commandStatus.HasFlag(CommandStatus.Supported))
                ret |= OLECMDF.OLECMDF_SUPPORTED;
            if (commandStatus.HasFlag(CommandStatus.Enabled))
                ret |= OLECMDF.OLECMDF_ENABLED;
            if (commandStatus.HasFlag(CommandStatus.Invisible))
                ret |= OLECMDF.OLECMDF_INVISIBLE;
            return ret;
        }

        [Flags]
        private enum CommandStatus
        {
            None = 0,
            Supported = 1 << 0,
            Enabled = 1 << 1,
            Invisible = 1 << 2
        }
    }
}
