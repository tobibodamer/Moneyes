Name "Moneyes Installer"
OutFile MoneyesInstaller.exe
SilentInstall silent
RequestExecutionLevel user

Section

InitPluginsDir
SetOutPath "$PLUGINSDIR"

File /r "publish\*"

ExecWait "$PLUGINSDIR/Moneyes.UI.exe"
SectionEnd