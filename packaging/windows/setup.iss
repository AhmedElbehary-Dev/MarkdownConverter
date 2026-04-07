[Setup]
AppId={{783eb077-d4fa-4dc4-bbf8-661cd9dc6ee8}
AppName=MarkdownConverter
AppVersion=2.0.8
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

function GetInstalledVersion(): String;
var
  sUnInstPath: String;
  sVersion: String;
begin
  sUnInstPath := ExpandConstant('Software\Microsoft\Windows\CurrentVersion\Uninstall\{#emit SetupSetting("AppId")}_is1');
  sVersion := '';
  if not RegQueryStringValue(HKLM, sUnInstPath, 'DisplayVersion', sVersion) then
    RegQueryStringValue(HKCU, sUnInstPath, 'DisplayVersion', sVersion);
  Result := sVersion;
end;

function CompareVersion(Ver1, Ver2: String): Integer;
var
  Ver1Parts, Ver2Parts: TArrayOfString;
  Ver1Count, Ver2Count, i, Count: Integer;
  Num1, Num2: Integer;
begin
  Ver1Count := 0;
  Ver2Count := 0;
  
  // Split version strings
  if Length(Ver1) > 0 then
    Ver1Count := GetTokens(Ver1, '.', Ver1Parts);
  if Length(Ver2) > 0 then
    Ver2Count := GetTokens(Ver2, '.', Ver2Parts);
  
  // Use the larger count
  if Ver1Count > Ver2Count then
    Count := Ver1Count
  else
    Count := Ver2Count;
  
  for i := 0 to Count - 1 do
  begin
    Num1 := 0;
    Num2 := 0;
    
    if i < Ver1Count then
      StrToInt(Ver1Parts[i], Num1);
    if i < Ver2Count then
      StrToInt(Ver2Parts[i], Num2);
    
    if Num1 > Num2 then
    begin
      Result := 1; // Ver1 > Ver2
      Exit;
    end
    else if Num1 < Num2 then
    begin
      Result := -1; // Ver1 < Ver2
      Exit;
    end;
  end;
  
  Result := 0; // Equal
end;

function IsUpgrade(): Boolean;
begin
  Result := (GetUninstallString() <> '');
end;

function IsDowngrade(): Boolean;
var
  InstalledVersion, CurrentVersion: String;
  CmpResult: Integer;
begin
  InstalledVersion := GetInstalledVersion();
  CurrentVersion := '{#emit SetupSetting("AppVersion")}';
  
  if InstalledVersion = '' then
  begin
    Result := False;
    Exit;
  end;
  
  CmpResult := CompareVersion(InstalledVersion, CurrentVersion);
  Result := (CmpResult > 0); // Installed version is newer
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

function InitializeSetup(): Boolean;
begin
  Result := True;
  
  if IsDowngrade() then
  begin
    if MsgBox('A newer version is already installed. Downgrading may cause issues. Do you want to continue?', mbError, MB_YESNO) = IDNO then
    begin
      Result := False;
      Exit;
    end;
  end;
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
    // Remove all user data and configuration files
    DelTree(ExpandConstant('{app}'), True, True, True);
    
    // Remove Start Menu shortcuts folder
    DelTree(ExpandConstant('{group}'), True, True, False);
  end;
end;
