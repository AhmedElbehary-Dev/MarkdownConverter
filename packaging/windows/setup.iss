[Setup]
AppId={{783eb077-d4fa-4dc4-bbf8-661cd9dc6ee8}
AppName=MarkdownConverter
AppVersion=2.0.9
DefaultDirName={autopf}\MarkdownConverter
DefaultGroupName=MarkdownConverter
UninstallDisplayIcon={app}\MarkdownConverter.exe
Compression=lzma2
SolidCompression=yes
OutputDir=..\..\release
OutputBaseFilename=MarkdownConverter-Setup-x64
ArchitecturesInstallIn64BitMode=x64
SetupMutex=MarkdownConverterSetupMutex

[Files]
Source: "..\..\bin\Release\net10.0\win-x64\publish\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs

[Icons]
Name: "{group}\MarkdownConverter"; Filename: "{app}\MarkdownConverter.exe"
Name: "{autodesktop}\MarkdownConverter"; Filename: "{app}\MarkdownConverter.exe"; Tasks: desktopicon

[Tasks]
Name: "desktopicon"; Description: "{cm:CreateDesktopIcon}"; GroupDescription: "{cm:AdditionalIcons}"; Flags: unchecked

[Code]
function GetUninstallString(): String;
var
  sUnInstPath: String;
  sUnInstallString: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sUnInstallString := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'UninstallString', sUnInstallString) then
    RegQueryStringValue(HKCU, sUnInstPath, 'UninstallString', sUnInstallString);
  Result := sUnInstallString;
end;

function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

function UnInstallOldVersion(): Integer;
var
  sUnInstallString: String;
  iResultCode: Integer;
begin
  Result := 0;
  sUnInstallString := GetUninstallString();
  if sUnInstallString <> '' then begin
    sUnInstallString := RemoveQuotes(sUnInstallString);
    if Exec(sUnInstallString, '/SILENT /NORESTART /SUPPRESSMSGBOXES','', SW_HIDE, ewWaitUntilTerminated, iResultCode) then
      Result := 3
    else
      Result := 2;
  end else
    Result := 1;
end;

procedure CurStepChanged(CurStep: TSetupStep);
begin
  if (CurStep = ssInstall) then
  begin
    if IsUpgrade() then
    begin
      UnInstallOldVersion();
    end;
  end;
end;

procedure CurUninstallStepChanged(CurStep: TUninstallStep);
begin
  if CurStep = usPostUninstall then
  begin
    DelTree(ExpandConstant('{app}'), True, True, True);
    DelTree(ExpandConstant('{group}'), True, True, False);
  end;
end;
